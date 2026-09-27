# 08: Test Evaluator cancellation and the fault-budget abort path

**What to build:** `Evaluator.cs:323-332` distinguishes a genuine `OperationCanceledException`
(triggered by the caller's `CancellationToken`) from a predicate that faults with some other
exception, and separately tracks a `FaultBudget` that aborts evaluation once exceeded. Every
existing test that touches cancellation passes `CancellationToken.None` — grepping
`tests/BooleanRulesEngine.Tests/*.cs` for `CancellationTokenSource` or `.Cancel(` returns zero
hits, meaning the actual cancellation path (as opposed to "cancellation not requested") has never
been exercised.

Follow TDD: write each test first against `CompiledRule<TContext>.EvaluateAsync(...)`, run it,
then only change `Evaluator.cs` if a test reveals an actual defect.

Cover:

- A predicate that observes a `CancellationTokenSource.Cancel()` mid-evaluation (e.g. a test
  predicate that calls `ct.ThrowIfCancellationRequested()` or throws
  `OperationCanceledException` itself once the token is cancelled) causes `EvaluateAsync` to
  propagate the cancellation rather than recording it as a `Fault` — the `when` clause at line 323
  should route a *real* cancellation around the `faults.Add` branch entirely
- A predicate that throws `OperationCanceledException` on its own initiative, *without* the
  caller's token being cancelled, is recorded as an ordinary `Fault` (per the `when` clause's
  `!this.cancellationToken.IsCancellationRequested` half) — the existing fault/memoization tests
  in `ArgumentsAndMemoizationTests.cs` use a "faulting" test predicate as a pattern to follow, but
  check whether any of them specifically throw `OperationCanceledException` (as opposed to a plain
  exception) — if not, this is a genuinely new case
- `EvaluationOptions.FaultBudget` set to a small value (e.g. `1`): the first fault is tolerated
  (evaluation continues, contributing `Unknown`/faulted state per the existing Kleene rules) but a
  second fault exceeding the budget causes evaluation to abort early — check
  `EvaluationOptionsTests.cs` for whether this exact boundary (`faults.Count > budget`, i.e.
  strictly greater-than) is already covered before writing a duplicate test

**Blocked by:** none

**Status:** ready-for-agent

- [ ] All three bullets above have corresponding tests (checking first whether
      `EvaluationOptionsTests.cs` already covers the fault-budget boundary, to avoid duplication)
- [ ] `dotnet test` passes
- [ ] No change to `Evaluator.cs` unless a test reveals a real defect — note any such finding in
      this ticket's Comments
