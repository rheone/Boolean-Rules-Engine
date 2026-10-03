# 03: Resource limits for expansion and rewrites

**What to build:** ExpandToPrimitives and the NAND-only / NOR-only expansions can produce trees larger than the compile node limit, because operands that a definition mentions twice are repeated, and threshold forms in gate-only expansions grow as C(n, k) (issues-log rows 28 and 29). Add an explicit size guard for the rewrites with a clear, exception-free failure result (or a documented option to raise the cap) and tests for the largest accepted and the first refused size, so a hostile or large rule cannot exhaust memory or time. Document the growth characteristics.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Every rewrite has a documented maximum output size and returns a clear failure result instead of exhausting memory when exceeded
- [ ] Tests cover a rule just inside the cap, just over it, and a wide threshold in NAND/NOR form
- [ ] Normal rules are unaffected and the suite runtime does not grow noticeably
- [ ] README documents the cap and the growth behaviour
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).
