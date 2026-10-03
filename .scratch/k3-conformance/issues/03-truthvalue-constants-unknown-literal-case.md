# 03: TruthValue constants, Unknown literal, case-insensitivity

**What to build:** Constants are TruthValue end to end so a rule can contain Unknown. True, False, Unknown and every operator name are case-insensitive; the canonical printer emits upper camel / upper case. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 01

**Status:** done

- [x] Unknown literal parses, builds, prints and round-trips in DSL, JSON and YAML; JSON schema updated
- [x] Mixed-case constants and operator names compile to the same tree
- [x] Canonical printer output is consistent upper camel / upper case
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Constants are `TruthValue` end to end: `ConstantExpression`, `ConstantNode`, the DSL parser (`Unknown` is a reserved word), `RuleNodeCompiler`, `Evaluator`, `OperatorInfo`, `CanonicalPrinter`, JSON/YAML parsers and printers, `RuleBuilder.Constant(TruthValue)` (the `bool` overload remains) and `rule-tree.schema.json`. A shared `TruthValueText` helper owns the spelling table.

Decisions:
- JSON keeps `{"const": true|false}` for compatibility; `Unknown` is `{"const": "unknown"}`. A string constant in any case is accepted for all three values. YAML is `const: unknown`. Recorded in ADR-0005 decision 16.
- Canonical text, descriptors and evaluated-tree descriptions now say `True`/`False`/`Unknown` (previously lower-case `true`/`false`).
- Failed-node substitution stays `False` here; that is ticket 04.
- Interim analyzer: each `Unknown` literal is a fresh BDD variable so `Unknown AND NOT Unknown` is not flagged as a contradiction; the proper dual-rail handling is ticket 06.
- Operator names were already case-insensitive in DSL, JSON and YAML; tests now lock this in.
- Updated tests that encoded the old behaviour (JSON/YAML const error text, builder canonical text, constant construction, round-trip generator now includes `Unknown`).
