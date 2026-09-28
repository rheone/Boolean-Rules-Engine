# 01: Investigate and plan context-bound term arguments

**What to build:** A written investigation and design proposal (not an implementation) for context-bound term arguments — e.g. `IsManagerOf({{resource.PersonId}})`, `IsOwnerOf({{resource.ResourceId}})`, `IsDelegateOf({{resource.Manager}})` — a path-expression syntax that lets a rule-text argument reference a value on `TContext` instead of only a closed-set literal.

This is a named, already-deferred item: `CONTEXT.md`'s [Deferred](../../../CONTEXT.md#deferred) table lists "Context-bound term arguments" with the same `IsManagerOf({{resource.ownerId}})`-shaped example, and ADR-0003's ["String DSL — canonical form"](../../../docs/adr/0003-rule-syntax-and-serialization.md) section explains why v1 rejected it: (a) it requires a real typed path-expression grammar to do safely, and (b) it breaks static, structural canonical rule-to-rule equality (used by the analyzer for constant/contradiction detection, per `CONTEXT.md#term-identity`), since two "same shaped" rules would no longer be comparable without also evaluating what their path expressions resolve to. This ticket's job is to investigate whether/how those two objections can be addressed, not to relitigate whether the feature is desirable.

**Blocked by:** `predicate-catalog` ticket 01 (its "well-known context values as distinct named predicates" survey is a direct input to this ticket's narrower-alternative recommendation)

**Status:** done

- [ ] Document how a path-expression grammar would be scoped (e.g. dotted-path only vs. indexers, what `TContext` shapes it can bind against, how a mistyped path is reported — compile-time vs. evaluation-time fault).
- [ ] Document the canonical-equality impact directly: does two terms with the same predicate name and same path-expression argument text count as the same term (structural equality on the path text, not on its resolved value), and does this actually preserve `CONTEXT.md#term-identity`'s guarantees or only partially?
- [ ] Document the interaction with existing closed-set `LiteralKind` arguments — is a context-bound argument a new `PredicateArgumentSchema` shape alongside literal args, or a distinct argument kind entirely?
- [ ] Document the interaction with the DSL/JSON/YAML tri-format guarantee (ADR-0003's `parse(print(x))` round-trip) — a path expression must round-trip identically across all three surfaces.
- [ ] Recommend one of: proceed with a scoped design, proceed with a narrower alternative (e.g. a small enumerated set of "well-known" context paths rather than a general grammar), or remain deferred — with the reasoning for whichever is chosen.
- [ ] Read `predicate-catalog` ticket 01's output before recommending — if it already proposes named predicates covering the common context-value cases this ticket's examples target (`IsManagerOf`, `IsOwnerOf`, `IsDelegateOf`), that's evidence toward "remain deferred" rather than duplicating effort with a new grammar.
- [ ] No production code is changed by this ticket — output is the written investigation, to become an ADR amendment or a new ADR only after review.

## Comments

### Method

Read `CONTEXT.md`'s Deferred table entry and `#term-identity` section, ADR-0003 in full
(particularly "String DSL — canonical form" and its rejection rationale), `predicate-catalog`
ticket 01's full `## Comments` section (already `done`), and `src/TruthWeaver.Abstractions`'s
`LiteralKind`, `LiteralValue`, `PredicateArgumentSchema`, and `PredicateSchema`. No production
code was touched.

### 1. How a path-expression grammar would be scoped

If this were built, the minimal viable scope is:

- **Dotted-path only, no indexers, no method calls.** `resource.PersonId`, `resource.Manager.Id`.
  No `resource.Tags[0]`, no `resource.Tags.Where(...)`. Indexers reopen exactly the "real
  sub-language" problem ADR-0003 warns about (now you need array-bounds semantics, a fault path
  for out-of-range, and a decision about whether the index itself can be dynamic) for a feature
  that's supposed to stay a thin, closed grammar. A rule author needing collection indexing
  already has `CollectionPredicates`/the numeric family from the predicate-catalog proposal, or a
  host-authored predicate — there is no case in the ticket's own examples (`resource.PersonId`,
  `resource.ResourceId`, `resource.Manager`) that needs anything past member access.
- **Binds against `TContext`'s public instance property graph only**, resolved via reflection or
  (more likely, for AOT/perf parity with the rest of the compiled expression tree) a compiled
  `Expression<Func<TContext, object?>>` built once at predicate-registration/compile time, not
  re-parsed per evaluation. This mirrors the existing selector-factory pattern
  (`Func<TContext, T?> selector`) the predicate-catalog ticket found already covers the ticket's
  worked examples — a path expression is, structurally, just a textual encoding of the same
  selector a host would otherwise write in C#.
- **Fault reporting must be split at two different times, and this is the crux of the scoping
  problem:**
  - *Compile-time*, only what's checkable without an instance: syntax validity (is it a legal
    dotted identifier chain), and, if `TContext`'s shape is statically known to the registry at
    registration time (it is — `PredicateRegistry<TContext>` is generic over `TContext`), that
    each path segment names a real public property and the leaf type matches the argument's
    declared `LiteralKind`. This is genuinely achievable — reflection over `typeof(TContext)` at
    schema-registration time can validate the whole path chain before any rule referencing it is
    ever compiled.
  - *Evaluation-time, unavoidably*: a null anywhere in the middle of the chain
    (`resource.Manager` is null, so `.Id` cannot be read). CONTEXT.md's Kleene fault model
    (`#failure-model`) already has a home for this — a null-chain read becomes a `Fault`/`Unknown`
    for that term, exactly like any other predicate exception — but it means a context-bound
    argument is the **first argument-level construct that can fault purely from argument
    resolution**, before the predicate body even runs. Every other argument today is a literal:
    argument resolution cannot fail once compilation succeeds. This is a new failure surface, not
    just a new syntax, and needs its own line in the failure model's documentation, not a silent
    fold into "predicates can fault."

### 2. Canonical-equality impact — does path-text structural equality preserve term-identity?

**Only partially, and the gap is not cosmetic.** CONTEXT.md's term-identity rule is: two terms are
the same variable iff predicate name + arguments (sorted by name, compared by exact
type-normalized *value*) match. The entire point of that rule, per ADR-0003's Consequences
section, is that canonical equality is "a pure structural/value comparison with no evaluation
semantics entangled in it" — which is what lets the BDD-based analyzer detect constant and
contradictory rules by comparing terms *without running any predicate*.

A path expression breaks the "value" side of that comparison in a specific way:

- Structural equality on path *text* (`resource.PersonId` == `resource.PersonId`) is sound and
  cheap — two terms with identical predicate name and identical path text are provably the same
  term, because the text has no free variables of its own. This half of term identity survives.
- But the analyzer's job is not just "are these two terms the same" — it's "are these two terms
  the same *variable*, i.e. does evaluating them against the same context instance twice yield the
  same answer both times." For a literal argument, path text and resolved value are the same thing
  by construction (`role: "Y"` always means the string `"Y"`). For a context-bound argument,
  `IsManagerOf({{resource.PersonId}})` compared against `IsManagerOf({{resource.OwnerId}})` are
  **structurally different terms** (different path text) even though they may resolve to the
  *same* value on a given context instance, and are **structurally identical terms** even though
  they resolve to *different* values across two different context instances (two evaluations of
  the same compiled rule against two different requests). Term identity as CONTEXT.md defines it
  is inherently about "same term, evaluated once against one context" — memoization within a
  single evaluation pass is unaffected (path text is still stable input to the resolver within one
  pass). But the analyzer's constant/contradiction detection is a **cross-evaluation, context-free**
  static claim ("this rule is always true regardless of input") and that claim becomes false the
  moment a term's effective value depends on which context instance is supplied. Concretely: `AND(
  IsManagerOf({{resource.PersonId}}), NOT(IsManagerOf({{resource.PersonId}})) )` is still
  correctly flagged as a contradiction (same path text on both sides), but
  `AND(IsManagerOf({{resource.PersonId}}), NOT(IsManagerOf({{resource.OwnerId}})))` is *not* a
  contradiction under structural equality even though it might be one for every context instance a
  particular host ever supplies (if `PersonId` and `OwnerId` always happen to be equal for that
  host's data) — the analyzer simply has no way to know that, and correctly doesn't claim it.
  That's a correct, sound restriction (no false positives), so the "partial" preservation is on the
  safe side — but it does mean context-bound arguments shrink the analyzer's detection power
  exactly at the terms that use them, silently and per-term, compared to the current all-literal
  world where every term's value is knowable at compile time.

### 3. Interaction with existing closed-set `LiteralKind` arguments

A distinct argument kind, not a new `PredicateArgumentSchema.Type` value. `LiteralKind` is
documented as "the closed set of literal argument-value types" — a context-bound argument is not a
value at all until evaluation, so folding it into `LiteralKind` would be a category error (every
existing `LiteralKind` consumer, e.g. `LiteralValue`'s `Kind`-switch, `RuleCompiler` argument
validation, JSON/YAML serializers, would need an "is this actually resolved yet" branch it
currently never needs). The clean shape is a **sibling argument-value kind at the AST level**,
something like `RuleArgument = Literal(LiteralValue) | ContextPath(string path, LiteralKind
expectedType)`, with `PredicateArgumentSchema.Type` still declaring the expected `LiteralKind` the
path must resolve to (compile-time-checkable per point 1), but the *argument value itself* in a
compiled term becomes a small discriminated union instead of always being a `LiteralValue`. This
is a real, if contained, change to every place that currently assumes "a term's arguments are all
`LiteralValue`" — the compiler's argument-schema validator, the canonical printer, the JSON/YAML
node shape, and the evaluator's argument-resolution step (which currently has nothing to do at
all, since literals need no resolution) all gain a second case.

### 4. Interaction with the DSL/JSON/YAML tri-format round-trip guarantee

Achievable, but not free. `parse(print(x))` structural equality only requires that the *tree*
(now including the new `ContextPath` argument-value case) round-trips — it does not require
resolving the path, so this guarantee is orthogonal to the term-identity concern in point 2. The
concrete surfaces needing a change, each already enumerated by ADR-0003's existing DSL/JSON/YAML
triad:

- **DSL**: needs a distinguishable literal-vs-path syntax inside an argument position, e.g. the
  ticket's own `{{resource.PersonId}}` handlebar convention read literally — that's a new lexical
  token the DSL grammar and printer must both handle, on top of the existing quoted-string/
  number/bool/array literal forms. The canonical printer must always emit that same delimited
  form, deterministically, for round-trip stability (no ambiguity with a string literal that
  happens to contain literal `{{`/`}}` text — would likely need to reserve or escape that
  sequence in string literals, a small but real breaking change to string-literal escaping rules,
  which is adjacent to the still-open `dsl-escaping` ticket work visible in this repo's `.scratch`
  tree).
- **JSON/YAML**: the flat `{"predicate": ..., "args": {...}}` node shape needs a way to
  discriminate a path argument from a literal argument value at the same `args` key — either a
  wrapper (`{"path": "resource.PersonId"}` vs. a bare literal `"Y"`) or a sigil-prefixed string
  convention. A wrapper object is more explicit and avoids ambiguity with a legitimate string
  literal that happens to look like a path, at the cost of breaking the current assumption that
  every `args` value is a bare JSON scalar/array — every JSON/YAML consumer of the current shape
  (including the published `rule-tree.schema.json`) needs a schema-version bump.
- Both surfaces are equally achievable engineering; neither is a blocker on its own. The real cost
  is that this is a second grammar change (DSL lexical + JSON schema + YAML shape, three surfaces,
  one guarantee) layered on top of the argument-kind change in point 3, which is the same "real
  sub-language to design and version" cost ADR-0003 already flagged for the document-navigation
  case it declined for the same reason.

### 5. Recommendation: remain deferred

**Remain deferred**, for the reasons below, in this priority order:

1. **The predicate-catalog ticket's finding directly answers this ticket's motivating examples.**
   `IsManagerOf`, `IsOwnerOf`, and `IsDelegateOf` — the exact three examples both this ticket and
   CONTEXT.md's Deferred table use to justify the feature — are already fully expressible today,
   with zero engine changes, as host-authored, selector-parameterized predicates (e.g.
   `RelationshipPredicates.IsManagerOf<TContext>(selector: ctx => ctx.CurrentUserId, ...)`
   compared against a literal argument). The predicate-catalog ticket deliberately did not ship a
   generic `RelationshipPredicates` catalog category, on the grounds that "manager of"/"owner
   of"/"delegate of" are host-domain concepts, not general-use comparison logic — which is exactly
   ADR-0003's existing line ("a predicate needing a context value is authored as a distinct
   predicate"). That finding is direct evidence that the concrete pain this ticket exists to solve
   is not actually blocked on anything — it is already solvable, today, with the existing
   selector-factory pattern, and the *only* thing a path-expression grammar would add on top of
   that is letting the *rule text itself* (rather than the predicate's C# registration) name which
   context property to read.
2. **The cost is not "one grammar," it's the intersection of three grammars plus a new AST
   argument kind plus a term-identity carve-out.** Points 1, 3, and 4 above are each individually
   tractable, but they are not independent — a path expression needs simultaneous, consistent
   design across the DSL lexer/printer, the JSON/YAML node shape and published schema, the AST's
   argument-value representation, the compile-time validator, and the evaluator's fault model.
   That is a materially larger and more version-sensitive surface than anything else currently in
   `LiteralKind`/`PredicateArgumentSchema` (which is why ADR-0003 called it out by name as a
   rejected-not-forgotten item rather than silently declining it).
3. **The term-identity cost in point 2 is a real, permanent reduction in analyzer power that is
   easy to under-appreciate until it bites.** Every other feature this engine ships keeps constant/
   contradiction detection sound and complete for the terms it touches; a context-bound argument
   introduces the first class of term where two structurally-identical terms are not guaranteed to
   agree in value across evaluations of different context instances passed through the *same*
   compiled rule, and two structurally-different terms are not guaranteed to disagree. That's a
   quiet trade a rule author would not obviously notice until a "the analyzer should have caught
   this contradiction and didn't" bug report arrives, months later, against a rule that happens to
   use one context-bound argument among otherwise-literal ones.
4. **The "narrower alternative" (small enumerated set of well-known context paths) does not
   actually avoid either cost.** A closed enum of "well-known" paths (e.g. `CurrentUser.Id`,
   `Resource.OwnerId`) still needs: (a) a way to spell that enum value across DSL/JSON/YAML (the
   tri-format cost from point 4 is smaller — one closed vocabulary instead of an open grammar — but
   still present), and (b) the identical term-identity carve-out from point 2, since the analyzer
   still cannot know what a `WellKnownContextPath.CurrentUserId` term resolves to without
   evaluating a context. The narrower alternative saves the *grammar-design* cost (no path syntax,
   no indexer scoping debate) but not the *term-identity* cost, and it reintroduces exactly the
   "host must adapt its domain shape to the engine's fixed vocabulary" friction that the
   selector-factory pattern (predicate-catalog ticket) already avoids by letting the host name the
   selector however its own `TContext` is shaped. It is strictly worse than "remain deferred" once
   the selector-factory alternative is on the table, because it pays part of the same cost for less
   flexibility than what already exists.

Net: nothing currently blocked on this feature is actually blocked — the concrete motivating
examples resolve today via the existing selector-factory pattern the predicate-catalog ticket
confirmed and did not need to extend. The grammar/term-identity/tri-format costs documented above
are real and non-trivial. Recommend closing this out as "remain deferred" with no ADR amendment;
if a concrete future need arises that the selector-factory pattern genuinely cannot express (e.g.
a rule-authoring UI that must let a *non-developer* rule author pick a context path at rule-write
time, with no C# registration step available), that would be a new ticket re-opening this
investigation with that specific, concrete UI requirement as its forcing function, rather than
reopening it speculatively.

### Addendum: the concrete alternative this recommendation pointed at is now documented and tested

The "existing selector-factory pattern" referenced above (point 5.1) has since been named,
documented, and tested as its own pattern family — see README's ["n arguments, class-based,
externally-resolved value"](../../../README.md#n-arguments-class-based-externally-resolved-value)
section and the `externally-resolved-value-predicates` ticket set
(`.scratch/externally-resolved-value-predicates/`) that produced it, including worked-example
tests for the single-value, single-sided, and two-sided shapes and a `ResolvedValuePredicates`
factory for the safe-to-share-resolving-client case. `CONTEXT.md`'s `Deferred` table row for
this ticket now links to that section directly. This recommendation (remain deferred) and its
reasoning are unchanged; this note only points a future reader at the concrete, shipped
alternative rather than leaving it implicit in "the existing selector-factory pattern."
