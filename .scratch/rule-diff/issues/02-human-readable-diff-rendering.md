# 02: Human-readable diff rendering

**What to build:** A formatter that renders the structural diff from ticket 01 as human-readable text (or another display-ready shape) suitable for an audit-log entry or a rule-review UI.

**Blocked by:** 01 (structural diff between two compiled rules)

**Status:** ready-for-agent

- [ ] A rendering function turns a diff result into readable text describing each change (e.g. "added operand: hasRole(role: Y)")
- [ ] Output uses each node's `Label`/`Description` (from `RuleDescription`) rather than raw AST type names
- [ ] Example usage documented (README or XML doc) showing a before/after rule and the resulting rendered diff
