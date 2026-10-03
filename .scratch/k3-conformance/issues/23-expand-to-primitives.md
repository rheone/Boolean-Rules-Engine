# 23: Expand to primitives

**What to build:** An opt-in transform on the immutable tree that expands derived operators into the primitive kernel (NOT, AND, OR, AtLeast, AtMost, Exactly, COALESCE).

**Blocked by:** 10, 11, 12, 13, 14, 17

**Status:** ready-for-agent

- [ ] For any rule and any {T,F,U} assignment, the expanded rule evaluates equal to the original (property test)
- [ ] Only primitive nodes remain in the output
- [ ] The original tree is untouched
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
