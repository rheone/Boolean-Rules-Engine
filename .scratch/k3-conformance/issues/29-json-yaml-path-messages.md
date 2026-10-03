# 29: JSON/YAML path-based messages

**What to build:** The same readable diagnostics for malformed JSON and YAML rules, located by JSON/YAML path instead of line and column.

**Blocked by:** 28

**Status:** done

- [x] Malformed JSON and YAML trees give code, explanation, path, expected and found
- [x] Plain-text rendering matches the DSL style
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

JSON/YAML diagnostics located by path (ADR-0005 decision 11, k3-conformance 29).

- `Diagnostic.Path` (added in 28, null for the DSL) now carries `$.operands[1].op`-style paths from both tree parsers
  and from the compiler, via an internal `Path` on `RuleNode`/`ArgumentNode`. JSON and YAML share one syntax
  (`TreePath`). Reuses ticket 28's suggestion engine (`NameSuggester`) for unknown ops (tree spelling, aliases
  included), `Collapse` policies, predicate names and argument names; a predicate in a tree is never answered with a
  DSL operator word.
- Covered: unknown op, missing/ill-typed `operands`, wrong operand count, bad `const`/`policy`/`unknownAs`/`min`/`max`/`k`,
  wrong JSON/YAML node types, non-string `op`, node without a discriminator key, unknown predicate, missing/unknown/
  mistyped arguments, and invalid JSON/YAML syntax (path of the innermost open container via `TreePathTracker`, plus the
  parser's position as a span).
- YAML diagnostics also have a `Span` (YamlDotNet marks) so the formatter adds line, column and the source line; JSON
  diagnostics have none except for syntax errors.
- Behaviour fixes made on the way: a fractional or oversized `k` threw from `GetInt32` and is now a diagnostic; the
  operator name is validated before the operands, so a typo is not hidden behind an operand problem; a non-string `op`
  has its own diagnostic.
- Tests: `TreeDiagnosticsTests` (43 cases). README "Reading diagnostics" gains a JSON and YAML subsection.
