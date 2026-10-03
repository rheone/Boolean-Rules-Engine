# 10: Mutation testing with Stryker.NET

**What to build:** Run mutation testing on the core packages (roadmap item 'Stryker.NET mutation testing') to see whether the 2000+ tests, many of them generated, actually catch faults in the evaluator, analyzer, parser and rewrites. Record the mutation score and survivors, add tests for surviving mutants that matter, and decide whether to run it in CI on a schedule rather than every pull request.

**Blocked by:** 06

**Status:** ready-for-agent

- [ ] A mutation report exists for the evaluator, analyzer, parser and rewrite code with the score recorded
- [ ] Surviving mutants are triaged: killed by a new test, or documented as equivalent
- [ ] The run time and a CI recommendation are recorded
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).
