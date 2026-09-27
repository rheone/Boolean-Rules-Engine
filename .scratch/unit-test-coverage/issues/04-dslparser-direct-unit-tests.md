# 04: Direct unit tests for DslParser (zero direct coverage, largest file in the repo)

**What to build:** A new `DslParserTests.cs` exercising `src/BooleanRulesEngine/Parsing/DslParser.cs`
directly via its `Parse(string source)` entry point (needs ticket 01). At 447 lines it's the
largest source file in the repository, and every existing test reaches it only through
`RuleCompiler.Compile(...)`, which mixes parser behavior with lexing, validation, analysis, and
building in one assertion — a real syntax error can be masked or misattributed by the time it
reaches a black-box test.

Follow TDD: write each test first against `DslParser.Parse(source)`'s returned
`(RuleNode Root, IReadOnlyList<Diagnostic> Diagnostics)`, run it, then only change `DslParser.cs`
if a test reveals an actual defect.

Cover:

- Trailing garbage after a complete expression (`"a AND b )"`) raises the "Unexpected token ...
  after end of expression" diagnostic at lines 63–72, with the root node still parsed correctly
  for the valid prefix
- Mixing `XOR` with `AND`/`OR` at the same syntactic level without parentheses (`"a XOR b AND c"`)
  raises `DiagnosticCodes.AmbiguousOperatorMixing` (via `ReportAmbiguousMixing`, lines 118–121);
  parenthesizing (`"(a XOR b) AND c"`) does not
- Mixing `XOR` and `XNOR` at the same chain level (`"a XOR b XNOR c"`) raises the specific
  "Mixing XOR with XNOR ..." message from lines 162–168; a chain using only one or the other does
  not
- `ParsePrimary`'s fallback error path (line 251–269): an input that starts with something that's
  none of `(`, `TRUE`, `FALSE`, a threshold/`EXACTLYONE` keyword, or an identifier (e.g. a bare
  `,` or `)`) raises "Expected a term, constant, or '('" and returns an `ErrorNode`, and parsing
  still reaches `Eof` afterward rather than looping
- `ParseThreshold` (lines 371–403): omitting the integer first argument (`"AtLeast(a, b)"`) raises
  "Expected an integer threshold as AtLeast's first argument" and defaults `k` to `0` rather than
  throwing
- `ParseArgument`/`ParseLiteral` (lines 298–341): a term argument with a missing `:` (`"f(x 1)"`)
  raises "Expected ':'"; a literal position holding something that isn't a string, number,
  `true`/`false`, or `[` (e.g. a bare identifier) raises "Expected a literal value" and recovers
  with a `false` literal rather than throwing
- `ParseArrayLiteral` (lines 343–361): an empty array (`"f(x: [])"`) parses to a zero-element
  array literal; a missing closing `]` raises "Expected ']'"
- `IsReservedWord` returns `true` for every keyword in `ReservedWords` (case-insensitively, e.g.
  `"and"`, `"And"`, `"AND"`) and `false` for an arbitrary predicate name

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] New `DslParserTests.cs` under `tests/BooleanRulesEngine.Tests/`
- [ ] Every bullet above has a corresponding test
- [ ] `dotnet test` passes
- [ ] No change to `DslParser.cs` unless a test reveals a real defect — note any such finding in
      this ticket's Comments
