# 08: YAML tree parse/print

**What to build:** The identical tree shape from ticket 07, expressed in YAML via YamlDotNet, isolated in the `BooleanRulesEngine.Yaml` package so that a consumer with no interest in YAML never acquires the YamlDotNet dependency transitively.

**Blocked by:** 07

**Status:** done (verified against existing codebase — already implemented prior to this pass)

- [ ] The YAML form of the ADR-0003 worked example parses to a tree structurally equal to the DSL and JSON forms of the same rule
- [ ] A `CompiledRule` can be printed to the YAML tree shape, and parsing that YAML back reproduces a structurally equal tree
- [ ] `BooleanRulesEngine.Yaml` is the only package in the solution referencing YamlDotNet
- [ ] Malformed YAML produces a compile diagnostic, not an unhandled exception
