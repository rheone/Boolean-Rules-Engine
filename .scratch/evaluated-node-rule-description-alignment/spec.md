# EvaluatedNode / RuleDescription alignment

**Status:** ready-for-agent

## Problem Statement

`RuleRenderTree.Build` (`src/BooleanRulesEngine/Printing/RuleRenderTree.cs`) zips
two independently-authored recursive traversals positionally: `Evaluator` builds
an `EvaluatedNode` tree during evaluation, and `CompiledRule.DescribeNode` builds
a `RuleDescription` tree from the same `Expression`. `RuleRenderTree`'s own doc
comment states the invariant it depends on: both traversals visit
`Expression.Operands` "in the same order," so they align positionally with no
need to match by text. That invariant is enforced only by the comment — nothing
in the type system or a shared traversal stops a future change to either
traversal (e.g. an evaluation-order optimization) from silently breaking the
positional zip, which would render evaluation state against the wrong label
with no compile or runtime signal.

`RuleRenderTree` itself is a deep, well-tested module — it's the one place both
`MermaidTreePrinter` and `PlainTextTreePrinter` delegate to for skip-propagation
and state logic, and it's exercised through the real `Describe()`/
`EvaluateAsync()` path in existing tests, not hand-built trees. The friction is
one level up, in the two independently-built inputs it's handed.

## Solution

Two tickets, sequenced independently of each other:

1. An immediate, cheap guard: a debug-only assertion in `RuleRenderTree.Build`
   that `EvaluatedNode` and `RuleDescription` agree on operand count at every
   level, so a future divergence fails loudly instead of silently mislabeling.
2. The full structural fix: build both `EvaluatedNode` and `RuleDescription`
   from the shared node-shape seam introduced in `expression-node-shape-seam`,
   so the "same operand order" invariant holds by construction rather than by
   convention. Once this lands, the guard from ticket 1 becomes permanently
   unreachable but is left in place as insurance.

## User Stories

1. As a maintainer, I want a divergence between `EvaluatedNode` and
   `RuleDescription` to fail loudly in a debug build, so that a silent
   mislabeling bug doesn't ship first and get diagnosed later.
2. As a maintainer, I want both trees built from one shared traversal, so the
   "same operand order" guarantee doesn't rely on two authors remembering to
   keep two separate recursive functions in lockstep.
