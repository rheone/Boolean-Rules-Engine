# Predicate catalog: K3 gap list

Gap list of the predicates named in the **Final Semantic Inventory** of
[`.scratch/2026-10-02-TODO.md`](../2026-10-02-TODO.md) against what
`TruthWeaver.Predicates` and `TruthWeaver.Testing` provide today. It is the hand-off for the
predicate-catalog track (see [issue 01](issues/01-brainstorm-general-use-predicates-and-literal-kinds.md))
from k3-conformance ticket 30. **No predicate is implemented by this document.**

## Method

Verified by reading the source on branch `StrongK3+Operations` (after k3-conformance tickets 01-29):

- `src/TruthWeaver.Predicates/StringPredicates.cs`, `RegexPredicates.cs`, `CollectionPredicates.cs`,
  `ResolvedValuePredicates.cs`, `PredicateResult.cs`
- `src/TruthWeaver.Testing/FakePredicates.cs`
- `src/TruthWeaver.Abstractions/{LiteralKind,PredicateArguments,IPredicate}.cs`
- [CONTEXT.md](../../CONTEXT.md), [ADR-0001](../../docs/adr/0001-kleene-failure-model.md),
  [ADR-0003](../../docs/adr/0003-rule-syntax-and-serialization.md),
  [ADR-0005](../../docs/adr/0005-strong-k3-language-surface.md)

Status vocabulary: **present** (same name, same meaning), **present-under-another-name**, **missing**.

## What the catalog provides today

Every catalog member is a selector-parameterized factory returning a tuple
`(PredicateSchema Schema, Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate)`.
The host supplies `Func<TContext, T?> selector` at registration; rule text supplies literal arguments.

| Member | Class | Selector | Arguments (`LiteralKind`) |
| --- | --- | --- | --- |
| `Equals` | `StringPredicates` | `string?` | `value` (`String`) |
| `EqualsIgnoreCase` | `StringPredicates` | `string?` | `value` (`String`) |
| `EqualsConfigurable` | `StringPredicates` | `string?` | `value` (`String`), `ignoreCase` (`Boolean`, default `true`), `culture` (`String`, default `""` = invariant), `trim` (`Boolean`, default `false`) |
| `StartsWith` | `StringPredicates` | `string?` | `value` (`String`) |
| `EndsWith` | `StringPredicates` | `string?` | `value` (`String`) |
| `Contains` | `StringPredicates` | `string?` | `value` (`String`) |
| `IsNullOrEmpty` | `StringPredicates` | `string?` | none |
| `Matches` | `RegexPredicates` | `string?` | `pattern` (`String`) |
| `SetEquals` | `CollectionPredicates` | `IReadOnlyCollection<string>?` | `values` (`StringArray`) |
| `Create<TContext, TResolved>` / `Create<TContext>` | `ResolvedValuePredicates` | host-defined | host-defined (plumbing, not a predicate) |

`TruthWeaver.Testing.FakePredicates` provides only test doubles (`Returning` with `bool` or `TruthValue`,
`Faulting`, `Scripted`); it contributes no inventory predicate.

### Conventions observed in the existing members

- **Return type.** The delegate type is already `ValueTask<TruthValue>`, so a predicate *can* return
  `Unknown`. In practice **no catalog member ever does**: all of them go through the internal
  `PredicateResult.FromBoolAsync(bool)`, so the catalog is effectively two-valued. `Unknown` appears only
  when a predicate faults (invalid regex, bad culture name) per ADR-0001.
- **Null selected value.** Every string and regex member returns `False` for a `null` selection;
  `SetEquals` treats a `null` collection as empty; `IsNullOrEmpty` returns `True` for `null` (its
  definition). Nothing returns `Unknown` for null. See [Open questions](#open-questions-for-the-repo-owner).
- **Comparison.** Ordinal and case-sensitive by default. `EqualsIgnoreCase` is ordinal-ignore-case. Only
  `EqualsConfigurable` is culture-sensitive (`CultureInfo.CompareInfo.Compare`).
- **Naming.** Members are named like BCL methods (`Equals`, `StartsWith`); the inventory uses `Equal`,
  `IsEmpty`, etc. The registered predicate `name` is chosen by the host at registration, so the member name
  is the factory name, not the rule-text name.
- **Literal kinds.** `LiteralKind` is the closed set `String`, `Int64`, `Decimal`, `Boolean`,
  `DateTimeOffset`, `Guid` plus the matching `*Array` forms. There is no `DateTime`, `TimeSpan`, `Uri` or
  "any value" kind.

## Summary

| Inventory section | Items | Present | Present under another name | Missing |
| --- | ---: | ---: | ---: | ---: |
| Primitive types | 10 | 0 | 0 | 10 |
| Numeric | 4 | 0 | 0 | 4 |
| String | 13 | 5 | 1 | 7 |
| Collection | 10 | 0 | 0 | 10 |
| DateTimeOffset | 8 | 0 | 0 | 8 |
| Type tests (`Predicate`) | 10 | 0 | 0 | 10 |
| **Total** | **55** | **5** | **1** | **49** |

Present members that are **not** in the inventory: `StringPredicates.EqualsIgnoreCase` (covered by the
inventory's `ignoreCase` option) and `CollectionPredicates.SetEquals` (no inventory counterpart; the
inventory's collection `In`/`Contains` are different operations).

## Primitive types

Inventory: `Equal`, `NotEqual`, `LessThan`, `GreaterThan`, `LessThanOrEqual`, `GreaterThanOrEqual`,
`IsDefault`, `IsNotDefault`, `IsNull`, `IsNotNull`.

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `Equal` | missing | none for `Int64`/`Decimal`/`Boolean`/`DateTimeOffset`/`Guid` (string equality is under [String](#string)) | Needs one thin public overload per scalar kind over a shared generic helper. `Decimal` vs `Int64` comparison semantics (cross-kind promotion) must be decided. |
| `NotEqual` | missing | none | As `Equal`. Null input: see open question 1. |
| `LessThan` | missing | none | Ordering uses `CompareTo` on the value type, no culture dimension. `Boolean` and `Guid` ordering is arguably meaningless; decide whether the ordering family is limited to `Int64`, `Decimal`, `DateTimeOffset`. |
| `GreaterThan` | missing | none | As `LessThan`. |
| `LessThanOrEqual` | missing | none | As `LessThan`. |
| `GreaterThanOrEqual` | missing | none | As `LessThan`. |
| `IsDefault` | missing | none | Needs a generic `TValue` selector and no literal argument, so no new `LiteralKind`. `default(T)` for a reference type is `null`, which overlaps `IsNull`; decide whether `IsDefault` is value-type-only. |
| `IsNotDefault` | missing | none | As `IsDefault`. |
| `IsNull` | missing | none (`StringPredicates.IsNullOrEmpty` is a different test) | Needs a generic nullable selector. Input being `null` is the *subject* of the test, so the result is `True`, never `Unknown`; this is the one family where the null convention inverts. |
| `IsNotNull` | missing | none | As `IsNull`. |

## Numeric

Inventory: `Between(value, n, k)`, `Outside(value, n, k)`, `In(value, candidate1, ...)`,
`NotIn(value, candidate1, ...)`.

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `Between` | missing | none | Arguments `n`, `k` as `Int64` or `Decimal`. Inclusive or exclusive bounds is undecided (issue 01 proposed inclusive `[min, max]`). Reversed bounds (`n > k`) need a defined result. |
| `Outside` | missing | none | Complement of `Between`; must be the K3 complement (`Unknown` stays `Unknown`), not a boolean negation, if null maps to `Unknown`. |
| `In` | missing | none | Candidates map to `Int64Array` / `DecimalArray`; the inventory's variadic form is one array argument in the predicate schema. |
| `NotIn` | missing | none | As `In`. K3 caveat: SQL-style `NOT IN` with a null candidate is `Unknown`; candidates here are literals and cannot be null, so there is no such case. |

## String

Inventory: each predicate "with Trim, Culture, ignoreCase".

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `Contains` | present | `StringPredicates.Contains` | Ordinal, case-sensitive, no `Trim`/`Culture`/`ignoreCase` arguments. Options are missing, not the predicate. |
| `EndsWith` | present | `StringPredicates.EndsWith` | As `Contains`. |
| `StartsWith` | present | `StringPredicates.StartsWith` | As `Contains`. |
| `Equal` | present-under-another-name | `StringPredicates.Equals` (exact), `EqualsIgnoreCase`, `EqualsConfigurable` | `EqualsConfigurable` is the only member with `ignoreCase`, `culture` and `trim` arguments (defaults `true`, invariant, `false`). It is culture-sensitive via `CompareInfo`, see open question 3. |
| `IsNullOrEmpty` | present | `StringPredicates.IsNullOrEmpty` | No arguments. `Trim` is meaningless here and `ignoreCase`/`Culture` do not apply. Returns `True` for `null`. |
| `Matches` | present | `RegexPredicates.Matches` | `pattern` only; `RegexOptions.None`, 1-second timeout, cached per pattern. `ignoreCase` would map to `RegexOptions.IgnoreCase` (a new optional `Boolean` argument); `Culture` maps to `CultureInvariant`; `Trim` is questionable for a regex. |
| `IsEmpty` | missing | none (`IsNullOrEmpty` includes null) | Non-null empty string. Null input: `False` or `Unknown`, see open question 1. |
| `IsNotNullOrEmpty` | missing | none | Negation of `IsNullOrEmpty`; returns `False` for `null`. |
| `IsNullOrWhiteSpace` | missing | none | `Trim` is implied. Listed in issue 01 as a candidate. |
| `IsNotNullOrWhiteSpace` | missing | none | Negation of `IsNullOrWhiteSpace`. |
| `NotContains` | missing | none | K3 complement of `Contains`. If null selects `False` for `Contains`, `NotContains` of null must not be `True` by accident; decide per open question 1. |
| `NotEqual` | missing | none | As `NotContains`, against `Equal`. |
| `NotMatches` | missing | none | As `NotContains`, against `Matches`. An invalid pattern still faults to `Unknown`. |

## Collection

Inventory: `IsEmpty`, `IsNotEmpty`, `Contains`, `In`, `NotIn`, `CountEqual`, `CountLessThan`,
`CountGreaterThan`, `CountLessThanOrEqual`, `CountGreaterThanOrEqual`.

Today only `CollectionPredicates.SetEquals` exists, over `IReadOnlyCollection<string>?`. Element type is
therefore `string` only; `Int64`/`Decimal`/`Guid` collections need generic or per-kind overloads.

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `IsEmpty` | missing | none | A `null` collection is treated as empty by `SetEquals`; `IsEmpty(null)` would be `True` by that convention. |
| `IsNotEmpty` | missing | none | As `IsEmpty`, so `IsNotEmpty(null)` is `False`. |
| `Contains` | missing | none | Collection contains the literal value: arguments `value` (`String`, or per-kind). Distinct from `StringPredicates.Contains`. |
| `In` | missing | none | **Ambiguous in the inventory**: it can mean "every element is in the candidate set" (subset) or "the selected scalar is in the candidate set". See open question 4. |
| `NotIn` | missing | none | As `In`. |
| `CountEqual` | missing | none | Argument `count` (`Int64`). Null collection counts as 0 (issue 01 convention) or yields `Unknown`. |
| `CountLessThan` | missing | none | As `CountEqual`. |
| `CountGreaterThan` | missing | none | As `CountEqual`. |
| `CountLessThanOrEqual` | missing | none | As `CountEqual`. |
| `CountGreaterThanOrEqual` | missing | none | As `CountEqual`. |

## DateTimeOffset

Inventory: `AfterNow`, `BeforeNow`, `After` and `Before` (each with `DateTimeOffset` and `DateTime`
arguments), `Between` (each with `DateTimeOffset` and `DateTime` arguments).

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `AfterNow` | missing | none | **Needs a time provider** (`TimeProvider`). CONTEXT.md says ambient state "is the predicate's problem, not the engine's", and issue 01 explicitly rejected catalog predicates that read the clock. The factory would take a `TimeProvider` parameter at registration, which resolves that objection only if the owner accepts a clock-reading catalog member. Also determines whether "now" is read once per evaluation (consistency within one `Evaluate`) or per term. |
| `BeforeNow` | missing | none | As `AfterNow`. |
| `After(value, DateTimeOffset)` | missing | none | Argument kind `DateTimeOffset` already exists. Strict `>`. |
| `After(value, DateTime)` | missing | none | **Needs a new `LiteralKind`** (`DateTime`) or a documented conversion. `DateTime` has no offset and a `Kind` of `Utc`/`Local`/`Unspecified`, so conversion to `DateTimeOffset` is host-timezone-dependent for `Local`/`Unspecified`. Adding a `LiteralKind` is a closed-set extension (ADR-0003, CONTEXT.md) and a breaking change for exhaustive switches. Recommend not adding it: accept the `DateTimeOffset` literal and let the host convert. |
| `Before(value, DateTimeOffset)` | missing | none | As `After`. |
| `Before(value, DateTime)` | missing | none | As `After(value, DateTime)`. |
| `Between(value, DateTimeOffset, DateTimeOffset)` | missing | none | Inclusive or exclusive bounds undecided; reversed bounds need a defined result. |
| `Between(value, DateTime, DateTime)` | missing | none | As `After(value, DateTime)`. |

Overload by argument type is not expressible in a single predicate schema (one name, one schema), so the
`DateTime` overloads are either separate predicate names or are dropped in favour of the `DateTimeOffset`
form.

## Type tests (`Predicate` group)

Inventory: `IsGuid`, `IsNotGuid`, `IsNumeric`, `IsNotNumeric`, `IsUrl`, `IsNotUrl`, `IsString`, `IsNotString`,
`IsDateTimeOffset`, `IsNotDateTimeOffset`.

These imply a selector returning `object?` (or `string?` for the parse-based tests). No catalog member
takes an `object?` selector today.

| Predicate | Status | Existing member | Notes |
| --- | --- | --- | --- |
| `IsGuid` | missing | none | `string?` selector: `Guid.TryParse`; `object?` selector: also accepts a `Guid` instance. Format specifiers accepted by `TryParse` (braces, no hyphens) need a decision. |
| `IsNotGuid` | missing | none | K3 complement of `IsGuid`. |
| `IsNumeric` | missing | none | Parse test must use `CultureInfo.InvariantCulture` to honour the no-culture-sensitive rule; define `NumberStyles` (integers only, decimals, exponent, thousands separators). Also decide whether a numeric-typed `object` counts. |
| `IsNotNumeric` | missing | none | K3 complement of `IsNumeric`. |
| `IsUrl` | missing | none | Needs a definition: `Uri.TryCreate(..., UriKind.Absolute)`, restricted to `http`/`https`, or any scheme. |
| `IsNotUrl` | missing | none | K3 complement of `IsUrl`. |
| `IsString` | missing | none | Only meaningful for an `object?` selector (is the runtime type `string`). With a `string?` selector it degenerates to a null test. |
| `IsNotString` | missing | none | K3 complement of `IsString`. |
| `IsDateTimeOffset` | missing | none | `string?` selector: `DateTimeOffset.TryParse` with `InvariantCulture` and a defined `DateTimeStyles`/format (ISO 8601 only is the safest); `object?` selector: runtime type test. |
| `IsNotDateTimeOffset` | missing | none | K3 complement of `IsDateTimeOffset`. |

## Open questions for the repo owner

1. **Null input: `False` or `Unknown`?** The existing convention (every catalog XML doc and issue 01) is
   "null selected value is `False`, never a fault". Now that predicates return `TruthValue`, a missing
   value is the textbook K3 case for `Unknown` (`NULL > 5` is unknown, not false). The consequence is
   concrete: with `False`, `NOT GreaterThan(x, 5)` is `True` for a null `x`; with `Unknown` it stays
   `Unknown` and `IsSatisfied` is fail-closed. This decision also determines whether `NotX` predicates may
   be implemented as a K3 `NOT` of their positive form. Recommendation: `Unknown` for comparison, range and
   count predicates; keep `True`/`False` for the null tests themselves (`IsNull`, `IsNullOrEmpty`,
   `IsNullOrWhiteSpace`); leave the existing string members as they are unless the owner wants consistency
   (changing them is a behaviour change for current consumers).
2. **`NotX` predicates.** Whether each negated predicate is a first-class registered predicate (as the
   inventory lists) or is left to `NOT` in the rule. If first-class, define them as the K3 complement
   (`Unknown` maps to `Unknown`).
3. **Culture.** The inventory asks for a `Culture` option on every string predicate, but the catalog's
   stated stance (issue 01, `StringPredicates` XML docs) is "ordinal or invariant only, never
   culture-sensitive", and `EqualsConfigurable` already breaks it by accepting an arbitrary culture name
   (it also faults on an unknown name). Note that CONTEXT.md does not itself state a culture rule; the rule
   lives in issue 01 and the predicate XML docs, so the "CONTEXT.md no-culture-sensitive-comparison rule"
   is not actually recorded there. Options: (a) restrict `Culture` to `""` (invariant) and the ordinal
   modes, (b) allow named cultures as `EqualsConfigurable` does and document the exception, (c) drop
   `Culture` from the other predicates. Recommendation: (a), and record the rule in CONTEXT.md.
4. **Collection `In`/`NotIn` meaning.** Subset ("all elements are candidates"), intersection
   ("any element is a candidate") or scalar membership. Needs a one-line definition before implementation.
5. **`DateTime` arguments.** Add a `DateTime` `LiteralKind` (breaking, closed set) or accept only
   `DateTimeOffset`. Recommendation: accept only `DateTimeOffset`.
6. **Clock predicates.** Whether `AfterNow`/`BeforeNow` belong in the shipped catalog (with a
   `TimeProvider` registration parameter) or stay host-authored, as issue 01 and CONTEXT.md currently say.
7. **Bounds.** Inclusive vs exclusive for `Between`/`Outside` (numeric and date) and the result for
   reversed bounds.
8. **Selector shapes.** Generic `Func<TContext, TValue?>` factories with per-kind overloads versus
   per-kind classes (`NumericPredicates`, `DateTimePredicates`, `TypePredicates`); issue 01 proposed the
   latter for numeric and date/time.

## Suggested grouping for follow-on tickets

Smallest independent slices, in a sensible order (each is a catalog addition, with no engine change):

1. String completions: `IsEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace`, `IsNotNullOrWhiteSpace`,
   `NotContains`, `NotEqual`, `NotMatches` (blocked by questions 1-3).
2. Numeric and primitive comparison family (blocked by questions 1, 7).
3. Collection family (blocked by questions 1, 4).
4. Date/time comparison without clock predicates (blocked by questions 5, 7); clock predicates after 6.
5. Type tests (blocked by question 8 and the definitions of numeric/URL).
