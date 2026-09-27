# 02: `CompileYaml(YamlNode)` overload

**What to build:** Mirror ticket 01 for YAML: a `CompileYaml<TContext>(this RuleCompiler<TContext> compiler, YamlNode node)` overload on `YamlRuleExtensions`, alongside the existing `CompileYaml(string yaml)`. `YamlTreeParser` gains a `Parse(YamlNode)` overload both overloads funnel through. Also add the short design note this feature needs in the docs, since it establishes a stance (no path/pointer language) that isn't obviously derivable from the code alone.

**Blocked by:** 01 (mirrors its shape and test structure)

**Status:** done

- [x] `YamlRuleExtensions.CompileYaml<TContext>(RuleCompiler<TContext> compiler, YamlNode node)` overload added
- [x] `YamlTreeParser.Parse(YamlNode)` internal overload added; `Parse(string)` refactored to call it after its own YAML deserialization step
- [x] Test: build a multi-field YAML document with YamlDotNet, extract one mapping node, compile it via the new overload, and confirm the result is structurally equal to compiling the same tree as standalone YAML text via `CompileYaml(string)`
- [x] ADR-0003 (or CONTEXT.md, whichever already hosts the "arguments are literals only, no context-path syntax" decision) gains a short note: sub-tree extraction is caller-side navigation via the JSON/YAML library's own node APIs, not a path/pointer syntax added to the engine — same rationale as the existing decision against context-bound argument values
