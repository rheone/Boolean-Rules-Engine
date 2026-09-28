# 03: Cover unnamed rule-swap notification

**What to build:** Verify that swapping a rule without supplying an explicit rule identifier still produces a sensible, correctly-labeled notification/log entry (falling back to the default "(unnamed)" identifier) via `RuleCompiler.NotifyRuleSwapped()`.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Calling the rule-swap path without an explicit `ruleIdentifier` argument produces a notification/log entry showing the default fallback identifier.
- [x] Existing tests that pass an explicit identifier continue to pass unchanged.

## Comments

Already covered on `main` (commit `209c847`, "Cover unnamed rule-swap notification (test-coverage-gaps ticket 03)") — `LoggingTests.Rule_swap_notification_without_an_identifier_falls_back_to_unnamed` calls `NotifyRuleSwapped()` with no argument and asserts the logged `RuleIdentifier` field is `"(unnamed)"`; the pre-existing explicit-identifier test still passes. Verified via `dotnet test` (620/620 passing) — no code or test changes were needed this pass.
