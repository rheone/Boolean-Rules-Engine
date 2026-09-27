# 04: Regex match predicate

**What to build:** A `Matches` predicate over a `Func<TContext, string?>` selector and a `pattern` string argument, backed by `System.Text.RegularExpressions.Regex`.

**Blocked by:** 01

**Status:** done

- [x] Predicate compiles/caches its `Regex` once per distinct pattern (not once per evaluation call) — check whether the registry gives predicates a natural place to do this at registration time, or whether a small internal pattern cache keyed on the literal pattern string is needed — decided: a process-wide `ConcurrentDictionary<string, Regex>` keyed on the pattern text, since the pattern is a rule-text argument resolved only at evaluation time, not at registration time
- [x] Decide and document how an invalid regex pattern is surfaced: as a compile-time diagnostic if the predicate/argument schema supports pattern validation at registration or compile time, or as an evaluation-time fault (`Unknown`, per the Kleene failure model in ADR-0001) if it doesn't. Whichever is chosen, state it explicitly in the predicate's `Description` so a rule author isn't surprised — decided: evaluation-time fault, since `PredicateArgumentSchema` only declares a `LiteralKind`, not pattern well-formedness, and there is no compile/registration-time hook for content validation
- [x] A `null` selected value is treated as not-matching, not a fault, consistent with ticket 02's string predicates
- [x] Tests: matching pattern, non-matching pattern, `null` selected value, and the chosen invalid-pattern behavior
