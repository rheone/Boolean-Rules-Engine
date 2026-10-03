# 07: Symbol aliases for existing operators

**What to build:** Rule authors can write &&, ||, !, ∧, ∨, ¬ and ⊕ and get exactly the same tree as the named operators; the canonical printer stays word-only.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] Each symbol compiles to a tree equal to its named operator
- [ ] Canonical printer emits named form only
- [ ] Precedence is unchanged
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
