# Symbolic/C-style operator rendering for tree printers

**Status:** ready-for-agent

## Problem Statement

The two presentation tree printers (`PlainTextTreePrinter`,
`MermaidTreePrinter`) render every operator using its word-form `Label` from
`RuleDescription` (e.g. `"AND"`, `"OR"`), which is the right default but not
the only notation a consumer might want for a diagram or report —
mathematical logic notation (`∧`, `∨`, `¬`, `⊕`, `↔`) or C-family symbols
(`&&`, `||`, `!`, `^`, `==`) are both common alternate conventions for
reading a boolean tree at a glance.

This is deliberately scoped to presentation-only rendering. The canonical,
persisted DSL text (`CanonicalPrinter`) intentionally stays word-only per
CONTEXT.md's existing deferred decision against symbol operator aliases in
the authored/parseable surface ("Word operators only, to keep the surface to
one thing to learn and test") — this feature does not revisit that decision.

## Solution

Add a selectable `OperatorStyle` (`Word` default, `Symbolic`, `CStyle`) that
both tree printers accept via their shared `RuleRenderTree`/`RenderNode`
infrastructure, mapping each infix-shaped operator (`AND`/`OR`/`NOT`/`XOR`/
`XNOR`) to its symbolic or C-style form while leaving operators with no
natural infix symbol (`ExactlyOne`, the threshold family) in word form in
every style.

Sequenced after the `dsl-escaping` fixes land, since both touch the same
printer-side rendering code paths and doing the escaping fix first avoids
rework here.

## User Stories

1. As a consumer generating a diagram or report from a compiled rule, I want
   to choose mathematical logic symbols (`∧ ∨ ¬ ⊕ ↔`) instead of word
   operators, so that the rendering matches conventions familiar to a
   technical/academic audience.
2. As a consumer with a C-family-literate audience, I want `&& || ! ^ ==`
   rendering instead, so that the rendering matches conventions familiar to
   a software audience.
3. As a maintainer, I want `ExactlyOne` and the threshold family to keep
   their word/function-call form regardless of style, since they have no
   natural infix symbol and forcing one would be more confusing, not less.
