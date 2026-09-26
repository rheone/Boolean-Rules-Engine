# 01: Prefactor: package split & dependency cleanup

**What to build:** Restructure the single `src/BooleanRulesEngine` project into the three packages ADR-0004 specifies, and clear out stale scaffolding so later tickets build on a clean base:

- `BooleanRulesEngine.Abstractions` — zero third-party dependencies.
- `BooleanRulesEngine` — depends only on `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Logging.Abstractions`.
- `BooleanRulesEngine.Yaml` — depends on `BooleanRulesEngine` and YamlDotNet only.

`Directory.Packages.props` currently carries a large set of package versions unrelated to this project (Entity Framework Core, NHibernate, Autofac, FluentValidation, ASP.NET Core testing, etc.), apparently templated from a different repo, and has no `YamlDotNet` entry at all. Resolve this: add what's needed, remove what isn't, and confirm whether `Microsoft.CodeAnalysis.CSharp` is actually needed (the ADRs describe a hand-written DSL parser, not a Roslyn-based one) or should be dropped as unused.

The current placeholder `Rule.cs` (an abstract `Rule` with a synchronous parameterless `Evaluate()`) does not match the ADRs' shape (evaluation is async, takes a `TContext` and `IServiceProvider`, returns a `Decision`) — delete it and its empty `RuleTests.cs` counterpart rather than adapting them.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Solution contains three projects/packages matching the dependency graph in ADR-0004 (`Abstractions` has zero third-party dependencies; `BooleanRulesEngine` depends on it plus the two named `Microsoft.Extensions.*.Abstractions` packages; `BooleanRulesEngine.Yaml` depends on `BooleanRulesEngine` plus YamlDotNet)
- [ ] `BooleanRulesEngine.slnx` references all three projects (plus a test project per package, or a shared test project referencing all three — whichever this repo's existing test-project convention supports)
- [ ] `Directory.Packages.props` contains a `YamlDotNet` version entry and no longer carries package versions with no consumer in this repo
- [ ] The disposition of `Microsoft.CodeAnalysis.CSharp` (kept with a documented reason, or removed) is resolved, not left ambiguous
- [ ] `dotnet build` and `dotnet restore --locked-mode` succeed against the new project layout
- [ ] The stale `Rule.cs` and its placeholder test class are removed
