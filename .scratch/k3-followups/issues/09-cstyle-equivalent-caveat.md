# 09: Document the C-style EQUIVALENT caveat

**What to build:** The C-style operator rendering prints EQUIVALENT as ==, but in C# null == null is true while Unknown EQUIVALENT Unknown is Unknown, so the spelling is misleading. Keep word forms for IMPLIES, NAND and NOR in C-style (no natural C spelling; avoid => and ->) and document the == caveat in the OperatorStyle XML docs and README. Findings: research items 3a and the headline discoveries.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] OperatorStyle XML docs and README state the divergence for Unknown
- [ ] Issues-log rows 8 and 10 closed as 'keep'
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
