# 01: Shared node-shape seam for Expression

**What to build:** A single internal seam that, given any `Expression` node, returns its op-name, its threshold `K` (when applicable), and its operand list — replacing the six independent switches in `OperatorInfo`, `CanonicalPrinter`, `JsonTreePrinter`, `YamlTreePrinter`, `Evaluator`, and `CompiledRule` that each currently re-derive this same structural fact. Each of the six keeps its own format-specific rendering/evaluation logic behind the seam; only the structural-shape question is unified.

**Blocked by:** none

**Status:** done

- [x] A new internal seam exists that returns a node's op-name, threshold `K` (if applicable), and operand list for every `Expression` variant
- [x] `OperatorInfo`, `CanonicalPrinter`, `JsonTreePrinter`, `YamlTreePrinter`, `Evaluator`, and `CompiledRule` all consume the seam instead of their own independent switches over `Expression`
- [x] Adding a new operand-shape fact (e.g. a hypothetical new operator) now requires touching the seam once, not six independent switches — demonstrate by tracing what a new operator would touch
- [x] No behavior change: all existing tests (DSL/JSON/YAML round-trip, evaluation, `Describe()`, analyzer) pass unchanged
- [x] ADR-0004 gains an amendment note recording the current real touch-point count for adding a new operator, distinct from the four-subsystem count the ADR originally scoped
