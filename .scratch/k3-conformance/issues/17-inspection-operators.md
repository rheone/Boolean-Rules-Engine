# 17: Inspection operators

**What to build:** IsTrue, IsFalse, IsUnknown and IsKnown test a result's state without collapsing the enclosing expression (they always yield a definite True or False). Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 03, 06

**Status:** done

- [x] Each operator matches the oracle
- [x] An inspection inside a larger rule does not fault or collapse the rest
- [x] Analyzer understands inspection
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`IsTrue`, `IsFalse`, `IsUnknown` and `IsKnown` are implemented as **one** first-class `InspectionExpression(Kind, Operand)` with a public `InspectionKind` enum, rather than four node types: they differ only in the state they test (the same shape as `ThresholdExpression`), `NodeShape.OpName` is the kind name, and every consumer (printers, JSON/YAML, descriptors, analyzer, evaluator) needs a single case plus a switch on the kind. Semantics (verified against `K3Oracle.IsTrue/IsFalse/IsUnknown/IsKnown`, which are defined directly because no K3 connective can detect a state): `IsTrue(x)` = x is True, `IsFalse(x)` = x is False, `IsUnknown(x)` = x is Unknown, `IsKnown(x)` = x is not Unknown; the result is always a definite True/False. They are reserved function-call words (any letter case) with exactly one operand (`MalformedTree` otherwise, also for a JSON/YAML node). The operand is always evaluated; a faulting predicate inside still records its own fault and becomes Unknown, but the inspection adds no fault and its definite answer keeps the rest of the enclosing rule evaluating normally (`IsUnknown(boom) AND ok` is True with `ok` evaluated). Analyzer: both rails are the same BDD (`IsTrue` = D, `IsFalse` = NOT P, `IsUnknown` = P AND NOT D, `IsKnown` = D OR NOT P), so `IsUnknown(a) OR IsKnown(a)` is reported as a genuine tautology and `IsTrue(a) AND IsFalse(a)` as a contradiction; the analyzer-vs-oracle generator gained an inspection case. The canonical printer writes `IsTrue(a)`; the label is the same word in every `OperatorStyle` (no symbolic/C-style spelling). JSON/YAML ops `isTrue`/`isFalse`/`isUnknown`/`isKnown` (schema: added to the unary node's op enum); `RuleBuilder.IsTrue/IsFalse/IsUnknown/IsKnown`. Tests: `InspectionTests` plus the parity, shape, descriptor, schema, alignment, round-trip property and analyzer generators. README, CONTEXT.md and ADR-0005 updated.
