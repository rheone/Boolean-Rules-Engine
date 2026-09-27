# Improve per-unit test coverage, least-covered first

**Status:** ready-for-agent

## Problem Statement

`dotnet test` currently passes 215 tests, but coverage is concentrated at the
black-box/integration level: most test files exercise the public
`RuleCompiler<TContext>.Compile(...)` → `CompiledRule<TContext>.EvaluateAsync(...)` pipeline end to
end (`CompilationTests`, `KleeneOperatorTests`, `XorExactlyOneThresholdTests`, etc.), rather than
targeting individual internal units directly. Cross-referencing every source file's type name
against every test file turned up several internal units with **zero** direct references anywhere
in `tests/`:

- `Lexer` (`src/BooleanRulesEngine/Parsing/Lexer.cs`, 191 lines)
- `DslParser` (`src/BooleanRulesEngine/Parsing/DslParser.cs`, 447 lines — the largest source file
  in the repo)
- `Analyzer` / `BddManager` (`src/BooleanRulesEngine/Analysis/`, 216 + 176 lines — a hand-rolled
  ROBDD engine backing every structural tautology/contradiction diagnostic)
- `RuleNodeCompiler`, `CanonicalPrinter`, `JsonTreeParser`/`JsonTreePrinter`,
  `YamlTreeParser`/`YamlTreePrinter`, the new `Printing/*` tree renderers

This isn't just a naming-search artifact: **the test project has no `InternalsVisibleTo` grant**.
`src/BooleanRulesEngine/AssemblyInfo.cs:6` grants it only to `BooleanRulesEngine.Yaml`. Every one
of the internal types above is therefore structurally impossible to construct or call directly
from `tests/BooleanRulesEngine.Tests` today — the only way to reach them is indirectly, through
`RuleCompiler`. That's a real gap in *how much of each unit's behavior a test can pin down*: a
black-box test proves the pipeline produces the right final answer for the cases it tries, but
can't cheaply enumerate a unit's internal edge cases (e.g. every `BddManager.Ite` reduction rule,
every `Lexer` recovery path) without going through several unrelated layers first.

`BddManager` is the highest-priority gap by risk, not just by line count: ROBDD engines are
notoriously easy to get subtly wrong (variable ordering, node uniquification, `Ite` reduction
short-circuits), and a bug there doesn't crash — it silently produces a wrong
tautology/contradiction diagnostic, which is a warning users have no independent way to check.

## Solution

1. Grant the test project `InternalsVisibleTo` access (ticket 01) — a prerequisite for everything
   else.
2. Add direct unit tests to the least-covered, highest-risk internal units first: `BddManager`,
   then `Lexer`, then `DslParser`, then the `Analyzer` term-cap boundary that has no test at all
   today.
3. Round out thinner areas identified along the way: `RuleBuilder` (264 lines, 8 tests),
   `Evaluator` internals not already pinned down by the existing Kleene/threshold/memoization
   suites, and the JSON/YAML front ends' malformed-input diagnostic paths (only happy-path
   round-trips are tested today).

## Constraints

- Use TDD: for each ticket, write the test first, run it, and only then decide whether the
  production code needs a fix. Most of these units are expected to already behave correctly —
  the point is pinning down that behavior with a fast, isolated test, not assuming bugs exist.
  If a test does reveal a real bug, fix it and note the discovery in the ticket's Comments.
- Tests target observable behavior (inputs → outputs/diagnostics), not implementation details —
  e.g. assert on `BddManager.True`/`False`/node-equality results, not on private field layout.
- Do not weaken or remove any existing test.
- Run `dotnet test` (and `dotnet csharpier check .` / `dotnet format --verify-no-changes
  --severity info`) after each ticket.

## User Stories

1. As a maintainer, I want the test project to be able to construct and call internal types
   directly, so future unit tests aren't forced into black-box-only testing through
   `RuleCompiler`.
2. As a maintainer, I want `BddManager`'s `Ite`/`And`/`Or`/`Not`/`Xor`/node-sharing behavior
   directly tested, so a subtle BDD bug is caught by a fast, targeted test instead of surfacing as
   a wrong warning in someone's rule.
3. As a maintainer, I want `Lexer` and `DslParser` edge cases (malformed input, error recovery,
   ambiguous operator mixing) directly tested, so the parser's error-diagnostic paths — not just
   its happy path — are pinned down.
4. As a maintainer, I want the `Analyzer`'s `MaxAnalysisTerms` cap behavior tested, so the
   `AnalysisSkippedTooManyTerms` diagnostic (currently asserted nowhere) is verified to actually
   fire at the boundary.
