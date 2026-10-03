# 22: Whitespace normalisation and operator spacing

**What to build:** Printed rule text has whitespace collapsed to a single space, is trimmed, and has spaces around operators.

**Blocked by:** 09

**Status:** done

- [x] Output is deterministic for any input spacing
- [x] Re-parsing normalised text gives an equal tree
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

(b) The canonical printer already emitted single spaces around infix operators and after commas, with no leading/trailing space; tests now pin that for any input spacing. (a) Added the public `RuleText.NormalizeWhitespace(string)` in `TruthWeaver.Printing`: a token-level formatter built on the existing `Lexer`, so it works on text that does not compile, needs no registry, and keeps the author's operators, case and delimiters (a compile-and-reprint would also rewrite those, which is `CanonicalText`'s job).

Decisions: the layout is a pure function of the token sequence (space between tokens except: none after an opener, none before a closer or comma or an argument's colon, none after prefix `!`/`¬`, none between a non-operator name and its `(`); a ternary's `:` is told from an argument's `:` by counting unmatched `?` per nesting level, and is spaced like an operator; string literal contents are copied verbatim from the source; characters the lexer rejects are kept in place as separate pieces rather than dropped. A CsCheck property pads the printed text of generated trees with random whitespace and checks normalisation equals the tidy print's and re-parses to an equal tree.
