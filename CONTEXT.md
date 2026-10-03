# CONTEXT.md — TruthWeaver

This document is the shared vocabulary and domain model for the
`TruthWeaver` library. Read it before making structural changes to the
engine, and update it when the vocabulary changes. See `docs/adr/` for the
reasoning behind individual decisions, and
[`.scratch/deferred-features`](.scratch/deferred-features/spec.md) for
capabilities intentionally left out of the current design.

## What this is

A general-purpose boolean expression engine for .NET. A **rule** is authored
as text, compiled once into an immutable tree, and evaluated many times
against an application-supplied context. It answers *"is this expression
true right now, for this context?"* — nothing more.

It is not an authorization engine, a workflow engine, or a policy engine.
Those are all things you can *build on top of it* — permission checks
("can the current user do X"), process-flow gating, feature-flag
combination logic — but the engine itself has no opinion about permit/deny,
effects, or side effects. See
[`.scratch/deferred-features`](.scratch/deferred-features/spec.md) for why
an authorization layer is intentionally out of scope.

## Vocabulary

| Term | Definition |
| --- | --- |
| **Rule** | A named, versioned unit of persistence: metadata plus one `Expression`. |
| **Expression** | The boolean tree: operators over terms and sub-expressions. |
| **Predicate** | A registered, reusable implementation — `IPredicate<TContext>` — such as `hasTopping` or `lovesPineapple`. The *function*, not any particular call to it. |
| **Term** | A predicate bound to concrete arguments, e.g. `hasTopping(topping: "greenOlives")`. The tree's leaf node, and the unit of [term identity](#term-identity) and memoization. |
| **Operator** | `AND`, `OR`, `NOT`, `XOR`, `EQUIVALENT` (aliases `IFF`, legacy `XNOR`), `IMPLIES`, `NAND`, `NOR`, `NXOR`, `ANY`, `ALL`, `NONE`, `BETWEEN(min, max)`, `COALESCE` (infix `??`), `If` (ternary `c ? t : f`), the inspections `IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown`, `Project(x, True)`/`Project(x, False)`, `ExactlyOne`, and the threshold family `AtLeast(k)`/`AtMost(k)`/`GreaterThan(k)`/`LessThan(k)`/`Exactly(k)`, plus the constants `True`/`False`/`Unknown` (case-insensitive; printed upper camel). Never called a "gate." Every operator has a `Label`/`Description` exposed via `OperatorInfo.Describe`. |
| **Decision** | The result of evaluating an expression: a `TruthValue` plus any faults recorded along the way, and optionally a trace. |
| **Collapse** | The final boundary (not an operator) that turns a three-valued result into a two-valued answer under a **CollapsePolicy** (`UnknownAsFalse`, `UnknownAsTrue`, `UnknownIsError`), producing a **CollapseOutcome** (`True`, `False`, `RejectedUnresolved`). Available as `Decision.Collapse(policy)` at the API, and as the DSL function `Collapse(expr, policy)`, which is accepted only as a rule's outermost expression (`NestedCollapse` otherwise). `RejectedUnresolved` means "not known"; it is a normal outcome, not a **Fault**, and `Decision.IsSatisfied` stays fail-closed (true only for `True`). |
| **TruthValue** | `True` / `False` / `Unknown` — a dedicated three-valued (Kleene) type, never `bool?`. |
| **Fault** | A predicate failed to produce an answer during one evaluation (exception, timeout, cancellation). Faults become `Unknown`, not thrown exceptions, at the expression level. A predicate that simply returns `Unknown` is a normal answer and records no fault. |
| **CompiledRule** | The immutable, thread-safe result of compiling a rule's text. Safe to cache and share; compile once, evaluate many times. |
| **PredicateRegistry** | Where predicate implementations are registered under a name, with their argument schema. Every predicate carries a required, read-only `Label` and `Description`; every argument carries a required `Description`. |

Avoid these near-synonyms once the term above is established: "term" and
"predicate" are not interchangeable (a predicate is the function; a term is
a bound call to it); "gate" is circuit vocabulary, not used here;
`EvaluationContext` is not a type in this design — the context is just
`TContext`, owned entirely by the application.

## Conceptual model

```mermaid
classDiagram
    class Rule {
        +Expression Expression
    }
    class Expression {
        <<abstract>>
    }
    class Term {
        +string PredicateName
    }
    class AndExpression
    class OrExpression
    class NotExpression
    class XorExpression
    class EquivalentExpression
    class ImpliesExpression
    class NandExpression
    class NorExpression
    class NxorExpression
    class AnyExpression
    class AllExpression
    class NoneExpression
    class BetweenExpression {
        +int Min
        +int Max
    }
    class CoalesceExpression
    class InspectionExpression {
        +InspectionKind Kind
    }
    class ProjectExpression {
        +bool UnknownAs
    }
    class IfExpression {
        +Expression Condition
        +Expression WhenTrue
        +Expression WhenFalse
    }
    class ExactlyOneExpression
    class ThresholdExpression {
        +int K
        +ThresholdKind Kind
    }
    class ConstantExpression {
        +TruthValue Value
    }
    class Predicate {
        <<Interface>>
        +EvaluateAsync() TruthValue
    }

    Rule "1" *-- "1" Expression : has
    Expression <|-- Term
    Expression <|-- AndExpression
    Expression <|-- OrExpression
    Expression <|-- NotExpression
    Expression <|-- XorExpression
    Expression <|-- EquivalentExpression
    Expression <|-- ImpliesExpression
    Expression <|-- NandExpression
    Expression <|-- NorExpression
    Expression <|-- NxorExpression
    Expression <|-- AnyExpression
    Expression <|-- AllExpression
    Expression <|-- NoneExpression
    Expression <|-- BetweenExpression
    Expression <|-- CoalesceExpression
    Expression <|-- IfExpression
    Expression <|-- InspectionExpression
    Expression <|-- ProjectExpression
    Expression <|-- ExactlyOneExpression
    Expression <|-- ThresholdExpression
    Expression <|-- ConstantExpression
    AndExpression "1" o-- "2..*" Expression : operands
    OrExpression "1" o-- "2..*" Expression : operands
    NotExpression "1" o-- "1" Expression : operand
    XorExpression "1" o-- "2" Expression : operands
    EquivalentExpression "1" o-- "2" Expression : operands
    ImpliesExpression "1" o-- "2" Expression : antecedent, consequent
    NandExpression "1" o-- "2" Expression : operands
    NorExpression "1" o-- "2" Expression : operands
    NxorExpression "1" o-- "2..*" Expression : operands
    AnyExpression "1" o-- "2..*" Expression : operands
    AllExpression "1" o-- "2..*" Expression : operands
    NoneExpression "1" o-- "2..*" Expression : operands
    BetweenExpression "1" o-- "2..*" Expression : operands
    CoalesceExpression "1" o-- "2..*" Expression : operands
    IfExpression "1" o-- "3" Expression : condition, whenTrue, whenFalse
    InspectionExpression "1" o-- "1" Expression : operand
    ProjectExpression "1" o-- "1" Expression : operand
    ExactlyOneExpression "1" o-- "2..*" Expression : operands
    ThresholdExpression "1" o-- "2..*" Expression : operands
    Term "1" --> "1" Predicate : bound to
```

The same shape, as a grammar:

```text
Rule = Expression

Expression =
      Term
    | AND(Expression, Expression, ...)
    | OR(Expression, Expression, ...)
    | NOT(Expression)
    | XOR(Expression, Expression)          // binary only
    | EQUIVALENT(Expression, Expression)   // binary only; NOT(XOR(...)); aliases IFF, XNOR
    | IMPLIES(Expression, Expression)      // binary only; OR(NOT(antecedent), consequent)
    | NAND(Expression, Expression)         // binary only; NOT(AND(...))
    | NOR(Expression, Expression)          // binary only; NOT(OR(...))
    | NXOR(Expression, Expression, ...)    // n-ary parity; Unknown if any operand is Unknown
    | ANY(Expression, Expression, ...)     // AtLeast(1, ...)
    | ALL(Expression, Expression, ...)     // AtLeast(n, ...)
    | NONE(Expression, Expression, ...)    // AtMost(0, ...)
    | BETWEEN(min, max, Expression, Expression, ...)  // AND(AtLeast(min, ...), AtMost(max, ...))
    | COALESCE(Expression, Expression, ...)           // first non-Unknown operand; infix: a ?? b ?? c
    | If(Expression, Expression, Expression)          // condition, whenTrue, whenFalse; ternary: c ? t : f
    | IsTrue(Expression) | IsFalse(Expression) | IsUnknown(Expression) | IsKnown(Expression)  // always a definite True/False
    | Project(Expression, True | False)               // keeps True/False, Unknown becomes the chosen constant; always definite
    | ExactlyOne(Expression, Expression, ...)
    | AtLeast(k, Expression, Expression, ...)
    | AtMost(k, Expression, Expression, ...)
    | GreaterThan(k, Expression, Expression, ...)
    | LessThan(k, Expression, Expression, ...)
    | Exactly(k, Expression, Expression, ...)
    | true | false

Rule = Expression
     | Collapse(Expression, UnknownAsFalse | UnknownAsTrue | UnknownIsError)  // outermost only, never inside an Expression
```

Every expression evaluates to exactly one `TruthValue`. At the API boundary,
a `Decision.IsSatisfied` is `true` only when the result is `True` — `Unknown`
fails closed.

## Term identity

Term identity is what makes memoization, canonical equality, and constant/
contradiction analysis sound — two terms are "the same variable" if and only
if:

- predicate name, normalized case-insensitively to the registered casing, **and**
- arguments, sorted by name, each compared by exact type-normalized value.

Argument **values** are case-**sensitive** (`role: "Y"` and `role: "y"` are
different terms — role codes are frequently case-significant, and folding
them silently would be a security bug in an authorization consumer).
Argument **order** in the source text does not affect identity. Array-valued
arguments **are** order-sensitive.

## Equivalency rules

The operator set above is intentionally closed, not minimal — several
operators (and threshold-family edge values) are semantically equivalent to
a composition of others. These equivalences are recorded here so authors and
reviewers can recognize them, and so the set is never accidentally widened
with an operator that would just be a synonym for one of these:

| Expression | Equivalent to |
| --- | --- |
| `AtMost(0, ...)` | `NOT(OR(...))` (also `NONE(...)`) |
| `AtLeast(1, ...)` | `ANY(...)` (and, in K3, `OR(...)`) |
| `AtLeast(n, ...)`, where `n` is the operand count | `ALL(...)` (and, in K3, `AND(...)`) |
| `AND(AtLeast(min, ...), AtMost(max, ...))` | `BETWEEN(min, max, ...)` |
| `Exactly(n, ...)`, where `n` is the operand count | `AND(...)` |
| `Exactly(1, ...)` | `ExactlyOne(...)` |
| `EQUIVALENT(a, b)` (`IFF`, legacy `XNOR`) | `NOT(XOR(a, b))` |
| `IMPLIES(a, b)` | `OR(NOT(a), b)` |
| `NAND(a, b)` | `NOT(AND(a, b))` |
| `NOR(a, b)` | `NOT(OR(a, b))` |
| `GreaterThan(0, ...)` | `OR(...)` |
| `LessThan(n, ...)`, where `n` is the operand count | `NOT(AND(...))` |

> **Superseded by [ADR-0005](docs/adr/0005-strong-k3-language-surface.md):** `ANY`, `ALL` and `NONE` now exist as first-class derived cardinality operators (`AtLeast(1, ...)`, `AtLeast(n, ...)`, `AtMost(0, ...)`), kept as their own nodes so a rule round-trips as written. ADR-0003's earlier "no `All`/`None`" reasoning no longer applies. `BETWEEN` is likewise a first-class node: `BETWEEN(min, max, ...)` is `AND(AtLeast(min, ...), AtMost(max, ...))` over the interval, with the two integer bounds written first. It needs two or more operands, `0 <= min <= max <= n`, and rejects the whole range `0..n` as an always-true constant (`InvalidThresholdValue`). `COALESCE(a, b, ...)` and the infix `a ?? b ?? c` are one node that replaces only `Unknown` with the next operand (`True`/`False` pass through, short-circuiting at the first known value); a chain of `??` is accepted as one n-ary node because the operator is associative, but `??` still follows the no-mixing rule against `AND`/`OR` and other infix operators. `If(condition, whenTrue, whenFalse)` and the ternary `c ? t : f` are one node: a definite condition picks its branch (the other is not evaluated), while an `Unknown` condition does not guess, giving a value only when both branches are the same definite value (`(c AND t) OR (NOT c AND f) OR (t AND f)`); the ternary may not be mixed with other infix operators or a nested ternary without parentheses. `IsTrue(x)`, `IsFalse(x)`, `IsUnknown(x)` and `IsKnown(x)` are one `InspectionExpression` node with a kind: they test the K3 state of `x` and always yield a definite `True`/`False` (never `Unknown`), so they never collapse or fault the enclosing rule. `Project(x, True)` / `Project(x, False)` is a `ProjectExpression`: it keeps `True`/`False` and replaces only `Unknown` with the chosen constant (a literal `True` or `False`, never `Unknown` or an expression), so the result is always definite and equals `COALESCE(x, value)`. `Collapse(expr, policy)` is deliberately not an `Expression` node: it is the evaluation boundary around one, remembered on the `CompiledRule` as its `CollapsePolicy` and applied when the rule is evaluated, so the analyzer and the operators only ever see the inner expression.

`ANY(...)`, `ALL(...)` and `NONE(...)` take two or more operands, like `AND`/`OR`/`ExactlyOne`. In Strong K3 they happen to coincide with `OR(...)`, `AND(...)` and `NOT(OR(...))` (the cardinality interval collapses to the same truth tables); they exist as named, intent-revealing spellings.

These equivalences are documentation, not a normalization pass: the compiler
does not rewrite one form into the other, and both sides of each row remain
independently valid, distinct things a rule author can write. See the
[threshold operator family amendment](docs/adr/0003-rule-syntax-and-serialization.md#threshold-operator-family-supersedes-the-single-atleastk-)
in ADR-0003 for the full `AtLeast`/`AtMost`/`GreaterThan`/`LessThan`/`Exactly`
design this table draws its threshold rows from.

## The predicate-author contract

This is the one rule every predicate implementation must follow, and the one
the whole memoization/simplification/analysis story is unsound without:

> **A predicate must return the same answer for the same [term identity](#term-identity)
> within a single evaluation.**

Nothing broader is claimed or enforced:

- **No cross-evaluation guarantee.** A predicate reading `IOptions<T>` or a
  slowly-changing database row may return a different answer on the *next*
  evaluation. That's fine — memoization is scoped to one evaluation only.
- **Ambient state (clocks, timezones) is the predicate's problem, not the
  engine's.** `IsToday` is just a predicate that happens to read
  `TimeProvider` internally; the engine only ever sees and memoizes its
  `TruthValue` answer. The engine does not claim overall determinism across time.
- **IO volume and sequencing inside a predicate is the predicate author's
  responsibility.** The engine will not fan out or batch calls on a
  predicate's behalf; a predicate that makes 40 sequential HTTP calls is a
  predicate-authoring problem, not an engine problem.

See [ADR-0002](docs/adr/0002-evaluation-semantics.md) for the full
evaluation model this contract supports.

## Failure model (summary)

Internally three-valued (Kleene), two-valued at the boundary. A predicate
may answer `Unknown` directly (no fault) and signals a failure by throwing; the evaluator catches it, records a `Fault`
(predicate identity + exception), and treats the term as `Unknown` rather
than aborting evaluation. Evaluation continues wherever the logic can still
reach a determinate answer (`Unknown OR True` is `True`), because a fault
that can't affect the outcome shouldn't turn a transient blip into a denial.
Full reasoning and the truth tables: [ADR-0001](docs/adr/0001-kleene-failure-model.md).

## Compilation and persistence (summary)

`Compile` never throws for authoring errors; it returns a `CompilationResult`
of a nullable `CompiledRule` plus a list of diagnostics (code, severity,
source span). **Nothing that fails compilation is ever persisted** — the
write path treats diagnostics as form-validation messages, and a rejected
save leaves the previously persisted rule active. `CompiledRule` is
immutable, so runtime rule changes are a compile-and-swap-a-reference: no
locking, in-flight evaluations finish against the old rule. Full reasoning:
[ADR-0002](docs/adr/0002-evaluation-semantics.md).

The analyzer step reasons in Strong K3 too (dual-rail BDD: "definitely true" /
"possibly true"). It warns (`BRE0012` tautology, `BRE0013` contradiction) only
when a sub-expression is `True` (resp. `False`) for every
`{True, False, Unknown}` assignment of its terms, so `A AND NOT A` and
`A OR NOT A` are not reported: both are `Unknown` when `A` is.
([ADR-0005](docs/adr/0005-strong-k3-language-surface.md) decision 17.)

## Syntax and serialization (summary)

The string DSL (word operators, plus the symbol aliases `&&`, `||`, `!`, `∧`, `∨`,
`¬`, `⊕`, `→` that compile to the same nodes and are never printed; `NOT > AND > OR` precedence, every infix operator other than `NOT`/`AND`/`OR`
never mixed with `AND`/`OR` or with a different infix operator without parentheses; `()`, `[]` and `{}` are interchangeable grouping delimiters that the tree does not retain; `CanonicalText` prints parentheses only, `PrintText(GroupingStyle.DepthCycling)` is the opt-in depth-varying rendering) is
canonical and is what gets persisted. JSON and YAML are interchange/tooling
formats that compile to the same AST and round-trip losslessly with the DSL.
A rule can also be assembled programmatically via `RuleBuilder`
(`TruthWeaver.Building`), which renders to the same JSON tree shape
and compiles through the identical pipeline. The JSON tree shape is also
published as a JSON Schema document,
[`rule-tree.schema.json`](src/TruthWeaver/Json/rule-tree.schema.json),
shipped as a content asset in the `TruthWeaver` package. Full grammar
and schema: [ADR-0003](docs/adr/0003-rule-syntax-and-serialization.md).

## Package boundaries (summary)

`TruthWeaver.Abstractions` (the kernel: `IPredicate<TContext>`,
argument schema, `TruthValue`, `Decision` — zero dependencies, shared across
projects that only *implement* predicates), `TruthWeaver` (AST,
parser, compiler, analyzer, evaluator, System.Text.Json support, DI
extensions), `TruthWeaver.Yaml` (YamlDotNet only). Full reasoning:
[ADR-0004](docs/adr/0004-package-boundaries-and-extensibility.md).

## AOT / trim compatibility

Trimming/AOT is a design constraint rather than an afterthought here, so
it's verified rather than assumed. `src/Directory.Build.props` sets
`IsAotCompatible` for every shipping package (`TruthWeaver.Abstractions`,
`TruthWeaver`, `TruthWeaver.Predicates`, `TruthWeaver.Testing`,
`TruthWeaver.Yaml`), enabling both the trim analyzer (`IL2xxx`) and the NativeAOT
analyzer (`IL3xxx`), and CI (`.github/workflows/ci.yml`) promotes their warnings to build
errors. As of this writing that analysis is clean: zero trim/AOT warnings across all five
packages.

This holds by construction, not by suppression:

- The rule tree's JSON support (`TruthWeaver/Json`) reads and writes `JsonElement`/
  `JsonNode`/`JsonObject`/`JsonArray` directly — never `JsonSerializer.Deserialize<T>` — so
  there is no reflection-based (de)serialization to source-generate around.
- The DI registration extension (`AddTruthWeaver<TContext>`) registers a
  closed-generic instance and a factory delegate, not an open-generic or reflection-driven
  registration.
- No production code path uses `Activator.CreateInstance`, `MakeGenericMethod`, assembly
  scanning, or runtime code generation (`System.Reflection.Emit`, `Expression.Compile`, etc.).

**Known limitation — YamlDotNet:** `TruthWeaver.Yaml` only depends on YamlDotNet's
low-level `RepresentationModel` DOM (`YamlStream`/`YamlNode`), not its reflection-based
object-graph (de)serializer, so nothing in this package's own code triggers a trim/AOT
warning today. However, YamlDotNet 18.1.0 does not itself ship `IsTrimmable`/AOT annotations
(no `ILLink` metadata in its NuGet package), so the trim/AOT analyzer can't see into it and
verify its internals — a real incompatibility inside YamlDotNet's own reflection paths would
not surface as a build warning here. `TruthWeaver.Yaml` is trim/AOT-*analyzed* clean,
not independently *proven* safe end-to-end; a consumer publishing with `PublishAot`/
`PublishTrimmed` who reaches this package should smoke-test that specific scenario.

No dedicated `PublishAot` smoke-test host was added: `PublishAot`/`PublishTrimmed` are
publish-time settings for an executable, and none of these five packages is one. The
build-time analyzer (`IsAotCompatible`) is the correct and sufficient check for a library —
it's the same mechanism the .NET runtime's own libraries use to stay AOT-compatible without
publishing themselves.

## Related documents

- [ADR-0001: Kleene failure model](docs/adr/0001-kleene-failure-model.md)
- [ADR-0002: Evaluation semantics](docs/adr/0002-evaluation-semantics.md)
- [ADR-0003: Rule syntax and serialization](docs/adr/0003-rule-syntax-and-serialization.md)
- [ADR-0004: Package boundaries and extensibility](docs/adr/0004-package-boundaries-and-extensibility.md)
- [README.md](README.md)
