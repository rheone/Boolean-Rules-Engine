# 08: K3-aware rule equivalence check

**What to build:** A public way to ask whether two rules are equivalent under Strong K3, built on the dual-rail analyzer and canonicalisation (roadmap item 'public rule-equivalence check'). Return equivalent, not equivalent (with a counter-example assignment of True/False/Unknown to the terms) or undecidable within the term cap, never throwing for normal input. Extend rule diff to report whether a structural change preserves meaning. Verify with the oracle over generated rule pairs.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] Equivalent and non-equivalent pairs are classified correctly over generated rules against the oracle
- [ ] A counter-example assignment is returned for non-equivalent rules
- [ ] The term cap is respected with a clear 'cannot decide' result
- [ ] README documents the API and its limits
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).
