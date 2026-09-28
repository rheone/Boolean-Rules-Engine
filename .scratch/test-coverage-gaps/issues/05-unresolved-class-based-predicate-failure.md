# 05: Cover unresolved class-based predicate failure

**What to build:** Verify that evaluating a rule which references a class-based predicate whose implementation type isn't registered in the DI container fails with a clear, actionable error rather than an unrelated exception or silent misbehavior.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Evaluating a rule with a class-based predicate whose implementation type has no service registration throws with a message naming the unresolved type.
- [x] Existing scoped-resolution tests covering the successful resolution path continue to pass unchanged.

## Comments

Already covered on `main` (commit `63c7980`, "Cover unresolved class-based predicate failure (test-coverage-gaps ticket 05)") — `ScopedResolutionAndRegistrationTests.Class_based_predicate_with_no_service_registration_is_absorbed_as_a_fault_naming_the_unresolved_type` confirms the `InvalidOperationException` from `Evaluator.InvokeClassBasedAsync` names `ScopedFlagPredicate` and is recorded as a `Fault` on the `Decision` (per the engine's fault-absorption design, ADR-0002) rather than escaping `EvaluateAsync` unhandled or being silently swallowed. The successful-resolution tests in the same file still pass. Verified via `dotnet test` (620/620 passing) — no code or test changes were needed this pass.
