# 01: Add an "Equivalency rules" section to CONTEXT.md

**What to build:** A new section in `CONTEXT.md`, placed near the conceptual model / operator table, listing the operator identities below and the recorded reasoning against `All`/`None`.

**Blocked by:** none

**Status:** ready-for-agent

- [ ] `AtMost(0, ...)` ≡ `NOT(OR(...))` (equivalently `NOR`)
- [ ] `Exactly(n, ...)` where `n` equals the operand count ≡ `AND(...)`
- [ ] `Exactly(1, ...)` ≡ `ExactlyOne(...)`
- [ ] `XNOR(a, b)` ≡ `NOT(XOR(a, b))`
- [ ] `GreaterThan(0, ...)` ≡ `OR(...)`
- [ ] `LessThan(n, ...)` where `n` equals the operand count ≡ `NOT(AND(...))`
- [ ] Explicit note: no `All`/`None` operators exist because they would duplicate `AND`/`NOT(OR(...))` with no new semantics — consistent with ADR-0003's decision against operator synonyms (`IMPLIES`, symbol aliases)
- [ ] Section cross-linked from ADR-0003's threshold-family amendment (the section documenting `AtLeast`/`AtMost`/`GreaterThan`/`LessThan`/`Exactly`)
- [ ] No code changes — documentation only; verify no analyzer/doc-lint step in the required validation list is affected
