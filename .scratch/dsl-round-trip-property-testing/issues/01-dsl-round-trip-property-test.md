# 01: Property-based round-trip test for the DSL printer/parser

**What to build:** A property-based test suite that generates random valid rule ASTs (covering every operator and every `LiteralKind`), prints them via `CanonicalPrinter`, reparses them via `RuleCompiler`, and asserts structural equality with the original tree; on failure, the test framework shrinks to a minimal counterexample instead of reporting the full random tree.

**Blocked by:** none

**Status:** done

- [x] A generator produces valid `Expression` trees spanning `AND`/`OR`/`NOT`/`XOR`/`XNOR`/`ExactlyOne`/the threshold family/constants and terms with every `LiteralKind`
- [x] The property test asserts `parse(print(x))` is structurally equal to `x` for every generated tree
- [x] A failing case shrinks to a minimal counterexample rather than reporting the full random tree
- [x] The test runs as part of the existing test suite (`dotnet test`) with a bounded iteration count — no unbounded or flaky runtime
- [x] Generated arity/argument constraints respect the engine's own rules (e.g. `XOR`/`XNOR` exactly binary, threshold `k` in range) so the generator never produces a tree the compiler would reject for reasons unrelated to round-tripping
