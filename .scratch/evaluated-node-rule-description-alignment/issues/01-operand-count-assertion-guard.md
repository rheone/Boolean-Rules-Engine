# 01: Debug-only operand-count assertion in RuleRenderTree.Build

**What to build:** A debug-only assertion inside `RuleRenderTree.Build` that `EvaluatedNode` and `RuleDescription` agree on operand count at every level of the tree it zips, so a future divergence between the two independently-built traversals fails loudly instead of silently mislabeling evaluation state.

**Blocked by:** none

**Status:** ready-for-agent

- [ ] `RuleRenderTree.Build` asserts (debug-only, not a production-path exception) that `EvaluatedNode.Children.Count` matches `RuleDescription.Operands.Count` at every level when both are present
- [ ] A test that deliberately constructs mismatched trees demonstrates the assertion fires
- [ ] No behavior change for the existing, correctly-aligned case: all current `RuleTreeRenderingTests`/`EvaluatedTreeTests` pass unchanged
- [ ] The assertion is documented (code comment) as a guard against the positional-zip invariant, pointing at the fuller fix in `evaluated-node-rule-description-alignment` ticket 02
