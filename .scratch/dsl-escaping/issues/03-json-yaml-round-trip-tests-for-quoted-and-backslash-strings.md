# 03: Add JSON/YAML round-trip tests for string literals containing `"` or `\`

**What to build:** Ticket 01 fixed and tested DSL round-tripping of a string-literal argument containing `"` or `\` (`hasRole(role: "V\"IP")`). `JsonTreePrinter`/`YamlTreePrinter` never had a bespoke escaping bug to fix — they delegate entirely to `System.Text.Json`'s `JsonValue.Create` and YamlDotNet's `YamlScalarNode`/`ScalarStyle.DoubleQuoted` writer — but no existing test in `TruthWeaver.Tests`/`TruthWeaver.Yaml.Tests` actually exercises a string argument with an embedded `"` or `\` through `CompileJson`/`PrintJson` or `CompileYaml`/`PrintYaml`. This ticket closes that gap so the "JSON/YAML escaping is already handled by the underlying library" assumption is a verified regression test, not just an inference from reading the printer source.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A term with a string argument containing `"` (e.g. `hasRole(role: "V\"IP")`) round-trips through `PrintJson()` → `CompileJson()` to a structurally equal `CompiledRule`, and the emitted JSON's string value is valid, correctly-escaped JSON.
- [x] The same case round-trips through `PrintYaml()` → `CompileYaml()`, and the emitted YAML's scalar is valid, correctly-escaped double-quoted YAML.
- [x] A term with a string argument containing `\` (no adjacent quote) round-trips through both JSON and YAML the same way.
- [x] A term with a string argument containing both `"` and `\` in combination round-trips through both JSON and YAML.
- [x] Tests live alongside the existing round-trip suites (`DslRoundTripPropertyTests`-adjacent for JSON, the YAML tree tests for YAML), not a new standalone file, matching how ticket 01's DSL cases were placed.

## Comments

Added three new `[Fact]` tests to `tests/TruthWeaver.Tests/JsonTreeTests.cs`
(quote, backslash, and both-combined) and three to
`tests/TruthWeaver.Yaml.Tests/YamlLiteralRoundTripTests.cs` (same three
cases). Each test compiles a `hasRole(role: ...)` term with the DSL-escaped
literal, round-trips it through `PrintJson()`/`CompileJson()` or
`PrintYaml()`/`CompileYaml()`, asserts `CanonicalText` equality (structural
round-trip), and independently re-parses the emitted text with
`System.Text.Json`/YamlDotNet to assert the decoded string value is exactly
the intended raw value (e.g. `V"IP`, `C:\Temp`, `V"\IP`) — proving the
emitted format is valid, correctly-escaped JSON/YAML, not just something
this library's own parser happens to accept.

JSON tests were placed in the existing `JsonTreeTests.cs` (same directory as
`DslRoundTripPropertyTests.cs`) rather than a new file, since that file is
already the JSON round-trip suite. YAML tests were placed in the existing
`YamlLiteralRoundTripTests.cs`, since it already collects literal-argument
edge-case round-trip tests (GUID, DateTimeOffset, arrays, etc.) — the same
category as the quote/backslash string cases added here. A small private
`ReadRoleScalar` helper (using `YamlDotNet.RepresentationModel`) was added
at the end of the YAML test class, after all `[Fact]` methods, to satisfy
SA1202 (public members before private).
