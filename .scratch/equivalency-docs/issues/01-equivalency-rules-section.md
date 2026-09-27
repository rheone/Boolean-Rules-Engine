# 01: Add an "Equivalency rules" section to CONTEXT.md

**What to build:** A new section in `CONTEXT.md`, placed near the conceptual model / operator table, listing the operator identities below and the recorded reasoning against `All`/`None`.

**Blocked by:** none

**Status:** done

- [x] `AtMost(0, ...)` ≡ `NOT(OR(...))` (equivalently `NOR`)
- [x] `Exactly(n, ...)` where `n` equals the operand count ≡ `AND(...)`
- [x] `Exactly(1, ...)` ≡ `ExactlyOne(...)`
- [x] `XNOR(a, b)` ≡ `NOT(XOR(a, b))`
- [x] `GreaterThan(0, ...)` ≡ `OR(...)`
- [x] `LessThan(n, ...)` where `n` equals the operand count ≡ `NOT(AND(...))`
- [x] Explicit note: no `All`/`None` operators exist because they would duplicate `AND`/`NOT(OR(...))` with no new semantics — consistent with ADR-0003's decision against operator synonyms (`IMPLIES`, symbol aliases)
- [x] Section cross-linked from ADR-0003's threshold-family amendment (the section documenting `AtLeast`/`AtMost`/`GreaterThan`/`LessThan`/`Exactly`)
- [x] No code changes — documentation only; verify no analyzer/doc-lint step in the required validation list is affected
