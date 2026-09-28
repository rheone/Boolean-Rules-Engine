# 02: Document DSL string literal escaping in README.md

**What to build:** README.md never mentions that DSL string-literal arguments support escape sequences. Ticket 01 (done) implemented and tested `\"`, `\\`, `\n`, `\t` in `Lexer.ReadString` (an unrecognized sequence like `\p` is a compile-time `InvalidEscapeSequence` diagnostic, not a silent value change) and matching escape-on-print in the canonical printer, so `parse(print(x))` round-trips correctly for a string argument containing `"` or `\`. Add this to README.md, most naturally as a short subsection under [Named arguments](../../../README.md#3-named-arguments) or [All operators](../../../README.md#all-operators) wherever string-literal syntax is otherwise introduced.

**Blocked by:** None (can start immediately) — ticket 01 is already done.

**Status:** done

- [x] README documents the supported escape sequences in a DSL string literal (`\"`, `\\`, `\n`, `\t`) with a short example, e.g. `hasRole(role: "V\"IP")`.
- [x] README documents that an unrecognized escape sequence is a compile-time diagnostic (`InvalidEscapeSequence`), not a silently-corrupted literal value.
- [x] README notes this escaping rule is DSL-specific — JSON and YAML use their own format's native string escaping (`System.Text.Json` / YamlDotNet), not this rule.
- [x] The addition fits the existing README voice/structure (short prose + a fenced example), not a new top-level section, and the table of contents is updated only if a new heading is added.

## Comments

Added a short paragraph after the "Named arguments" subsection (section 3,
before "4. `XOR`, `XNOR`, `ExactlyOne`, and the threshold family") in
`README.md`. It documents the four supported escape sequences (`\"`, `\\`,
`\n`, `\t`), gives the worked example `hasRole(role: "V\"IP")` compiling to
the value `V"IP` and printing back unchanged, notes that an unrecognized
escape is a compile-time `InvalidEscapeSequence` diagnostic rather than a
silent corruption, and clarifies the rule is DSL-specific — JSON/YAML use
their own libraries' native string escaping. No new heading was added, so
the table of contents was left untouched, per the acceptance checklist.
