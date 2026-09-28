# 30: Cover YamlTreePrinter's const-node false branch

**What to build:** `YamlTreePrinter.ToNode`'s `ConstantExpression` case (src/TruthWeaver.Yaml/YamlTreePrinter.cs, line ~33, `c.Value ? "true" : "false"`) has only its `true` side exercised, merged across all six Cobertura reports from ticket 11's coverage run — printing a constant-`false` node as tree-format YAML is untested. This is the only remaining gap in `YamlTreePrinter` once results from every test project are combined; ticket 07 already closed the Decimal/array-literal printing gaps this ticket's earlier draft (based on a single project's report) mistakenly still listed.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Printing a rule whose root (or a subexpression) is `ConstantExpression(false)` to tree-format YAML renders `const: false`, and round-trips back to a structurally equal `ConstantExpression(false)` via `YamlTreeParser`.
- [x] Existing `YamlTreeTests` continue to pass unchanged.
