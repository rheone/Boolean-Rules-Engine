# 02: Predicates return TruthValue

**What to build:** Predicates can answer Unknown directly. IPredicate, all built-in predicate delegates, the externally-resolved-value helper delegate and FakePredicates return TruthValue; an Unknown result records no fault, while throw/timeout/cancellation still yield Unknown plus a Fault.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Predicate returning Unknown evaluates to Unknown with no Fault
- [ ] Throwing, timed-out and cancelled predicates still give Unknown plus a Fault
- [ ] FakePredicates returns Unknown directly and keeps a separate fault simulator
- [ ] Built-in predicate libraries and tests migrated; null-selected-value convention preserved
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
