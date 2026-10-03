# 10: Record the predicate catalog rules

**What to build:** CONTEXT.md records the rules the predicate catalog follows, resolving the open questions in the predicate gap list: a null selected value is a definite False for the existing built-in members while new comparison families return Unknown for a null input; string comparison is ordinal only (no culture-sensitive comparison); only DateTimeOffset is accepted for date/time (no DateTime literal kind); clock predicates receive a TimeProvider at registration so evaluation stays testable. Findings: research item 7.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] CONTEXT.md states each rule with a one-line rationale
- [ ] The gap list's open questions are marked resolved or deferred with links
- [ ] Issues-log row 41 closed

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
