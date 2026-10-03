# TruthWeaver

## Project

This repository contains the **TruthWeaver** C# package library.
The project is a general-purpose **Strong Kleene Logic** (K3) expression engine for .NET.
The trinary values for three value logic are the set `True`, `False`, and `Unknown`.
Author a rule once, compile it into an immutable tree, and evaluate it many times against whatever application context you supply; a user, a request, a resource, or anything else.

The project is written with .NET 11 and C# 15

Target framework `net11.0`; SDK pinned in `global.json` (`11.0.100-rc.1.26425.128`, rolls forward to later majors including RC). Local tools (CSharpier, Husky.Net, Roslynator) are in `dotnet-tools.json`; Husky pre-commit tasks are in `.husky/task-runner.json`. Central Package Management (`Directory.Packages.props`), xUnit v3, SLNX solution.

## **Cardinal Rules**

0. Always follow cardinal rules unless explicitly directed not to.
1. If a request is ambiguous, ask clarifying questions before doing anything. If possible, provide a list of potential selectable answers and state which is your recommendation.
2. When a request is made of you, and you think something additional or an alternate path should be considered state such and allow me to respond to approve, iterate, or decline
3. don't change anything I didn't ask you to change, ask clarification is you believe the scope of a request should be expanded
4. Preplan your tool calls, and group in batches where it makes sense.
5. Don't trust assumptions when verifiable information exists. Assess confidence before acting:
6. Do not apologize, just fix it and tell me what changed.
7. This file is for the LLM, mutate it as necessary while following all of the prescribed rules.

## Architecture

- Packages: `TruthWeaver` (compiler, evaluator, printers, analyzer), `TruthWeaver.Abstractions` (`TruthValue`, `Decision`, `IPredicate`), `TruthWeaver.Predicates`, `TruthWeaver.Testing` (`FakePredicates`, `DecisionAssertions`), `TruthWeaver.Yaml`. Each has a matching `tests/` project; `TruthWeaver.Architecture.Tests` enforces boundaries.
- Pipeline (ADR-0003): Parse → Validate → Analyze → Build. `Compile` never throws for authoring errors; it returns `CompilationResult` (nullable `CompiledRule` plus diagnostics).
- Rule text (DSL), JSON, YAML and `RuleBuilder` all compile to the same immutable `Expression` tree, so semantics live in one place. Adding or changing an operator touches the parser, `RuleNodeCompiler`, `Evaluator`, `Analyzer`/`BddManager`, `NodeShape`/`OperatorInfo`, the printers, JSON/YAML and `rule-tree.schema.json`.
- Evaluation is Strong Kleene K3 over `TruthValue` (`True`/`False`/`Unknown`); predicate exceptions, timeouts and cancellation become `Unknown` plus a `Fault` (ADR-0001, ADR-0002). `Decision.IsSatisfied` is fail-closed.
- Domain terms are in `CONTEXT.md`; accepted decisions are in `docs/adr/`.

## In-flight work

The k3-conformance effort (Strong K3 language surface, `.scratch/k3-conformance/`, tickets 01-31) is complete: [ADR-0005](docs/adr/0005-strong-k3-language-surface.md) is the authority for the operator set, notation, boundaries, rewrites and diagnostics, and supersedes the operator-set, alias and `XOR`/`XNOR` decisions in ADR-0003 (marked in place). Open owner questions are summarised at the top of `.scratch/k3-conformance/issues-log.md`. Predicate catalog gaps are in `.scratch/predicate-catalog/k3-gap-list.md` (not implemented). Do not implement from `.scratch/k3-conformance/_superseded/` or `.scratch/engine-v1`. Material in `.tmp/` is reference only and may be subtly wrong; verify it.

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
- All public APIs should be well documented.
- All non-trivial code should have value added comments.
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

### Running a subset of tests

The test runner is Microsoft.Testing.Platform (xUnit v3), configured in `global.json`:

```powershell
dotnet test tests/TruthWeaver.Tests --filter-class "*KleeneOperatorTests"
dotnet test tests/TruthWeaver.Tests --filter-method "*Not_of_a_faulting_term*"
```

## Formatting

- CSharpier is the authoritative C# formatter.
- Use: `dotnet csharpier format .`
- Do not manually fight CSharpier's formatting.

## Testing

- Tests should follow Arrange / Act / Assert.
- Tests should be named in the format "{MemberUnderTest}_{Scenario}_{Expectation}_Test"
- Tests should describe behavior rather than implementation details.
- Tests should have XML comments to describe what is being tested.
- Prefer one logical behavior per test.

## Git

- Use work trees when appropriate
- Husky.Net provides local pre-commit validation.
- CI is authoritative; hooks provide fast local feedback.

## Claude Code

- Treat this file as repository-level development guidance.
- Inspect existing conventions before making broad changes.
- Do not modify generated or configuration files unnecessarily.

## Agent skills

### Issue tracker

Issues are tracked as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context layout: `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
.tmp may contain additional context or information.

### Markdown Documentation

- When writing markdown use the `github-markdown` skill
- When it would be value added to the documentation to include diagrams use the `mermaid-diagram-generator` skill
