# 01: Brainstorm value-added general-use predicates and LiteralKind types

**What to build:** A written proposal (not an implementation) surveying candidate additions to `TruthWeaver.Predicates` (currently `StringPredicates`, `RegexPredicates`, `CollectionPredicates`) and candidate additions to `LiteralKind` (`src/TruthWeaver.Abstractions/LiteralKind.cs`, currently string/Int64/Decimal/Boolean/DateTimeOffset/Guid + array forms). Each candidate predicate/kind must be justified as genuinely reusable across hosts, not something a consumer would obviously write as its own named predicate against its own `TContext` per ADR-0003's "a predicate needing a context value is authored as a distinct predicate" stance.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] Candidate predicates are listed per existing category (string, collection, regex) plus any new category proposed (e.g. numeric comparison, date/time comparison), each with: proposed name, argument schema, one-sentence semantics, and why it's general-use rather than host-specific.
- [ ] Candidate `LiteralKind` additions (if any) are listed with justification — note that `LiteralKind` is a closed set per ADR-0003/CONTEXT.md, so any addition is flagged as a deliberate extension of that closed set, not a casual one.
- [ ] Each candidate is checked against `CONTEXT.md`'s existing rules (case-sensitivity default, no culture-sensitive comparison, null-selected-value-is-false-not-fault convention used by `StringPredicates`/`CollectionPredicates`/`RegexPredicates`) for consistency.
- [ ] The proposal explicitly calls out any candidate that would duplicate something better solved by [[context-bound-term-arguments]] instead of a new predicate, so the two efforts don't converge on overlapping solutions independently.
- [ ] No production code is changed by this ticket — output is the written proposal, to be turned into follow-on implementation tickets after review.

## Comments

### Method

Surveyed `src/TruthWeaver.Predicates/{StringPredicates,RegexPredicates,CollectionPredicates}.cs`,
`src/TruthWeaver.Abstractions/{LiteralKind,PredicateArguments}.cs`, `CONTEXT.md`, and ADR-0003
(the "argument values are literals only... a rule needing something like 'the resource's owner
id' defines a predicate that reaches into its own `TContext`" stance, and the closed-set literal
list). Every candidate below is a **selector-parameterized factory** in the existing style —
`Func<TContext, T?> selector` supplied at registration, one or more literal arguments supplied in
rule text — never a predicate that itself reaches into a specific host's domain model. That
selector-factory shape is exactly what makes a candidate "general-use": the factory is
host-agnostic generic code: the host supplies *which* value to read via the selector, the factory
supplies *how* to compare it. A predicate is host-specific, not a catalog candidate, when the
comparison logic itself only makes sense for one host's domain concept (e.g. "is a manager of
resource X" — that's ADR-0003's `IsManagerOfResourceOwner` case, not a catalog case).

Existing conventions confirmed and applied uniformly below:

- **Case sensitivity default:** ordinal, case-**sensitive** comparison is the default (`Equals`,
  `Contains`, etc.); a case-insensitive variant exists only where a plain ordinal-ignore-case
  reading is meaningful (`EqualsIgnoreCase`). No candidate below introduces `StringComparison`
  values beyond `Ordinal`/`OrdinalIgnoreCase`.
- **No culture-sensitive comparison:** every string/numeric/date comparison uses ordinal or
  invariant comparison only (`CompareTo`, `Ordinal`), never `CurrentCulture`. Numeric and
  date/time comparisons have no culture dimension in their comparison logic (they compare
  underlying `long`/`decimal`/`DateTimeOffset` values, not formatted strings), so this convention
  transfers cleanly to the new categories.
- **Null-selected-value-is-false-not-fault:** every candidate below treats a `null` selector
  result as a normal `false` (or, for collection candidates, an empty collection), consistent
  with `StringPredicates`/`CollectionPredicates`/`RegexPredicates`. None of them throw or fault on
  a null selected value — only a malformed *argument* (e.g. an invalid regex pattern, already the
  precedent in `RegexPredicates.Matches`) is allowed to surface as an evaluation-time fault.

### Candidate predicates — existing category: string (`StringPredicates`)

| Proposed name | Argument schema | Semantics (one sentence) | Why general-use |
| --- | --- | --- | --- |
| `IsNullOrWhiteSpace` | none | True when the selected string is null, empty, or consists only of whitespace. | Every host has "blank" text fields; this is a strict superset of the existing `IsNullOrEmpty` and equally host-agnostic. |
| `ContainsIgnoreCase` | `value: string` | True when the selected string contains the argument as a substring, ordinal case-insensitive. | Mirrors the existing `Equals`/`EqualsIgnoreCase` pairing — `Contains` currently has no case-insensitive sibling, an inconsistency worth closing. |
| `StartsWithIgnoreCase` / `EndsWithIgnoreCase` | `value: string` | Same as `StartsWith`/`EndsWith`, ordinal case-insensitive. | Same pairing gap as `ContainsIgnoreCase`. |
| `LengthEquals` / `LengthGreaterThan` / `LengthLessThan` | `length: Int64` | True when the selected string's `.Length` compares as named against the integer argument. | Field-length gating ("code must be exactly 6 chars", "comment under 500 chars") is a cross-host authoring need, not a domain concept. |
| `IsOneOf` (string) | `values: StringArray` | True when the selected string exactly equals (ordinal) any element of the argument array. | Generalizes `Equals` from one comparison target to a set of allowed values — a very common "is this an allowed status/code" shape that would otherwise force authors to write `OR(Equals(...), Equals(...), ...)` chains. |

### Candidate predicates — existing category: collection (`CollectionPredicates`)

| Proposed name | Argument schema | Semantics | Why general-use |
| --- | --- | --- | --- |
| `Contains` (collection) | `value: string` | True when the selected collection contains the single argument value (ordinal), treating a null selected collection as empty. | The existing `SetEquals` only checks whole-set equality; membership-of-one is a distinct and more common need ("does the user have role X", not "does the user have exactly this role set"). |
| `IsSubsetOf` / `IsSupersetOf` | `values: StringArray` | True when the selected collection's distinct elements are a (non-strict) subset/superset of the argument array, ordinal comparison. | Natural set-relationship generalizations alongside the existing `SetEquals`; same order-insensitive, duplicate-insensitive reasoning already documented on `SetEquals`. |
| `CountEquals` / `CountGreaterThan` / `CountLessThan` | `count: Int64` | True when the selected collection's element count compares as named against the integer argument, treating null as count 0. | "Has at least N roles/tags" is a common cross-host authoring shape distinct from any particular element's identity. |
| `IsEmpty` | none | True when the selected collection is null or has zero elements. | Complement to the count family; reads naturally standalone and avoids an awkward `CountEquals(count: 0)` spelling. |

### New category: numeric comparison (`NumericPredicates`)

Neither `StringPredicates` nor `CollectionPredicates` covers plain numeric comparison today —
`Decimal`/`Int64` literal kinds exist in `LiteralKind` but nothing in the predicate catalog
consumes them yet. This is the most clearly missing general-use category.

| Proposed name | Argument schema | Semantics | Why general-use |
| --- | --- | --- | --- |
| `EqualTo` (decimal/int64, two overloads or a shared generic helper) | `value: Decimal` (or `Int64`) | True when the selected numeric value equals the argument exactly. | Numeric equality ("age == 18", "score == 100") is domain-free comparison logic. |
| `GreaterThan` / `GreaterThanOrEqualTo` / `LessThan` / `LessThanOrEqualTo` | `value: Decimal` (or `Int64`) | True when the selected numeric value compares as named against the argument, using `CompareTo` (no culture dimension). | Threshold gating ("balance >= 100", "age > 21") is the single most common rule-authoring shape outside pure string/collection matching, and appears identically across every host domain. |
| `IsBetween` | `min: Decimal, max: Decimal` (inclusive) | True when the selected numeric value falls within `[min, max]`. | Range gating is common enough (age bands, score bands, date-adjacent numeric windows) to warrant a single predicate over an `AND(GreaterThanOrEqualTo, LessThanOrEqualTo)` pair, though both remain valid — analogous to the operator-family equivalences CONTEXT.md documents without eliminating either spelling. |

A null selected numeric value cannot be "false" the way a null string can (there's no natural
"is 5 greater than null" reading) — the convention this proposal recommends is that a
**null selected numeric value evaluates to `false` for every comparison in this family**,
mirroring the `StringPredicates` null convention exactly rather than inventing a new rule; this
should be stated explicitly in each predicate's XML doc when implemented, the same way
`RegexPredicates.Matches` states its null convention.

### New category: date/time comparison (`DateTimePredicates`)

`DateTimeOffset` is already a `LiteralKind`; nothing consumes it yet either.

| Proposed name | Argument schema | Semantics | Why general-use |
| --- | --- | --- | --- |
| `IsBefore` / `IsAfter` | `value: DateTimeOffset` | True when the selected `DateTimeOffset` is strictly before/after the argument. | Basic temporal ordering ("expires before X", "created after X") is domain-free. |
| `IsBetween` (date/time) | `start: DateTimeOffset, end: DateTimeOffset` (inclusive) | True when the selected value falls within `[start, end]`. | Window/range gating (subscription windows, business-hours-adjacent checks — though see caveat below on "is it currently business hours", which is host-specific) is common. |

**Explicitly rejected from this category:** `IsToday`, `IsInThePast`, `IsInTheFuture`, or any
predicate that reads the *current* time internally. CONTEXT.md's ambient-state section already
names `IsToday` as "just a predicate that happens to read `TimeProvider` internally" — i.e. a
host-authored predicate, not a catalog candidate, because "now" is not an argument the rule text
supplies; it is ambient state a specific host chooses to read via its own selector/predicate
composition. A catalog `DateTimePredicates` factory only ever compares two values it is handed
(one via selector, one via literal argument) — it must not read `TimeProvider` itself, or it stops
being a reusable, host-agnostic comparison and becomes exactly the kind of ambient-state predicate
CONTEXT.md says belongs to the predicate author, not the engine's shipped catalog.

### New category: numeric/collection crossover — explicitly NOT proposed

Considered and rejected: a generic `IsOneOf` for `Int64`/`Decimal`/`Guid` mirroring the string
`IsOneOf` above. Deferred out of this proposal only because it multiplies the same shape across
every scalar `LiteralKind` (5 argument-type variants) with no new semantics — worth doing as a
follow-on implementation-ticket detail (one generic helper, multiple thin public overloads,
matching the existing `StringPredicates.Create` private-helper pattern) rather than a distinct
catalog decision here.

### Candidate `LiteralKind` additions

**None are proposed.** Every candidate predicate above type-checks entirely against the six
existing scalar kinds (`String`, `Int64`, `Decimal`, `Boolean`, `DateTimeOffset`, `Guid`) and their
array forms — numeric comparison uses `Int64`/`Decimal`, date comparison uses `DateTimeOffset`,
`IsOneOf`/`IsSubsetOf`/etc. use the existing array kinds. No candidate here needs a new literal
shape (no floating-point/`double`, no `TimeSpan`, no `Uri`, no raw `DateTime` distinct from
`DateTimeOffset`).

Per the ticket's instruction to flag any addition as a *deliberate* extension of a closed set
(ADR-0003, CONTEXT.md): if a future ticket does propose one, `TimeSpan` (for "selected duration
compares against argument duration," e.g. SLA-window predicates) is the most plausible candidate
this survey surfaced, since `DateTimeOffset` alone can't express a duration comparison cleanly.
It is explicitly **not** proposed here — flagging it only so a future author doesn't have to
re-derive it from scratch, and so it's clear this proposal considered and declined to add it now
(no concrete predicate in this catalog needs it yet; adding a `LiteralKind` case is a breaking
enum change for every exhaustive `switch` over it, so it should wait for a concrete consumer).

### Convention compliance summary

| Candidate | Case-sensitivity default | Culture-sensitivity | Null-selected-value convention |
| --- | --- | --- | --- |
| `IsNullOrWhiteSpace` | n/a | n/a | selected value itself defines "null" case; no separate null handling needed |
| `ContainsIgnoreCase`/`StartsWithIgnoreCase`/`EndsWithIgnoreCase` | case-insensitive (named, paired with existing case-sensitive default) | ordinal only | null selected → false |
| `LengthEquals`/`GreaterThan`/`LessThan` (string) | n/a (length is not case-bearing) | n/a | null selected → false (length of null treated as not matching, not as length 0, to stay consistent with "null is false" rather than "null coerces to a zero value") |
| `IsOneOf` (string) | case-sensitive (ordinal), matching `Equals`'s default | ordinal only | null selected → false |
| `Contains`/`IsSubsetOf`/`IsSupersetOf`/`CountEquals`/`IsEmpty` (collection) | case-sensitive (ordinal), matching `SetEquals`'s existing stance | ordinal only | null selected collection → treated as empty, matching `SetEquals`'s existing documented convention |
| Numeric comparison family | n/a (no case dimension) | none (uses `CompareTo` on the underlying value type, never string formatting) | null selected → false |
| Date/time comparison family | n/a | none (`DateTimeOffset.CompareTo`, no timezone-normalization opinions beyond what `DateTimeOffset` already guarantees) | null selected → false |

### Overlap with [[context-bound-term-arguments]]

None of the candidates above overlap with that effort — every candidate here compares a
**selector-read context value against a literal rule-text argument**, which is exactly the
pattern ADR-0003 already endorses and which context-bound-term-arguments is not trying to change.
context-bound-term-arguments is about the *other* direction: letting a rule-text argument itself
reference a path into `TContext` (e.g. `IsManagerOf({{resource.PersonId}})`) instead of a literal,
which is a fundamentally different argument-*source* question, not an argument-*comparison*
question. Nothing in this catalog proposal introduces or requires path-expression argument
syntax, so there is no duplicated design surface between the two tickets.

One indirect connection worth flagging for that ticket's "narrower alternative" evaluation: the
`IsManagerOf`/`IsOwnerOf`/`IsDelegateOf` examples in the context-bound-term-arguments ticket are
themselves already ADR-0003-compliant as **host-authored, selector-parameterized predicates** —
e.g. a host could register `RelationshipPredicates.IsManagerOf<TContext>(name, selector: ctx =>
ctx.CurrentUserId, ...)` today, comparing the selected id against a literal argument, with no
context-bound-argument feature needed at all. That said, this proposal does **not** add a
`RelationshipPredicates` category to the catalog itself, because "manager of," "owner of," and
"delegate of" are relationship concepts specific to each host's domain model (what a "resource"
is, what "ownership" means) — exactly the ADR-0003 line this ticket must respect ("a predicate
needing a context value is authored as a distinct predicate," i.e. by the host, not shipped
generically). This observation is offered as context for the context-bound-term-arguments ticket's
"remain deferred" recommendation, not as a new catalog candidate.
