# 01: `RuleCompiler.CompileJson(JsonElement)` overload

**What to build:** A public `CompileJson(JsonElement element)` overload on `RuleCompiler<TContext>`, alongside the existing `CompileJson(string json)`. `JsonTreeParser` gains a `Parse(JsonElement)` overload that both the new overload and the existing string overload (after its own `JsonDocument.Parse`) funnel through, so there is one tree-parsing implementation.

**Blocked by:** none

**Status:** done

- [x] `RuleCompiler<TContext>.CompileJson(JsonElement element)` public overload added
- [x] `JsonTreeParser.Parse(JsonElement)` internal overload added; `Parse(string)` is refactored to call it after `JsonDocument.Parse`
- [x] A malformed or wrong-shape element (missing all of `op`/`predicate`/`const`) produces the same diagnostic (`MalformedTree` or equivalent) as today's `CompileJson(string)` would for the equivalent standalone JSON text
- [x] Test: build a multi-field JSON document with `System.Text.Json`, extract one property's value as a `JsonElement`, compile it via the new overload, and confirm the result is structurally equal to compiling the same tree as a standalone JSON string via `CompileJson(string)`
- [x] Test: two sibling rule expressions embedded in one document compile independently via two separate calls, with no cross-talk between their diagnostics
