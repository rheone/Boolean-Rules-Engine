# 01: Cover Mermaid label escaping

**What to build:** Verify that predicate/node labels containing characters that would otherwise break Mermaid diagram syntax (double quotes, carriage returns, newlines) are rendered as valid Mermaid output when a rule tree is printed via `MermaidTreePrinter`.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A rule/predicate whose label contains a `"` character produces a printed Mermaid diagram with the quote properly escaped (no syntax break).
- [x] A label containing `\r` and/or `\n` is sanitized in the printed output.
- [x] Existing `MermaidTreePrinter` tests continue to pass unchanged.

## Comments

Already covered on `main` (commit `ac931c4`, "Cover Mermaid label escaping (test-coverage-gaps ticket 01)") — `RuleTreeRenderingTests.Mermaid_output_escapes_double_quotes_in_labels` and the theorized `Mermaid_output_sanitizes_carriage_returns_and_newlines_in_labels` (covering `\r\n`, `\r`, and `\n` separately) exercise `MermaidTreePrinter.Escape`. Verified via `dotnet test` (620/620 passing) — no code or test changes were needed this pass.
