# 30: Predicate catalog gap list

**What to build:** A written gap list of predicates in the spec's Final Semantic Inventory that do not yet exist in TruthWeaver.Predicates, handed to the predicate-catalog track. No predicates are implemented here.

**Blocked by:** 02

**Status:** done

- [x] Every inventory predicate is marked present or missing
- [x] Gap list is linked from the predicate-catalog track
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Delivered as a document only; no predicates were implemented and no C# changed.

- Gap list: [`.scratch/predicate-catalog/k3-gap-list.md`](../../predicate-catalog/k3-gap-list.md), linked from the new
  [`.scratch/predicate-catalog/README.md`](../../predicate-catalog/README.md).
- Result: of the 55 inventory predicates, 5 are present (`Contains`, `EndsWith`, `StartsWith`, `IsNullOrEmpty`,
  `Matches`), 1 is present under another name (`Equal` as `Equals`/`EqualsIgnoreCase`/`EqualsConfigurable`) and 49 are
  missing. Primitive, numeric, collection (except `SetEquals`, not in the inventory), DateTimeOffset and type-test
  groups have nothing today.
- Findings: the catalog is effectively two-valued (every member goes through `PredicateResult.FromBoolAsync`, none
  returns `Unknown`); only `EqualsConfigurable` offers `Trim`/`Culture`/`ignoreCase` and it is culture-sensitive;
  CONTEXT.md does not actually record a no-culture-sensitive-comparison rule (it lives in predicate-catalog issue 01 and
  XML docs). Eight open questions for the owner are listed in the document (null as `False` vs `Unknown`, `Culture`,
  collection `In` meaning, `DateTime` literal kind, clock predicates, bounds, and others).
- Validation boxes: docs-only ticket, so the build/test gates are satisfied by the unchanged code (checked at the end of
  the documentation sweep, ticket 31); "built test-first" does not apply.
