# 03: AND / OR / NOT with Kleene semantics

**What to build:** Rules combining terms with `AND`, `OR`, and `NOT` — parsed with `NOT > AND > OR` precedence, evaluated per the three-valued Kleene truth tables in ADR-0001, strictly left-to-right with short-circuiting (`AND` stops at the first `False` operand, `OR` stops at the first `True` operand), and a trace that explicitly records which nodes were `NotEvaluated` due to short-circuiting rather than omitting them.

A fault in one operand does not abort the evaluation of the whole expression — evaluation continues wherever the truth tables can still reach a determinate result (e.g. `OR(<fault>, True)` evaluates to `True`, not `Unknown`), per the predicate-author contract and ADR-0001's reasoning about why fail-closed coercion (`NOT(fault)` silently becoming `True`) is actively dangerous.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] `hasRole AND isManager`-shaped rules (using the zero-arg predicates from ticket 02) parse and evaluate correctly for all combinations of `True`/`False`/`Unknown` operands, matching the `AND`/`OR`/`NOT` truth tables in ADR-0001 exactly
- [ ] `NOT` binds tighter than `AND`, which binds tighter than `OR`, with no parentheses required for the common cases (e.g. `NOT a AND b` parses as `(NOT a) AND b`)
- [ ] `AND` stops evaluating operands after the first `False`; `OR` stops after the first `True`; a trace records unevaluated operands as `NotEvaluated`, not omitted
- [ ] `NOT(<term that faults>)` evaluates to `Unknown`, not `True` — the fail-closed-coercion bug ADR-0001 explicitly guards against does not occur
- [ ] An `OR` with one faulting operand and one `True` operand evaluates to `True` (the fault doesn't abort or degrade the result); an `AND` with one faulting operand and one `False` operand evaluates to `False`
- [ ] Sibling operands are evaluated in the order written, not concurrently
