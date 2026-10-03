# TruthWeaver

[![CI](https://github.com/rheone/TruthWeaver/actions/workflows/ci.yml/badge.svg)](https://github.com/rheone/TruthWeaver/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-11.0-512BD4)](global.json)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)

A general-purpose boolean expression engine for .NET. Author a rule once as
text, compile it into an immutable tree, and evaluate it many times against
whatever application context you supply — a user, a request, a resource, or
anything else.

<details>
<summary><strong>Table of contents</strong></summary>

- [What it is (and isn't)](#what-it-is-and-isnt)
- [Requirements](#requirements)
- [Getting started](#getting-started)
- [A tour of the codebase](#a-tour-of-the-codebase)
- [Packages](#packages)
- [Features](#features)
- [Operators](#operators)
  - [Order of operations](#order-of-operations)
  - [Binary vs. unary operators](#binary-vs-unary-operators)
  - [All operators](#all-operators)
- [Choosing a rule format](#choosing-a-rule-format)
- [Predicate types](#predicate-types)
- [Examples](#examples)
- [Building rules programmatically](#building-rules-programmatically)
  - [Converting between DSL, JSON, and YAML](#converting-between-dsl-json-and-yaml)
  - [`RuleBuilder` reference](#rulebuilder-reference)
  - [Describing a compiled rule](#describing-a-compiled-rule)
  - [Rendering a rule as a diagram](#rendering-a-rule-as-a-diagram)
- [Evaluation flow](#evaluation-flow)
- [Compilation pipeline](#compilation-pipeline)
- [Benchmarks](#benchmarks)
- [Glossary](#glossary)
- [Appendix: Truth tables](#appendix-truth-tables)
- [Design documents](#design-documents)
- [License](#license)

</details>

## What it is (and isn't)

`TruthWeaver` answers one question: *is this expression true right
now, for this context?* It knows about `AND`, `OR`, `NOT`, `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`,
`NXOR`, `ExactlyOne`, the threshold family (`AtLeast`/`AtMost`/`GreaterThan`/
`LessThan`/`Exactly`), terms, and evaluation. It does not know about
permissions, workflows, or policies — those are things you build *on top* of
it. A permission check ("can the current user do X") is one consumer of this
engine, not what the engine itself is.

| Concept | Meaning |
| --- | --- |
| **Rule** | A named unit of persistence: metadata + one expression. |
| **Expression** | The boolean tree — operators over terms and sub-expressions. |
| **Predicate** | A registered, reusable implementation, e.g. `hasTopping`, `lovesPineapple`. |
| **Term** | A predicate bound to concrete arguments, e.g. `hasTopping(topping: "greenOlives")` — the tree's leaf node. |
| **Operator** | `AND` `OR` `NOT` `XOR` `EQUIVALENT` `IMPLIES` `NAND` `NOR` `NXOR` `ExactlyOne` and the threshold family (`AtLeast(k)`/`AtMost(k)`/`GreaterThan(k)`/`LessThan(k)`/`Exactly(k)`), plus `true`/`false`. See [Operators](#operators) below. |
| **Decision** | The evaluation result: a `TruthValue` plus any faults, and optionally a trace. |

Full vocabulary and the predicate-author contract: [CONTEXT.md](CONTEXT.md).

## Requirements

Building from source needs at least the SDK version floor in
[`global.json`](global.json) — currently `11.0.100-rc.1.26425.128` — but
`rollForward: latestMajor` with `allowPrerelease: true` means any later
major .NET SDK on the machine, preview or RC included, is accepted. This
isn't a pin to that exact patch.

## Getting started

1. **Reference the packages you need.** A service that only *implements*
   predicates references `TruthWeaver.Abstractions`; a host that
   authors and evaluates rules references `TruthWeaver` (and
   `TruthWeaver.Yaml` if it wants YAML too). See
   [Packages](#packages) below.

   ```xml
   <ProjectReference Include="..\TruthWeaver\TruthWeaver.csproj" />
   ```

2. **Implement a predicate.** A zero-argument predicate is the simplest
   shape — a class implementing `IPredicate<TContext>`. A predicate answers
   a three-valued `TruthValue`: return `TruthValue.Unknown` when the answer
   is legitimately indeterminate (that is a normal result and records no
   fault), and throw only for a genuine failure:

   ```csharp
   public sealed class LovesPineapple : IPredicate<Customer>
   {
       public static PredicateSchema Schema =>
           PredicateSchema.NoArguments("lovesPineapple", "Loves Pineapple", "Does this customer like pineapple on pizza?");

       public ValueTask<TruthValue> EvaluateAsync(Customer customer, PredicateArguments args, CancellationToken ct) =>
           ValueTask.FromResult(customer.LovesPineapple ? TruthValue.True : TruthValue.False);
   }
   ```

3. **Register it and compile a rule:**

   ```csharp
   PredicateRegistry<Customer> registry = PredicateRegistry<Customer>.CreateBuilder().Add<LovesPineapple>().Build();
   RuleCompiler<Customer> compiler = new(registry);
   CompilationResult<Customer> result = compiler.Compile("lovesPineapple");

   if (!result.Succeeded)
   {
       // result.Diagnostics explains why - surface it to whoever authored the rule.
       return;
   }
   ```

4. **Evaluate it against a context:**

   ```csharp
   Decision decision = await result.CompiledRule!.EvaluateAsync(customer, serviceProvider, cancellationToken: ct);

   if (decision.IsSatisfied)
   {
       // allowed
   }
   ```

That's the whole lifecycle: implement → register → compile once → evaluate
many times. [A tour of the codebase](#a-tour-of-the-codebase) below maps
that lifecycle onto the actual folders, and [Examples](#examples) builds up
from here to named arguments, the full operator set, and the full ADR-0003
worked example in DSL, JSON, and YAML.

## A tour of the codebase

The five `src` projects mirror the rule lifecycle from
[Getting started](#getting-started): a predicate-implementing service only
needs the zero-dependency kernel, while a rule-authoring host pulls in the
parser, compiler, analyzer, and evaluator.

```mermaid
flowchart TD
    subgraph Kernel["TruthWeaver.Abstractions — the zero-dependency kernel"]
        IPredicate["IPredicate&lt;TContext&gt;, PredicateSchema"]
        Types["TruthValue, Decision, Fault, Trace"]
    end

    subgraph Core["TruthWeaver — parse, compile, analyze, evaluate"]
        direction TB
        Parsing["Parsing<br/>DSL lexer + parser"]
        Ast["Ast<br/>Expression tree, operator metadata"]
        Compilation["Compilation<br/>RuleCompiler, CompilerOptions"]
        Analysis["Analysis<br/>BddManager, constant/contradiction analyzer"]
        Building["Building<br/>RuleBuilder (assemble without text)"]
        Registry["Registry<br/>PredicateRegistry(Builder)"]
        Evaluation["Evaluation<br/>Evaluator, CompiledRule, EvaluationOptions"]
        Diagnostics["Diagnostics<br/>Diagnostic, DiagnosticSeverity"]
        Json["Json<br/>JSON tree parser/printer + schema"]
        Printing["Printing<br/>CanonicalPrinter, MermaidTreePrinter"]
        Diffing["Diffing<br/>RuleDiff, RuleDiffPrinter"]
        Logging["Logging<br/>structured log events"]
        Metrics["Metrics<br/>TruthWeaverMetrics (Meter)"]
        DI["DependencyInjection<br/>AddTruthWeaver extension"]

        Parsing --> Ast
        Building --> Ast
        Json --> Ast
        Ast --> Compilation
        Compilation --> Analysis
        Compilation --> Registry
        Analysis --> Diagnostics
        Compilation --> Diagnostics
        Ast --> Evaluation
        Ast --> Printing
        Ast --> Diffing
    end

    subgraph YamlPkg["TruthWeaver.Yaml"]
        Yaml["YAML tree parser/printer"]
    end

    subgraph Extras["Optional add-ons"]
        Predicates["TruthWeaver.Predicates<br/>ready-made IPredicate implementations"]
        Testing["TruthWeaver.Testing<br/>Decision assertions, fake predicates"]
    end

    Core --> Kernel
    YamlPkg --> Core
    Predicates --> Kernel
    Testing --> Kernel
```

Starting points for common tasks:

| Task | Start here |
| --- | --- |
| Implement a new predicate | [`IPredicate<TContext>`](src/TruthWeaver.Abstractions/IPredicate.cs), or a factory in [`TruthWeaver.Predicates`](src/TruthWeaver.Predicates) if it's a generic string/collection/regex check |
| Register predicates and compile a rule | [`PredicateRegistryBuilder`](src/TruthWeaver/Registry/PredicateRegistryBuilder.cs), [`RuleCompiler`](src/TruthWeaver/Compilation/RuleCompiler.cs) |
| Understand DSL parsing | [`Lexer`](src/TruthWeaver/Parsing/Lexer.cs) → [`DslParser`](src/TruthWeaver/Parsing/DslParser.cs) |
| Understand the compiled tree shape | [`Expression`](src/TruthWeaver/Ast/Expression.cs) |
| Understand constant/contradiction detection | [`BddManager`](src/TruthWeaver/Analysis/BddManager.cs), [`Analyzer`](src/TruthWeaver/Analysis/Analyzer.cs) |
| Understand evaluation and short-circuiting | [`Evaluator`](src/TruthWeaver/Evaluation/Evaluator.cs), [`CompiledRule`](src/TruthWeaver/Evaluation/CompiledRule.cs) |
| Assemble a rule without hand-writing text | [`RuleBuilder`](src/TruthWeaver/Building/RuleBuilder.cs) |
| Print or diagram a compiled rule | [`CanonicalPrinter`](src/TruthWeaver/Printing/CanonicalPrinter.cs), [`MermaidTreePrinter`](src/TruthWeaver/Printing/MermaidTreePrinter.cs) |
| Diff two compiled rules | [`RuleDiff`](src/TruthWeaver/Diffing/RuleDiff.cs) |
| Wire into a DI container | [`TruthWeaverServiceCollectionExtensions`](src/TruthWeaver/DependencyInjection/TruthWeaverServiceCollectionExtensions.cs) |
| Compile JSON or YAML instead of the DSL | [`JsonTreeParser`](src/TruthWeaver/Json/JsonTreeParser.cs), [`YamlTreeParser`](src/TruthWeaver.Yaml/YamlTreeParser.cs) |
| Write a unit test against a `Decision` | [`DecisionAssertions`](src/TruthWeaver.Testing/DecisionAssertions.cs), [`FakePredicates`](src/TruthWeaver.Testing/FakePredicates.cs) |

Beyond `src`, the rest of the repository:

- [`tests/TruthWeaver.Tests`](tests/TruthWeaver.Tests) — unit tests for all
  five packages, one file per behavior area (parsing, compilation,
  evaluation, memoization, YAML/JSON round-tripping, diffing, and so on).
- [`benchmarks/TruthWeaver.Benchmarks`](benchmarks/TruthWeaver.Benchmarks) —
  a BenchmarkDotNet suite measuring compile-time and evaluation-time cost
  (dev-only; see [Benchmarks](#benchmarks)).
- [`docs/adr/`](docs/adr/) — the architecture decision records behind every
  major design choice.
- [`CONTEXT.md`](CONTEXT.md) — the domain vocabulary and conceptual model,
  kept in sync with the code.

## Packages

| Package | Depends on | Ships |
| --- | --- | --- |
| `TruthWeaver.Abstractions` | *(nothing third-party)* | `IPredicate<TContext>`, `PredicateSchema`, `PredicateArguments`, `TruthValue`, `Decision`, `Fault` — everything a predicate-implementing service needs. |
| `TruthWeaver` | `Abstractions`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions` | The DSL parser, `RuleCompiler<TContext>`, `CompiledRule<TContext>`, the BDD-based analyzer, the evaluator, `System.Text.Json` tree support, printing/diffing, and DI registration extensions. |
| `TruthWeaver.Yaml` | `TruthWeaver`, YamlDotNet | YAML tree support (`CompileYaml`/`PrintYaml`), isolated so a consumer with no interest in YAML never pulls in YamlDotNet. |
| `TruthWeaver.Predicates` | `TruthWeaver.Abstractions` | Ready-made generic `IPredicate<TContext>` factories — string comparison, null/empty, set equality, regex matching, and externally-resolved-value predicates for a safe-to-share resolving client — for a consumer that wants common checks without writing a class, and without acquiring the parser, compiler, or analyzer. |
| `TruthWeaver.Testing` | `TruthWeaver.Abstractions` | Fluent `Decision` assertions and fake/scripted predicate factories for tests, without a hand-written `IPredicate<TContext>` per test. |

```mermaid
flowchart LR
    subgraph Abstractions["TruthWeaver.Abstractions<br/>(zero third-party dependencies)"]
        IPredicate["IPredicate&lt;TContext&gt;"]
        Schema["PredicateSchema / PredicateArguments"]
        Truth["TruthValue / Decision / Fault"]
    end

    subgraph Core["TruthWeaver"]
        Parser["DSL parser"]
        Compiler["RuleCompiler&lt;TContext&gt;"]
        Analyzer["Analyzer (BDD)"]
        Evaluator["Evaluator"]
        Json["System.Text.Json tree support"]
        DI["DI registration extensions"]
    end

    subgraph YamlPkg["TruthWeaver.Yaml"]
        Yaml["YAML tree support"]
    end

    subgraph PredicatesPkg["TruthWeaver.Predicates"]
        ReadyMade["Ready-made predicate factories"]
    end

    subgraph TestingPkg["TruthWeaver.Testing"]
        Assertions["Decision assertions + fake predicates"]
    end

    Core --> Abstractions
    YamlPkg --> Core
    PredicatesPkg --> Abstractions
    TestingPkg --> Abstractions

    App["Predicate-implementing service"] -.->|"references only"| Abstractions
    Host["Rule-authoring / evaluation host"] -->|"references"| Core
    Host -.->|"optional"| YamlPkg
    Host -.->|"optional"| PredicatesPkg
    Host -.->|"optional, test projects only"| TestingPkg
```

A service that only *implements* domain predicates references
`Abstractions` alone — no parser, no BDD analyzer, no YAML library. See
[ADR-0004](docs/adr/0004-package-boundaries-and-extensibility.md).

## Features

- **Kleene three-valued logic.** Every operator follows the three-valued
  truth tables in [ADR-0001](docs/adr/0001-kleene-failure-model.md) (full
  tables: [Appendix](#appendix-truth-tables)) — a predicate fault becomes
  `Unknown`, never a thrown exception or a silently coerced `false`. Entry
  point: [`Evaluator`](src/TruthWeaver/Evaluation/Evaluator.cs).
- **Per-evaluation memoization.** A term referenced from multiple branches
  of the same rule is invoked at most once per evaluation, keyed by
  structural term identity (see [CONTEXT.md#term-identity](CONTEXT.md#term-identity)).
  Entry point: [`Evaluator`](src/TruthWeaver/Evaluation/Evaluator.cs).
- **A Strong K3 BDD analyzer**, not brute-force truth tables, flags
  sub-expressions that are `True` (or `False`) for every `{True, False, Unknown}`
  assignment of their terms (e.g.
  `hasTopping(topping: "greenOlives") AND FALSE`) as compile diagnostics. It does
  not flag `A AND NOT A` or `A OR NOT A`: both are `Unknown` when `A` is.
  Entry point: [`Analyzer`](src/TruthWeaver/Analysis/Analyzer.cs) and
  [`BddManager`](src/TruthWeaver/Analysis/BddManager.cs).
- **Resource limits and `CompilationMode.Lenient`.** `CompilerOptions`
  bounds tree depth, node count, and the analyzer's term cap so an
  admin-authored rule can't hang a request thread; `Lenient` mode compiles
  an unregistered predicate to a permanent `Unknown` term instead of an
  error. Entry point: [`CompilerOptions`](src/TruthWeaver/Compilation/CompilerOptions.cs).
- **`EvaluationOptions`**: an opt-in `FaultBudget` for fail-fast behavior
  during a known outage, an `Exhaustive` mode that runs every reachable term
  without changing the result, and an overall evaluation timeout linked into
  the caller's `CancellationToken`. Entry point:
  [`EvaluationOptions`](src/TruthWeaver/Evaluation/EvaluationOptions.cs).
- **Scoped DI resolution.** Class-based predicates resolve fresh from the
  `IServiceProvider` supplied to each evaluation call, so a predicate with a
  scoped dependency works correctly even though a `CompiledRule<TContext>`
  is long-lived and shared. Entry point:
  [`TruthWeaverServiceCollectionExtensions`](src/TruthWeaver/DependencyInjection/TruthWeaverServiceCollectionExtensions.cs).
- **Structured logging and metrics.** Faults, compile diagnostics, and
  rule-swap notifications log as structured events through `ILogger<T>`; a
  `"TruthWeaver"` `Meter` exposes counters for evaluations, faults, and
  compile diagnostics, observable through OpenTelemetry's `AddMeter` with no
  new dependency. Entry points: [`src/TruthWeaver/Logging`](src/TruthWeaver/Logging)
  and [`TruthWeaverMetrics`](src/TruthWeaver/Metrics/TruthWeaverMetrics.cs).
- **Structural rule diffing.** `RuleDiff.Compare` compares two compiled
  rules and reports which operator, term, or constant nodes were added,
  removed, or changed, each located by operand-index path and paired with a
  human-readable description — useful for "what did this edit actually
  change" tooling. Entry point:
  [`RuleDiff`](src/TruthWeaver/Diffing/RuleDiff.cs).
- **Diagram rendering.** A compiled rule renders as a Mermaid flowchart or
  an indented plain-text tree, optionally colored by one evaluation's
  result and short-circuit path — see
  [Rendering a rule as a diagram](#rendering-a-rule-as-a-diagram). Entry
  points: [`MermaidTreePrinter`](src/TruthWeaver/Printing/MermaidTreePrinter.cs),
  [`PlainTextTreePrinter`](src/TruthWeaver/Printing/PlainTextTreePrinter.cs).
- **Ready-made predicates.** `TruthWeaver.Predicates` ships generic
  string-comparison, null/empty, set-equality, and regex-matching predicate
  factories so common checks don't need a hand-written class. Entry point:
  [`src/TruthWeaver.Predicates`](src/TruthWeaver.Predicates).
- **Test support.** `TruthWeaver.Testing` ships fluent `Decision`
  assertions and fake/scripted predicate factories (fixed answer, simulated
  fault, sequenced answers, `Unknown` answered directly) for testing without a hand-written
  `IPredicate<TContext>` per test. Entry point:
  [`src/TruthWeaver.Testing`](src/TruthWeaver.Testing).

## Operators

### Symbol notation

Every existing operator can also be written with a symbol. Symbols compile to
exactly the same tree as the named operator, so notation is a style choice and
never a semantic one; the canonical printer (and persisted DSL text) always
prints the named form.

| Named | Symbols |
| --- | --- |
| `AND` | `&&`, `∧` |
| `OR` | `\|\|`, `∨` |
| `NOT` | `!`, `¬` |
| `XOR` | `⊕` |
| `IMPLIES` | `→` |
| `NAND` | `↑` |
| `NOR` | `↓` |
| `EQUIVALENT` | `↔` (words `IFF` and the legacy `XNOR` are accepted too) |

Symbols and words mix freely (`a && b OR c`) and follow the same precedence
and no-mixing rules as the named operators. A lone `&` or `|` is a syntax error.

### Order of operations

Precedence governs *parsing* the DSL only — the canonical printer always
disambiguates explicitly (see [below](#choosing-a-rule-format)), so a
persisted or printed rule never depends on a reader holding this table in
their head.

1. **Parentheses** (`(...)`) — always evaluated first, exactly as written.
2. **`NOT`** — binds tightest of the operators; right-associative (`NOT NOT
   a` is valid, if odd).
3. **`AND`** — binds tighter than `OR`.
4. **`OR`** — binds loosest of the infix operators.

`lovesPineapple AND NOT isBanned OR isVip` therefore parses as
`(lovesPineapple AND (NOT isBanned)) OR isVip`.

Every infix operator other than `NOT`/`AND`/`OR` (today `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND` and `NOR`)
is **not** part of this precedence chain: mixing one with `AND`/`OR`, or with
a *different* infix operator, at the same syntactic level without explicit
parentheses is a **compile error** (`AmbiguousOperatorMixing`) rather than
resolved by an implicit precedence guess. The diagnostic points at the
offending operator (or at the bare infix expression sitting next to
`AND`/`OR`) and tells you to add parentheses — see
[ADR-0005](docs/adr/0005-strong-k3-language-surface.md) decision 8 (which
extends [ADR-0003](docs/adr/0003-rule-syntax-and-serialization.md)'s rule) for
why. Function-call-style operators (`NXOR(...)`, `ExactlyOne(...)` and the threshold
family) are self-delimiting — their parentheses are part of the call syntax,
not grouping, so they never participate in precedence at all.

### Binary vs. unary operators

| Arity | Operators | Notes |
| --- | --- | --- |
| **Unary** | `NOT` | Takes exactly one operand. |
| **Binary only** | `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR` | Always exactly two operands — a compile error otherwise (`XorArityViolation`). `XOR` with three or more operands is an error whose message points at `NXOR` (n-ary parity) and `ExactlyOne` (see [ADR-0005](docs/adr/0005-strong-k3-language-surface.md) decision 7); a chain such as `a IMPLIES b IMPLIES c` or `a NAND b NAND c` is rejected too — parenthesize it. |
| **N-ary (≥ 2)** | `AND`, `OR`, `NXOR`, `ExactlyOne`, `AtLeast`, `AtMost`, `GreaterThan`, `LessThan`, `Exactly` | Take two or more operands. `AND`/`OR` are commonly thought of as "binary" from C-family languages, but this engine treats them as flat n-ary chains (`AND(a, b, c)`, not `AND(AND(a, b), c)`). |
| **0-ary** | `True`, `False`, `Unknown` | Constants, not operators over operands. Written in any letter case; printed upper camel. |

### All operators

| Operator | Arity | Description |
| --- | --- | --- |
| `AND` | n-ary | True iff every operand is true. Short-circuits at the first `False`. |
| `OR` | n-ary | True iff at least one operand is true. Short-circuits at the first `True`. |
| `NOT` | unary | Logical negation. `Unknown` stays `Unknown`. |
| `XOR(a, b)` | binary | True iff exactly one of the two operands is true. `Unknown` if either operand is `Unknown`. |
| `EQUIVALENT(a, b)` / `a ↔ b` | binary | Logical biconditional — true iff both operands agree (both true or both false). The negation of `XOR`; `Unknown` if either operand is `Unknown`. `IFF` and the legacy `XNOR` are accepted on input and compile to the same node; the canonical printer writes `EQUIVALENT`. |
| `IMPLIES(a, b)` / `a → b` | binary | Strong Kleene material implication, `NOT a OR b`. `True` when `a` is `False` or `b` is `True`; `False` only for `True → False`; otherwise `Unknown`. |
| `NAND(a, b)` / `a ↑ b` | binary | Negated conjunction, `NOT (a AND b)`. `False` only when both operands are `True`; `True` if either is `False`; otherwise `Unknown`. Both operands are always evaluated. |
| `NOR(a, b)` / `a ↓ b` | binary | Negated disjunction, `NOT (a OR b)`. `True` only when both operands are `False`; `False` if either is `True`; otherwise `Unknown`. Both operands are always evaluated. |
| `NXOR(a, b, ...)` | n-ary | Parity: `True` iff an odd number of operands are `True`, `False` iff an even number are, and `Unknown` whenever any operand is `Unknown`. At two operands it equals `XOR`; from three operands it differs from `ExactlyOne` (`NXOR(a, b, c)` is `True` when all three are `True`). A function call, so it has no precedence and needs no parentheses next to other operators. |
| `ExactlyOne(...)` | n-ary | True iff exactly one operand is true — the unambiguous name for what `XOR` only means at exactly two operands. |
| `AtLeast(k, ...)` | n-ary | True iff at least `k` operands are true. |
| `AtMost(k, ...)` | n-ary | True iff at most `k` operands are true. |
| `GreaterThan(k, ...)` | n-ary | True iff more than `k` operands are true. |
| `LessThan(k, ...)` | n-ary | True iff fewer than `k` operands are true. |
| `Exactly(k, ...)` | n-ary | True iff exactly `k` operands are true. |
| `True` / `False` / `Unknown` | constant | Fixed K3 truth value (any letter case; the canonical printer writes `True`, `False`, `Unknown`). `Unknown` models an indeterminate constant, e.g. when stubbing out incomplete logic. In JSON a constant is `{"const": true}` or, for `Unknown`, `{"const": "unknown"}`; in YAML `const: unknown`. Operator names are case-insensitive in every format. |

Every operator above follows the three-valued Kleene truth tables in
[ADR-0001](docs/adr/0001-kleene-failure-model.md) — see the
[truth table appendix](#appendix-truth-tables) for the full tables.

## Choosing a rule format

DSL, JSON, and YAML compile to the exact same tree through the exact same
Parse → Validate → Analyze → Build pipeline (see
[Compilation pipeline](#compilation-pipeline) below) — none of them is more
"real" than another at evaluation time. What differs is who's meant to
write and read each one:

| | DSL string | JSON tree | YAML tree |
| --- | --- | --- | --- |
| **Canonical / persisted form?** | Yes — this is what a `CompiledRule<TContext>` prints back to. | No — an interchange format. | No — an interchange format. |
| **Best for** | A human author or reviewer typing/reading a rule directly (a database column, a code review, a log line). | A UI rule builder generating or consuming a tree without writing a parser. | The same as JSON, when the host's tooling already prefers YAML (config files, GitOps). |
| **Package** | `TruthWeaver` | `TruthWeaver` | `TruthWeaver.Yaml` |
| **Compile with** | `compiler.Compile(text)` | `compiler.CompileJson(json)` | `compiler.CompileYaml(yaml)` |
| **Print with** | `rule.CanonicalText` | `rule.PrintJson()` | `rule.PrintYaml()` |
| **Round-trips losslessly?** | Yes, by definition. | Yes — `parse(print(x))` is structurally equal to `x` (ticket 07). | Yes — same guarantee (ticket 08). |
| **Nesting for `AND`/`OR`/`XOR`/`EQUIVALENT`/`IMPLIES`/`NAND`/`NOR`** | Infix with precedence (see [Operators](#operators)); `XOR`/`EQUIVALENT`/`IMPLIES`/`NAND`/`NOR` mixed with `AND`/`OR`, or with each other, needs explicit parens. | Explicit `{"op": "...", "operands": [...]}` nodes — no precedence to get wrong. | Same explicit `op`/`operands` shape as JSON. |
| **Comments** | No | No (JSON has none) | Yes (`#`) — a practical reason to prefer YAML for hand-maintained rule files. |

See [ADR-0003](docs/adr/0003-rule-syntax-and-serialization.md) for the full
grammar and tree schema. There's also a fourth way to produce a rule without
writing text in any of these formats by hand — see
[Building rules programmatically](#building-rules-programmatically) below.

A fifth format, of sorts: `CanonicalText` isn't merely "minimal" — it
parenthesizes an operand whenever it's a different operator than the one
it's nested under (e.g. `a AND b OR c` prints as `(a AND b) OR c`), even
where precedence alone already makes the parse unambiguous. The goal is a
rule that reads clearly at a glance without the reader reconstructing
precedence mentally, not merely one that reparses correctly.

## Predicate types

Every predicate is one of four registration shapes, and they mix freely
within one `PredicateRegistryBuilder<TContext>.Build()`. Two independent
axes: **how many rule-authored arguments** it takes (zero, one, or several —
"n"), and **where its implementation comes from** (a stateless lambda, or a
class resolved from DI).

| Shape | Arguments | Implementation | When to use |
| --- | --- | --- | --- |
| Lambda, 0 args | none | stateless delegate | A simple stateless check with no rule-authored parameter. |
| Lambda, 1 arg | one | stateless delegate with a 1-argument schema | The common case — a stateless check parameterized by the rule text, e.g. `hasTopping(topping: "greenOlives")`. |
| Class-based (DI), 0 args | none | `IPredicate<TContext>` | Needs a scoped/injected dependency but no rule-authored parameter. |
| Class-based (DI), n args | several | `IPredicate<TContext>` with a multi-argument schema | Needs both rule-authored parameters *and* one or more injected dependencies. |

Before writing one by hand, check whether
[`TruthWeaver.Predicates`](src/TruthWeaver.Predicates) already has it —
`StringPredicates`, `CollectionPredicates`, and `RegexPredicates` cover
string comparison, null/empty checks, set equality, and regex matching as
generic factories parameterized by a value selector, and
`ResolvedValuePredicates` covers the externally-resolved-value pattern
(below) for a safe-to-share resolving client. Every method on
`StringPredicates` except one is ordinal-only and fixed-behavior by
design — a case-insensitive variant is a separate predicate
(`EqualsIgnoreCase`), never a rule-text flag on `Equals`. The exception,
`StringPredicates.EqualsConfigurable`, deliberately inverts that: it's one
predicate whose `ignoreCase`/`culture`/`trim` arguments are set per rule
(case-insensitive and `InvariantCulture` by default), for the case where a
rule author genuinely needs that flexibility rather than a fixed-behavior
predicate per name.

### 0 arguments, stateless lambda

```csharp
.Add(
    PredicateSchema.NoArguments("isBanned", "Is Banned", "Is the current customer's account banned?"),
    (customer, args, ct) => ValueTask.FromResult(customer.IsBanned ? TruthValue.True : TruthValue.False))
```

### 1 argument, stateless lambda

See [Example 3](#3-named-arguments)'s `hasTopping(topping: "greenOlives")` —
a single named `string` argument, no injected dependency.

### 0 arguments, class-based (DI)

See [Example 1](#1-a-single-predicate)'s `LovesPineapple` — a class
implementing `IPredicate<TContext>`, resolved fresh from `IServiceProvider`
on every evaluation (the right shape whenever a scoped dependency, e.g. a
`DbContext`, is involved, even with no rule-authored parameter).

### n arguments, class-based, multiple injected dependencies

The shape that combines everything: two rule-authored arguments *and* two
constructor-injected dependencies, resolved from DI per evaluation:

```csharp
public sealed class HasEarnedEnoughLoyaltyStamps : IPredicate<PizzaOrder>
{
    private readonly ILoyaltyStampStore stamps;
    private readonly TimeProvider clock;

    public HasEarnedEnoughLoyaltyStamps(ILoyaltyStampStore stamps, TimeProvider clock)
    {
        this.stamps = stamps;
        this.clock = clock;
    }

    public static PredicateSchema Schema =>
        new(
            "hasEarnedEnoughLoyaltyStamps",
            "Has Earned Enough Loyalty Stamps",
            "Has the order's customer earned at least the given number of loyalty stamps within the given time window?",
            [
                new PredicateArgumentSchema("minCount", "The minimum number of loyalty stamps required.", LiteralKind.Int64),
                new PredicateArgumentSchema("withinDays", "The lookback window, in days.", LiteralKind.Int64),
            ]);

    public async ValueTask<TruthValue> EvaluateAsync(PizzaOrder order, PredicateArguments args, CancellationToken ct)
    {
        long minCount = args.GetInt64("minCount");
        long withinDays = args.GetInt64("withinDays");
        DateTimeOffset cutoff = this.clock.GetUtcNow().AddDays(-withinDays);

        long count = await this.stamps.CountStampsSinceAsync(order.Id, cutoff, ct);
        return count >= minCount ? TruthValue.True : TruthValue.False;
    }
}
```

Used in a rule as `hasEarnedEnoughLoyaltyStamps(minCount: 5, withinDays: 30)`.
`ILoyaltyStampStore` might be scoped (an `IDbContextFactory`-backed store)
and `TimeProvider` is typically a singleton — both resolve correctly on
every evaluation because the predicate itself is resolved fresh from
`IServiceProvider`, not constructed once at registration.

Wiring it up: **`AddTruthWeaver` registers the registry and compiler,
not the predicate types themselves** — a class-based predicate (and its own
dependencies) must be registered in the host's container separately, same
as any other DI service:

```csharp
services.AddScoped<ILoyaltyStampStore, LoyaltyStampStore>();
services.AddSingleton(TimeProvider.System);
services.AddScoped<HasEarnedEnoughLoyaltyStamps>();  // the predicate type itself
services.AddScoped<LovesPineapple>();

services.AddTruthWeaver<PizzaOrder>(builder => builder
    .Add<LovesPineapple>()
    .Add<HasEarnedEnoughLoyaltyStamps>());
```

Both lambda and class-based predicates register against the same
`PredicateRegistryBuilder<TContext>.Add(...)` overloads — the difference is
purely dependency lifetime and how many rule-authored arguments the schema
declares, never a difference in rule text or how the compiler validates a
term. See
[ADR-0002](docs/adr/0002-evaluation-semantics.md#predicate-registration-and-dependency-lifetimes).

### n arguments, class-based, externally-resolved value

`HasEarnedEnoughLoyaltyStamps` above injects a dependency to read a value it
already knows how to interpret (`minCount`, `withinDays` are values, used
directly). A related but distinct shape: a rule-text literal argument and/or
a `TContext`-supplied value is a **key to be resolved** — not a value
already ready to use — and a constructor-injected service performs that
live resolution before the predicate can answer anything. There is no
single canonical shape here; it covers three distinct cases, none more
central than the others:

1. **Single-value, no comparison target.** The literal key resolves
   directly to the answer — there's no "other side" to compare
   against, and `TContext` may not be read at all. A feature-flag check is
   the classic instance:

   ```csharp
   public sealed class IsPromoActive(IPromoService promos) : IPredicate<object?>
   {
       public static PredicateSchema Schema =>
           new(
               "isPromoActive",
               "Is Promo Active",
               "Is the given promo code currently active, resolved live from the promotions service?",
               [new PredicateArgumentSchema("promoCode", "The promo code to look up.", LiteralKind.String)]);

       public async ValueTask<TruthValue> EvaluateAsync(object? context, PredicateArguments args, CancellationToken ct) =>
           await promos.IsActiveAsync(args.GetString("promoCode"), ct) ? TruthValue.True : TruthValue.False;
   }
   ```

   Used in a rule as `isPromoActive(promoCode: "SUMMER-2026")`.

2. **Single-sided value check.** One side — the argument or a context
   value — is resolved live; the other side is a plain value already
   sitting on `TContext`, needing no resolution of its own. Note that
   `TContext` here has no user field at all — this pattern isn't about "the
   current user," it's about a key that needs a live lookup:

   ```csharp
   public sealed class IsWithinZoneLimit(IZoneLimitLookupService zoneLimits) : IPredicate<DeliveryRun>
   {
       public static PredicateSchema Schema =>
           new(
               "isWithinZoneLimit",
               "Is Within Zone Limit",
               "Is the delivery run's amount within the live order limit resolved for the given delivery zone code?",
               [new PredicateArgumentSchema("zoneCode", "The delivery zone code to look up a live limit for.", LiteralKind.String)]);

       public async ValueTask<TruthValue> EvaluateAsync(DeliveryRun run, PredicateArguments args, CancellationToken ct)
       {
           string zoneCode = args.GetString("zoneCode");
           decimal limit = await zoneLimits.ResolveLimitAsync(zoneCode, ct);
           return run.Amount <= limit ? TruthValue.True : TruthValue.False;
       }
   }
   ```

   Used in a rule as `isWithinZoneLimit(zoneCode: "Z-100")`. Only
   `zoneCode` is resolved; `run.Amount` is read straight off
   `TContext`, no lookup needed.

3. **Two-sided comparison.** Both a `TContext`-supplied anchor and the
   rule-text argument are independently resolved through the injected
   service, and the two *resolved* results are compared — the original
   motivating case (a relationship check), but only one instance of this
   family, not the pattern itself:

   ```csharp
   public sealed class IsAssignedToCandidateDriver(IDriverLookupService drivers) : IPredicate<PizzaOrder>
   {
       public static PredicateSchema Schema =>
           new(
               "isAssignedToCandidateDriver",
               "Is Assigned To Candidate Driver",
               "Does the order's actual assigned driver, resolved live, match the given candidate?",
               [new PredicateArgumentSchema("candidateDriverId", "The candidate driver to validate.", LiteralKind.Guid)]);

       public async ValueTask<TruthValue> EvaluateAsync(PizzaOrder order, PredicateArguments args, CancellationToken ct)
       {
           Guid candidateDriverId = args.GetGuid("candidateDriverId");
           // candidateDriverId here belongs to "Mister Moneybags," our top delivery driver.
           Guid actualDriverId = await drivers.ResolveDriverIdAsync(order.Id, ct);
           return actualDriverId == candidateDriverId ? TruthValue.True : TruthValue.False;
       }
   }
   ```

   Used in a rule as
   `isAssignedToCandidateDriver(candidateDriverId: "3fa85f64-5717-4562-b3fc-2c963f66afa6")`.
   `order` (the context) is an order id, not "the current user" — it
   needs its own resolution just as much as the argument does. Neither side
   of a two-sided comparison is privileged as "the identity one."

A few things stay true across all three shapes:

- The rule-text argument is a **key**, not necessarily an identity — a
  `String` cost-center code is exactly as valid a key as a `Guid`. Whatever
  it resolves to plays no role in the DSL, JSON, or YAML surface; it exists
  only inside `EvaluateAsync`.
- `TContext` participation is optional. Shape 1 above never reads it at
  all; shapes 2 and 3 read it, but nothing about this pattern requires that.
- Term identity ([CONTEXT.md#term-identity](CONTEXT.md#term-identity)) is
  unaffected: the literal argument is still compared as an ordinary literal
  for memoization purposes. What it resolves to on any given evaluation
  never enters term identity. The predicate-author contract
  ([CONTEXT.md#the-predicate-author-contract](CONTEXT.md#the-predicate-author-contract))
  still applies — the same argument plus the same context within *one*
  evaluation must yield the same answer, so a resolution service that's
  internally consistent within a single evaluation (even if the underlying
  data could change between evaluations) is what the contract expects.
- Because the live call happens inside `EvaluateAsync`, a lookup failure
  (timeout, connection error) is absorbed the same way any other predicate
  fault is — as a `Fault` and `TruthValue.Unknown` (ADR-0001), never an
  unhandled exception. A predicate that merely cannot decide (for example
  the data is not available) returns `TruthValue.Unknown` directly, with no
  `Fault`. No special handling is needed in the predicate itself; see [`IPredicate<TContext>`](src/TruthWeaver.Abstractions/IPredicate.cs).

This is the documented alternative to the deferred
"[context-bound term arguments](.scratch/deferred-features/spec.md)" feature (a
path-expression mini-language like `IsManagerOf({{resource.ownerId}})`) —
every shape above is expressible today, with no engine changes, by letting
the predicate itself resolve whatever it needs.

**All three examples above are class-based**, which is the right choice
whenever the thing doing the resolving is a scoped dependency (a
`DbContext`, a per-request `HttpClient`) that must be re-resolved fresh on
every evaluation. When the resolving client is instead safe to capture once
— a long-lived, thread-safe instance such as a cached feature-flag reader or
an `HttpClient`-backed lookup wrapper already held by the host —
`ResolvedValuePredicates` in [`TruthWeaver.Predicates`](src/TruthWeaver.Predicates)
covers the same pattern as a lighter-weight lambda factory, with no one-off
class needed. The single-value convenience overload matches shape 1 above:

```csharp
(PredicateSchema schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
    ResolvedValuePredicates.Create<object?>(
        "isPromoActive",
        "Is Promo Active",
        "Is the given promo code currently active, resolved live from the promotions service?",
        async (_, args, ct) => await promos.IsActiveAsync(args.GetString("promoCode"), ct) ? TruthValue.True : TruthValue.False,
        new PredicateArgumentSchema("promoCode", "The promo code to look up.", LiteralKind.String));
```

A second overload takes a separate `test` delegate for shapes 2 and 3 above,
when it reads more clearly to keep "resolve" and "turn the resolved value
into an answer" apart. Both paths solve the same conceptual pattern; neither
replaces the other — reach for `ResolvedValuePredicates` when the resolving
client is safe to share, and a hand-written `IPredicate<TContext>` (as shown
above) when it isn't.

## Examples

Seven examples, each adding one more piece — a single predicate, combining
predicates, named arguments, `XOR`/`EQUIVALENT`/`ExactlyOne`/the threshold family,
the full worked example in all three formats, assembling that same rule with
`RuleBuilder` instead of writing text, and matching a value against one or
several constants — plus a bonus on turning a denial into a human-readable
sentence.

### 1. A single predicate

```text
lovesPineapple
```

```csharp
PredicateRegistry<Customer> registry = PredicateRegistry<Customer>.CreateBuilder().Add<LovesPineapple>().Build();
RuleCompiler<Customer> compiler = new(registry);
CompiledRule<Customer> rule = compiler.Compile("lovesPineapple").CompiledRule!;

Decision decision = await rule.EvaluateAsync(customer, serviceProvider, cancellationToken: ct);
```

### 2. Combining predicates: `AND` / `OR` / `NOT`

```text
lovesPineapple AND NOT isBanned
```

`NOT` binds tighter than `AND`, which binds tighter than `OR`, so this
parses as `lovesPineapple AND (NOT isBanned)` without needing parentheses.

### 3. Named arguments

```text
hasTopping(topping: "greenOlives")
```

```csharp
PredicateRegistry<Customer> registry = PredicateRegistry<Customer>.CreateBuilder()
    .Add(
        new PredicateSchema(
            "hasTopping",
            "Has Topping",
            "Does the order include the given topping?",
            [new PredicateArgumentSchema("topping", "The topping to check for.", LiteralKind.String)]),
        (customer, args, ct) =>
            ValueTask.FromResult(customer.Toppings.Contains(args.GetString("topping")) ? TruthValue.True : TruthValue.False))
    .Build();
```

Argument order in the source text never matters (`hasTopping(topping:
"greenOlives")` and a predicate with several arguments written in any order
compile to the same term identity); argument *values* are case-sensitive
(`"greenOlives"` and `"Greenolives"` are different terms — see
[CONTEXT.md#term-identity](CONTEXT.md#term-identity)). `Description` is
required on both `PredicateSchema` and `PredicateArgumentSchema`, and
`PredicateSchema` also requires a `Label` — a short display name distinct
from the machine-facing `Name` used in rule text (e.g. `Name: "hasTopping"`,
`Label: "Has Topping"`) — so a rule-authoring UI or generated documentation
always has something to show for every predicate and argument. See
[Describing a compiled rule](#describing-a-compiled-rule) below for how this
pairs with operators' own label/description.

A DSL string-literal argument supports four escape sequences: `\"` for a
literal quote, `\\` for a literal backslash, `\n` for a newline, and `\t` for
a tab. For example, `hasTopping(topping: "Chef's \"Special\"")` compiles to
a string argument whose value is `Chef's "Special"`, and printing that
compiled rule back to DSL text reproduces `hasTopping(topping: "Chef's
\"Special\"")` unchanged. Any other backslash sequence
(e.g. `\p`) is a compile-time `InvalidEscapeSequence` diagnostic, not a
silently-corrupted literal value — the compilation fails rather than
guessing what you meant. This escaping rule is specific to the DSL text
format: the JSON and YAML forms (see
[Converting between DSL, JSON, and YAML](#converting-between-dsl-json-and-yaml))
use their own format's native string escaping (`System.Text.Json` and
YamlDotNet respectively), not this rule.

A predicate can take more than one named argument — same registration shape,
just a longer `PredicateArgumentSchema` array and an `EvaluateAsync` that
reads more than one `Get*` call:

```text
hasToppingAmount(topping: "pepperoni", amount: "extra")
```

```csharp
PredicateRegistry<Customer> registry = PredicateRegistry<Customer>.CreateBuilder()
    .Add(
        new PredicateSchema(
            "hasToppingAmount",
            "Has Topping Amount",
            "Does the order include the given topping at the given amount?",
            [
                new PredicateArgumentSchema("topping", "The topping to check for.", LiteralKind.String),
                new PredicateArgumentSchema("amount", "The amount requested (e.g. \"regular\" or \"extra\").", LiteralKind.String),
            ]),
        (customer, args, ct) =>
            ValueTask.FromResult(
                customer.ToppingAmounts.TryGetValue(args.GetString("topping"), out string? amount)
                && amount == args.GetString("amount")
                    ? TruthValue.True
                    : TruthValue.False))
    .Build();
```

`hasToppingAmount(topping: "pepperoni", amount: "extra")` and
`hasToppingAmount(amount: "extra", topping: "pepperoni")` compile to the
exact same term identity — argument order in the source text still never
matters, however many arguments a predicate declares (see
[Term identity](CONTEXT.md#term-identity)).

### 4. `XOR`, `EQUIVALENT`, `ExactlyOne`, and the threshold family

```text
AtLeast(2, approvedByAlice, approvedByBob, approvedByCarol)
```

"At least two of these three approvals." Its siblings read the same way:
`AtMost(1, ...)`, `GreaterThan(1, ...)`, `LessThan(2, ...)`, and
`Exactly(2, ...)` all compile to one shared `ThresholdExpression` node,
differing only in which comparison against the true-operand count they
apply (see [Operators](#operators) for the full table).

`ExactlyOne(a, b, c)` is the n-ary "exactly one of these" operator; `XOR` is
binary-only — a third operand is a compile error that points at both
alternatives. Use `ExactlyOne` for "exactly one", or `NXOR(a, b, c)` for
n-ary *parity* (an odd number are true; `Unknown` if any operand is `Unknown`).
The two differ from three operands: with all of `a`, `b`, `c` true, `NXOR` is
`True` and `ExactlyOne` is `False`.

`EQUIVALENT` (`IFF`, `↔`) is `XOR`'s counterpart — "these two must agree":

```text
isPrimaryReviewer EQUIVALENT isBackupReviewer
```

reads as "exactly one of primary/backup reviewer status, or neither" — true
when both are reviewers or neither is, false when exactly one is.

`IMPLIES` (or `→`) is material implication — "if this holds, that must too":

```text
isContractor IMPLIES hasSignedNda
```

It is `NOT isContractor OR hasSignedNda`, so a non-contractor passes
regardless of the NDA, and when `isContractor` is `True` the result is just
`hasSignedNda`. Like `XOR`/`EQUIVALENT` it is binary and must be parenthesized
next to `AND`/`OR` or another infix operator
(`(isContractor IMPLIES hasSignedNda) AND isActive`); in JSON/YAML it is
`{"op": "implies", "operands": [antecedent, consequent]}`.

### 5. The full worked example, in all three formats

Rule text (the canonical, persisted form):

```text
hasTopping(topping: "greenOlives") AND (hasCrust(crust: "thin") OR hasCrust(crust: "stuffed", ignoreCase: false) OR (isDineIn XOR isTakeout))
```

The same rule as JSON:

```json
{
  "op": "and",
  "operands": [
    { "predicate": "hasTopping", "args": { "topping": "greenOlives" } },
    {
      "op": "or",
      "operands": [
        { "predicate": "hasCrust", "args": { "crust": "thin" } },
        { "predicate": "hasCrust", "args": { "crust": "stuffed", "ignoreCase": false } },
        {
          "op": "xor",
          "operands": [
            { "predicate": "isDineIn" },
            { "predicate": "isTakeout" }
          ]
        }
      ]
    }
  ]
}
```

...and in YAML (`TruthWeaver.Yaml`):

```yaml
op: and
operands:
  - predicate: hasTopping
    args:
      topping: "greenOlives"
  - op: or
    operands:
      - predicate: hasCrust
        args:
          crust: "thin"
      - predicate: hasCrust
        args:
          crust: "stuffed"
          ignoreCase: false
      - op: xor
        operands:
          - predicate: isDineIn
          - predicate: isTakeout
```

Registering predicates and evaluating:

```csharp
(PredicateSchema hasCrustSchema, var hasCrustEvaluate) =
    StringPredicates.EqualsConfigurable<PizzaOrder>("hasCrust", order => order.Crust, "Has Crust", argumentName: "crust");

PredicateRegistry<PizzaOrder> registry = PredicateRegistry<PizzaOrder>.CreateBuilder()
    .Add<IsDineIn>()
    .Add<IsTakeout>()
    .Add(
        new PredicateSchema(
            "hasTopping",
            "Has Topping",
            "Does the order include the given topping?",
            [new PredicateArgumentSchema("topping", "The topping to check for.", LiteralKind.String)]),
        (order, args, ct) =>
            ValueTask.FromResult(order.Toppings.Contains(args.GetString("topping")) ? TruthValue.True : TruthValue.False))
    .Add(hasCrustSchema, hasCrustEvaluate)
    .Build();

RuleCompiler<PizzaOrder> compiler = new(registry);
CompilationResult<PizzaOrder> result = compiler.Compile(ruleText);

if (!result.Succeeded)
{
    // Surface result.Diagnostics to whoever is authoring the rule.
    // The previously persisted rule (if any) stays active — see ADR-0002.
    return;
}

CompiledRule<PizzaOrder> rule = result.CompiledRule!;
Decision decision = await rule.EvaluateAsync(order, serviceProvider, cancellationToken: cancellationToken);

if (decision.IsSatisfied)
{
    // allowed
}
```

`IsDineIn`/`IsTakeout` are class-based predicates (`IPredicate<PizzaOrder>`),
resolved fresh from `serviceProvider` on every call — the right shape for a
predicate with a scoped dependency such as a `DbContext`. `hasTopping` is a
hand-written stateless lambda; `hasCrust` comes from the ready-made
`StringPredicates.EqualsConfigurable` factory instead (see
[Predicate types](#predicate-types)) — it takes `crust` as its rule-text
comparison target, plus `ignoreCase`/`culture`/`trim` arguments with sensible
defaults, so `hasCrust(crust: "thin")` alone already compiles. All three
forms register against the same `PredicateRegistryBuilder<TContext>`; see
[ADR-0002](docs/adr/0002-evaluation-semantics.md#predicate-registration-and-dependency-lifetimes).

Wiring into a host's DI container instead of constructing things by hand:

```csharp
services.AddTruthWeaver<PizzaOrder>(builder => builder
    .Add<IsDineIn>()
    .Add<IsTakeout>());
```

### 6. The same rule, assembled with `RuleBuilder` instead of text

Same tree as example 5's `hasTopping(topping: "greenOlives") AND
(hasCrust(...) OR hasCrust(...) OR (isDineIn XOR isTakeout))`, built without
writing DSL, JSON, or YAML text by hand — useful when a rule's shape comes
from application logic (e.g. a dynamically assembled list of conditions)
rather than an author typing it directly:

```csharp
using TruthWeaver.Building;

RuleBuilder rule = RuleBuilder.And(
    RuleBuilder.Predicate("hasTopping", ("topping", "greenOlives")),
    RuleBuilder.Or(
        RuleBuilder.Predicate("hasCrust", ("crust", "thin")),
        RuleBuilder.Predicate("hasCrust", ("crust", "stuffed"), ("ignoreCase", false)),
        RuleBuilder.Xor(RuleBuilder.Predicate("isDineIn"), RuleBuilder.Predicate("isTakeout"))));

CompilationResult<PizzaOrder> result = rule.Compile(compiler);
```

Rendering the same rule as a Mermaid diagram:

```csharp
string mermaid = result.CompiledRule!.PrintMermaid();
```

```mermaid
flowchart TD
    Start(["Start"]) --> n0
    n0["AND"]
    n1["Has Topping (topping: #quot;greenOlives#quot;)"]
    n0 --> n1
    n2["OR"]
    n3["Has Crust (crust: #quot;thin#quot;, culture: #quot;#quot;, ignoreCase: true, trim: false)"]
    n2 --> n3
    n4["Has Crust (crust: #quot;stuffed#quot;, culture: #quot;#quot;, ignoreCase: false, trim: false)"]
    n2 --> n4
    n5["XOR"]
    n6["Is Dine In"]
    n5 --> n6
    n7["Is Takeout"]
    n5 --> n7
    n2 --> n5
    n0 --> n2
```

Rendering the same rule as a text tree:

```csharp
string tree = result.CompiledRule!.PrintPlainText();
```

```text
AND
├─ Has Topping (topping: "greenOlives")
└─ OR
   ├─ Has Crust (crust: "thin", culture: "", ignoreCase: true, trim: false)
   ├─ Has Crust (crust: "stuffed", culture: "", ignoreCase: false, trim: false)
   └─ XOR
      ├─ Is Dine In
      └─ Is Takeout
```

The second `hasCrust` term deliberately sets `ignoreCase: false` in the rule
text itself, rather than leaving every optional argument at its default —
`StringPredicates.EqualsConfigurable` (see [Predicate types](#predicate-types))
declares four rule-text arguments (`crust`, `ignoreCase`, `culture`, `trim`),
and this shows a rule actually setting more than one of them, not just the
one required argument every other predicate in this example takes. Both
diagrams show every term's rule-text argument values by default — the first
`hasCrust` term's `ignoreCase`/`culture`/`trim` are filled in from their
schema defaults even though its rule text never mentions them (ADR-0003's
compiler behavior for optional arguments), which is also why the two
`hasCrust` terms are visually distinct here, unlike a predicate label alone.
Pass `showArgumentValues: false` to either `PrintMermaid`/`PrintPlainText`
overload to render structure-only labels instead (see
[Rendering a rule as a diagram](#rendering-a-rule-as-a-diagram)).

`RuleBuilder` is not a fourth parallel parser into the AST — every builder
method renders to the exact same flat JSON tree shape [ADR-0003](docs/adr/0003-rule-syntax-and-serialization.md)
defines, and `Compile` hands that JSON to the same `CompileJson` any other
JSON-producing tool would use. A builder-assembled rule therefore gets every
diagnostic a hand-written one would — an unknown predicate, a bad argument,
an out-of-range threshold, `XOR`/`EQUIVALENT`/`IMPLIES`/`NAND`/`NOR` arity, resource limits, structural
tautology/contradiction — nothing here bypasses the Validate/Analyze stages
of the [compilation pipeline](#compilation-pipeline). See
[Building rules programmatically](#building-rules-programmatically) below
for the full API.

### 7. Matching against a constant, or any of several constants

"Has at least one topping of either pepperoni, mushroom, or green olives" —
two ways to write this, depending on whether the set of alternatives already
has a predicate per value or not.

**If a single-value predicate already exists** (e.g. [Example 3](#3-named-arguments)'s
`hasTopping(topping: "greenOlives")`), just `OR` it together per
alternative — no new predicate needed:

```text
hasTopping(topping: "pepperoni") OR hasTopping(topping: "mushroom") OR hasTopping(topping: "greenOlives")
```

Each call is a distinct term (and a distinct memoization unit), so this
reads clearly for a handful of alternatives but gets verbose as the set
grows, and the set of alternatives is baked into the rule text rather than
passed as data.

**For an arbitrary-size set, write a predicate that takes an array
argument** and checks membership itself — one term, one predicate call,
and the alternatives are rule-authored data rather than repeated rule
structure:

```csharp
public sealed class HasAnyTopping : IPredicate<Customer>
{
    public static PredicateSchema Schema =>
        new(
            "hasAnyTopping",
            "Has Any Topping",
            "Does the order include at least one of the given toppings?",
            [
                new PredicateArgumentSchema(
                    "toppings",
                    "The toppings to check for (any match).",
                    LiteralKind.StringArray
                ),
            ]);

    public ValueTask<TruthValue> EvaluateAsync(Customer customer, PredicateArguments args, CancellationToken ct)
    {
        IReadOnlyList<string> toppings = args.GetStringArray("toppings");
        bool result = toppings.Any(topping => customer.Toppings.Any(t => string.Equals(t, topping, StringComparison.Ordinal)));
        return ValueTask.FromResult(result ? TruthValue.True : TruthValue.False);
    }
}
```

Used in a rule as:

```text
hasAnyTopping(toppings: ["pepperoni", "mushroom", "greenOlives"])
```

**Case sensitivity is the predicate's own decision, not the engine's.** Term
identity (which two term references count as "the same variable" for
memoization) is always exact/case-sensitive — `"pepperoni"` and
`"Pepperoni"` are different arguments, full stop (see
[CONTEXT.md#term-identity](CONTEXT.md#term-identity)). But *what the
predicate does* with the string it reads via `GetString`/`GetStringArray` is
ordinary C#: the example above uses `StringComparison.Ordinal`
(case-sensitive); switch that one argument to
`StringComparison.OrdinalIgnoreCase` and the same predicate becomes
case-insensitive, with no other change. If both variants are needed, they're
two distinct predicates (e.g. `hasAnyTopping` vs. `hasAnyToppingIgnoreCase`)
rather than a flag threaded through rule text, keeping each one's behavior
fixed and inspectable from its name alone.

The same shape works for "equals one specific constant" too — just compare
against a single value instead of checking array membership (e.g.
`args.GetGuid("id") == expectedId`, or the `hasTopping`/`hasFlavor`-style
single-argument predicates already shown). Whether the constant(s) come
from a `LiteralKind.String`, `Int64`, `Decimal`, `Boolean`, `DateTimeOffset`,
or `Guid` argument (scalar or array) is purely a schema choice — the
compiler validates and converts each one identically (see
[Guid literal tests](tests/TruthWeaver.Tests/GuidLiteralTests.cs)
for a worked `Guid` example).

**"Matches a pattern" instead of "matches a fixed set"** is the same idea
again, just with `Regex.IsMatch` instead of set membership — the pattern
itself is a rule-authored `string` argument, not a special literal kind:

```csharp
public sealed class HasToppingMatching : IPredicate<Customer>
{
    public static PredicateSchema Schema =>
        new(
            "hasToppingMatching",
            "Has Topping Matching",
            "Does the order include a topping whose code matches the given regular expression?",
            [new PredicateArgumentSchema("pattern", "The regular expression to match a topping code against.", LiteralKind.String)]);

    public ValueTask<TruthValue> EvaluateAsync(Customer customer, PredicateArguments args, CancellationToken ct)
    {
        Regex pattern = new(args.GetString("pattern"), RegexOptions.None, TimeSpan.FromMilliseconds(100));
        return ValueTask.FromResult(customer.Toppings.Any(pattern.IsMatch) ? TruthValue.True : TruthValue.False);
    }
}
```

Used in a rule as `hasToppingMatching(pattern: "^EXTRA-.+$")` — "any
topping code of the form `EXTRA-CHEESE`." Pass `RegexOptions.IgnoreCase`
instead of `RegexOptions.None` for a case-insensitive match, same as the
`StringComparison` choice above. The explicit timeout matters here more than
in the other examples: unlike a fixed-set comparison, a pattern is
rule-authored text that could — accidentally or not — be pathologically
slow to match (catastrophic backtracking), and a predicate is exactly where
that risk should be contained, rather than letting it stall evaluation for
every rule that reaches this term.

`RegexPredicates` in [`TruthWeaver.Predicates`](src/TruthWeaver.Predicates)
already wraps this pattern with the same timeout discipline, if a
hand-written predicate isn't needed.

### Bonus: explaining a denied decision

`Decision`/`Fault`/`Trace` give you the *structured* reason for a denial;
turning that into a sentence for an end user or a support ticket is the
host's job. [Humanizer](https://github.com/Humanizr/Humanizer) is a
convenient pairing — predicate names are already camelCase words, and fault
counts are already numbers:

```csharp
using Humanizer;

if (!decision.IsSatisfied && decision.Faults.Count > 0)
{
    string summary = decision.Faults.Count.ToQuantity("predicate");
    Console.WriteLine($"Couldn't reach a decision: {summary} failed to answer.");

    foreach (Fault fault in decision.Faults)
    {
        // "hasTopping" -> "has topping"
        Console.WriteLine($"  - {fault.Term.PredicateName.Humanize()}: {fault.Exception.Message}");
    }
}
```

## Building rules programmatically

### Converting between DSL, JSON, and YAML

Any compiled rule converts losslessly to any of the three surfaces by
printing from one and compiling from the other — nothing about the compiled
tree itself is format-specific:

```csharp
CompiledRule<PizzaOrder> rule = compiler.Compile(dslText).CompiledRule!;

string json = rule.PrintJson();                       // DSL -> JSON
string yaml = rule.PrintYaml();                        // DSL -> YAML (TruthWeaver.Yaml)

CompiledRule<PizzaOrder> fromJson = compiler.CompileJson(json).CompiledRule!;
string backToDsl = fromJson.CanonicalText;              // JSON -> DSL

// backToDsl == rule.CanonicalText always: parse(print(x)) is structurally
// equal to x in every direction (ADR-0003), so converting formats never
// silently changes a rule's meaning.
```

This is exactly how a rule-authoring UI would offer "export as JSON/YAML" or
"paste JSON, get back DSL to review" without needing its own parser for
anything but the format it's currently editing.

### `RuleBuilder` reference

[Example 6](#6-the-same-rule-assembled-with-rulebuilder-instead-of-text)
shows `RuleBuilder` end to end. Every operator has a matching static factory
on `TruthWeaver.Building.RuleBuilder`:

| Operator | Factory method |
| --- | --- |
| `True` / `False` / `Unknown` | `RuleBuilder.Constant(bool value)` / `RuleBuilder.Constant(TruthValue value)` |
| A term | `RuleBuilder.Predicate(string name)` / `RuleBuilder.Predicate(string name, params (string Name, object Value)[] arguments)` |
| `AND` | `RuleBuilder.And(params RuleBuilder[] operands)` |
| `OR` | `RuleBuilder.Or(params RuleBuilder[] operands)` |
| `NOT` | `RuleBuilder.Not(RuleBuilder operand)` |
| `XOR` | `RuleBuilder.Xor(RuleBuilder left, RuleBuilder right)` |
| `EQUIVALENT` | `RuleBuilder.Equivalent(RuleBuilder left, RuleBuilder right)` (`RuleBuilder.Xnor` is kept and forwards to it) |
| `IMPLIES` | `RuleBuilder.Implies(RuleBuilder antecedent, RuleBuilder consequent)` |
| `NAND` | `RuleBuilder.Nand(RuleBuilder left, RuleBuilder right)` |
| `NOR` | `RuleBuilder.Nor(RuleBuilder left, RuleBuilder right)` |
| `NXOR` | `RuleBuilder.Nxor(params RuleBuilder[] operands)` |
| `ExactlyOne` | `RuleBuilder.ExactlyOne(params RuleBuilder[] operands)` |
| `AtLeast(k)` / `AtMost(k)` / `GreaterThan(k)` / `LessThan(k)` / `Exactly(k)` | `RuleBuilder.AtLeast(int k, params RuleBuilder[] operands)` (and the four siblings, same shape) |

`RuleBuilder.Compile(compiler)` is a thin wrapper around
`compiler.CompileJson(builder.ToJson())` — nothing bypasses the
Validate/Analyze pipeline described in
[Compilation pipeline](#compilation-pipeline). `ToJson()` alone is useful
too, e.g. for logging or persisting the tree a builder assembled without
compiling it immediately.

### Describing a compiled rule

Every predicate carries a required `Label`/`Description` on its
`PredicateSchema` ([Predicate types](#predicate-types)); every operator has
the equivalent, exposed via `OperatorInfo.Describe` in
`TruthWeaver.Ast`. `CompiledRule<TContext>.Describe()` combines both
into one recursive, walkable description of an entire compiled rule —
useful for a rule-authoring UI or a generated "what does this rule mean"
report, without needing access to the closed-set AST types themselves:

```csharp
CompiledRule<Customer> rule = compiler.Compile("lovesPineapple AND hasTopping(topping: \"greenOlives\")").CompiledRule!;

RuleDescription description = rule.Describe();
// description.Label       == "AND"
// description.Description == "True iff every operand is true. Short-circuits at the first False."
// description.Operands[0].Label == "Loves Pineapple"   (from LovesPineapple's PredicateSchema.Label)
// description.Operands[1].Label == "Has Topping"       (from hasTopping's PredicateSchema.Label)
```

A simple recursive print, for the shape of a "what does this rule mean"
report:

```csharp
void Print(RuleDescription node, int depth = 0)
{
    Console.WriteLine($"{new string(' ', depth * 2)}{node.Label} — {node.Description}");
    foreach (RuleDescription operand in node.Operands)
    {
        Print(operand, depth + 1);
    }
}
```

### Rendering a rule as a diagram

`RuleDescription` also feeds
[`MermaidTreePrinter`](src/TruthWeaver/Printing/MermaidTreePrinter.cs) and
[`PlainTextTreePrinter`](src/TruthWeaver/Printing/PlainTextTreePrinter.cs),
which render it as a Mermaid `flowchart` or an indented ASCII tree
respectively — either structure only, or colored/annotated by one
evaluation's result and short-circuit path:

```csharp
CompiledRule<Customer> rule = compiler.Compile("lovesPineapple AND hasTopping(topping: \"greenOlives\")").CompiledRule!;
RuleDescription description = rule.Describe();

// Structure only:
string mermaid = MermaidTreePrinter.Print(description);
string plainText = PlainTextTreePrinter.Print(description);

// Colored/annotated by one evaluation (Mermaid: green = contributed True, red = contributed False,
// gray = short-circuited; plain text: a "[true]"/"[false]"/"[skipped]" suffix per node):
Decision decision = await rule.EvaluateAsync(customer, serviceProvider, cancellationToken: ct);
string coloredMermaid = MermaidTreePrinter.Print(description, decision.EvaluatedTree);
string annotatedText = PlainTextTreePrinter.Print(description, decision.EvaluatedTree);
```

`MermaidTreePrinter`'s result is plain Mermaid text — paste it into any
Mermaid renderer, or hand it to a UI that already embeds one, to see the
rule's structure (and optionally, why one particular evaluation came out the
way it did) as a diagram instead of a nested expression. Its output always
includes a synthetic `Start` node pointing at the root, so the diagram shows
where evaluation begins without the reader having to infer it from "the node
with no incoming edge." `PlainTextTreePrinter`'s result needs no renderer at
all — the same information as an indented tree, suitable for a log line or a
terminal. Both printers include each term's rule-text argument values in its
label by default (e.g. `Has Crust (crust: "thin")`) — pass
`showArgumentValues: false` to either `Print` overload (or to
`CompiledRule<TContext>`'s `PrintMermaid`/`PrintPlainText` below) for
structure-only labels instead. `CompiledRule<TContext>` also exposes both
directly as `PrintMermaid()`/`PrintMermaid(decision)` and
`PrintPlainText()`/`PrintPlainText(decision)`, without a separate
`Describe()` call.

## Evaluation flow

```mermaid
flowchart TD
    Start(["Evaluate(context, ct)"]) --> Visit["Visit next operand<br/>(left to right)"]
    Visit --> IsTerm{"Term or operator?"}

    IsTerm -->|"Term"| Memo{"Already evaluated<br/>this term identity<br/>in this evaluation?"}
    Memo -->|"Yes"| Reuse["Reuse memoized TruthValue"]
    Memo -->|"No"| Invoke["Invoke predicate"]

    Invoke -->|"success"| Record["Memoize TruthValue"]
    Invoke -->|"throws"| Fault["Record Fault →<br/>treat as Unknown"]

    Reuse --> Combine
    Record --> Combine
    Fault --> Combine["Combine via operator's<br/>Kleene truth table"]

    IsTerm -->|"Operator"| Combine

    Combine --> ShortCircuit{"Result already<br/>determinate?<br/>(short-circuit)"}
    ShortCircuit -->|"Yes"| SkipRest["Mark remaining operands<br/>NotEvaluated in trace"]
    ShortCircuit -->|"No, more operands"| Visit

    SkipRest --> Done
    ShortCircuit -->|"No operands remain"| Done(["Decision<br/>(TruthValue + Faults + Trace)"])
```

Short-circuit is real (an `AND` stops at the first `False`, an `OR` stops at
the first `True`) but the trace still records what was skipped, rather than
omitting it — the point of a trace is to explain a decision, and a hole
where an unevaluated branch should be defeats that. Faults don't abort
evaluation; they become `Unknown` and are absorbed wherever the operator's
truth table allows. Full reasoning: [ADR-0001](docs/adr/0001-kleene-failure-model.md)
and [ADR-0002](docs/adr/0002-evaluation-semantics.md).

## Compilation pipeline

Rule text — DSL, JSON, or YAML — all funnel through the same
Parse → Validate → Analyze → Build pipeline, which is why
`parse(print(x))` round-trips structurally regardless of which surface a
rule came from:

```mermaid
flowchart TD
    Source["Rule text<br/>(DSL, JSON, or YAML)"] --> Parse[Parse]
    Parse -->|"syntax error"| Diag1[["Diagnostics<br/>(Error)"]]
    Parse -->|"raw tree"| Validate["Validate<br/>(known predicates, argument schema,<br/>depth/node limits, CompilerOptions)"]
    Validate -->|"validation error"| Diag2[["Diagnostics<br/>(Error / Warning / Info)"]]
    Validate -->|"valid tree"| Analyze["Analyze<br/>(dual-rail BDD Strong K3 constant/contradiction detection)"]
    Analyze --> Diag3[["Diagnostics<br/>(Warning / Info)"]]
    Analyze --> Build["Build immutable expression tree"]
    Build --> Result["CompilationResult&lt;TContext&gt;<br/>CompiledRule&lt;TContext&gt;? + Diagnostics"]

    Diag1 --> Result
    Diag2 --> Result
    Diag3 --> Result
```

`Compile` never throws for an authoring error — every problem, from a
syntax error to a Strong K3 tautology, becomes a `Diagnostic` (code,
severity, source span) in the returned `CompilationResult<TContext>`.
`CompiledRule<TContext>` is populated only when there are no `Error`-severity
diagnostics, which is what makes "a bad edit is rejected, the previously
persisted rule stays active" true by construction rather than by convention.
Full reasoning: [ADR-0003](docs/adr/0003-rule-syntax-and-serialization.md).

## Benchmarks

`benchmarks/TruthWeaver.Benchmarks` is a [BenchmarkDotNet](https://benchmarkdotnet.org/)
console project (dev-only — never packed, never referenced by `src/`) measuring:

- **Compile-time cost** (`CompileBenchmarks.Compile`) — `RuleCompiler.CompileJson`'s full
  Parse → Validate → Analyze → Build pipeline, including the BDD-based tautology/contradiction
  analyzer, across a small (10-term) and a large (200-term) representative rule.
- **Eval-time memoized term lookup** (`EvaluationBenchmarks.EvaluateAsync`) — `CompiledRule.EvaluateAsync`
  against a rule whose branches all share one term, at increasing branch fan-out, exercising the
  per-evaluation term memoization ADR-0002 describes.

A committed baseline (captured with `--job Short`) lives at
[`benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md`](benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md).

Run the full suite (this repo's `net11.0` preview target isn't yet recognized by BenchmarkDotNet's
default toolchain, so `--inProcess` is required — see the code comment on `CompileBenchmarks`/
`EvaluationBenchmarks`' host project for why):

```powershell
dotnet build benchmarks/TruthWeaver.Benchmarks -c Release
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*" --inProcess
```

Useful variations:

```powershell
# Discover benchmark names without running them
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --list flat

# Fast smoke test (one iteration per case, no meaningful measurement)
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*" --job Dry --inProcess

# Regenerate the committed baseline
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*" --job Short --inProcess --exporters github --artifacts ./benchmarks/TruthWeaver.Benchmarks/results
```

## Glossary

The short version lives in the [concept table](#what-it-is-and-isnt) above;
this is the full vocabulary, alphabetically. The authoritative version,
with the reasoning behind each term, is [CONTEXT.md](CONTEXT.md).

| Term | Meaning |
| --- | --- |
| `AtLeast(k, ...)` / `AtMost(k, ...)` / `GreaterThan(k, ...)` / `LessThan(k, ...)` / `Exactly(k, ...)` | The threshold operator family: n-ary comparisons against the true-operand count, all compiling to one shared `ThresholdExpression` node — see [Operators](#operators). |
| BDD analyzer | The compiler's constant/contradiction-detection pass, backed by a real binary decision diagram rather than brute-force truth tables. It reasons in Strong K3 with a dual-rail BDD ("definitely true" / "possibly true" per sub-expression, each term contributing an independent `True`/`False`/`Unknown` state), so its warnings (`BRE0012` tautology, `BRE0013` contradiction) mean the sub-expression is `True` (resp. `False`) for every `{True, False, Unknown}` assignment: `A AND NOT A` and `A OR NOT A` are not reported because they are `Unknown` when `A` is (ADR-0005 decision 17) — see [Compilation pipeline](#compilation-pipeline). |
| `CompilationMode` | `Strict` (default — an unregistered predicate is a compile error) or `Lenient` (an unregistered predicate compiles to a permanent `Unknown` term, for services sharing a rule store with different predicate sets). |
| `CompilationResult<TContext>` | What `Compile`/`CompileJson`/`CompileYaml` return: a nullable `CompiledRule<TContext>` plus every `Diagnostic` raised. |
| `CompiledRule<TContext>` | The immutable, thread-safe result of a successful compile. Safe to cache, share, and evaluate repeatedly; swapping the reference that holds it is how a host applies a rule edit at runtime. |
| `CompilerOptions` | Compile-time resource bounds — max tree depth, max node count, the BDD analyzer's term cap — plus `CompilationMode`. |
| `Decision` | The result of one evaluation: a `TruthValue`, the `Fault`s absorbed along the way, and optionally a `Trace`. `Decision.IsSatisfied` is true only when the result is `TruthValue.True`. |
| `Diagnostic` | One compile-time problem: a code, a `DiagnosticSeverity` (`Error`/`Warning`/`Info`), a message, and a source span. `Error` severity is what blocks `CompiledRule<TContext>` from being populated. |
| `EvaluationOptions` | Per-call evaluation knobs: `FaultBudget` (abort after N faults), `Mode` (`Default` or `Exhaustive`), and an overall timeout. |
| `NXOR(...)` | N-ary parity: true iff an odd number of operands are true; `Unknown` whenever any operand is `Unknown`. The unambiguous name for what `XOR` would mean past two operands. |
| `ExactlyOne(...)` | N-ary operator: true iff exactly one operand is true. The explicit name for "exactly one," so it's never confused with `XOR`'s binary-only meaning or `NXOR`'s parity. |
| Expression | The boolean tree itself — operators over terms and sub-expressions. What a `CompiledRule<TContext>` wraps. |
| `Fault` | A record of one predicate failing to produce an answer during one evaluation: the faulting term's identity plus the exception. Faults are absorbed as `Unknown`, never rethrown. |
| `IMPLIES(a, b)` / `→` | Strong Kleene material implication, `NOT a OR b`; a first-class binary node that prints as written (`(a IMPLIES b)`). Mixing it with `AND`/`OR` or another infix operator without parentheses is a compile error. See [Operators](#operators). |
| Kleene logic | Three-valued logic (`True`/`False`/`Unknown`) instead of two-valued boolean logic — the reason a predicate fault becomes `Unknown` rather than a thrown exception or a silently coerced `false`. See [ADR-0001](docs/adr/0001-kleene-failure-model.md). |
| Memoization | Within one evaluation, a given term identity is invoked at most once, however many places in the tree reference it. Never carries across separate `EvaluateAsync` calls. |
| Operator | `AND`, `OR`, `NOT`, `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`, `NXOR`, `ExactlyOne`, the threshold family, and the `True/`False`/`Unknown` constants — the closed set of ways to combine terms and sub-expressions. Every operator has a `Label`/`Description` via `OperatorInfo.Describe`. See [Operators](#operators). |
| `OperatorInfo` / `OperatorDescriptor` | `OperatorInfo.Describe(node)` (`TruthWeaver.Ast`) returns an operator node's `OperatorDescriptor` (`Label`, `Description`) — the operator-side counterpart to a predicate's `PredicateSchema.Label`/`Description`. See [Describing a compiled rule](#describing-a-compiled-rule). |
| Predicate | A registered, reusable implementation (e.g. `hasTopping`, `lovesPineapple`) — the *function*, not any one call to it. Implements `IPredicate<TContext>` or is registered as a stateless lambda. Required to carry a `Label` and `Description`; see [Predicate types](#predicate-types). |
| `PredicateArguments` | The non-generic accessor (`GetString`, `GetInt64`, ...) a predicate uses to read its own term's arguments inside `EvaluateAsync`. |
| `PredicateRegistry<TContext>` | Where predicates are registered under a name, with their `PredicateSchema`. Built once via `PredicateRegistryBuilder<TContext>`; no attribute or assembly scanning. `TryGetSchema` looks one up by name. |
| `PredicateSchema` | A predicate's registered name, a required read-only `Label` and `Description`, and its named-argument declarations (each also carrying a required `Description`), validated against a term's arguments at compile time. |
| `RuleBuilder` | A fluent API (`TruthWeaver.Building`) for assembling a rule tree from application logic without hand-writing DSL/JSON/YAML text; renders to the same JSON tree shape and compiles through the same `CompileJson` pipeline. See [Building rules programmatically](#building-rules-programmatically). |
| `RuleDescription` | The recursive result of `CompiledRule<TContext>.Describe()`: a node's `Label`, `Description`, and its `Operands` described the same way — the "what does this rule mean" view of a compiled tree, without exposing the AST types themselves. See [Describing a compiled rule](#describing-a-compiled-rule). |
| `RuleDiff` | Computes a structural diff between two compiled rules — which operator, term, or constant nodes were added, removed, or changed, located by operand-index path. See [Features](#features). |
| Rule | A named unit of persistence: metadata plus one expression. What gets compiled into a `CompiledRule<TContext>`. |
| Short-circuit | `AND` stops evaluating operands at the first `False`; `OR` stops at the first `True`. Skipped operands are recorded as `NotEvaluated` in the trace, not omitted. |
| Term | A predicate bound to concrete, literal arguments (e.g. `hasTopping(topping: "greenOlives")`) — the tree's leaf node, and the unit of memoization. |
| Term identity | What makes two term references "the same variable": predicate name (normalized to registered casing) plus arguments sorted by name and compared by exact, case-sensitive value. Argument order in source text never matters; array-valued arguments are order-sensitive. |
| `Trace` | An ordered, literal record of every node an evaluation visited or explicitly skipped — the "why was this denied" explanation. |
| `TruthValue` | The three-valued result type: `True`, `False`, or `Unknown`. Never `bool?`. |
| `EQUIVALENT(a, b)` / `↔` | The Strong Kleene biconditional (`IFF`; formerly and still readable as `XNOR`) — the negation of `XOR`, deliberately binary. Mixing `EQUIVALENT` with `AND`/`OR`, or with `XOR`, at the same level without parentheses is a compile error — see [Examples #4](#4-xor-equivalent-exactlyone-and-the-threshold-family). |
| `XOR(a, b)` | Binary exclusive-or; `NXOR(...)` is the n-ary parity operator. Mixing `XOR` with `AND`/`OR`, or with `EQUIVALENT`, at the same level without parentheses is a compile error — see [Examples #4](#4-xor-equivalent-exactlyone-and-the-threshold-family). |

## Appendix: Truth tables

Kleene three-valued truth tables for every binary/unary operator, in both
logical-name and boolean-algebra notation. `T` = `TruthValue.True`, `F` =
`TruthValue.False`, `?` = `TruthValue.Unknown`. Algebra notation: `∧` = AND,
`∨` = OR, `¬` = NOT, `⊕` = XOR, `↔` = EQUIVALENT (biconditional / IFF / legacy XNOR), `→` = IMPLIES, `↑` = NAND, `↓` = NOR, `1` = true,
`0` = false. Full reasoning: [ADR-0001](docs/adr/0001-kleene-failure-model.md).

### Unary: `NOT`

| a | `NOT a` | ¬a |
| :-: | :-: | :-: |
| T | F | ¬1 = 0 |
| F | T | ¬0 = 1 |
| ? | ? | ¬? = ? |

### Binary: `AND`

| a | b | `a AND b` | a∧b |
| :-: | :-: | :-: | :-: |
| T | T | T | 1∧1 = 1 |
| T | F | F | 1∧0 = 0 |
| T | ? | ? | 1∧? = ? |
| F | T | F | 0∧1 = 0 |
| F | F | F | 0∧0 = 0 |
| F | ? | F | 0∧? = 0 |
| ? | T | ? | ?∧1 = ? |
| ? | F | F | ?∧0 = 0 |
| ? | ? | ? | ?∧? = ? |

### Binary: `OR`

| a | b | `a OR b` | a∨b |
| :-: | :-: | :-: | :-: |
| T | T | T | 1∨1 = 1 |
| T | F | T | 1∨0 = 1 |
| T | ? | T | 1∨? = 1 |
| F | T | T | 0∨1 = 1 |
| F | F | F | 0∨0 = 0 |
| F | ? | ? | 0∨? = ? |
| ? | T | T | ?∨1 = 1 |
| ? | F | ? | ?∨0 = ? |
| ? | ? | ? | ?∨? = ? |

### Binary: `XOR`

| a | b | `a XOR b` | a⊕b |
| :-: | :-: | :-: | :-: |
| T | T | F | 1⊕1 = 0 |
| T | F | T | 1⊕0 = 1 |
| T | ? | ? | 1⊕? = ? |
| F | T | T | 0⊕1 = 1 |
| F | F | F | 0⊕0 = 0 |
| F | ? | ? | 0⊕? = ? |
| ? | T | ? | ?⊕1 = ? |
| ? | F | ? | ?⊕0 = ? |
| ? | ? | ? | ?⊕? = ? |

### Binary: `EQUIVALENT` (`NOT (a XOR b)`)

| a | b | `a EQUIVALENT b` | a↔b |
| :-: | :-: | :-: | :-: |
| T | T | T | 1↔1 = 1 |
| T | F | F | 1↔0 = 0 |
| T | ? | ? | 1↔? = ? |
| F | T | F | 0↔1 = 0 |
| F | F | T | 0↔0 = 1 |
| F | ? | ? | 0↔? = ? |
| ? | T | ? | ?↔1 = ? |
| ? | F | ? | ?↔0 = ? |
| ? | ? | ? | ?↔? = ? |

### Binary: `IMPLIES` (`NOT a OR b`)

| a | b | `a IMPLIES b` | a→b |
| :-: | :-: | :-: | :-: |
| T | T | T | 1→1 = 1 |
| T | F | F | 1→0 = 0 |
| T | ? | ? | 1→? = ? |
| F | T | T | 0→1 = 1 |
| F | F | T | 0→0 = 1 |
| F | ? | T | 0→? = 1 |
| ? | T | T | ?→1 = 1 |
| ? | F | ? | ?→0 = ? |
| ? | ? | ? | ?→? = ? |

### Binary: `NAND` (`NOT (a AND b)`)

| a | b | `a NAND b` | a↑b |
| :-: | :-: | :-: | :-: |
| T | T | F | 1↑1 = 0 |
| T | F | T | 1↑0 = 1 |
| T | ? | ? | 1↑? = ? |
| F | T | T | 0↑1 = 1 |
| F | F | T | 0↑0 = 1 |
| F | ? | T | 0↑? = 1 |
| ? | T | ? | ?↑1 = ? |
| ? | F | T | ?↑0 = 1 |
| ? | ? | ? | ?↑? = ? |

### Binary: `NOR` (`NOT (a OR b)`)

| a | b | `a NOR b` | a↓b |
| :-: | :-: | :-: | :-: |
| T | T | F | 1↓1 = 0 |
| T | F | F | 1↓0 = 0 |
| T | ? | F | 1↓? = 0 |
| F | T | F | 0↓1 = 0 |
| F | F | T | 0↓0 = 1 |
| F | ? | ? | 0↓? = ? |
| ? | T | F | ?↓1 = 0 |
| ? | F | ? | ?↓0 = ? |
| ? | ? | ? | ?↓? = ? |

### N-ary: `NXOR` (parity)

`NXOR` is `Unknown` whenever any operand is `Unknown`; otherwise it is `True`
exactly when an odd number of operands are `True`. Two operands give the
`XOR` table above; three operands:

| a | b | c | `NXOR(a, b, c)` |
| :-: | :-: | :-: | :-: |
| T | T | T | T |
| T | T | F | F |
| T | F | F | T |
| F | F | F | F |
| T | T | ? | ? |
| F | F | ? | ? |
| T | ? | F | ? |

`ExactlyOne(...)` and the threshold family don't get their own table here —
they're n-ary counting operators over the *number* of `True` operands, not
fixed two-input truth tables; their exact Kleene semantics (what counts as
"certain" vs. "still possibly reachable" when some operands are `Unknown`)
are covered by the evaluator's behavior described in
[Evaluation flow](#evaluation-flow) and tested directly in
`XorExactlyOneThresholdTests`.

## Design documents

- [CONTEXT.md](CONTEXT.md) — vocabulary, conceptual model, and the
  predicate-author contract.
- [ADR-0001: Kleene failure model](docs/adr/0001-kleene-failure-model.md) —
  why evaluation is three-valued internally and fails closed at the boundary.
- [ADR-0002: Evaluation semantics](docs/adr/0002-evaluation-semantics.md) —
  async predicates, per-evaluation memoization, short-circuit, fault
  handling, predicate registration, and the compile-and-swap rule lifecycle.
- [ADR-0003: Rule syntax and serialization](docs/adr/0003-rule-syntax-and-serialization.md) —
  the operator set, the DSL grammar, and the JSON/YAML tree form.
- [ADR-0004: Package boundaries and extensibility](docs/adr/0004-package-boundaries-and-extensibility.md) —
  why the library ships as three packages and how predicates and operators
  are extended.

## License

Apache License 2.0 — see [LICENSE](LICENSE).

See [CLAUDE.md](CLAUDE.md) for development rules, required validation
commands, and formatting/testing conventions.
