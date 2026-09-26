# 04: Named arguments, term identity, per-evaluation memoization

**What to build:** Predicates that take named arguments — `hasRole(role: "Y")` — from the closed literal type set (`string`, `long`, `decimal`, `bool`, `DateTimeOffset`, and arrays of those), validated against the predicate's declared `PredicateSchema` at compile time so a missing or mistyped argument is a compile diagnostic, never a runtime failure inside `EvaluateAsync`. Zero-argument terms continue to be written bare (`isManager`, not `isManager()`).

Term identity is enforced exactly as `CONTEXT.md` defines it: predicate name normalized case-insensitively to the registered casing, plus arguments sorted by name and compared by exact type-normalized value; argument order in the source text does not affect identity; array-valued arguments are order-sensitive; argument values themselves are case-sensitive. Within a single evaluation, each distinct term identity is evaluated at most once — a term referenced from multiple branches of the same rule reuses the memoized result rather than invoking the predicate again.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] A predicate can declare a `PredicateSchema` with one or more named arguments (name, type, required/default) and read them via a `PredicateArguments` accessor (`GetString`, `GetInt64`, `GetDecimal`, `GetBool`, `GetDateTimeOffset`, and array variants)
- [ ] `hasRole(role: "Y")`-shaped terms parse with named arguments in any order in the source text, and a rule with a missing required argument or a type-mismatched argument value produces a compile `Error` diagnostic (source span included), not a runtime exception
- [ ] Two terms with the same predicate name (case-insensitive) and the same arguments (compared by sorted name + type-normalized value) are the same term identity; `hasRole(role: "Y")` and `hasRole(role: "y")` are distinct terms (case-sensitive values); argument order in the source text does not change identity; two array-valued arguments with the same elements in a different order are distinct terms
- [ ] A rule referencing the identical term identity from two different branches (e.g. both operands of an `OR`) invokes the underlying predicate exactly once per evaluation, reusing the memoized `TruthValue` for the second reference
- [ ] Memoization is scoped to a single evaluation only — calling `EvaluateAsync` a second time on the same `CompiledRule` re-invokes predicates rather than reusing results from the prior call
