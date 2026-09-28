# 01: Document the externally-resolved-value predicate pattern family in README.md

**What to build:** Add a new subsection to README.md's existing
[Predicate types](../../../README.md#predicate-types) section, immediately
after "n arguments, class-based, multiple injected dependencies" (the
`HasEnoughRecentApprovals` example), documenting a related but distinct
family: a class-based predicate where a rule-text literal argument and/or a
`TContext`-supplied value is a **key to be resolved**, not a value already
ready to use — a constructor-injected service performs the live resolution.

This is **not** a "relationship" pattern specifically — that's only one of
three shapes it covers, and the doc must not present it as the primary or
only shape. See `spec.md`'s Background for the full reasoning; do not
re-derive it independently or default back to a relationship-only framing.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] New subsection titled along the lines of "n arguments, class-based,
  externally-resolved value" (exact heading text at the agent's discretion,
  matching the voice of the surrounding subsections) — the heading must not
  say "relationship," since that's only one shape among three.
- [x] The doc presents, at minimum in prose (full code examples for all
  three aren't required if that would bloat the section — one full worked
  example plus two shorter illustrations is enough), all three shapes from
  `spec.md`:
  1. **Single-value, no comparison target** — the literal key resolves
     directly to the boolean answer (e.g. a feature-flag-style predicate);
     `TContext` may be unused entirely.
  2. **Single-sided value check** — one side (argument or context) is
     resolved live; the other is a plain value already on `TContext`,
     needing no resolution.
  3. **Two-sided comparison** — both a context anchor and the argument are
     independently resolved, then compared.
- [x] The full worked code example (whichever shape the agent picks as
  primary) must **not** frame `TContext` as "the current user" or otherwise
  imply either side is privileged as "the identity one" or that a
  relationship between two parties is required. A resource-scoped context
  with no user field at all is a safe choice, e.g.:

  ```csharp
  public sealed class IsWithinBudget(IBudgetLookupService budget) : IPredicate<PurchaseRequest>
  {
      public static PredicateSchema Schema =>
          new(
              "isWithinBudget",
              "Is Within Budget",
              "Is the request's amount within the live spending limit resolved for the given cost center code?",
              [new PredicateArgumentSchema("costCenterCode", "The cost center code to look up a live limit for.", LiteralKind.String)]);

      public async ValueTask<bool> EvaluateAsync(PurchaseRequest request, PredicateArguments args, CancellationToken ct)
      {
          string costCenterCode = args.GetString("costCenterCode");
          decimal limit = await budget.ResolveLimitAsync(costCenterCode, ct);
          return request.Amount <= limit;
      }
  }
  ```

  (Illustrative — the agent should verify this compiles against the current
  `IPredicate<TContext>`/`PredicateSchema`/`PredicateArgumentSchema` shapes
  before committing it, since those are read from source, not assumed.)
- [x] Explicitly states, in prose, that the rule-text argument is a **key**,
  not necessarily an identity, and that `TContext` participation is
  optional — some instances of this pattern don't read `TContext` at all.
- [x] Notes that term identity (per `CONTEXT.md#term-identity`) is
  unaffected by this pattern — the literal argument is still compared as an
  ordinary literal; nothing about "what it resolves to" enters term
  identity or memoization, and the predicate-author contract
  (`CONTEXT.md#the-predicate-author-contract`) still applies: same
  argument + same context within one evaluation must yield the same answer.
- [x] Cross-references `CONTEXT.md`'s `Deferred` table entry for
  "Context-bound term arguments" (updated by ticket 02) so a reader who
  lands on either doc finds the other.
- [x] Fits the existing README voice/structure (prose + fenced example(s),
  consistent with the sibling subsections); table of contents updated only
  if TOC entries exist for subsections at this heading level (check
  existing convention before adding).

## Comments

Added a new "n arguments, class-based, externally-resolved value" subsection
to README.md's Predicate types section, immediately after the
`HasEnoughRecentApprovals` example. It presents all three shapes from
`spec.md` with equal weight — a short `IsFeatureEnabled` illustration
(single-value, no comparison target), a full worked `IsWithinBudget`
example (single-sided value check, using the ticket's suggested
resource-scoped `PurchaseRequest` context with no user field), and a short
`IsManagedByCandidate` illustration (two-sided comparison, explicitly noting
neither `resource` nor the candidate argument is privileged as "the
identity one"). Followed by prose covering: the argument is a key not
necessarily an identity, `TContext` participation is optional, term
identity/the predicate-author contract are unaffected, and lookup failures
are absorbed as an ordinary `Fault`/`Unknown` per ADR-0001. Closes with a
cross-reference to `CONTEXT.md#deferred`'s "Context-bound term arguments"
entry (ticket 02 will wire the reverse link once it lands). No TOC entries
exist at this heading depth (sub-subsections under "Predicate types" aren't
listed in the TOC), so the TOC was left unchanged, matching existing
convention. Deliberately did not reference the ticket-07
`ResolvedValuePredicates` factory here since it doesn't exist yet at this
point in the ticket sequence; ticket 07 adds the cross-reference back into
this section when it lands.
