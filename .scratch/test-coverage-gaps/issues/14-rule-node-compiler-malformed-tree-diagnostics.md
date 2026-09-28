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

**Status:** ready-for-agent

- [ ] Compiling a raw `AndNode`/`OrNode`/`ExactlyOneNode` with fewer than 2 operands produces a `MalformedTree` diagnostic and the node compiles to a constant `false`.
- [ ] Compiling a raw `ThresholdNode` with zero operands produces a `MalformedTree` diagnostic and the node compiles to a constant `false`.
- [ ] Compiling a term that supplies an argument name not declared on the predicate's schema produces an `UnknownArgument` diagnostic.
- [ ] Compiling a term that omits an optional argument with a declared default value resolves that argument to its default when evaluated.
- [ ] Existing `RuleNodeCompiler`/compilation diagnostic tests continue to pass unchanged.
