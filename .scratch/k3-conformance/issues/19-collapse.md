# 19: Collapse

**What to build:** Collapse(expr, policy) as the evaluation-API boundary (and an outermost-only DSL function) that turns a K3 result into a two-valued answer. UnknownIsError produces an explicit rejected-unresolved outcome with no Fault. Decision.IsSatisfied stays fail-closed.

**Blocked by:** 02, 18

**Status:** done

- [x] Each policy is exercised against the oracle
- [x] UnknownIsError distinguishes not-known from something-broke and records no Fault
- [x] Collapse nested inside a rule is rejected with a diagnostic
- [x] Decision.IsSatisfied is still true only for True
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`Collapse(expr, policy)` is the evaluation-API boundary. Design (recorded in ADR-0005 decision 14 and the README "Collapse" section):

- **API.** `CollapsePolicy` (`UnknownAsFalse`, `UnknownAsTrue`, `UnknownIsError`) and `CollapseOutcome` (`False`, `True`, `RejectedUnresolved`) in `TruthWeaver.Abstractions`, plus `Decision.Collapse(policy)` (pure) and an optional trailing `Decision.Outcome` (`null` unless the rule declared a collapse). "Rejected: unresolved" is the third `CollapseOutcome` value, so the two-valued answer and the rejection are one small type; it is not a `Fault` and nothing is thrown, so `Decision.Faults` still means "a predicate broke" and a faulting predicate under `UnknownIsError` yields a rejected outcome *and* its fault.
- **`IsSatisfied` stays fail-closed** (`Result == True`). `decision.Collapse(...)` never mutates a decision. A policy *declared in the rule* is the author's explicit choice: `UnknownAsFalse`/`UnknownAsTrue` make `Decision.Result` the collapsed definite value (the raw value is the single child of the evaluated tree); `UnknownIsError` leaves `Result` three-valued, so a rejected decision is not satisfied.
- **DSL / compiler.** `Collapse` is a reserved function-call word (any case) with an expression and a policy name (any case); a wrong shape or unknown policy is a `SyntaxError` at the offending token. It is not an `Expression` node: the parsers build a `CollapseNode` anywhere, the compiler peels a root one into `CompiledRule.CollapsePolicy` and reports any other as the new `NestedCollapse` (`BRE0016`) error with a span covering exactly the nested collapse (also for `Collapse(Collapse(...), ...)`, branches, operators, builder and JSON/YAML nodes).
- **Rendering.** The canonical text is `Collapse(inner, Policy)` and recompiles identically; `Describe()` and the evaluated tree get a matching `Collapse(UnknownAsFalse)` root so plain-text/Mermaid output and the alignment guarantee hold. The analyzer analyzes the inner expression unchanged.
- **JSON/YAML.** Supported, as an outermost `{"op": "collapse", "policy": "unknownAsFalse", "operands": [x]}` node (`op: collapse`, `policy: ...`), so a declared collapse survives every round trip; the schema has a `collapseOperatorNode`. `RuleBuilder.Collapse` is new.
- **`RuleDiff`.** Describing a collapse root would misalign `RuleDiff`'s walk of the expression tree against `Describe()`, so it now diffs the inner description and reports a changed/added/removed policy as one root `Changed` entry. While there, `SameShape` now also distinguishes `InspectionExpression` kinds and `ProjectExpression` policies (a ticket 17/18 gap: `IsTrue(a)` vs `IsFalse(a)` and `Project(a, True)` vs `Project(a, False)` previously diffed as identical).
- `UnknownRequiresResolution` is out of scope.

Tests: `CollapseTests` (oracle over every policy and input, composed operands, faults, `IsSatisfied`, nested rejection with spans, bad policies/shapes, DSL/JSON/YAML/builder round trips, analyzer, description/evaluated tree/renderings), `TruthValueAndDecisionTests` (`Decision.Collapse`/`Outcome`), `RuleDiffTests`, and the alignment, op-name parity and schema fixtures. README, CONTEXT.md and ADR-0005 updated.
