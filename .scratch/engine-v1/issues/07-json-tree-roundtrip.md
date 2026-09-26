# 07: JSON tree parse/print & round-trip

**What to build:** `System.Text.Json` support (in the `BooleanRulesEngine` package) for the flat, key-discriminated JSON tree shape from ADR-0003 — a node is discriminated by which key is present (`op` vs. `predicate`) rather than an extra wrapper object: `{"op": "and"|"or"|"not"|"xor"|"exactlyOne"|"atLeast", "operands": [...]}` (with `atLeast` additionally carrying its threshold `k`) versus `{"predicate": "...", "args": {...}}`. Parsing a JSON tree produces exactly the same AST as parsing the equivalent DSL text, and a compiled rule can be printed back out to the same JSON shape.

**Blocked by:** 06

**Status:** ready-for-agent

- [ ] The JSON shape from ADR-0003 (including the worked example) parses to a `CompiledRule` (or an intermediate AST feeding the same compiler pipeline as the DSL) structurally equal to parsing the equivalent DSL text
- [ ] `AtLeast`'s threshold `k` round-trips correctly through the JSON form
- [ ] A `CompiledRule` can be printed to the JSON tree shape, and parsing that JSON back reproduces a structurally equal tree (`parse(print(x))` round-trip, both directions: JSON→AST→JSON, and DSL→AST→JSON→AST is structurally equal to DSL→AST)
- [ ] Malformed JSON (unknown `op` value, a node with neither `op` nor `predicate`, wrong argument value type) produces a compile diagnostic, not an unhandled exception
