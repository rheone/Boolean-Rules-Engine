# ADR-0002: Evaluation semantics

## Status

Accepted

## Context

Once a rule is compiled, evaluating it against a context needs precise
answers to several coupled questions: how predicates are invoked (sync vs.
async), how term results are reused within one evaluation, in what order
operands are visited, what happens when a term faults mid-evaluation, how
predicates are registered and resolve their own dependencies, and how a rule
gets updated at runtime without downtime or locking. These are treated
together because they constrain each other — for example, memoization is
only sound if term identity is stable and predicates are pure for the
duration of one evaluation, and short-circuit evaluation is only safe if
predicates have no side effects a caller depends on.

## Decision

### Async predicates, required cancellation

```csharp
public interface IPredicate<in TContext>
{
    static abstract PredicateSchema Schema { get; }

    ValueTask<TruthValue> EvaluateAsync(
        TContext context,
        PredicateArguments args,
        CancellationToken cancellationToken);
}
```

Predicates return `ValueTask<TruthValue>` (originally `ValueTask<bool>`; changed by
[ADR-0005](0005-strong-k3-language-surface.md) decision 15 so a predicate can answer
`Unknown` directly) and take a required `CancellationToken`
(no default-cancellation-token overload on the evaluation path). This engine's
primary consumers do I/O — a database lookup, an HTTP call, a config read via
`IOptions<T>` — and async is the only choice that doesn't force every
predicate author into a blocking call or a bespoke sync/async split. A purely
synchronous predicate (e.g. reading an in-memory flag) costs nothing extra
via `ValueTask.FromResult(...)`; forcing the reverse — wrapping IO-bound work
behind a synchronous facade — is materially worse.

Arguments are exposed through a small, non-generic `PredicateArguments`
accessor (`args.GetString("role")`, `args.GetInt64("days")`, etc.) validated
against the predicate's declared `PredicateSchema` at *compile* time, so a
missing or mistyped argument is a compile diagnostic
([ADR-0003](0003-rule-syntax-and-serialization.md)), never a runtime failure
inside `EvaluateAsync`.

A predicate signals a fault by throwing (see
[ADR-0001](0001-kleene-failure-model.md)); it may also return a value from an
explicit "unknown" pathway for authors who want to express indeterminacy
without treating it as exceptional, but throwing on genuine failure (a
timeout, a connection failure) is the expected, low-friction path and
requires no special handling by the predicate author beyond letting the
exception propagate naturally.

### Term identity and per-evaluation memoization

A term's identity is the normalized predicate name plus its arguments sorted
by name and canonically value-formatted (full rule in
[CONTEXT.md](../../CONTEXT.md#term-identity)). Within a single evaluation,
each distinct term identity is evaluated **at most once**, regardless of how
many places in the tree reference it — the second and subsequent references
reuse the memoized `TruthValue`.

This is scoped to **one evaluation only**. The predicate-author contract is
exactly: *the same term identity returns the same answer within a single
evaluation*. Nothing is claimed or cached across evaluations — a predicate
reading a slowly-changing database row or `IOptions<T>` may legitimately
return a different answer next time `Evaluate` is called, and that is
correct behavior, not a bug. Ambient state such as a clock is entirely the
predicate's concern (e.g. an `IsToday` predicate reads `TimeProvider`
internally); the engine has no special knowledge of time and makes no
determinism claim spanning evaluations.

### Left-to-right, short-circuit, no implicit concurrency

Operands are evaluated strictly left to right. `AND` stops as soon as a
`False` is reached (see the Kleene tables in
[ADR-0001](0001-kleene-failure-model.md) — `False AND anything` is `False`
regardless of `Unknown`); `OR` stops as soon as a `True` is reached. This is
observable in the trace: unevaluated nodes are recorded explicitly as
`NotEvaluated`, not omitted, so a "why was I denied" trace shows what *didn't*
run rather than silently having a hole where the reader would expect an
entry.

Sibling operands are **not** evaluated concurrently in v1. Concurrency would
be a real latency win for independent I/O-bound terms, but it costs
deterministic fault ordering, straightforward cancellation, and a trace that
reads in the order the rule was written — all correctness/debuggability
properties this v1 prioritizes over the latency win. This is deliberately
left as an additive `EvaluationOptions` knob for later, since it becomes safe
purely as a consequence of predicates already being contractually pure.

An `EvaluationMode.Exhaustive` option evaluates every reachable term (no
short-circuit) and collects every fault, for diagnostic/support use. This
mode never changes `Decision.Result` — it only changes which terms run and
what the trace/fault list contains — and that invariant must hold for any
future change to this mode.

### Faults absorb, they don't abort

When a predicate faults, the evaluator does not abort the evaluation. It
records the `Fault` and treats that term as `Unknown`, and evaluation
continues wherever the operator tables permit a determinate result to still
be reached (see [ADR-0001](0001-kleene-failure-model.md)). This is the entire
point of using Kleene logic: a transient database blip that turns out to be
irrelevant to the final answer (e.g. it's one operand of an `OR` whose other
operand is `True`) should not turn into a denial.

`EvaluationOptions.FaultBudget` (default: unlimited) lets a caller opt into
fail-fast behavior — e.g. set to `1` during a known outage so the first
fault aborts evaluation immediately rather than continuing to spend I/O
trying to route around it. This is a caller-level operational choice, not a
change to the engine's default semantics.

### Predicate registration and dependency lifetimes

The `PredicateRegistry` stores **descriptors** — name, `PredicateSchema`,
and either an implementation type or a stateless lambda — not instances.
Class-based predicates are resolved from a per-evaluation `IServiceProvider`
supplied alongside the context, so a predicate with a **scoped** dependency
(a `DbContext`, a per-request `HttpClient`) resolves correctly on every
evaluation rather than being captured once at registration time, which would
be wrong the moment a rule outlives the scope it was compiled in.

Registration is **explicit only** — a builder API, plus the lambda form for
stateless predicates. No attribute scanning / assembly scanning: it is
implicit magic, it works against trimming and AOT, and it contradicts this
repo's own rule against unnecessary abstraction. A rule referencing a
predicate name with no matching registration is a compile-time diagnostic
(see [ADR-0003](0003-rule-syntax-and-serialization.md)), not a runtime
surprise.

### Rule lifecycle: compile-and-swap, not a framework

The library owns **compilation and evaluation only** — not rule storage,
scheduling, or invalidation. `CompiledRule` is immutable and thread-safe.
Runtime rule changes (a rule edited in a database, a config file reloaded)
are handled entirely by the *consuming application*: detect the change,
call `RuleCompiler.Compile` again, and assign the result to the field/
property holding the active `CompiledRule`. Because the assignment is a
single reference swap, in-flight evaluations finish against the rule they
started with, new evaluations pick up the new one, and no lock is needed.

The library deliberately does **not** ship an `IRuleSource`, a rule cache, or
change-token plumbing in v1 — that is exactly the kind of scope creep that
turns a rules engine into an unwanted framework. The compile-and-swap pattern
is documented (here and in the README) as the recommended integration, not
built as API surface.

### Persisted rules and compile failures

`RuleCompiler.Compile` never throws for an authoring error (unknown
predicate, bad argument type, malformed syntax); it returns a
`CompilationResult` — nullable `CompiledRule` plus a list of diagnostics.
The intended integration is a form-submission model: an editing/persistence
path calls `Compile`, and if there are `Error`-severity diagnostics, the
save is rejected and the diagnostics are surfaced to whoever is editing the
rule (with source spans, so an editor can underline the offending token) —
**the previously persisted, previously-compiled rule remains active and
nothing new is written**. A rule that successfully compiles once cannot
later become invalid without an explicit new write, so the running system
never silently starts failing to compile a rule it already accepted.

`CompilationMode.Lenient` is available for a different, legitimate scenario:
multiple services sharing one rule store with different predicate sets
registered per service. There, an unknown predicate is not necessarily a
bug in the rule, so it compiles as a permanent `Unknown` fault on that term
rather than an error — but this mode is never used on the write/persistence
path, only on read paths where cross-service predicate mismatch is an
accepted topology.

### Resource limits

`CompilerOptions` bounds are enforced during compilation so that a rule
authored by an admin — potentially against a database, not reviewed as code
— cannot pathologically hang a request thread: max tree depth (default 32),
max node count (default 512), and a cap on the number of distinct terms
subject to BDD-based constant/contradiction analysis (default 20; beyond the
cap, analysis is skipped and reported as an `Info` diagnostic — never
silently claimed as "not constant," and never left to hang). All bounds are
overridable via `CompilerOptions`, defaulted to values no legitimate rule is
expected to approach. `EvaluationOptions` similarly exposes an overall
evaluation timeout, linked into the caller's `CancellationToken`, default
off.

I/O volume and sequencing *inside* a single predicate (e.g. a predicate that
happens to make several sequential HTTP calls) is out of scope for these
limits and is the predicate author's responsibility, per the
predicate-author contract in [CONTEXT.md](../../CONTEXT.md).

## Consequences

- Evaluations are cheap to repeat (per-evaluation memoization + short-circuit
  + a shared `CompiledRule`), which is the intended answer to "evaluate this
  rule for many candidate users" — the caller loops, rather than the engine
  offering a bulk/partial-evaluation API in v1 (deferred, see
  [CONTEXT.md](../../CONTEXT.md#deferred)).
- The trace is an accurate, literal record of what was and wasn't evaluated,
  which makes it trustworthy for "why was this denied" without being
  misleading about short-circuited branches.
- Runtime rule updates require zero engine-provided infrastructure, at the
  cost of the consuming application being responsible for detecting changes
  and triggering recompilation — an explicit, intended trade-off.
- A rule that fails to compile can never be silently persisted; validation
  and persistence are inseparable by construction.

## Related

- [ADR-0001: Kleene failure model](0001-kleene-failure-model.md)
- [ADR-0003: Rule syntax and serialization](0003-rule-syntax-and-serialization.md)
- [CONTEXT.md](../../CONTEXT.md)
