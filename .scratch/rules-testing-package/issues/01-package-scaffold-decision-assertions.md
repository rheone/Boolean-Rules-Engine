# 01: BooleanRulesEngine.Testing package with fluent Decision assertions

**What to build:** The `BooleanRulesEngine.Testing` package project (referencing `BooleanRulesEngine.Abstractions` only, following the repo's central package management and package-boundary conventions from ADR-0004) with fluent assertions over a `Decision`, publishable and consumable from a test project immediately.

**Blocked by:** none

**Status:** done

- [x] New `BooleanRulesEngine.Testing` project depends only on `BooleanRulesEngine.Abstractions`
- [x] Fluent assertions exist for at least: `Decision.IsSatisfied`, the resulting `TruthValue`, and presence/absence of a `Fault`
- [x] A consumer test project referencing the package can assert on a `Decision` without accessing `IsSatisfied`/`Faults` directly
- [x] Project follows repo conventions (CSharpier formatting, nullable reference types enabled, file-scoped namespaces, StyleCop clean, no unnecessary abstractions)
