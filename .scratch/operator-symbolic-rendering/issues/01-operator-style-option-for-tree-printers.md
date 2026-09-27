# 01: Operator style option (word / symbolic / C-style) for the tree printers

**What to build:** An `OperatorStyle` option accepted by `PlainTextTreePrinter.Print(...)` and `MermaidTreePrinter.Print(...)` (via their shared `RuleRenderTree`/`RenderNode` infrastructure), rendering `AND`/`OR`/`NOT`/`XOR`/`XNOR` in the chosen style (`Word` default, `Symbolic`: `∧ ∨ ¬ ⊕ ↔`, `CStyle`: `&& || ! ^ ==`) while `ExactlyOne` and the threshold family keep their word form in every style.

**Blocked by:** `dsl-escaping` ticket 01 (escape string literals on print), `dsl-escaping` ticket 02 (diagnose unrecognized escape)

**Status:** done

- [x] An `OperatorStyle` enum (`Word`, `Symbolic`, `CStyle`) is accepted by both `PlainTextTreePrinter.Print` and `MermaidTreePrinter.Print`, defaulting to `Word` (no behavior change for existing callers)
- [x] `Symbolic` style renders `AND`/`OR`/`NOT`/`XOR`/`XNOR` as `∧ ∨ ¬ ⊕ ↔` respectively
- [x] `CStyle` renders `AND`/`OR`/`NOT`/`XOR`/`XNOR` as `&& || ! ^ ==` respectively
- [x] `ExactlyOne` and the threshold family render in word/function-call form in every style
- [x] Covered by tests for both printers across all three styles
