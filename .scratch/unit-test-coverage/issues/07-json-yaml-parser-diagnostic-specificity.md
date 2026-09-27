# 07: Pin down specific JSON/YAML malformed-input diagnostic messages

**What to build:** `JsonTreeTests.cs` has one theory,
`Malformed_json_produces_a_diagnostic_not_an_exception`, covering 4 malformed inputs — but it only
asserts *some* `Error`-severity diagnostic was produced, not which one. `JsonTreeParser.cs` emits
`DiagnosticCodes.MalformedTree` from at least 8 distinct call sites with distinct messages
(`'const' must be a JSON boolean`, `'predicate' must be a JSON string`, `'args' must be a JSON
object`, `'not' requires exactly one operand`, `Unknown operator '{op}'`, `'{op}' requires a
numeric 'k'`, plus two more around the array/operand-shape checks). Several of these specific
branches have no input in the existing theory data that would hit them, and none of the existing
assertions check the message/branch, so a change that broke one specific check but still emitted
*some* error would pass today's suite silently.

Check `src/BooleanRulesEngine.Yaml/YamlTreeParser.cs` for the equivalent set of diagnostic call
sites — `YamlTreeTests.cs` likely has the same shallow "some diagnostic" pattern and should get the
same treatment.

Follow TDD: add one `[InlineData]` case per currently-unreached branch, asserting on the specific
diagnostic message or a distinguishing substring — not just `Severity == Error` — then confirm it
passes.

**Blocked by:** none

**Status:** done

- [x] Every distinct `DiagnosticCodes.MalformedTree` message in `JsonTreeParser.cs` has at least
      one test input that triggers it specifically, with an assertion on that message (or a
      distinguishing substring)
- [x] The equivalent gap in `YamlTreeParser.cs`/`YamlTreeTests.cs` is closed the same way
- [x] `dotnet test` passes

## Comments

Both `JsonTreeTests.cs` and `YamlTreeTests.cs` already had an
`Every_distinct_malformed_tree_branch_raises_its_specific_message` theory covering every distinct
`DiagnosticCodes.MalformedTree` message in their respective parsers. No new tests were needed;
`dotnet test` passes (307/307).
