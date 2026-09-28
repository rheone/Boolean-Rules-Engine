# 29: Cover YamlTreeParser's nested array-literal failure propagation and unsupported-node-type diagnostic

**What to build:** `YamlTreeParser.ParseLiteral`'s sequence case (src/TruthWeaver.Yaml/YamlTreeParser.cs, line ~269) never propagates a nested-element parse failure out of an array literal in any current test, and its `default` case's diagnostic for an unsupported YAML node type as a literal (line ~286) is never triggered, merged across all six Cobertura reports from ticket 11's coverage run. [[28-yamltreeparser-not-and-exactlyone-nodes]] covers this file's operator-node gaps. Separately, `ParseNode`'s `default` throw for an unhandled canonical op-name (line ~229) is defensive against `TreeFormatOpNames` ever mapping to a name this switch doesn't handle — likely unreachable through the public API today (same shape as ticket 15's `OperatorInfo`/`ThresholdDescription` precedent); confirm that reasoning (or cover it directly via internals access) rather than leaving it silently untested.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A tree-format YAML array literal containing one malformed element (e.g. an unsupported nested node type) fails to parse as a whole, rather than silently dropping or truncating the bad element.
- [x] Parsing a literal value node of an unsupported YAML node type (i.e. neither scalar nor sequence) adds a `MalformedTree` diagnostic naming the node type and returns `null`.
- [x] `ParseNode`'s unhandled-canonical-op-name `default` branch is either exercised directly or its unreachability is documented with the same rationale ticket 08/15 used, matching this ticket's checklist precedent.
- [x] Existing `YamlTreeTests` continue to pass unchanged.

## Comments

Added one test to `tests/TruthWeaver.Tests/YamlTreeTests.cs`:

- `A_malformed_element_in_an_array_literal_fails_the_whole_literal_rather_than_truncating_it` — an
  `args` value is a YAML sequence whose second element is a mapping (neither scalar nor sequence);
  asserts compilation fails with exactly one `MalformedTree` diagnostic mentioning "Unsupported YAML
  node type", proving `ParseLiteral`'s sequence case propagates a nested element's failure out of the
  whole array rather than silently dropping or truncating it.

The literal-value-node-of-unsupported-type diagnostic itself (`ParseLiteral`'s `default` case) was
already covered by the pre-existing `Every_distinct_malformed_tree_branch_raises_its_specific_message`
theory case `"predicate: isManager\nargs:\n  x:\n    weird: 1"`, which hits the same diagnostic at the
top level (a mapping used directly as an arg value); the new test above exercises the same branch
reached through the nested/array path specifically.

`ParseOperator`'s unhandled-canonical-op-name `default` branch (an `InvalidOperationException`) was
documented as unreachable rather than exercised, via a `<remarks>` block on `YamlTreeTests`. Unlike
ticket 15's `ThresholdDescription`, whose switch discriminant is a method parameter a test can hand-build
with a bogus value, this switch's discriminant is the out-value of the internal
`TreeFormatOpNames.TryFromTreeFormat` lookup, whose backing dictionary has exactly the same ten entries
as the switch's non-default cases. There is no parameter or public seam through which a test can make
that lookup produce an eleventh, unhandled canonical name, so the branch is genuinely unreachable rather
than merely untested.

All 696 tests pass (695 pre-existing + 1 new). `dotnet csharpier check .` and
`dotnet format --verify-no-changes --severity info` both pass clean.
