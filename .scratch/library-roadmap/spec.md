# Library roadmap: feature candidates, impact/complexity, and verdicts

**Status:** brainstorm

## Purpose

A prioritized write-up of the feature candidates raised in discussion,
following on from [`equation-rendering`](../equation-rendering/spec.md) and
[`diagram-rendering-options`](../diagram-rendering-options/spec.md). Each
item is scored on **impact** (value if built) and **complexity** (effort/risk
to build), and given a verdict: **Do now** (clear win, no blocker), **Do
(sequenced)** (real value, but has a dependency or a design subtlety to
resolve first), **Not now** (valid idea, deliberately deferred, with a
reason), or **Don't do** (rejected, with a reason — not "maybe later").

Impact/complexity are both Low/Medium/High. This is a prioritization
write-up, not a commitment — nothing here is scheduled.

## At a glance

| Item | Impact | Complexity | Verdict |
| --- | --- | --- | --- |
| Rule linting (style/hygiene pass) | Medium–High | Medium | **Do now** |
| Registry-wide unused-predicate audit | Low–Medium | Low | **Do now** |
| Predicate deprecation marker + diagnostic | Medium | Low | **Do now** |
| Source-generator predicate registration | Medium | Medium–High | Do (sequenced) |
| Ready-made predicate factories (numeric/Guid/nullability) | Medium | Low | **Do now** |
| Date/time: fixed cutoff before/after | Medium | Low | **Do now** |
| Date/time: day-of-week/month/time-window (IANA timezone) | Medium–High | Medium | Do (sequenced) |
| Date/time: calendar/holiday predicates *in the engine* | — | — | **Don't do** |
| Culture/case: extend `EqualsConfigurable` pattern to `StartsWith`/`EndsWith`/`Contains`/`SetEquals` | Medium | Low | **Do now** |
| Unicode normalization (NFC/NFKC) support | Low–Medium | Medium | Not now |
| Starter templates (`dotnet new`) | Medium | Low | **Do now** |
| Rule complexity metrics (incl. BDD-node-count) | Medium | Low | **Do now** |
| Joining compiled rules via an operator | High | Medium–High | Do (sequenced) |
| Public `RuleFuzzer` (structural/AST fuzzing) | Medium | Low | **Do now** |
| Stryker.NET mutation testing (CI, repo-internal) | Medium | Low | **Do now** |
| **Predicate test harness** (new — see below) | Medium–High | Medium | **Do now** |
| Public rule-equivalence check (BDD-based) | High | Low | **Do now** |
| De Morgan's / negation-normal-form print mode | Low–Medium | Low | **Do now** |
| BDD-based "simplify my rule" re-synthesis | Medium–High | High | Do (sequenced, last) |
| Quine–McCluskey-style guaranteed-minimal minimization | — | — | **Don't do** |
| SMT/Z3 integration | — | — | **Don't do** |
| K-maps | — | — | **Don't do** |

## Do now

### Rule linting (style/hygiene pass)

Compile diagnostics today are either hard errors or the BDD's whole-rule
constant/contradiction warnings — nothing covers authoring style. A
`RuleLinter` walking the compiled tree could flag: nesting approaching (not
yet violating) `CompilerOptions`' depth limit; wide n-ary chains that would
read cleaner as an array predicate (the README already documents this
pattern manually); and, the genuinely new value here, **sibling
sub-expression redundancy already provable by the existing BDD**. The BDD
manager (`src/TruthWeaver/Analysis/BddManager.cs`) already proves `a OR (a
AND b)` and `a` are the same node — today that fact is only used for
whole-rule tautology/contradiction. A lint pass comparing sibling
sub-expressions' BDD node ids would surface "this branch is provably
equivalent to a simpler one" using infrastructure that already exists.
Medium complexity because the hard part (BDD walking) is built; the new work
is authoring what counts as a finding.

### Registry-wide unused-predicate audit

A standalone tool (not a compiler feature) taking a `PredicateRegistry` plus
a set of compiled/persisted rules and reporting which registered predicates
are referenced by none of them — "unused NuGet package" detection for
predicates. Outside the engine's scope (it doesn't own a rule store), so
this ships as a separate small utility, not a `RuleCompiler` change.

### Predicate deprecation marker + diagnostic

`PredicateSchema` gains an optional `Deprecated`/`ReplacedBy` marker; the
compiler emits an `Info`-severity diagnostic when a rule references a
deprecated-but-still-registered predicate. Cheap (one field, one diagnostic
code, reuses the existing `DiagnosticSeverity` model), genuinely useful for
migrating a live predicate catalog without breaking existing rules.

### Ready-made predicate factories (numeric/Guid/nullability)

Filling out the (type × operation) matrix the existing factories already
establish: `InRange`/`Between` for `Int64`/`Decimal`, generic non-string
nullability checks, `Guid` set-membership (today's `CollectionPredicates.SetEquals`
is string-only). Purely mechanical, same template as every existing factory
— this is the [`predicate-catalog`](../predicate-catalog/issues/01-brainstorm-general-use-predicates-and-literal-kinds.md)
brainstorm's territory; batch it as one ticket rather than one-offs.

### Date/time: fixed cutoff before/after

Not fragile at all — a plain `DateTimeOffset` comparison against a literal,
already fully supported by `LiteralKind.DateTimeOffset` and `GetDateTimeOffset`.
Almost free today; ship as a documented factory/example.

### Culture/case: extend the `EqualsConfigurable` pattern

Now that `StringPredicates.EqualsConfigurable` exists (ignoreCase/culture/trim
as rule-text arguments, default case-insensitive + `InvariantCulture`),
extending the same shape to `StartsWith`/`EndsWith`/`Contains` and to
`CollectionPredicates.SetEquals` is small, mechanical follow-on work using a
proven template — including the Turkish-I-problem test pattern already
established for verifying `culture` actually changes behavior.

### Starter templates (`dotnet new`)

`dotnet new truthweaver-consumer` (sample context, both predicate shapes, a
sample rule, DI wiring, one `TruthWeaver.Testing` test) and a leaner
`dotnet new truthweaver-predicate-library` (Abstractions-only, matching
ADR-0004's package story for a predicate-only team). Low effort, real
onboarding-time payoff.

### Rule complexity metrics

Beyond the obvious (node count, depth, distinct-predicate count), the
non-obvious win: **BDD node count as a complexity proxy**. It's already
computed during compilation for analysis and can diverge sharply from
source-text size — certain structures (XOR-heavy trees especially) are
classically BDD-adversarial and can blow up even when the DSL text looks
small. Surfacing "this rule's BDD has 500 nodes" catches a genuine
performance risk a naive AST-node count would miss entirely. Cheap to expose
(a `RuleMetrics` DTO alongside `CompilationResult`), reuses data already
computed.

### Public `RuleFuzzer` (structural/AST fuzzing)

`DslRoundTripPropertyTests`-style random-AST generation already exists
*internally*. Making it public in `TruthWeaver.Testing` lets consumers fuzz
their own predicate registries against a wide variety of generated **rule
shapes** — distinct from the predicate test harness below, which fuzzes
**argument values** against one predicate, not rule structure. Nearly free:
the generator logic already exists and is proven.

### Stryker.NET mutation testing (CI, repo-internal)

A process recommendation, not a library feature: run `dotnet-stryker`
against `src/TruthWeaver`/`TruthWeaver.Predicates` in CI to find
under-tested logic (an off-by-one in a threshold comparison, an unasserted
BDD branch). Pure quality payoff, no new API surface, low effort to wire in.

### Predicate test harness (new)

Distinct from `TruthWeaver.Testing`'s existing `FakePredicates` (which
*stand in for* predicates so a rule's evaluation logic can be tested without
real implementations) — this is tooling to verify a **real** predicate
implementation behaves correctly, and it's the natural home for the
fuzz/boundary-value ideas raised alongside it rather than three separate
overlapping features. A `PredicateHarness<TContext>` (fluent, in the style
of `DecisionAssertions`) taking a predicate's `(Schema, Evaluate)` tuple and
running standard checks:

- **Determinism/purity.** Call `Evaluate` twice with identical arguments and
  context; assert the same answer — this is CONTEXT.md's predicate-author
  contract ("the same argument plus the same context within one evaluation
  must yield the same answer") turned into an automated, repeatable test
  instead of a documented expectation nobody actually checks.
- **Boundary-value robustness**, generated per declared `LiteralKind`: empty
  and very long strings, `Int64`/`Decimal` min/max, `Guid.Empty`,
  `DateTimeOffset.MinValue`/`MaxValue`, empty arrays. Asserts the predicate
  doesn't throw for anything outside its own documented fault conditions —
  this *is* the "boundary-value generator" idea raised earlier, folded in
  here rather than kept separate.
- **Schema conformance.** Every argument the schema declares is actually
  read (an unread declared argument is very likely an authoring mistake);
  no argument is read that isn't declared (already throws via `GetRaw`, but
  the harness reports it clearly instead of surfacing a raw
  `KeyNotFoundException` in a consumer's own test output).
- **Cancellation behavior**, reported rather than enforced: pass an
  already-canceled token and record whether the predicate honors it — not
  every predicate needs to be cancellation-aware, but an author should know
  which behavior theirs has.

Medium complexity (the boundary-value generator and the fluent API both need
real design), but high value: it directly targets the failure mode that's
hardest to catch by hand — silent behavior divergence in a predicate a
consumer wrote — and both of the previously-discussed fuzzing ideas become
components of this one coherent feature instead of separate, overlapping
asks.

### Public rule-equivalence check (BDD-based)

The single best effort-to-impact ratio on this entire list. A Reduced
Ordered BDD is a *canonical form* — for a fixed variable order, two
logically equivalent formulas produce the literally identical node graph.
This mechanism already exists and already powers constant/contradiction
detection; extending it to compare **two different rules** just means
building both into one shared `BddManager` instance with a shared variable
ordering and comparing root node ids. `RuleEquivalence.AreEquivalent(ruleA,
ruleB)` (or similar) is a small new public surface over an already-existing,
already-tested mechanism — genuinely differentiated capability (few
app-facing boolean-rule libraries expose this at all) for very little new
code.

### De Morgan's / negation-normal-form print mode

A purely local, always-safe, always-terminating rewrite
(`NOT(A AND B) == NOT A OR NOT B`), independent of the BDD machinery
entirely. Offered as a *display* option (a canonicalizing printer mode,
alongside `CanonicalPrinter`/the equation renderer), not an optimizer. Small,
low-risk, no NP-hardness anywhere near it.

## Do (sequenced) — real value, but with a dependency or open design question

### Source-generator predicate registration

Worth being precise about why this isn't the already-rejected idea: the
deferred doc rejects attribute-based registration for *runtime* assembly
scanning (reflection, breaks trimming/AOT, "magic"). A Roslyn **incremental
source generator** triggered by `[Predicate("hasRole")]` does its scanning
at *compile time* and emits ordinary generated C#
(`builder.Add<HasRole>().Add<IsManager>()...`) — zero runtime reflection,
fully AOT-safe. Could go further: a companion analyzer catching duplicate
predicate names across attributed types at `dotnet build` time, earlier than
today's runtime `.Add()`-time check. Real gotcha: badly-written incremental
generators are a classic IDE-slowdown source, so caching discipline is a
real design constraint, not an afterthought — that's the complexity driver,
not the core idea.

### Date/time: day-of-week/month/time-window with an explicit IANA timezone argument

The fragility here is specifically a **timezone** problem, not a "moving
now" problem — "moving now" is already solved by the `TimeProvider` pattern
`HasEarnedEnoughLoyaltyStamps` established, and per-evaluation memoization
already guarantees one consistent read per evaluation. But
`LiteralKind.DateTimeOffset` carries a numeric offset, not an IANA zone — a
fixed offset silently breaks across a DST transition if the author meant
"local business hours." Recommended shape: timezone as a plain `String`
rule-text argument (an IANA id), resolved via
`TimeZoneInfo.FindSystemTimeZoneById`, invalid id surfaces as a `Fault` —
exactly the `culture`-argument precedent `EqualsConfigurable` already
established. Sequenced because it needs real DST-transition test coverage
before shipping, not because the design is unclear.

### Joining compiled rules via an operator

Today only one tree is ever built from scratch; there's no first-class way
to combine two *already-compiled* rules (`ruleA AND ruleB`) without
round-tripping one into the other's JSON by hand. A `RuleBuilder.FromCompiled(CompiledRule<TContext>)`
bridge, splicing two compiled rules under a new operator and recompiling
once, is a real building block toward any future policy-combination story
(XACML's permit-overrides/deny-overrides algorithms are literally "join N
policies with a specific combinator" — see the deferred **Authorization
layer** item). Real subtlety, not hand-waved: the two source rules may have
been compiled against *different* registries, so a naive tree splice is
only sound if the destination registry is a superset of both, or the join
goes through a full re-`Validate`, not just structural surgery. Sequenced on
resolving that validation design, not on appetite.

### BDD-based "simplify my rule" re-synthesis

Once the equivalence check above ships and proves there's appetite for
BDD-exposed capabilities, the next real step is Shannon-expansion re-synthesis
— walking a rule's BDD back into a compact, correct, multi-level AND/OR/NOT
expression. This is a well-trodden hardware-logic-synthesis technique, not
sum-of-products minimality like Quine–McCluskey, and it's polynomial in the
BDD's own size. Two real complexity drivers: BDD size is variable-order
dependent (today's "order by first occurrence" is a simple heuristic, not
tuned — a bad order can blow up even a formula with a small good-order BDD),
and this must always be an opt-in, human-approved suggestion ("here's a
simpler equivalent form, review and accept") — never silent or automatic,
consistent with this repo's whole philosophy that structure is
author-controlled. Sequenced deliberately last among the BDD-related items.

## Not now — valid idea, deliberately deferred

### Unicode normalization (NFC/NFKC) support

A real correctness trap (two visually identical strings can be byte-different
Unicode sequences — same spirit as the Turkish-I problem already demonstrated
in tests), but normalization semantics are subtle and the audience is
narrow relative to the testing surface it would add. Worth documenting as a
known limitation on `StringPredicates`/`EqualsConfigurable` now; not worth
building yet.

## Don't do

### Date/time: calendar/holiday predicates baked into the engine

Holiday calendars and business-day definitions are themselves external,
versioned, jurisdiction-specific data — a rule referencing "is it a holiday"
implicitly depends on a data source that changes over time and by
jurisdiction. This is structurally identical to the already-documented
externally-resolved-value pattern (`IsPromoActive`/`IsWithinZoneLimit`). Any
attempt to bake calendar knowledge into a ready-made factory would either be
wrong somewhere or require shipping and maintaining calendar data as part of
this library, which is a different, much larger, and unrelated problem.
Reason to reject, not just defer: the correct answer already exists and is
already documented — this would be building a worse version of something
the library already supports.

### Quine–McCluskey-style guaranteed-minimal two-level minimization

NP-hard: prime-implicant generation and the covering step (itself set-cover)
both blow up exponentially well before realistic rule sizes. The BDD-based
re-synthesis approach above gives a genuine, tractable simplification path
using infrastructure that already exists; there's no realistic payoff to
also building an exact, exponential-worst-case algorithm alongside it.

### SMT/Z3 integration

This engine's predicates are deliberately **opaque booleans to the
compiler** — that's the entire foundation of CONTEXT.md's term-identity
philosophy. An SMT solver's extra reasoning power over the BDD approach
(arithmetic theories, uninterpreted functions) has nothing to attach to when
every predicate is an opaque black box by design; it can't reason about what
`hasCrust` *means* any more than the BDD manager can. Z3 specifically is
also a real, heavy native dependency — a concrete violation of "don't add
dependencies without a reason" for zero marginal capability over what the
existing BDD already provides.

### K-maps

A manual/visual technique for human use at small variable counts (roughly
≤4–6), not an algorithm — there's nothing here to automate or scale to real
rule sizes. Useful for teaching, not for a compiler pass.

## Related

- [`equation-rendering`](../equation-rendering/spec.md),
  [`diagram-rendering-options`](../diagram-rendering-options/spec.md) — the
  two display-focused write-ups from the same overall discussion; not
  re-scored here since they already have their own specs.
- [`deferred-features`](../deferred-features/spec.md) — the **Authorization
  layer** and **Partial evaluation / residual expressions** items referenced
  above as the larger context "joining compiled rules" and "rule
  equivalence" both feed into; not re-litigated here, just connected.
- [`predicate-catalog`](../predicate-catalog/issues/01-brainstorm-general-use-predicates-and-literal-kinds.md) —
  the existing brainstorm the "ready-made predicate factories" item above
  should be batched against rather than duplicated.
- [`src/TruthWeaver/Analysis/BddManager.cs`](../../src/TruthWeaver/Analysis/BddManager.cs) —
  the existing ROBDD manager underlying the equivalence-check and
  re-synthesis items.
- [`src/TruthWeaver.Testing/FakePredicates.cs`](../../src/TruthWeaver.Testing/FakePredicates.cs) —
  the existing testing package the predicate test harness is a sibling to,
  not a replacement for.
