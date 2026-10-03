# 27: Simplification

**What to build:** An opt-in transform that replaces an expression with an equivalent, cheaper one using only K3-sound rewrites; classical-only rewrites such as A OR NOT A => True are never applied.

**Blocked by:** 06, 26

**Status:** ready-for-agent

- [ ] Evaluation equals the original for all assignments (property test)
- [ ] Known classical-only rewrites are demonstrably not applied
- [ ] Output is never larger than the input
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
