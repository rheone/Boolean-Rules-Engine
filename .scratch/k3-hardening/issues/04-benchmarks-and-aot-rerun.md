# 04: Re-run benchmarks and the AOT/trim gate

**What to build:** The language surface roughly tripled and both JSON/YAML parsers were rewritten since the benchmarks and trim analysis were last run. Add benchmarks for the new operators (evaluation) and the rewrites (expand, compress, canonicalise, simplify) and for diagnostics formatting, re-run the existing compile and evaluation benchmarks, re-run the AOT/trim analysis and CI gate, and record the baseline so regressions are visible.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] Benchmarks exist for each new operator family and each rewrite, and compile/evaluate baselines are recorded
- [ ] The AOT/trim analysis passes, or each new warning is addressed or documented
- [ ] The baseline numbers are stored where the repository keeps benchmark notes
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).
