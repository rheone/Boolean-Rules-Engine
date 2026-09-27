# 11: EvaluationOptions: FaultBudget & Exhaustive mode

**What to build:** `EvaluationOptions`, passed into `CompiledRule.EvaluateAsync`, exposing:

- `FaultBudget` (default unlimited): once the number of faults recorded during one evaluation reaches this budget, evaluation aborts immediately rather than continuing to spend I/O trying to route around it — an explicit caller opt-in for fail-fast behavior (e.g. during a known outage), not a change to the engine's default semantics.
- `Mode` (`Default`, `Exhaustive`): in `Exhaustive` mode, every reachable term is evaluated (no short-circuit) and every fault is collected, for diagnostic/support use. This mode must never change `Decision.Result` — only which terms run and what the trace/fault list contains.
- An overall evaluation timeout, linked into the caller's `CancellationToken`, defaulted off.

**Blocked by:** 03

**Status:** done (verified against existing codebase — already implemented prior to this pass)

- [ ] With `FaultBudget = 1`, an evaluation that hits a second faulting term aborts immediately (the resulting `Decision` reflects an aborted evaluation, distinct from the unlimited-budget case where evaluation would have continued)
- [ ] With the default (unlimited) `FaultBudget`, an evaluation with multiple faulting terms continues to completion exactly as in tickets 02/03
- [ ] With `Mode = Exhaustive`, an `AND` with a `False` first operand still evaluates its remaining operands (rather than short-circuiting), and all their faults (if any) appear in `Decision.Faults`
- [ ] For any rule, `Decision.Result` is identical between `Mode = Default` and `Mode = Exhaustive` — only the set of terms evaluated and faults collected differs
- [ ] An `EvaluationOptions` timeout, once elapsed, cancels the in-flight evaluation via the linked `CancellationToken`; with no timeout configured (the default), evaluation is unaffected
