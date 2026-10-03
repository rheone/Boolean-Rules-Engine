# 18: Project

**What to build:** Project(expr, True|False) as an in-tree node that replaces Unknown with a chosen definite value. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 15

**Status:** done

- [x] Project yields a definite value for every input and matches the oracle
- [x] Known values pass through unchanged
- [x] Analyzer understands Project
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`Project(expr, True|False)` is a first-class `ProjectExpression(Operand, bool UnknownAs)` node: `True`/`False` pass through, `Unknown` becomes the chosen constant, the result is always definite and equals `COALESCE(expr, value)` (verified exhaustively against `K3Oracle.Project`, which is defined as `Coalesce([x, value])`). Decisions (recorded in ADR-0005 decision 13):

- **DSL second argument.** Parsed as a full expression and then required to be a `True`/`False` constant (any letter case). `Unknown`, any non-constant (`Project(a, b)`, `NOT True`, `b AND c`) and a missing or extra argument are a `SyntaxError` with a readable message and the span of the offending argument; parsing continues for recovery.
- **JSON/YAML.** `{"op": "project", "unknownAs": true, "operands": [x]}` / `op: project`, `unknownAs: true`. The policy is a field beside the single operand (as `BETWEEN` has `min`/`max` and thresholds have `k`), written as a plain boolean and read as a boolean or `"true"`/`"false"` string in any case; missing, `"unknown"` or non-boolean is `MalformedTree`. Schema gained `projectOperatorNode`; `NodeShape` gained an optional `UnknownAs`.
- **Analyzer.** Both rails are the same BDD: the operand's possible rail for `Project(x, True)` and its definite rail for `Project(x, False)`. The analyzer-vs-oracle generator gained a Project case.
- **Faults.** The operand's faults are kept; the projection makes the value definite but does not hide a fault.
- Canonical text `Project(a, True)`, label `Project(True)`/`Project(False)` in every `OperatorStyle`, `RuleBuilder.Project(operand, unknownAs)`.

Tests: `ProjectTests` plus the parity, shape, descriptor, schema, alignment, canonical printer, round-trip property and analyzer generators. README, CONTEXT.md and ADR-0005 updated.
