# 02: Diagnose unrecognized escape sequences in DSL string literals

**What to build:** `Lexer.ReadString` currently handles `\"`, `\\`, `\n`, `\t` and falls through to `_ => next` for anything else, silently discarding the backslash. Change the fallthrough case to raise a new lexer diagnostic instead of silently accepting the sequence, while still recovering (continuing to lex) so later syntax errors in the same rule text are also reported.

**Blocked by:** none

**Status:** ready-for-agent

- [ ] New `DiagnosticCodes` entry for an unrecognized escape sequence (e.g. `BRE00xx`, `InvalidEscapeSequence`)
- [ ] `Lexer.ReadString` raises this diagnostic when `\` is followed by a character other than `"`, `\`, `n`, `t`
- [ ] The diagnostic's `SourceSpan` covers exactly the two-character escape sequence (backslash + following character)
- [ ] Lexing recovers and continues (the string literal still produces a token; the rule still gets a `CompilationResult` with the error diagnostic and a null `CompiledRule`, per existing "never throws for authoring errors" behavior)
- [ ] Test: `hasRole(role: "a\pb")` compiles to a `CompilationResult` with the new error diagnostic and a null `CompiledRule`
- [ ] Test: each of the existing valid escapes (`\"`, `\\`, `\n`, `\t`) does not raise the diagnostic
