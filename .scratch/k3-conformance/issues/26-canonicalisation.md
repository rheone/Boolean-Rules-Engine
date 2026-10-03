# 26: Canonicalisation

**What to build:** Equivalent rules (under the K3-sound rewrite set) are given a single deterministic representation, so rules can be compared and de-duplicated.

**Blocked by:** 23

**Status:** ready-for-agent

- [ ] Canonical form is deterministic and idempotent
- [ ] Evaluation equals the original for all assignments
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
