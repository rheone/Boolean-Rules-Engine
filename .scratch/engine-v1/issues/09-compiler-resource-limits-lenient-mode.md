# 09: Compiler resource limits & lenient mode

**What to build:** `CompilerOptions` bounds so a rule authored somewhere not code-reviewed (e.g. an admin UI backed by a database) can't pathologically hang a request thread: max tree depth (default 32), max node count (default 512), and a cap on the number of distinct terms subject to BDD-based analysis (default 20 — beyond the cap, analysis is skipped and reported as an `Info` diagnostic, never silently treated as "not constant"). All bounds are overridable via `CompilerOptions`.

Also `CompilationMode.Lenient`: for the legitimate multi-service-shared-rule-store scenario, an unknown predicate compiles to a permanent `Unknown` term fault on that term rather than an `Error` diagnostic. This mode is distinct from the default `Strict` mode used in ticket 02, and is intended for read paths only, never the write/persistence path.

**Blocked by:** 05

**Status:** done (verified against existing codebase — already implemented prior to this pass)

- [ ] A rule exceeding the configured max tree depth produces a compile `Error` diagnostic rather than a stack overflow or hang
- [ ] A rule exceeding the configured max node count produces a compile `Error` diagnostic rather than an unbounded compile time
- [ ] A rule with more distinct terms than the configured BDD-analysis cap compiles successfully, skips constant/contradiction analysis, and reports that skip as an `Info` diagnostic (not an `Error`, and not silently claiming "not constant")
- [ ] All three bounds are configurable via `CompilerOptions` and default to the documented values (32 / 512 / 20)
- [ ] In `CompilationMode.Lenient`, a rule referencing an unregistered predicate name compiles successfully (`CompiledRule` is non-null) with that term permanently evaluating to `Unknown` at runtime, distinct from `CompilationMode.Strict`'s behavior (an `Error` diagnostic, null `CompiledRule`) for the identical rule text
