> **Historical.** Operator-set and alias decisions below were partly superseded by ADR-0005 (`docs/adr/0005-strong-k3-language-surface.md`).

# BooleanRulesEngine v1

**Status:** ready-for-agent

## Problem Statement

An application developer needs to answer "is this boolean expression true right now, for this context?" — e.g. "can this user do X" — where some of the leaf conditions require I/O (a database lookup, an HTTP call, a config read) and can fail. Hand-rolling this means either a runtime crash on a downstream timeout, a silent and potentially unsafe `false` coercion on failure (dangerous under `NOT`), or bespoke fault-handling duplicated at every call site. The developer also needs rules to be author-editable text (not recompiled C#), validated before being persisted, and evaluable repeatedly and cheaply against many candidates without re-parsing.

Today this repository has only a design (`CONTEXT.md`, `docs/adr/0001`–`0004`) and a placeholder `Rule` class with no implementation.

## Solution

A compile-once/evaluate-many boolean rule engine: rule text (a canonical word-operator DSL, or JSON/YAML that compiles to the identical tree) is parsed and validated into an immutable `CompiledRule`. Evaluating a `CompiledRule` against an application-supplied `TContext` and `IServiceProvider` invokes registered predicates, absorbs any predicate fault as `Unknown` (three-valued Kleene logic) rather than throwing or silently failing closed, and returns a `Decision` (`TruthValue` + faults + optional trace). Compilation never throws for authoring errors — it returns diagnostics so a persistence layer can reject a bad edit while keeping the previously-accepted rule active.

## User Stories

1. As a predicate author, I want to implement `IPredicate<TContext>` with a static `PredicateSchema` and an async `EvaluateAsync`, so that I can express a domain condition (`hasRole`, `isManager`) without depending on the parser, compiler, or analyzer.
2. As a predicate author, I want to signal "I cannot determine this" by simply throwing (a timeout, a connection failure), so that I don't have to write try/catch-and-wrap-as-Unknown boilerplate for the common case.
3. As a rule author, I want to write a rule as a plain-word boolean expression (`hasRole(role: "Y") AND (hasTraining(training: "Q") OR isManager)`), so that non-C#-developers can author and review rules directly.
4. As a rule author, I want named arguments only (`hasRole(role: "Y")`), so that argument order in the source never matters and a predicate's schema can be validated once at compile time.
5. As a rule author, I want zero-argument terms written bare (`isManager`, not `isManager()`), so that the common case reads cleanly.
6. As a rule author, I want `NOT` > `AND` > `OR` precedence when parsing, so that I don't have to parenthesize the common cases.
7. As a rule author, I want mixing `XOR` with `AND`/`OR` without parentheses to be a compile error, so that an ambiguous rule is caught before it's persisted rather than silently misinterpreted.
8. As a rule author, I want `XOR` restricted to exactly two operands (a compile error otherwise), so that I never accidentally rely on the parity generalization when I meant "exactly one."
9. As a rule author, I want an explicit `ExactlyOne(...)` operator for n-ary "exactly one of these," so that the "exactly one" meaning has its own unambiguous name.
10. As a rule author, I want an `AtLeast(k, ...)` threshold operator, so that I can express "any two of these three approvals."
11. As a rule author, I want `true`/`false` constants usable directly in a rule, so that I can express fixed conditions or stub out incomplete logic.
12. As a rule author, I want `IMPLIES` and symbol aliases (`&&`, `||`) to not exist, so that there is exactly one syntax to learn, document, and get right.
13. As a rule author, I want argument values restricted to a closed set of literal types (`string`, `long`, `decimal`, `bool`, `DateTimeOffset`, and arrays of those), so that argument validation and canonical equality stay purely structural.
14. As a rule author, I want a canonical printed form that always round-trips back to an equal tree and always parenthesizes `XOR` explicitly, so that two persisted rules can be diffed or compared as text.
15. As a UI rule-builder developer, I want to generate and consume a flat, key-discriminated JSON tree (`{"op": ..., "operands": [...]}` vs. `{"predicate": ..., "args": {...}}`), so that I don't need a DSL parser in my tooling.
16. As a UI rule-builder developer, I want the identical tree shape available in YAML, so that I can offer either format without a second grammar.
17. As a UI rule-builder developer, I want `parse(print(x))` to be structurally equal to `x` in both directions for DSL, JSON, and YAML, so that round-tripping through any surface never silently changes a rule's meaning.
18. As a rule-editing application, I want `RuleCompiler.Compile` to never throw for an authoring error, so that a bad rule submitted through a UI produces diagnostics I can display, not an unhandled exception.
19. As a rule-editing application, I want each diagnostic to carry a code, severity, and source span, so that an editor can underline the offending token.
20. As a rule-editing application, I want a rejected save (any `Error`-severity diagnostic) to leave the previously persisted rule active and write nothing new, so that validation and persistence are inseparable by construction.
21. As a rule-editing application on a shared rule store, I want an opt-in `CompilationMode.Lenient` where an unknown predicate compiles to a permanent `Unknown` term fault instead of an error, so that cross-service predicate mismatches don't block rules meant for a different service.
22. As an application evaluating rules, I want `Decision.IsSatisfied` to be `true` only when the result is `TruthValue.True`, so that `Unknown` fails closed by default (the safe default for an authorization consumer).
23. As an application evaluating rules, I want `Decision.Faults` populated with the term identity and exception for every absorbed fault, so that I can log, alert, or retry instead of silently denying with no signal anything went wrong.
24. As an application evaluating rules, I want a fault in one term to not necessarily abort the whole evaluation, so that a transient failure in an irrelevant branch (e.g. one operand of an `OR` whose other operand is `True`) doesn't turn into a denial.
25. As an application with a known active outage, I want an `EvaluationOptions.FaultBudget` I can set (e.g. to `1`), so that I can opt into fail-fast behavior without the engine changing its default semantics for everyone else.
26. As an application evaluating rules, I want operands evaluated strictly left-to-right with short-circuiting (`AND` stops at the first `False`, `OR` stops at the first `True`), so that evaluation cost and observable predicate-call order are predictable.
27. As an application debugging a denial, I want an `EvaluationMode.Exhaustive` option that evaluates every reachable term and collects every fault without changing `Decision.Result`, so that I can get full diagnostic information without altering the actual authorization outcome.
28. As an application evaluating rules, I want a trace that records explicitly which nodes were `NotEvaluated` due to short-circuiting, so that a "why was I denied" report doesn't have unexplained holes.
29. As an application evaluating a large or deeply nested rule many times, I want each distinct term identity evaluated at most once per evaluation (memoized within that evaluation only), so that a predicate referenced from multiple branches isn't invoked redundantly.
30. As a predicate author, I want term identity defined as predicate name (case-insensitive) plus arguments sorted by name and compared by exact type-normalized value, with argument order in the source irrelevant but array-valued arguments order-sensitive, so that memoization and canonical equality behave exactly as documented in `CONTEXT.md`.
31. As a predicate author, I want argument value comparison to be case-sensitive, so that `role: "Y"` and `role: "y"` are correctly treated as different terms.
32. As a predicate author with a scoped dependency (e.g. a `DbContext`), I want my class-based predicate resolved from a per-evaluation `IServiceProvider` rather than captured once at registration, so that it resolves correctly on every evaluation even though the rule long outlives any one scope.
33. As an application wiring up the engine, I want an explicit `PredicateRegistry` builder API (plus a lambda form for stateless predicates) with no attribute/assembly scanning, so that registration is inspectable, trimming/AOT-safe, and matches this repo's rule against implicit magic.
34. As an application wiring up the engine, I want a rule referencing an unregistered predicate name to be a compile-time diagnostic (in default/strict mode), so that a typo or missing registration is caught before evaluation, not as a runtime surprise.
35. As an application that hot-reloads rules, I want to call `RuleCompiler.Compile` again and assign the result to the field/property holding the active `CompiledRule` as a single reference swap, so that in-flight evaluations finish against the old rule, new evaluations pick up the new one, and no lock is needed.
36. As an application authoring rules from an untrusted or non-code-reviewed source (e.g. an admin UI backed by a database), I want compile-time bounds on tree depth (default 32), node count (default 512), and the number of distinct terms subject to BDD-based analysis (default 20, beyond which analysis is skipped and reported as `Info`, never silently claimed as "not constant"), so that a pathological rule can't hang a request thread.
37. As an application with latency requirements, I want an overall evaluation timeout in `EvaluationOptions` linked into my own `CancellationToken`, defaulted off, so that I can bound worst-case evaluation latency when I need to.
38. As a rule author, I want the analyzer to flag constant and contradictory sub-expressions as compile diagnostics, so that I can catch a logic error (e.g. `hasRole(role: "Y") AND NOT hasRole(role: "Y")`) before persisting the rule.
39. As a predicate-implementing service that has no interest in parsing, compiling, or analyzing rules, I want to depend only on `BooleanRulesEngine.Abstractions` (zero third-party dependencies: `IPredicate<TContext>`, `PredicateSchema`, `PredicateArguments`, `TruthValue`, `Decision`, `Fault`), so that I don't transitively acquire a parser, a BDD analyzer, or YamlDotNet.
40. As a rule-authoring/evaluation host, I want to depend on `BooleanRulesEngine` for the AST, DSL parser, `RuleCompiler`, analyzer, evaluator, `System.Text.Json` tree support, and DI registration extensions, so that I get the full authoring and evaluation surface from one package.
41. As a host with no interest in YAML, I want YAML tree support isolated in `BooleanRulesEngine.Yaml`, so that I never acquire a YamlDotNet dependency I don't use.
42. As a library maintainer, I want the library to log through `ILogger<T>` (`Microsoft.Extensions.Logging.Abstractions`) only, never a concrete provider, so that the host application can wire whatever logging provider it already uses.
43. As a library maintainer, I want faults, compile diagnostics, and rule-swap events logged as structured log events, so that operators get baseline observability without an OpenTelemetry dependency in v1.
44. As a code reviewer, I want `bool?` to never appear on this library's public surface, so that "optional bool" is never confused with the three-valued `TruthValue` and its Kleene semantics.

## Implementation Decisions

- **Packages** (per ADR-0004): `BooleanRulesEngine.Abstractions` (zero third-party dependencies — `IPredicate<TContext>`, `PredicateSchema`, `PredicateArguments`, `TruthValue`, `Decision`, `Fault`); `BooleanRulesEngine` (AST, DSL parser, `RuleCompiler`, `CompiledRule`, BDD-based analyzer, evaluator, `System.Text.Json` tree support, DI registration extensions — depends on `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Logging.Abstractions` only); `BooleanRulesEngine.Yaml` (YAML tree support via YamlDotNet only). The current single `src/BooleanRulesEngine` project must be split into these three projects/packages before or as part of the first vertical slice.
- **Core types**: `TruthValue` enum (`False`, `True`, `Unknown` — never `bool?`); `Decision` record (`Result`, `Faults`, optional `Trace`, computed `IsSatisfied`); `Fault` (term identity + exception); `IPredicate<TContext>` (`static abstract PredicateSchema Schema`, `ValueTask<bool> EvaluateAsync(TContext, PredicateArguments, CancellationToken)`); `PredicateArguments` as a small non-generic typed accessor (`GetString`, `GetInt64`, `GetDecimal`, `GetBool`, `GetDateTimeOffset`, and array variants).
- **AST / operators**: `Term` (predicate name + sorted named arguments), `AND`, `OR`, `NOT`, `XOR` (binary only), `ExactlyOne` (n-ary), `AtLeast(k, ...)` (n-ary threshold), and `true`/`false` constants. No `IMPLIES`, no symbol aliases.
- **Term identity**: predicate name normalized case-insensitively to registered casing, plus arguments sorted by name and compared by exact type-normalized value; argument order in source text does not affect identity; array-valued arguments are order-sensitive; argument values are case-sensitive.
- **DSL grammar**: word operators only, case-insensitive on input; precedence `NOT` > `AND` > `OR`; mixing `XOR` with `AND`/`OR` without parentheses is a compile error; zero-arg terms written bare; arguments named only. Canonical printer emits minimal-but-unambiguous parentheses, always parenthesizes `XOR`, and is deterministic.
- **JSON tree shape**: flat, key-discriminated node — `{"op": "and"|"or"|"not"|"xor"|"exactlyOne"|"atLeast", "operands": [...] }` vs. `{"predicate": "...", "args": {...}}`. `AtLeast` additionally carries its threshold `k`. YAML uses the identical shape.
- **Compilation pipeline**: Parse → Validate (known predicates, argument schema, depth/node limits) → Analyze (BDD-based constant/contradiction/redundancy) → Build immutable tree → `CompilationResult` (nullable `CompiledRule` + `IReadOnlyList<Diagnostic>`, each with code/severity/source span). `Compile` never throws for anything in this pipeline. `CompiledRule` is populated only when there are no `Error`-severity diagnostics.
- **`CompilerOptions`**: max tree depth (default 32), max node count (default 512), max distinct terms subject to BDD analysis (default 20; beyond this, analysis is skipped and reported as `Info`), `CompilationMode` (`Strict` default, `Lenient` — unknown predicate becomes a permanent `Unknown` term fault instead of an `Error` diagnostic).
- **Evaluation**: `CompiledRule.EvaluateAsync(TContext, IServiceProvider, EvaluationOptions?, CancellationToken)` returning `Decision`. Left-to-right, short-circuiting per the Kleene truth tables (ADR-0001); per-evaluation memoization keyed by term identity; class-based predicates resolved per-evaluation from the supplied `IServiceProvider` (never cached across evaluations); a fault (thrown exception) is caught at the term boundary, recorded, and the term treated as `Unknown` for the remainder of that evaluation only. No concurrent sibling evaluation in v1.
- **`EvaluationOptions`**: `FaultBudget` (default unlimited), `Mode` (`Default`, `Exhaustive` — never changes `Decision.Result`), overall evaluation timeout linked into the caller's `CancellationToken` (default off).
- **`PredicateRegistry`**: builder API for explicit registration — implementation type (resolved per-evaluation from `IServiceProvider`) or stateless lambda. No attribute/assembly scanning.
- **Logging**: `ILogger<T>` from `Microsoft.Extensions.Logging.Abstractions` only; structured log events for faults, compile diagnostics, and rule-swap events (rule-swap logging is the consuming application's responsibility to trigger, since the library doesn't own rule storage/swap scheduling — it logs what it's told).
- **Out-of-band consequence**: `Directory.Packages.props` currently carries a large set of unrelated package versions (Entity Framework Core, NHibernate, Autofac, FluentValidation, ASP.NET Core testing, etc.) apparently templated from a different repository, and has no `YamlDotNet` entry at all. This needs cleanup/addition as part of implementation, not left for a later ticket to trip over.

## Testing Decisions

- **Single seam**: drive tests through `RuleCompiler.Compile(...)` → `CompiledRule` — both `EvaluateAsync` (behavior) and the canonical printer/`ToString()` (round-trip) — using small hand-written `IPredicate<TContext>` test predicates (constant-true, constant-false, throwing, delayed/cancellable) rather than mocks for the majority of cases. This matches the compile-once/evaluate-many model the ADRs describe and keeps tests behavioral rather than tied to internal AST shape.
- **NSubstitute** is reserved specifically for verifying scoped-DI predicate resolution (a substitute `IServiceProvider`/scoped service to assert a predicate is resolved per-evaluation, not captured once at registration) — the one place a real collaborator can't easily observe the behavior under test.
- Only test observable behavior (`Decision.Result`, `Decision.Faults`, `Decision.IsSatisfied`, diagnostics and their codes/severities/spans, canonical printed text, round-trip equality) — never internal AST node types or private evaluator state.
- Prefer one logical behavior per test, Arrange/Act/Assert, per `CLAUDE.md`.
- Modules under test: the DSL parser, the JSON tree parser, the YAML tree parser (in `BooleanRulesEngine.Yaml`), the canonical printer, `RuleCompiler` (validation diagnostics, resource limits, `CompilationMode.Lenient`), the analyzer (constant/contradiction detection), the evaluator (Kleene truth tables for every operator, short-circuiting, memoization, fault absorption, `FaultBudget`, `EvaluationMode.Exhaustive`), and `PredicateRegistry` (explicit registration, per-evaluation resolution).
- Prior art: none yet in this repo — `RuleTests.cs` is currently an empty placeholder class; this spec's tickets are what populates it (likely split into multiple test classes/files as the projects split per the package-boundaries decision).

## Out of Scope

Everything listed under `CONTEXT.md`'s [Deferred](../../CONTEXT.md#deferred) table is explicitly out of scope for this spec: an authorization layer (policy sets, permit/forbid, forbid-overrides), partial evaluation / residual expressions, rule-to-rule references and named reusable fragments, cross-evaluation caching, OpenTelemetry-shaped observability, context-bound term arguments (path-expression syntax), symbol operator aliases, concurrent operand evaluation, minimal satisfying assignments, and attribute/assembly-scanned predicate registration.

Also out of scope: rule storage, scheduling, or invalidation (the library owns compilation and evaluation only, per ADR-0002); any concrete logging provider; a UI rule builder itself (only the JSON/YAML tree shape it would consume/produce).

## Post-v1 Amendments

Added after this spec's tickets shipped; see
[ADR-0003's Amendments section](../../docs/adr/0003-rule-syntax-and-serialization.md#amendments)
for full rationale on each:

- **Threshold operator family**: `AtLeast(k, ...)` generalized into
  `AtLeast`/`AtMost`/`GreaterThan`/`LessThan`/`Exactly`, all sharing one AST
  node (`ThresholdExpression`) and one compile-time out-of-range check.
- **`XNOR`** (logical biconditional / `IFF`) added alongside `XOR`, with the
  same binary-only restriction, always-parenthesized printing, and
  ambiguous-mixing compile error (now also covering `XOR`-vs-`XNOR` mixing).
- **Required predicate descriptions**: `PredicateSchema` and
  `PredicateArgumentSchema` both require a read-only `Description` string,
  so a rule-authoring UI or generated documentation always has something to
  show for every registered predicate and argument.
- **Canonical printer clarity parens**: the printer now parenthesizes an
  operand whenever it's a different operator than the one it's nested under
  (e.g. `a AND b OR c` prints as `(a AND b) OR c`), even where precedence
  alone already made the parse unambiguous — readability for a large nested
  rule, not just correctness.
- **`RuleBuilder`** (`BooleanRulesEngine.Building`): a fluent API for
  assembling a rule from application logic without hand-writing DSL/JSON/YAML
  text. Renders to the existing flat JSON tree shape and compiles through
  `RuleCompiler.CompileJson`, so it goes through the identical
  Validate/Analyze pipeline as any other rule source — not a new front end
  into the AST.

## Further Notes

- The BDD-based analyzer (constant/contradiction/redundancy detection) is nontrivial and load-bearing for several user stories (7, 8, 38) and for the node-count-based analysis cap (36) — it may warrant its own prefactoring/design pass before ticket-splitting, since it's the one component that isn't a fairly direct ADR-to-code translation.
- `Microsoft.CodeAnalysis.CSharp` is already present in `Directory.Packages.props`, seemingly left over from a different template; confirm during implementation whether the parser is meant to be Roslyn-based (unlikely, given "DSL parser" language throughout the ADRs, which suggests a hand-written recursive-descent or similar parser) or whether that package reference should simply be removed as unused.
- `src/BooleanRulesEngine/Rule.cs`'s current abstract `Rule.Evaluate()` shape does not match the ADRs (`Rule` isn't described as having a synchronous parameterless `Evaluate()`; evaluation is async, takes a `TContext` and `IServiceProvider`, and returns a `Decision` from a `CompiledRule`, not a `bool` from a `Rule`) — it should be treated as a stale placeholder to be replaced, not preserved.
