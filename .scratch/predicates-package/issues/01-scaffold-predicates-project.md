# 01: Scaffold `BooleanRulesEngine.Predicates`

**What to build:** The new project and its test project, wired into the solution and central package management, following the same conventions as the existing packages (`Directory.Build.props`, StyleCop, nullable/implicit-usings, file-scoped namespaces).

**Blocked by:** none

**Status:** ready-for-agent

- [ ] New project `src/BooleanRulesEngine.Predicates/BooleanRulesEngine.Predicates.csproj`, referencing only `BooleanRulesEngine.Abstractions` — no reference to `BooleanRulesEngine` or `BooleanRulesEngine.Yaml`
- [ ] Project added to the SLNX solution file
- [ ] Test project `tests/BooleanRulesEngine.Predicates.Tests`, xUnit v3 + NSubstitute per repo convention, added to the solution
- [ ] No new third-party package is required for the predicates planned in tickets 02–04 (regex uses `System.Text.RegularExpressions` from the BCL) — confirm `Directory.Packages.props` needs no change; if a later ticket needs one, that ticket updates it there, not ad hoc
- [ ] ADR-0004 amended with this package's boundary: what it depends on (`Abstractions` only), what would depend on it (any host opting in), and why it's a separate package rather than folded into `Abstractions` itself (keeps `Abstractions` a zero-opinion kernel; these predicates are a convenience layer, not part of the contract every predicate author must implement against)
- [ ] `dotnet restore --locked-mode`, `dotnet build`, and `dotnet csharpier check .` pass with the new (empty) project in place
