# 02: String comparison predicates

**What to build:** Generic predicates for case-sensitive equality, case-insensitive (ordinal) equality, `StartsWith`, `EndsWith`, and `Contains`, each parameterized by a `Func<TContext, string?>` value selector supplied at registration and a `string` comparison-target argument supplied in rule text.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `Equals`/case-sensitive and `EqualsIgnoreCase`/case-insensitive predicates (naming TBD against existing repo conventions — check `docs/agents/domain.md` and any existing predicate examples in `README.md` before finalizing names)
- [ ] `StartsWith`, `EndsWith`, `Contains` predicates, all ordinal comparison (no culture-sensitive comparison — avoids locale-dependent rule behavior)
- [ ] Each predicate declares a `PredicateSchema` with required `Label` and `Description`, and each argument declares a required `Description`, per the predicate-authoring contract in `CONTEXT.md`
- [ ] A `null` selected value from `TContext` is treated as not-equal / not-matching, not a thrown exception — a predicate returning a determinate `False` for "no value present" is preferable to a fault for this common case; document this choice in each predicate's `Description`
- [ ] Tests per predicate: matching case, non-matching case, and a `null` selected value
