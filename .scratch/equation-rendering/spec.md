# Equation rendering: LaTeX / GitHub math / plain Unicode / AsciiMath

**Status:** brainstorm

## Problem Statement

The two presentation tree printers (`PlainTextTreePrinter`,
`MermaidTreePrinter`) render a compiled rule as a diagram — a hierarchical
tree of nodes and edges. There's no flat, single-expression rendering
suitable for embedding in prose: a generated report, a PDF, a markdown doc,
a commit message, a chat message, or a rendered math block. That's a
different shape of output entirely — an infix equation, not a tree — and a
different set of audiences want it in different notations: a LaTeX document,
a GitHub-rendered markdown file, a plain-text log line, or a lightweight
AsciiMath snippet.

This is presentation-only, same scoping constraint
[`operator-symbolic-rendering`](../operator-symbolic-rendering/spec.md)
already established for the tree printers: **the canonical, persisted DSL
text (`CanonicalPrinter`) stays word-only.** This ticket does not revisit
that decision or propose a new authoring surface — it's another *read* view
of an already-compiled rule, exactly like the tree printers are.

## Architectural starting point

`CanonicalPrinter` (`src/TruthWeaver/Printing/CanonicalPrinter.cs`) is the
template to generalize, not `RuleRenderTree`. `RuleRenderTree`/`RenderNode`
produce tree-shaped (node/edge) output for the two diagram printers; an
equation is a flat infix string, which is exactly what `CanonicalPrinter`
already produces — it just hardcodes DSL word tokens (`" AND "`, `" OR "`,
`"NOT "`) and always-parenthesizes `XOR`/`XNOR`, using a context-aware
recursive walk (`PrintContext` tracks which operand position a node sits in,
to decide minimal-but-unambiguous parenthesization).

An equation renderer needs the same walk shape — infix join, context-aware
parenthesization — parameterized by a per-dialect token table (operator
tokens, a term formatter, and a wrap/escape function), rather than a new
algorithm from scratch.

## Two orthogonal configuration axes

1. **Operator vocabulary / symbol class.** This already exists as
   `OperatorStyle` (`Word`/`Symbolic`/`CStyle`) for the tree printers.
   `Symbolic` (`∧ ∨ ¬ ⊕ ↔`) already covers the plain-Unicode-text and
   AsciiMath dialects below (AsciiMath uses the same or near-identical
   ASCII-typable glyphs). LaTeX and GitHub math need a **distinct**
   vocabulary entry — LaTeX commands (`\land`, `\lor`, `\lnot`, `\oplus`,
   `\leftrightarrow`) are not the same tokens as the Unicode characters,
   even though they render to the same glyphs once typeset.

2. **Output dialect (wrapper + escaping).** Controls the envelope (no
   delimiter / `$$...$$` / a fenced math block / AsciiMath's own
   convention) and dialect-specific escaping of term labels and string
   literal argument values:
   - LaTeX must escape `_ % & # { } \` (and more) in any literal text.
   - GitHub math shares LaTeX's escaping rules but adds its own
     underscore/subscript-interpretation quirk inside `$...$` that raw
     LaTeX documents don't have to worry about.
   - Plain Unicode text needs no escaping at all — it's just characters.
   - AsciiMath has its own, simpler escaping/quoting rules.

   Important framing point: **"GitHub LaTeX" and "raw LaTeX" are not two
   vocabularies.** They're the same LaTeX command set with two different
   envelopes — `$$...$$` for GitHub's markdown renderer vs. no wrapper (or
   `\[...\]`) for a raw `.tex`/Pandoc pipeline. Model this as one dialect
   with a wrap-mode option, not two separate dialects.

3. **Argument-value inclusion.** Reuse the diagrams' existing
   `showArgumentValues`-style toggle (`src/TruthWeaver/Evaluation/CompiledRule.cs`,
   `src/TruthWeaver/Printing/RuleRenderTree.cs`) as a third axis, orthogonal
   to the two above — whether a term shows its full argument list or not is
   independent of which dialect or operator vocabulary is chosen.

## Term-rendering modes

Two modes, both wanted (per discussion with the user):

- **Full term call** (default, essential) — e.g. `hasCrust(crust: "thin")`,
  matching the convention already established by the DSL and the diagrams.
  Self-contained; no companion legend needed.
- **Abstract lettered variables + legend** (secondary) — e.g.
  `p ∧ (q ∨ r ∨ (s ⊕ t))`, with a separate legend mapping each letter back
  to its term (`p = hasTopping(topping: "greenOlives")`, etc.). This is how
  boolean algebra is usually written in a math/textbook context, and is a
  genuinely different (not just abbreviated) rendering concept from
  anything shipped so far. Needs a **stable, documented assignment order**
  (e.g. first-occurrence, depth-first left-to-right) so the same rule always
  gets the same lettering across repeated calls — this is a real design
  detail to pin down before implementation, not just an incidental choice.

A third option — label-only (`Has Crust`, no arguments, no letters) — was
considered and explicitly **not** chosen; it loses the argument-value
distinctions for no compensating benefit over the two modes above.

## Open notation questions (flagged, not resolved here)

- **`ExactlyOne` and the threshold family** (`AtLeast`/`AtMost`/
  `GreaterThan`/`LessThan`/`Exactly`) have no standard boolean-algebra infix
  symbol. `operator-symbolic-rendering` solved the equivalent problem for
  the tree printers by leaving these in word/function-call form in every
  `OperatorStyle`. The same fallback likely applies here (e.g.
  `\text{AtLeast}_2(a, b, c)` in LaTeX), but this needs an explicit decision
  per dialect before implementation, not an assumption.
- **Kleene's three-valued `Unknown`** has no standard symbol. This only
  matters if an equation renderer ever grows an evaluation-colored/
  annotated mode (mirroring the diagrams' `Decision`-colored overloads,
  e.g. marking which sub-expressions evaluated to `True`/`False`/`Unknown`
  inline). Not committed to for a first cut — see non-goals below — but
  worth recording now so it isn't silently invented later.
- **Escaping edge cases per dialect** (e.g. what happens to a predicate
  label or a string-literal argument value that itself contains LaTeX
  special characters, or GitHub math's specific MathJax-subset
  restrictions) need concrete per-dialect escape-function specs, not just
  "escape the obvious characters" — this is exactly the kind of gap that
  produced the ADR-0003 Mermaid diagram bug (`&quot;` vs `#quot;`) that was
  fixed earlier; a future implementation ticket should treat each dialect's
  escaping as its own tested unit, the same way `MermaidTreePrinter.Escape`
  is.

## User Stories

1. As a consumer generating a PDF or printed report from a compiled rule, I
   want to embed it as a raw LaTeX equation, so it typesets natively
   alongside the rest of a LaTeX/Pandoc document.
2. As a consumer writing a markdown doc or PR description on GitHub, I want
   to embed the rule as GitHub-flavored math (`$$...$$`), so it renders
   inline without needing a separate diagram image.
3. As a consumer logging or messaging about a rule (a log line, a chat
   message, a plain-text report), I want a plain-Unicode one-line rendering
   (`a ∧ (b ∨ c)`), with no delimiters and no escaping to worry about.
4. As a consumer presenting "the shape of this rule" independent of its
   specific predicates — e.g. explaining the logical structure to someone
   unfamiliar with the domain — I want the abstract-lettered-variable mode
   with a legend, so the equation reads like a textbook boolean-algebra
   expression rather than a wall of predicate calls.

## Explicit non-goals (for this write-up and any first implementation slice)

- No change to `CanonicalPrinter` or the persisted DSL surface.
- No MathML, Typst, or Office Math (OMML) support in a first pass — these
  are mentioned only as possible future dialects that would follow the same
  per-dialect-token-table pattern, if ever wanted.
- No evaluation-result coloring/annotation (a `Decision`-colored equation)
  in a first cut — flagged as an open notation question above, not
  committed to.

## Related

- [`diagram-rendering-options`](../diagram-rendering-options/spec.md) — the
  sibling Mermaid/diagram-knobs write-up from the same discussion thread;
  shares the "operator vocabulary" and "argument-value toggle" axes with
  this ticket, but is a different output shape (diagram vs. flat infix
  string).
- [`operator-symbolic-rendering`](../operator-symbolic-rendering/spec.md) —
  the direct precedent for a selectable operator vocabulary on presentation
  output, and for the "word/persisted DSL stays untouched" scoping
  constraint this ticket repeats.
- [`CanonicalPrinter`](../../src/TruthWeaver/Printing/CanonicalPrinter.cs) —
  the infix-walk-with-context-aware-parenthesization algorithm to
  generalize.
- [`RuleRenderTree`](../../src/TruthWeaver/Printing/RuleRenderTree.cs) /
  [`MermaidTreePrinter`](../../src/TruthWeaver/Printing/MermaidTreePrinter.cs) /
  [`PlainTextTreePrinter`](../../src/TruthWeaver/Printing/PlainTextTreePrinter.cs) —
  the existing diagram printers this feature is a sibling to, and the
  source of the `showArgumentValues`-style toggle precedent.
