# CONTEXT.md — BooleanRulesEngine

This document is the shared vocabulary and domain model for the
`BooleanRulesEngine` library. Read it before making structural changes to the
engine, and update it when the vocabulary or the deferred list changes. See
`docs/adr/` for the reasoning behind individual decisions.

## What this is

A general-purpose boolean expression engine for .NET. A **rule** is authored
as text, compiled once into an immutable tree, and evaluated many times
against an application-supplied context. It answers *"is this expression
true right now, for this context?"* — nothing more.

It is not an authorization engine, a workflow engine, or a policy engine.
Those are all things you can *build on top of it* — permission checks
("can the current user do X"), process-flow gating, feature-flag
combination logic — but the engine itself has no opinion about permit/deny,
effects, or side effects. See [Deferred](#deferred) for the authorization
layer this may grow later.

## Vocabulary

| Term | Definition |
| --- | --- |
| **Rule** | A named, versioned unit of persistence: metadata plus one `Expression`. |
| **Expression** | The boolean tree: operators over terms and sub-expressions. |
| **Predicate** | A registered, reusable implementation — `IPredicate<TContext>` — such as `hasRole` or `isManager`. The *function*, not any particular call to it. |
| **Term** | A predicate bound to concrete arguments, e.g. `hasRole(role: "Y")`. The tree's leaf node, and the unit of [term identity](#term-identity) and memoization. |
| **Operator** | `AND`, `OR`, `NOT`, `XOR`, `XNOR`, `ExactlyOne`, and the threshold family `AtLeast(k)`/`AtMost(k)`/`GreaterThan(k)`/`LessThan(k)`/`Exactly(k)`, plus the constants `true`/`false`. Never called a "gate." Every operator has a `Label`/`Description` exposed via `OperatorInfo.Describe`. |
| **Decision** | The result of evaluating an expression: a `TruthValue` plus any faults recorded along the way, and optionally a trace. |
| **TruthValue** | `True` / `False` / `Unknown` — a dedicated three-valued (Kleene) type, never `bool?`. |
| **Fault** | A predicate failed to produce an answer during one evaluation (exception, timeout, cancellation). Faults become `Unknown`, not thrown exceptions, at the expression level. |
| **CompiledRule** | The immutable, thread-safe result of compiling a rule's text. Safe to cache and share; compile once, evaluate many times. |
| **PredicateRegistry** | Where predicate implementations are registered under a name, with their argument schema. Every predicate carries a required, read-only `Label` and `Description`; every argument carries a required `Description`. |

Avoid these near-synonyms once the term above is established: "term" and
"predicate" are not interchangeable (a predicate is the function; a term is
a bound call to it); "gate" is circuit vocabulary, not used here;
`EvaluationContext` is not a type in this design — the context is just
`TContext`, owned entirely by the application.

## Conceptual model

```
Rule = Expression

Expression =
      Term
    | AND(Expression, Expression, ...)
    | OR(Expression, Expression, ...)
    | NOT(Expression)
    | XOR(Expression, Expression)          // binary only
    | XNOR(Expression, Expression)         // binary only; NOT(XOR(...))
    | ExactlyOne(Expression, Expression, ...)
    | AtLeast(k, Expression, Expression, ...)
    | AtMost(k, Expression, Expression, ...)
    | GreaterThan(k, Expression, Expression, ...)
    | LessThan(k, Expression, Expression, ...)
    | Exactly(k, Expression, Expression, ...)
    | true | false
```

Every expression evaluates to exactly one `TruthValue`. At the API boundary,
a `Decision.IsSatisfied` is `true` only when the result is `True` — `Unknown`
fails closed.

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
  boolean answer. The engine does not claim overall determinism across time.
- **IO volume and sequencing inside a predicate is the predicate author's
  responsibility.** The engine will not fan out or batch calls on a
  predicate's behalf; a predicate that makes 40 sequential HTTP calls is a
  predicate-authoring problem, not an engine problem.

See [ADR-0002](docs/adr/0002-evaluation-semantics.md) for the full
evaluation model this contract supports.

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

## Failure model (summary)

Internally three-valued (Kleene), two-valued at the boundary. A predicate
signals a fault by throwing; the evaluator catches it, records a `Fault`
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

## Syntax and serialization (summary)

The string DSL (word operators, `NOT > AND > OR` precedence, `XOR`/`XNOR`
never mixed with `AND`/`OR` or with each other without parentheses) is
canonical and is what gets persisted. JSON and YAML are interchange/tooling
formats that compile to the same AST and round-trip losslessly with the DSL.
A rule can also be assembled programmatically via `RuleBuilder`
(`BooleanRulesEngine.Building`), which renders to the same JSON tree shape
and compiles through the identical pipeline. Full grammar and schema:
[ADR-0003](docs/adr/0003-rule-syntax-and-serialization.md).

## Package boundaries (summary)

`BooleanRulesEngine.Abstractions` (the kernel: `IPredicate<TContext>`,
argument schema, `TruthValue`, `Decision` — zero dependencies, shared across
projects that only *implement* predicates), `BooleanRulesEngine` (AST,
parser, compiler, analyzer, evaluator, System.Text.Json support, DI
extensions), `BooleanRulesEngine.Yaml` (YamlDotNet only). Full reasoning:
[ADR-0004](docs/adr/0004-package-boundaries-and-extensibility.md).

## Deferred

Recorded so they're revisited deliberately rather than rediscovered from
scratch. None of these are rejected outright — the AST and compiler are
designed so each remains addable without a breaking rework.

| Item | Why deferred |
| --- | --- |
| **Authorization layer** (policy sets, permit/forbid, forbid-overrides, decision-with-provenance) | A genuinely different, larger problem than "evaluate one boolean expression." Belongs as a layer built *on* this engine, likely a separate package, once there's a concrete consumer. |
| **Partial evaluation / residual expressions** (bind known facts, simplify, hand the caller a residual expression to push into e.g. a SQL `WHERE` clause) | This is what "who can do X against many resources" really wants, but it requires predicates to be *translatable*, not just callable, which contradicts "a predicate is opaque application code." v1 is evaluate-only; callers loop over candidates, made cheap by per-evaluation memoization and a shared `CompiledRule`. |
| **Rule-to-rule references / named reusable fragments** | Valuable for a real rule library (shared sub-rules, cycle detection, compile-time inlining) but adds a resolver abstraction the v1 scope doesn't need yet. |
| **Cross-evaluation caching** | The predicate-author contract only promises stability *within* one evaluation. A cache spanning evaluations is a distinct feature with its own invalidation story. |
| **OpenTelemetry-shaped observability** (activity per rule, event per term, fault attributes) | v1 logs via `Microsoft.Extensions.Logging.Abstractions`. OTel is additive on top later, not a v1 requirement. |
| **Context-bound term arguments** (e.g. `IsManagerOf({{resource.ownerId}})`) | Requires a typed path-expression mini-language and breaks static canonical-equality between rules. v1 arguments are literals only; a predicate that needs a context value reaches into the context itself (`IsManagerOfResourceOwner`). |
| **Symbol operator aliases** (`&&`, `||`) | Word operators only, to keep the surface to one thing to learn and test. |
| **Concurrent operand evaluation** | Purely additive once predicates are contractually pure; left as an `EvaluationOptions` knob for later rather than v1 default behavior. |
| **Minimal satisfying assignments** (BDD-derived "what facts would make this true") | The BDD exists anyway for constant/contradiction diagnostics; exposing satisfying-assignment enumeration is an authoring-tool feature with no current consumer. |
| **Attribute-based / assembly-scanned predicate registration** | Explicit registration only in v1 — scanning is magic, breaks trimming/AOT, and the repo's own rule is "do not introduce unnecessary abstractions." |

## Related documents

- [ADR-0001: Kleene failure model](docs/adr/0001-kleene-failure-model.md)
- [ADR-0002: Evaluation semantics](docs/adr/0002-evaluation-semantics.md)
- [ADR-0003: Rule syntax and serialization](docs/adr/0003-rule-syntax-and-serialization.md)
- [ADR-0004: Package boundaries and extensibility](docs/adr/0004-package-boundaries-and-extensibility.md)
- [README.md](README.md)
