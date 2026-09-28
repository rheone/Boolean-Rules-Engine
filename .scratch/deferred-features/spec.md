# Deferred features

Moved out of [CONTEXT.md](../../CONTEXT.md) so the public docs describe only
what exists today. Recorded here so these are revisited deliberately rather
than rediscovered from scratch. None of these are rejected outright — the
AST and compiler are designed so each remains addable without a breaking
rework.

| Item | Why deferred |
| --- | --- |
| **Authorization layer** (policy sets, permit/forbid, forbid-overrides, decision-with-provenance) | A genuinely different, larger problem than "evaluate one boolean expression." Belongs as a layer built *on* this engine, likely a separate package, once there's a concrete consumer. |
| **Partial evaluation / residual expressions** (bind known facts, simplify, hand the caller a residual expression to push into e.g. a SQL `WHERE` clause) | This is what "who can do X against many resources" really wants, but it requires predicates to be *translatable*, not just callable, which contradicts "a predicate is opaque application code." The engine is evaluate-only; callers loop over candidates, made cheap by per-evaluation memoization and a shared `CompiledRule`. |
| **Rule-to-rule references / named reusable fragments** | Valuable for a real rule library (shared sub-rules, cycle detection, compile-time inlining) but adds a resolver abstraction the current scope doesn't need. |
| **Cross-evaluation caching** | The predicate-author contract only promises stability *within* one evaluation. A cache spanning evaluations is a distinct feature with its own invalidation story. |
| **OpenTelemetry-shaped observability** (activity per rule, event per term, fault attributes) | Logging goes through `Microsoft.Extensions.Logging.Abstractions` today. OTel would be additive on top, not part of the current design. |
| **Context-bound term arguments** (e.g. `IsManagerOf({{resource.ownerId}})`) | Requires a typed path-expression mini-language and breaks static canonical-equality between rules. Arguments are literals only; a predicate that needs a live-resolved value — keyed by a rule-text literal, a `TContext`-supplied value, or both, with no requirement that either side be an identity or "the current user" — resolves it itself. See [README's "n arguments, class-based, externally-resolved value"](../../README.md#n-arguments-class-based-externally-resolved-value) for the documented alternative. See also [`.scratch/context-bound-term-arguments`](../context-bound-term-arguments) for prior investigation. |
| **Symbol operator aliases** (`&&`, `||`) | Word operators only, to keep the surface to one thing to learn and test. |
| **Concurrent operand evaluation** | Purely additive once predicates are contractually pure; left as an `EvaluationOptions` knob for later rather than the current default behavior. |
| **Minimal satisfying assignments** (BDD-derived "what facts would make this true") | The BDD exists anyway for constant/contradiction diagnostics; exposing satisfying-assignment enumeration is an authoring-tool feature with no current consumer. |
| **Attribute-based / assembly-scanned predicate registration** | Explicit registration only — scanning is magic, breaks trimming/AOT, and the repo's own rule is "do not introduce unnecessary abstractions." |
