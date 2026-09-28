# 14: Cover RuleNodeCompiler malformed-tree diagnostics and argument defaulting

**What to build:** `RuleNodeCompiler.Build*` (src/TruthWeaver/Compilation/RuleNodeCompiler.cs) emits
several diagnostics for structurally invalid raw trees — reachable when compiling a hand-built or
embedded (JSON/YAML subtree) `RuleNode` that bypasses the DSL parser's own arity checks — plus an
optional-argument default-value substitution path. Per the ticket 11 Cobertura report, none of these
are exercised by any test:

- `BuildVariadic` (AND/OR/ExactlyOne) with fewer than 2 operands → `DiagnosticCodes.MalformedTree` (lines ~156-164).
- `BuildThreshold` with zero operands → `DiagnosticCodes.MalformedTree` (lines ~217-221).
- `BuildTerm` supplying an argument name the predicate's schema doesn't declare → `DiagnosticCodes.UnknownArgument` (lines ~275-283).
- `BuildTerm` omitting an optional argument that has a declared `Default` → the default value is substituted into the resolved arguments (lines ~318-321).

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Compiling a raw `AndNode`/`OrNode`/`ExactlyOneNode` with fewer than 2 operands produces a `MalformedTree` diagnostic and the node compiles to a constant `false`.
- [x] Compiling a raw `ThresholdNode` with zero operands produces a `MalformedTree` diagnostic and the node compiles to a constant `false`.
- [x] Compiling a term that supplies an argument name not declared on the predicate's schema produces an `UnknownArgument` diagnostic.
- [x] Compiling a term that omits an optional argument with a declared default value resolves that argument to its default when evaluated.
- [x] Existing `RuleNodeCompiler`/compilation diagnostic tests continue to pass unchanged.

## Comments

Added `tests/TruthWeaver.Tests/RuleNodeCompilerDiagnosticsTests.cs` with six tests, all going through
the public `RuleCompiler.Compile`/`CompileJson` surface — no `InternalsVisibleTo` reach into
`RuleNodeCompiler` was needed, since the JSON front end (`JsonTreeParser`) doesn't itself enforce
AND/OR/ExactlyOne/threshold operand-count arity at parse time, only the compiler's own `BuildVariadic`/
`BuildThreshold` guards do (the DSL parser's grammar makes an under-arity AND/OR/ExactlyOne
structurally unwritable, which is why this gap needed a hand-built JSON tree to reach at all):

- `A_variadic_operator_with_fewer_than_two_operands_produces_a_malformed_tree_diagnostic_not_an_exception`
  (theory over `and`/`or`/`exactlyOne` with 0-1 operands) — `BuildVariadic`'s arity guard.
- `A_threshold_operator_with_zero_operands_produces_a_malformed_tree_diagnostic_not_an_exception` —
  `BuildThreshold`'s zero-operand guard (checked before `ValidThresholdRange`, so any `k` reaches it).
- `A_term_argument_name_the_predicate_schema_does_not_declare_produces_an_unknown_argument_diagnostic` —
  `BuildTerm`'s `UnknownArgument` branch, reached via ordinary DSL text (`hasRole(bogusArg: "x")`; no
  hand-built tree needed here since the DSL parser doesn't validate argument names against a schema
  either — that's the compiler's job for every front end).
- `An_omitted_optional_argument_with_a_declared_default_resolves_to_that_default_when_evaluated` —
  registers a predicate with an optional argument (`Required: false`, `Default: LiteralValue.OfString("GUEST")`),
  compiles a bare reference to it (omitting the argument entirely), and evaluates to confirm the
  predicate observed the substituted default value at runtime.

All four are asserted as graceful diagnostics (`Succeeded == false`, `CompiledRule == null`, specific
`DiagnosticCodes` present with `DiagnosticSeverity.Error`) rather than exceptions, matching the
existing "malformed input is a diagnostic, not a throw" convention used throughout `JsonTreeTests`.
All six new tests and the full existing `RuleNodeCompiler`/compilation diagnostic test suite pass.
