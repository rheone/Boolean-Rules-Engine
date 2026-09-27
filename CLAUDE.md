# BooleanRulesEngine

## Project

This repository contains the **BooleanRulesEngine** C# library.

Target framework: `net11.0`
Pinned SDK: `11.0.100-rc.1.26425.128` (see `global.json`)

The repository uses:

- C#
- .NET
- Central Package Management
- xUnit v3
- NSubstitute
- Roslyn analyzers
- StyleCop.Analyzers
- Roslynator
- CSharpier
- Husky.Net
- SLNX

## Repository layout

- `src/BooleanRulesEngine` — production library
- `tests/BooleanRulesEngine.Tests` — unit tests
- `Directory.Build.props` — common MSBuild configuration
- `Directory.Build.targets` — common MSBuild targets
- `Directory.Packages.props` — central package versions
- `.editorconfig` — editor and analyzer configuration
- `stylecop.json` — StyleCop configuration
- `.husky` — Git hooks
- `.config/dotnet-tools.json` — local .NET tools

## Development rules

- Prefer modern idiomatic C#.
- Nullable reference types are enabled.
- Implicit usings are enabled.
- Prefer file-scoped namespaces.
- Do not introduce unnecessary abstractions.
- Keep APIs small and intentional.
- Prefer composition over inheritance unless inheritance represents a genuine type relationship.
- Do not use exceptions for normal business/control-flow failures.
- Public APIs should be deliberately designed for library consumers.
- Do not add dependencies without a concrete reason.
- Keep tests focused on observable behavior.
- Prefer NSubstitute for test doubles.
- Do not suppress analyzers merely to make a build pass.
- Do not weaken analyzer severity without documenting why.
- Do not add preview language features merely because the SDK is an RC.

## Required validation

Before considering work complete:

```powershell
dotnet restore --locked-mode
dotnet build
dotnet test
dotnet csharpier check .
dotnet format --verify-no-changes --severity info
dotnet roslynator analyze
```

When modifying dependencies, review:

```powershell
dotnet outdated
```

## Formatting

CSharpier is the authoritative C# formatter.

Use:

```powershell
dotnet csharpier format .
```

Do not manually fight CSharpier's formatting.

## Testing

Tests should generally follow Arrange / Act / Assert.

Tests should describe behavior rather than implementation details.

Prefer one logical behavior per test.

## Git

Husky.Net provides local pre-commit validation.

CI is authoritative; hooks provide fast local feedback.

## Claude Code

Treat this file as repository-level development guidance.

Inspect existing conventions before making broad changes.

Do not modify generated or configuration files unnecessarily.

## Agent skills

### Issue tracker

Issues are tracked as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context layout: `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
