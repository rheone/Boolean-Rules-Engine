# 12: NXOR and the XOR arity hint

**What to build:** n-ary parity NXOR, which is Unknown whenever any operand is Unknown; binary XOR with 3 or more operands is an error that points at NXOR. ExactlyOne keeps meaning exactly-one-true. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 09

**Status:** done

- [x] NXOR matches the oracle for up to 4 operands
- [x] XOR with 3+ operands yields a diagnostic naming NXOR
- [x] ExactlyOne behaviour is unchanged and tested to differ from NXOR
- [x] Analyzer handles NXOR
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`NXOR(a, b, ...)` is a first-class n-ary function-call node (`NxorExpression`, ADR-0005 decision 4) taking two or more operands (fewer is `MalformedTree`, matching `AND`/`OR`/`ExactlyOne`). It is `Unknown` whenever any operand is `Unknown`, otherwise `True` for an odd number of `True` operands. It was added to every layer following the ExactlyOne/NAND templates: `DslParser` (reserved word, function-call form so no precedence/mixing rule), `RuleNode`/`RuleNodeCompiler`, `Evaluator` (parity with its own `NXOR` label; all operands evaluated), `Analyzer` (fold of the XOR dual rail, plus term collection), `ExpressionShape`/`OperatorInfo`, `TreeFormatOpNames` (`nxor`), JSON/YAML parsers and the schema, `CanonicalPrinter` (`NXOR(a, b, c)`), and `RuleBuilder.Nxor`. Printers keep the word in every `OperatorStyle`. `K3Oracle.Nxor` folds the primitive-defined `Xor`. `NxorTests` checks 2..4 operands over every {T,F,U} assignment, differs-from-`ExactlyOne` cases (`T,T,T` and `T,T,?`), the XOR arity hint (DSL and JSON; message names `NXOR` and `ExactlyOne`, code still `XorArityViolation`), JSON/YAML round trip, builder, labels and descriptions; the shared parity/exhaustiveness tests, analyzer-vs-oracle generator and DSL round-trip generator include NXOR. Decisions: the minimum is two operands (a one-operand parity is just the operand and would hide a mistake); the shared arity code is reused as in issues 7 and 11.
