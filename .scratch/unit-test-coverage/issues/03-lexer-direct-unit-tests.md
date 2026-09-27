# 03: Direct unit tests for Lexer (zero direct coverage)

**What to build:** A new `LexerTests.cs` exercising `src/BooleanRulesEngine/Parsing/Lexer.cs`
directly (needs ticket 01). All existing DSL coverage goes through `RuleCompiler.Compile(...)`,
which only exercises tokenization paths that happen to appear in valid or deliberately-invalid
whole rule strings — several of `Lexer`'s own branches have no test verifying their token output
in isolation.

Follow TDD: write each test first against `new Lexer(source).Tokenize()` /
`.Diagnostics`, run it, then only change `Lexer.cs` if a test reveals an actual defect.

Cover:

- Every punctuation token in isolation (`(`, `)`, `[`, `]`, `,`, `:`) produces the correct
  `TokenKind` and 1-character `SourceSpan`
- A negative integer (`-5`) is read as a single `NumberLiteral` token via the `ReadNumber` path
  (lines 76–82 dispatch on `c == '-' && ... IsDigit(next)`), but a bare `-` not followed by a digit
  falls through and raises the "Unexpected character" diagnostic (line 89–93) instead
- A decimal number (`1.5`) is read as one `NumberLiteral` token; a trailing `.` with no digit
  after it (`1.`) stops before the `.` — i.e. `ReadNumber`'s lookahead at lines 127–132 correctly
  declines to consume the `.` when it isn't followed by a digit
- `ReadString` (lines 145–190): a string with each supported escape (`\"`, `\\`, `\n`, `\t`)
  decodes to the correct character; an unterminated string (no closing `"` before EOF) raises the
  "Unterminated string literal" diagnostic and still returns a `StringLiteral` token covering the
  consumed span, rather than throwing
- An unrecognized character (e.g. `#`) raises the "Unexpected character" diagnostic *and*
  tokenization continues past it (line 93 recurses via `NextToken()` rather than stopping) — assert
  the token stream still reaches `Eof` and contains whatever valid tokens follow the bad character
- `Tokenize()` on an empty/whitespace-only source returns a single-element list containing only
  the `Eof` token
- An identifier is read up to (but not including) the first non-identifier character, per
  `IsIdentifierStart`/`IsIdentifierPart` (letters, digits, `_`)

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] New `LexerTests.cs` under `tests/BooleanRulesEngine.Tests/`
- [ ] Every bullet above has a corresponding test
- [ ] `dotnet test` passes
- [ ] No change to `Lexer.cs` unless a test reveals a real defect — note any such finding in this
      ticket's Comments (the unrecognized-escape-sequence gap is already tracked separately in
      `.scratch/dsl-escaping/issues/02-diagnose-unrecognized-escape.md` — don't duplicate that fix
      here, just test today's actual behavior)
