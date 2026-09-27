# 01: Structural diff between two compiled rules

**What to build:** An API that takes two `CompiledRule<TContext>` instances (or their `RuleDescription` trees) and returns a structural diff describing added, removed, and changed nodes between them.

**Blocked by:** none

**Status:** ready-for-agent

- [ ] Diff API accepts two compiled rules and returns added/removed/changed node information
- [ ] Structurally identical rules produce an empty/no-change diff
- [ ] A changed operator, changed term arguments, and an added/removed operand each produce a distinguishable diff entry
- [ ] Covered by tests exercising at least one change per category above
