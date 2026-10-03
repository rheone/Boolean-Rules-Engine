# 08: Naming pass: diagnostic constant and print method

**What to build:** Small public-name fixes made while pre-1.0. Rename the C# constant for the shared arity diagnostic to InfixArityViolation while keeping the string code BRE0006 unchanged (diagnostic IDs are never reused or renamed). Rename CompiledRule.PrintText(GroupingStyle) to PrintRuleText to avoid confusion with PrintPlainText. Document Diagnostic.Path as an RFC 9535 singular query. Findings: research items 2b and 5.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] The constant is renamed everywhere; the string value BRE0006 is unchanged and tests assert it
- [x] PrintRuleText replaces PrintText in code, tests, README and ADR-0005 decision 9
- [x] Diagnostic.Path XML docs describe the path syntax
- [ ] Issues-log rows 7, 11, 26, 35, 37 updated (left for ticket 18, which reconciles the log)
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

- Renamed the constant to `InfixArityViolation` (string `BRE0006` unchanged; asserted by tests), `CompiledRule.PrintText` to `PrintRuleText`, and documented `Diagnostic.Path` as an RFC 9535 singular query. Historical tickets, the issues log and the research reports keep the old names on purpose. Remaining: the issues-log rows, done by ticket 18, so this ticket stays open until then.
