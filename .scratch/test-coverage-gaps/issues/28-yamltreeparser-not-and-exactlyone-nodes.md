# 28: Cover YamlTreeParser's Not/ExactlyOne node construction and operand-parse-failure propagation

**What to build:** `YamlTreeParser`'s operator-node switch (src/TruthWeaver.Yaml/YamlTreeParser.cs) has no coverage, merged across all six Cobertura reports from ticket 11's coverage run, for: the `Not` case's success path constructing a `NotNode` (line ~211), the `ExactlyOne` case's success path constructing an `ExactlyOneNode` (line ~217), or the operand-parsing loop's failure short-circuit (line ~184, `return null` when a nested operand fails to parse). `YamlTreeParser` round-trips the internal tree-format YAML representation (distinct from the higher-level document format `YamlRuleExtensions`/`YamlLiteralRoundTripTests` cover) and is exercised only by `tests/TruthWeaver.Tests/YamlTreeTests.cs` today, which doesn't reach these branches.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Parsing a tree-format YAML `not` node with exactly one operand produces a `NotNode` wrapping the parsed operand.
- [x] Parsing a tree-format YAML `exactlyOne` node produces an `ExactlyOneNode` wrapping its parsed operands.
- [x] Parsing an operator node where one of its `operands` entries is itself malformed (fails to parse) returns `null` and does not attempt to construct the parent node.
- [x] Existing `YamlTreeTests` continue to pass unchanged.
