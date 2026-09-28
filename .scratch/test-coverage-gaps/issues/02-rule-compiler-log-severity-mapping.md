# 02: Cover RuleCompilerLog severity mapping

**What to build:** Confirm that every `DiagnosticSeverity` value logged during rule compilation is mapped to the correct `LogLevel` by `RuleCompilerLog`'s `ToLogLevel`, not just the `Error` case that's currently exercised.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A diagnostic with `DiagnosticSeverity.Warning` logs at the log level `ToLogLevel` maps it to.
- [x] A diagnostic with `DiagnosticSeverity.Info` (or whichever value falls into the default branch) logs at the correct level.
- [x] The existing `Error`-severity test continues to pass unchanged.

## Comments

Already covered on `main` (commit `166f95a`, "Cover RuleCompilerLog severity mapping (test-coverage-gaps ticket 02)") — `LoggingTests.Compiling_a_rule_with_a_warning_diagnostic_logs_at_warning_level` and `Compiling_a_rule_with_an_info_diagnostic_logs_at_information_level` exercise the Warning and Info/default branches alongside the pre-existing Error test. Verified via `dotnet test` (620/620 passing) — no code or test changes were needed this pass.
