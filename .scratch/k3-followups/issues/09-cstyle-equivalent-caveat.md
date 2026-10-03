# 09: Document the C-style EQUIVALENT caveat

**What to build:** The C-style operator rendering prints EQUIVALENT as ==, but in C# null == null is true while Unknown EQUIVALENT Unknown is Unknown, so the spelling is misleading. Keep word forms for IMPLIES, NAND and NOR in C-style (no natural C spelling; avoid => and ->) and document the == caveat in the OperatorStyle XML docs and README. Findings: research items 3a and the headline discoveries.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] OperatorStyle XML docs state the divergence for Unknown (the README does not mention OperatorStyle, so no README change is needed)
- [ ] Issues-log rows 8 and 10 closed as 'keep' (left for ticket 18, which reconciles the log)
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

- Caveat added to the `OperatorStyle.CStyle` XML docs. README has no C-style section to extend (it only mentions C-family languages in the arity table), so no README change was made. Issues-log rows 8 and 10 were not edited here; ticket 18 reconciles the log.
