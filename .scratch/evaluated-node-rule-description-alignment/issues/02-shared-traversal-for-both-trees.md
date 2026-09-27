# 02: Build EvaluatedNode and RuleDescription from the shared node-shape seam

**What to build:** Change `Evaluator`'s `EvaluatedNode` construction and `CompiledRule.DescribeNode`'s `RuleDescription` construction so both are driven from the shared node-shape seam introduced in `expression-node-shape-seam`, rather than two independently-written recursive traversals over `Expression.Operands`. The "same operand order" invariant `RuleRenderTree.Build` depends on then holds by construction instead of by convention.

**Blocked by:** `expression-node-shape-seam` #01 (shared node-shape seam for Expression)

**Status:** ready-for-agent

- [ ] `Evaluator`'s `EvaluatedNode` construction and `CompiledRule.DescribeNode`'s `RuleDescription` construction both consume the shared node-shape seam's operand ordering
- [ ] The debug-only assertion guard from ticket 01 becomes unreachable under normal operation (left in place as insurance, not removed)
- [ ] No behavior change: existing `RuleTreeRenderingTests`, `EvaluatedTreeTests`, and evaluation tests pass unchanged
- [ ] A test demonstrates that reordering operands in one traversal without the seam is no longer possible — i.e. both trees structurally cannot diverge in operand order
