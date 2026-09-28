# 11: Add Coverlet.Collector and use coverage to find remaining gaps

**What to build:** Add the `coverlet.collector` package to every test project (via `Directory.Packages.props` central version + per-project `PackageReference`) so `dotnet test --collect:"XPlat Code Coverage"` produces a Cobertura report per project. Then run it, merge/inspect the results, and use the uncovered lines/branches to seed the next round of `test-coverage-gaps` tickets (same one-ticket-per-gap convention as 01–10) — this ticket is the tooling + triage pass, not the follow-on coverage fixes themselves.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Code coverage tooling is added to `Directory.Packages.props` with a pinned central version and referenced from every project under `tests/` (`TruthWeaver.Tests`, `TruthWeaver.Predicates.Tests`, `TruthWeaver.Testing.Tests`, `TruthWeaver.Abstractions.Tests`, `TruthWeaver.Yaml.Tests`, `TruthWeaver.Architecture.Tests`).
- [x] A coverage-collecting `dotnet test` invocation runs clean across the solution and produces a Cobertura XML report per test project.
- [x] A short written summary (below) lists the files/members with the lowest coverage, prioritized by how load-bearing they are (e.g. `src/TruthWeaver` over test-only support code).
- [x] New `NN-<slug>.md` tickets are filed in `.scratch/test-coverage-gaps/issues/` for the highest-value gaps found, following the existing 01–10 format.
- [x] Existing test run behavior (`dotnet test` without the coverage flag) is unaffected — coverage collection is opt-in via a flag, not a change to default `dotnet test` output.

## Comments

**Deviation from the ticket's literal ask, and why:** the ticket names `coverlet.collector`, but this
repo's test projects set `UseMicrosoftTestingPlatformRunner=true` (`Directory.Build.targets`), so they
build as native Microsoft.Testing.Platform (MTP) executables, not VSTest hosts. `coverlet.collector` is
a VSTest data collector and cannot attach to an MTP host — `dotnet test --collect:"XPlat Code
Coverage"` against these projects fails immediately with "Zero tests ran" / handshake-failure errors
(exit code 5), confirmed by trying it first. The functional equivalent for MTP is `coverlet.MTP` (same
publisher, same output formats, version 10.0.1 alongside `coverlet.collector` 10.0.1) — a native MTP
extension enabled via a CLI flag instead of a VSTest collector URI. Swapped the package (central version
in `Directory.Packages.props`, `PackageReference` in all six `tests/*.csproj`) and used:

```
dotnet test --coverlet --coverlet-output-format cobertura --results-directory ./coverage-results
```

which produced one `coverage.cobertura.<timestamp>.xml` per test project (6 files), all tests passing
(620/620), with plain `dotnet test` (no flag) unchanged and unaffected.

**Lowest-coverage `src/TruthWeaver` members** (from the `TruthWeaver.Tests` project's report, the
largest and most representative one — 298 classes, 23.3% overall line rate for that project's own
exercised surface; TruthWeaver.Testing.Tests/Predicates.Tests/Yaml.Tests exercise other packages and
were not the focus here), ranked by how load-bearing the class is:

1. `RuleNodeCompiler` (87%, `Compilation/RuleNodeCompiler.cs`) — several diagnostic-emitting guards
   (malformed AND/OR/ExactlyOne arity, zero-operand threshold, unknown argument name) and the
   optional-argument default-substitution path are never hit. Filed as ticket 14.
2. `JsonTreePrinter` (74%, `Json/JsonTreePrinter.cs`) — only `String`/`Int64` literal kinds are ever
   printed in a test; `Decimal`/`Boolean`/`Guid`/`DateTimeOffset`/array literals are untested. Filed as
   ticket 12.
3. `RuleBuilder` (80%, `Building/RuleBuilder.cs`) — the fluent API's `ValueToNode` only has its
   `string` branch exercised; `bool`/`int`/`long`/`double`/`decimal`/`DateTimeOffset`/array/unsupported-
   type branches are untested. Filed as ticket 13.
4. `OperatorInfo` (83%, `Ast/OperatorInfo.cs`) — both defensive exception branches (rejecting a
   `TermExpression`, an unhandled threshold comparison) are untested. Filed as ticket 15.

Lower-priority gaps not filed as tickets (smaller, more defensive/unreachable-in-practice code):
`Evaluator`'s private tracing-description switch (lines 61-67) and a couple of single-line defensive
`throw`s in `RuleNodeCompiler.Build`/`ValidThresholdRange` for AST node/enum values outside the closed
set (ADR-0004), which are unreachable through any public compilation path today.
