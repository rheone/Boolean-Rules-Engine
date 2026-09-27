# 01: Structural diff between two compiled rules

**What to build:** An API that takes two `CompiledRule<TContext>` instances (or their `RuleDescription` trees) and returns a structural diff describing added, removed, and changed nodes between them.

**Blocked by:** none

**Status:** done

- [x] Diff API accepts two compiled rules and returns added/removed/changed node information
- [x] Structurally identical rules produce an empty/no-change diff
- [x] A changed operator, changed term arguments, and an added/removed operand each produce a distinguishable diff entry
- [x] Covered by tests exercising at least one change per category above
