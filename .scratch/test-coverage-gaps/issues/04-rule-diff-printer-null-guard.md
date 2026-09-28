# 04: Cover RuleDiffPrinter null-guard

**What to build:** Confirm that calling `RuleDiffPrinter.Print` with a null diff result fails fast with a clear exception instead of a null-reference failure deeper in the call stack.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `RuleDiffPrinter.Print(null!)` throws `ArgumentNullException` naming the `diff` parameter.
- [x] Existing `RuleDiffPrinterTests` continue to pass unchanged.

## Comments

Already covered on `main` (commit `f0dc4e7`, "Cover RuleDiffPrinter null-guard (test-coverage-gaps ticket 04)") — `RuleDiffPrinterTests.A_null_diff_throws_argument_null_exception_naming_the_diff_parameter` asserts `ArgumentNullException.ParamName == "diff"`, matching the production `ArgumentNullException.ThrowIfNull(diff)` guard already in `RuleDiffPrinter.Print`. Verified via `dotnet test` (620/620 passing) — no code or test changes were needed this pass.
