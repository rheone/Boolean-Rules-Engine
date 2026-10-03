# 25: Compress to non-primitive forms

**What to build:** An opt-in transform that rewrites expanded primitives back into readable derived operators where possible.

**Blocked by:** 23

**Status:** ready-for-agent

- [ ] Compress(Expand(rule)) evaluates equal to the rule for all assignments
- [ ] Result is no larger than the expanded form
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
