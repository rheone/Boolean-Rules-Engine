# 01: Escape string literals when printing DSL text

**What to build:** Fix `LiteralValue.ToString()` (or a dedicated printer-facing rendering path, if `ToString()` needs to stay raw for other callers) so a `LiteralKind.String` value escapes `\` to `\\` and `"` to `\"` before being wrapped in quotes. This is what the canonical printer, `TermIdentity.ToString()`, and evaluator trace/log output all currently rely on for string arguments.

**Blocked by:** none

**Status:** done

- [x] A string literal containing `"` prints as `\"` inside the quoted DSL text
- [x] A string literal containing `\` prints as `\\` inside the quoted DSL text
- [x] A string literal containing both, in combination, round-trips: printing a compiled rule with such a term and reparsing the output produces a structurally equal tree
- [x] Existing plain string literals (no special characters) print unchanged — no regression to existing round-trip tests
- [x] JSON and YAML printers are unaffected (they do not go through this code path) — confirm with existing JSON/YAML round-trip tests still passing
- [x] New test alongside the existing canonical-printer round-trip tests (ticket 06 in `engine-v1`) covering a string argument with an embedded quote and an embedded backslash
