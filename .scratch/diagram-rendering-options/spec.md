# Diagram rendering options: Mermaid knobs, node styling, and a structured export for UI consumers

**Status:** brainstorm

## Problem Statement

`MermaidTreePrinter` and `PlainTextTreePrinter` already support two knobs
(`OperatorStyle`, `showArgumentValues`) plus `Decision`-based coloring, but
feedback on the README's worked-example diagram surfaced two more needs:

1. **Visual noise at the leaves.** A term's full argument list
   (`Has Crust (crust: "thin", culture: "", ignoreCase: true, trim: false)`)
   is accurate but dense — there's no way to give the predicate label visual
   priority over its arguments within one node.
2. **Only automatic, evaluation-driven coloring exists.** A caller can color
   by `Decision` (true/false/skipped), but can't manually highlight, mute,
   or gray out an arbitrary node or subtree independent of any evaluation —
   useful for a slide, a doc callout ("this is the part that changed"), or a
   UI letting a user click to inspect a branch.

A third input reframes scope: **one major consumer of this library is a
rule-authoring web UI**, not just static docs. A live UI embedding
mermaid.js itself is a materially richer target than GitHub's sandboxed
renderer — it can enable full HTML labels, custom themes, and interactive
`click` callbacks that GitHub's `securityLevel: strict` disables. Some ideas
below only make sense for one of these two audiences; each is labeled.

## What already exists (no new work needed)

Worth stating plainly, since it wasn't obvious from the README example: the
"just word operators, no other options" observation is about what the
worked example *demonstrates*, not what's *implemented*. `OperatorStyle`
(`Word`/`Symbolic`/`CStyle` — `∧ ∨ ¬ ⊕ ↔` / `&& || ! ^ ==`) and
`showArgumentValues` already apply to `MermaidTreePrinter.Print` today
(`src/TruthWeaver/Printing/MermaidTreePrinter.cs`), via the shared
`RuleRenderTree`. Decision-based coloring (green/red/gray via `classDef`)
also already exists. Any future ticket picking this up should start from
"these three knobs exist and compose" rather than rebuilding them.

## Verified constraints (checked against real Mermaid/GitHub behavior)

- GitHub's Mermaid renderer strips arbitrary HTML from node labels
  (`<span>`, `<small>`, inline `style=`) — only `<br/>` survives. True
  "dimmed subtext" via inline styling is **not achievable on GitHub**.
  Mermaid's own **markdown-string labels** (a label wrapped in backticks,
  e.g. `` node1["`**Has Crust**\ncrust: "thin"`"] ``, supporting
  `**bold**`/`*italic*`/line breaks) are the documented, portable
  alternative — not raw HTML, so it survives GitHub's sanitizer. [GitHub
  community discussion on `<br>`/HTML handling in Mermaid](https://github.com/orgs/community/discussions/204537)
- Mermaid's `click` directive (`click nodeId "tooltip"` /
  `click nodeId href "url" "tooltip"` / `click nodeId callback "tooltip"`)
  gives tooltips, links, and JS callbacks — but **callbacks are disabled
  under `securityLevel: strict`**, which is what GitHub uses. They work
  fully wherever a host embeds mermaid.js itself with a relaxed security
  level — the web-UI case, not the GitHub-doc case. [Mermaid flowchart
  syntax docs](https://mermaid.js.org/syntax/flowchart.html), [click-events
  behavior discussion](https://github.com/mermaid-js/mermaid/issues/1294)

## Proposed knobs (Mermaid-text track — static docs, GitHub-safe by default)

1. **Diagram direction** (`TD`/`LR`/`BT`/`RL`). Currently hardcoded to
   `flowchart TD` in `MermaidTreePrinter.Print`. Trivial, zero-risk addition
   — Mermaid natively supports all four; wide/flat rules likely read better
   `LR`.
2. **Node shape by role.** Every node currently uses the same `["..."]`
   rectangle regardless of whether it's an operator, a term, or a constant.
   Giving operators a distinct shape (e.g. `{AND}` diamond, matching
   "decision point" conventions) vs. terms (e.g. `(hasCrust...)` stadium/
   rounded) vs. constants (a third shape) adds visual scanability for free
   — no new data needed, just a shape-selection function keyed on the
   existing `RenderNode`/node-kind information.
3. **Two-line term labels via Mermaid markdown-string labels.** Render a
   term as a bold header line (the predicate `Label`) plus a plain second
   line (the argument text), using the backtick markdown-string syntax
   confirmed above — addresses the "noisy leaves" complaint within GitHub's
   actual constraints, rather than assuming HTML styling that gets stripped.
   True dimming/gray/smaller text specifically would need a custom
   stylesheet, which only a self-hosted web UI can apply (see below) — flag
   this distinction explicitly wherever it's documented, so a future
   implementer doesn't promise GitHub something it can't render.
4. **Manual/independent node highlighting**, orthogonal to `Decision`-based
   coloring. Reuse the exact `classDef`/`class` assignment plumbing
   `MermaidTreePrinter` already has (`ClassFor`/`WriteClassDefs`) — today
   driven only by evaluation state (`RenderState`) — and let a caller
   additionally (or instead) supply an explicit highlight/mute map (e.g.
   term identity or tree path → a CSS class/color), merged at render time.
   This is what lets someone say "gray out this branch for a slide" with no
   `Decision` involved at all.
5. **A selectable color palette**, since the current green/red/gray/yellow
   `classDef` values are hardcoded. A "default / colorblind-safe /
   print-monochrome / dark-mode" choice would help both the doc-embedding
   and web-UI cases render acceptably in more contexts.
6. **Wide n-ary chain compaction.** For a flat `AND`/`OR` chain with many
   leaf operands, an option to render it as one compact node (or a
   collapsed sub-graph with a "+N more" indicator) instead of one operator
   node fanning out to N edges — a direct answer to "noisy" for large
   real-world rules (not the pizza example, which is small, but flagged for
   when this scales up).

## Proposed track for the web-UI case: a structured, serializable render tree

This may matter more than incremental Mermaid-string knobs for "helping
users against a web UI workflow." A serious rule-authoring UI (drag layout,
inline click-to-edit, live re-render on evaluation) will likely want to
render its *own* diagram (React Flow, D3, canvas) rather than parse Mermaid
text back apart — Mermaid text is a one-way street to a Mermaid renderer.

Today, the shared coloring/skip-propagation logic
(`RuleRenderTree`/`RenderNode`/`RenderState` in
`src/TruthWeaver/Printing/`) that both printers already build is **internal**
— a UI can only get equivalent data by re-deriving it itself from the public
`RuleDescription` (`src/TruthWeaver/Evaluation/RuleDescription.cs`) and
`Decision.EvaluatedTree`, duplicating the zip/skip-propagation logic
`RuleRenderTree.Build` already centralizes (its own doc comment: "what a
skipped subtree looks like is decided once, not per format"). Making an
equivalent shape **public and JSON-friendly** — the same label/state/
children data the two printers already compute, without inventing new
concepts — would let a UI consume one already-correct structure directly,
instead of re-implementing it or scraping Mermaid text.

Complementary web-UI-only knobs once mermaid.js is embedded directly (not
GitHub):
- `click` directives for tooltips (predicate `Description` on hover) and/or
  a JS callback hook so clicking a node can open the host UI's own "term
  details" or "edit this predicate" panel — turning the diagram into a
  navigable view of the rule, not just a picture of one.
- Full HTML/CSS labels (dimming, font sizing, icons) once the host sets a
  relaxed `securityLevel` and supplies its own theme.

## Other Mermaid diagram types considered

- **`flowchart`** (current choice) remains the right fit — it's the only
  Mermaid type with directional edges, node coloring, and click hooks all
  supported together, which the operator/term structure genuinely needs
  (edge direction matters for `NOT`'s single operand and threshold `k`
  values; a boolean tree isn't undirected).
  - **`mindmap`** is a worse fit for the same reason — no edge labels/
  colors, and its hierarchy has no inherent "operator" semantics.
  - A Mermaid **`block`** diagram (grid/block layout, still beta as of this
  writing) could suit a different, unrelated idea — a compact "truth
  table" style summary — but it's immature and orthogonal to the tree view;
  not pursued here.
  - A **`sequenceDiagram`**/timeline for the evaluation *trace* (order, not
  structure) was already flagged as a separate future idea in an earlier
  discussion — distinct from this ticket's tree-diagram scope.

## Explicit non-goals (for this write-up)

- No decision yet on which of the above actually gets built, or in what
  order — this is a brainstorm, not a prioritized backlog.
- No commitment to exposing `RenderNode`/`RenderState` publicly as-is —
  only that an equivalent, public, serializable shape is worth designing
  deliberately (naming, versioning, JSON shape) rather than assumed.
- No specific UI framework or rendering library chosen for the "a UI
  renders its own diagram" track — that's the consuming host's decision,
  this library's job is only to expose the data well.

## Related

- [`operator-symbolic-rendering`](../operator-symbolic-rendering/spec.md) —
  the `OperatorStyle` precedent this ticket's knobs extend.
- [`equation-rendering`](../equation-rendering/spec.md) — the sibling
  flat-expression rendering write-up from the same discussion thread;
  shares the "operator vocabulary" and "argument-value toggle" axes with
  this ticket, but is a different output shape (infix string vs. diagram).
- [`RuleRenderTree`](../../src/TruthWeaver/Printing/RuleRenderTree.cs) /
  [`MermaidTreePrinter`](../../src/TruthWeaver/Printing/MermaidTreePrinter.cs) /
  [`PlainTextTreePrinter`](../../src/TruthWeaver/Printing/PlainTextTreePrinter.cs) /
  [`RenderNode`](../../src/TruthWeaver/Printing/RenderNode.cs) /
  [`RenderState`](../../src/TruthWeaver/Printing/RenderState.cs) — the
  existing shared rendering plumbing every idea above builds on or exposes.
