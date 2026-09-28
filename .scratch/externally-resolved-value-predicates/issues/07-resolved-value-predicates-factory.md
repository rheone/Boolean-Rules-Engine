# 07: Add a generic `ResolvedValuePredicates` factory to `TruthWeaver.Predicates`

**What to build:** A general-use, host-agnostic factory in
`TruthWeaver.Predicates` (alongside `StringPredicates`/`CollectionPredicates`/
`RegexPredicates`) that wraps the "resolve a literal key (and/or a context
value) to something, then test it" shape from this feature's `spec.md` —
**for the sub-case where the thing doing the resolving is safe to capture
once at registration** (a long-lived, thread-safe client — e.g. a cached
feature-flag reader, a `HttpClient`-backed lookup wrapper), not a scoped
per-request dependency.

This is deliberately narrower than tickets 01–06, which document/test the
**class-based** `IPredicate<TContext>` path (fresh-per-evaluation DI
resolution — the right answer whenever the dependency is scoped, e.g. a
`DbContext`). This ticket adds a **complementary, lighter-weight** path for
the common case where a host already holds a safe-to-share instance and
doesn't want to write a one-off class just to wrap it. Both paths solve the
same conceptual pattern; neither replaces the other — the doc from ticket
01 must make clear which one to reach for and why.

**Blocked by:** None for implementation. Should land after (or alongside)
ticket 01, since ticket 01's README doc should reference this factory as the
lighter-weight alternative once it exists — coordinate with whichever of the
two is worked second so neither references a non-existent section/type.

**Status:** done

Use `/tdd`: write the failing test against the factory's public shape first,
then implement the minimal factory to pass it — this is production code
(`TruthWeaver.Predicates`), not a docs/test-only ticket like 01–06.

- [x] New file `src/TruthWeaver.Predicates/ResolvedValuePredicates.cs`,
  matching the existing file's conventions (file-scoped namespace, XML docs
  with the same depth/style as `StringPredicates.cs`, no unnecessary
  abstraction beyond what's needed).
- [x] A generic `Create<TContext, TResolved>` factory:
  - Takes a `resolve: Func<TContext, PredicateArguments, CancellationToken, ValueTask<TResolved>>`
    delegate (reads whatever it needs from context and/or rule-text
    arguments and performs the live resolution) and a
    `test: Func<TResolved, bool>` delegate (turns the resolved value into
    the predicate's boolean answer).
  - Returns `(PredicateSchema, Func<TContext, PredicateArguments, CancellationToken, ValueTask<bool>>)`,
    matching the exact tuple shape `PredicateRegistryBuilder<TContext>.Add(schema, evaluate)`
    already expects (verify against current source, don't assume).
  - Accepts `name`, `label`, `description`, and a `params PredicateArgumentSchema[] arguments`
    (or `IReadOnlyList<PredicateArgumentSchema>`, matching `PredicateSchema`'s
    existing `Arguments` shape) so callers declare whatever rule-text
    arguments their `resolve` delegate needs.
- [x] A convenience overload for the single-value/no-comparison-target shape
  (`TResolved` is `bool` and the resolved value *is* the answer — no `test`
  delegate needed), so the flag-style case (spec.md shape 1) doesn't require
  a trivial `test: static x => x` at every call site.
- [x] XML doc on the type and both factory methods **explicitly states the
  capture-safety caveat**: `resolve` is captured once at registration time,
  same as any lambda predicate (`StringPredicates`'s existing selector
  pattern) — this factory is not appropriate when `resolve` needs a scoped
  dependency (a `DbContext`, a per-request `HttpClient`) re-resolved fresh
  per evaluation; use a class-based `IPredicate<TContext>` for that case
  instead, with a cross-reference to wherever ticket 01 lands its README
  section.
- [x] Tests in `tests/TruthWeaver.Predicates.Tests/` (matching the existing
  test project's location/conventions — check `StringPredicatesTests.cs` or
  equivalent for the established style) covering:
  - `Create` with a `resolve`/`test` pair: the produced predicate's schema
    matches what was passed in, and evaluating it calls `resolve` then
    `test` and returns the composed boolean result.
  - The single-value convenience overload: evaluating it returns exactly
    what `resolve` resolved to, with no separate `test` needed.
  - `resolve` throwing surfaces as an ordinary exception from the returned
    delegate (no swallowing/wrapping inside the factory) — the engine's
    existing fault-absorption (per ADR-0001) is what turns it into a
    `Fault`/`Unknown`; the factory itself must not add its own try/catch.
  - Two predicates built from `Create` with the same `name`/schema but
    different `resolve` closures remain independently usable (no shared
    mutable state between factory calls) — a basic sanity check that the
    factory doesn't accidentally capture anything across calls.
- [x] `dotnet build`, `dotnet test`, `dotnet csharpier check .`, and
  `dotnet format --verify-no-changes --severity info` all pass, per this
  repo's Required validation.

## Comments

Wrote `tests/TruthWeaver.Predicates.Tests/ResolvedValuePredicatesTests.cs`
first (confirmed it failed to compile with `CS0103` since
`ResolvedValuePredicates` didn't exist yet), then implemented
`src/TruthWeaver.Predicates/ResolvedValuePredicates.cs` with two static
factory methods: `Create<TContext, TResolved>(name, label, description,
resolve, test, params arguments)` and the single-value convenience overload
`Create<TContext>(name, label, description, resolve, params arguments)`
where `resolve` returns `ValueTask<bool>` directly. Both return the
`(PredicateSchema, Func<TContext, PredicateArguments, CancellationToken,
ValueTask<bool>>)` tuple `PredicateRegistryBuilder<TContext>.Add` expects.
The type-level XML doc states the capture-safety caveat and cross-references
README's "n arguments, class-based, externally-resolved value" section for
the scoped-dependency alternative. Six tests cover: schema round-trips
through `Create`; the two-delegate overload composes `resolve` then `test`
correctly (both true and false outcomes); the single-value overload returns
exactly what `resolve` resolves; `resolve` throwing surfaces unwrapped from
the returned delegate (no try/catch in the factory); and two predicates
built from `Create` with the same name/schema but different `resolve`
closures behave independently. Renamed an internal local function to
`EvaluateAsync` to satisfy VSTHRD200. Updated README.md: added a paragraph
and a `ResolvedValuePredicates.Create`
worked example immediately after ticket 01's three-shape section,
explaining when to reach for this factory versus the class-based path;
updated the "Predicate types" intro and the `TruthWeaver.Predicates`
packages-table row to mention it alongside `StringPredicates`/
`CollectionPredicates`/`RegexPredicates`.
