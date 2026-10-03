# 04: K3 value operations and boundaries

**Status:** `COALESCE`, `If`, inspection, `Project` ready-for-agent after 02; `Collapse` per ADR-0005 #14
**Blocked by:** 02

**What to build:** `COALESCE`/`??` (n-ary; first non-`Unknown`), `If`/`? :` (K3-aware conditional), inspection (`IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`), and boundaries `Project` and `Collapse`.

- [ ] `COALESCE` replaces only `Unknown` operands; otherwise returns the K3 value unchanged
- [ ] `If(c, t, f)`: `Unknown` condition behaviour defined and tested (result `Unknown` unless `t` and `f` agree)
- [ ] Inspection nodes always return a definite `True`/`False` and do not collapse the enclosing expression
- [ ] Boundary nodes yield a two-valued result explicitly; interaction with `EvaluationMode`/`Decision` documented
- [ ] Analyzer (BDD) treatment of inspection/boundary nodes documented and tested

## `Project` (settled, ADR-0005 #12) and `Collapse` (settled, ADR-0005 #14)

`Project(expr, T|F)` follows `.tmp/ProjectAndCollapse.md`. That document's `Collapse` (truth collapse `T`/`F,U→F`; falsity collapse `F`/`T,U→T`) is numerically identical to `Project(U→F)` / `Project(U→T)`, and it recommends reserving "collapse" for `IsTrue`/`IsFalse`, which already exist as inspection. It also contradicts `.tmp/Strong Kleene K3 Logic.md`, whose `Collapse` adds `UnknownIsError`. The proposal below keeps `Collapse` only for what `Project` cannot do.


Source: `.tmp/Strong Kleene K3 Logic.md` sections 27-29 and 42. The spec lists only `UnknownAsFalse`/`UnknownAsTrue` under Project and adds `UnknownIsError` under Collapse; it never states the difference beyond "mapping" vs "act of requiring a definite Boolean", and it also says projection is "not K3 semantics" while the invariants say every expression yields a K3 value. The proposal resolves that:

- `Project(expr, UnknownAsFalse | UnknownAsTrue)` is an in-tree node. It yields a definite `True`/`False` as a `TruthValue`, so the "every expression is K3" invariant holds. `Project(x, UnknownAsFalse)` equals `COALESCE(x, False)` and `Project(x, UnknownAsTrue)` equals `COALESCE(x, True)`; kept as a named alias for intent.
- `Collapse(expr, policy)` is the final boundary that produces a two-valued application result: policies `UnknownAsFalse`, `UnknownAsTrue`, `UnknownIsError`. `UnknownIsError` is reported as a failed `Decision`/diagnostic, never an exception (CLAUDE.md). It is exposed on the evaluation API (`Decision`/`EvaluationOptions`) and also accepted as the outermost DSL function; it may not be nested inside another operator.
- `UnknownRequiresResolution` (mentioned once in `.tmp/Recommended specification set.md`) is not defined anywhere and is out of scope.
