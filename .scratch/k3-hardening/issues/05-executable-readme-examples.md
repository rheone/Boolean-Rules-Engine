# 05: Executable README examples

**What to build:** The README and CONTEXT.md examples are currently verified only by hand (the documentation sweep used an uncommitted throwaway project). Turn the runnable examples (DSL, JSON, YAML, builder, diagnostics output, rewrite output, Decision.Collapse/Project examples) into tests that extract or mirror them, so any future change that breaks a documented example fails the build. Pick the smallest approach (a test that compiles and compares the documented snippets, or marker comments); record the choice.

**Blocked by:** k3-followups 15

**Status:** ready-for-agent

- [ ] Every runnable example in README.md and CONTEXT.md is covered by a test that fails if its documented output changes
- [ ] A deliberately broken example is shown to fail the check
- [ ] Adding a new example has a documented procedure
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).
