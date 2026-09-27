# 01: BenchmarkDotNet suite for compile-time analysis and eval-time lookup

**What to build:** A BenchmarkDotNet project with benchmarks for compile-time BDD analysis (varying rule size/shape) and eval-time memoized term lookup (varying shared-term fan-out), runnable via a documented command, with a baseline result committed to the repo.

**Blocked by:** none

**Status:** done

- [x] A benchmarks project exists using BenchmarkDotNet, following the repo's central package management conventions
- [x] At least one benchmark measures compile-time cost (including BDD analysis) across at least two representative rule sizes
- [x] At least one benchmark measures eval-time memoized lookup cost with shared terms referenced from multiple branches
- [x] A baseline result is committed (e.g. as a markdown/CSV export) and the run command is documented in the project README or CLAUDE.md
