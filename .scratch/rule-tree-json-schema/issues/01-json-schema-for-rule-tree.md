# 01: JSON Schema for the rule tree

**What to build:** A JSON Schema document for the ADR-0003 rule tree shape, validated in CI against the compiler's own test fixtures, and shipped as a content file in the `BooleanRulesEngine` package.

**Blocked by:** none

**Status:** ready-for-agent

- [ ] JSON Schema document exists covering every operator's tree shape (`op`/`operands`, `predicate`/`args`, threshold `k`, every `LiteralKind`) per ADR-0003
- [ ] A CI check validates every existing valid-rule JSON fixture against the schema, and every known-invalid fixture fails validation
- [ ] The schema file is packaged as a content asset in the `BooleanRulesEngine` NuGet package
- [ ] The schema is referenced from ADR-0003 and/or CONTEXT.md
