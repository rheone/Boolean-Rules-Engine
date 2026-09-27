# 01: Shared op-name lookup for JSON/YAML print and parse

**What to build:** One shared operator ⇄ tree-format-string lookup, covering every operator (`AND`/`OR`/`NOT`/`XOR`/`XNOR`/`ExactlyOne`/the threshold family), consumed by `JsonTreePrinter`, `YamlTreePrinter`, `JsonTreeParser`, and `YamlTreeParser` in place of their four independently-maintained copies (the duplicated `ThresholdOpName` method and the duplicated full op-name dispatch switch).

**Blocked by:** `expression-node-shape-seam` #01 (shared node-shape seam for Expression)

**Status:** done

- [x] A single op-name lookup exists mapping every operator to its tree-format string and back
- [x] `JsonTreePrinter` and `YamlTreePrinter` both consume the lookup instead of their own inline op-name strings and duplicated `ThresholdOpName` methods
- [x] `JsonTreeParser` and `YamlTreeParser` both consume the lookup instead of their own duplicated op-name dispatch switch
- [x] A shared test asserts JSON and YAML accept/produce the identical set of op-name strings for every operator
- [x] No behavior change: existing JSON/YAML round-trip tests pass unchanged
