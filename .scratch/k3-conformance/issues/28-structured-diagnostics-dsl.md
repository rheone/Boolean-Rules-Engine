# 28: Structured diagnostics and DSL messages

**What to build:** Malformed DSL produces structured diagnostics (code, plain explanation, span, expected versus found, optional did-you-mean suggestion) and a plain-text rendering, built on the existing diagnostic model.

**Blocked by:** 20

**Status:** done

- [x] Unknown operators and aliases get did-you-mean suggestions
- [x] Each message carries code, location, expected and found
- [x] Plain-text rendering is available alongside the structured data
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Structured diagnostics for malformed DSL (ADR-0005 decision 11, k3-conformance 28).

- `Diagnostic` keeps its positional members and gains init-only `Path`, `Expected`, `Found` and `Suggestion`
  (`DiagnosticSuggestion` of kind `Replacement` for "did you mean" or `Hint`), set through optional parameters on
  `Diagnostic.Error/Warning/Info`. Existing message text is unchanged, so no existing test needed updating.
- `SourceSpan.GetLocation(source)` returns a 1-based `SourceLocation`; `DiagnosticFormatter.Format` and
  `CompilationResult.FormatDiagnostics(source)` render the plain text (header, source line, caret underline,
  `Expected:`, `Found:`, `Did you mean:`/`Hint:`).
- Suggestions: internal `NameSuggester` (case-insensitive optimal-string-alignment distance, cut-off 1/2/3 edits for
  words up to 4/8/longer, ties to the ordinally first candidate) over `DslVocabulary` (operators, aliases, keywords)
  and the registry's predicate names. It covers unknown predicate/operator names, a misspelt infix word or trailing
  token, `Collapse` policies, undeclared argument names and a lone `&`/`|`.
- The no-mixing rule and the binary-operator arity errors carry a `Hint` suggestion (parentheses with the quoted text
  where there is a bare operand; `NXOR`/`ExactlyOne` for `XOR`). `BRE0006` stays shared by the five binary operators.
- Tests: `StructuredDiagnosticsTests` (27) and `DiagnosticFormatterTests` (8). README section "Reading diagnostics".
