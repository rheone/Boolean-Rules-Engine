# Strong K3 conformance and language surface

**Status:** ready-for-agent

Source requirements: `.scratch/2026-10-02-TODO.md`. Decisions: [ADR-0005](../../docs/adr/0005-strong-k3-language-surface.md) (decisions 1-17, no open decisions). Reference material in `.tmp/` was used as a source, not trusted blindly; every K3 behaviour is verified against the truth-table oracle described under Testing Decisions. Earlier category-level tickets live in `_superseded/` and must not be implemented.

## Problem Statement

TruthWeaver is meant to be a general-purpose Strong Kleene (K3) expression engine, but today it is only partly one:

- Operators evaluate in K3, but predicates cannot return `Unknown`; they return `bool`, and `Unknown` only appears when a predicate throws.
- `Unknown` cannot be written in a rule; constants are two-valued.
- The static analyzer reasons in classical two-valued logic, so it reports contradictions and tautologies that are not true in K3 (`A AND NOT A` is `Unknown` when `A` is `Unknown`).
- The authoring language is a small subset of the K3 language the reference specs describe: no `IMPLIES`, `NAND`, `NOR`, n-ary parity XOR, `COALESCE`, `If`, inspection or explicit boundaries, no cardinality aliases, no symbol notation, and a single grouping delimiter.
- Rules cannot be rewritten (expanded, compressed, simplified, canonicalised, whitespace-normalised) and syntax errors are not explained in a user-friendly way.

## Solution

A rule author writes a rule once in the DSL, JSON or YAML, using any of the supported notations (named, symbolic, any grouping delimiter, any letter case). It compiles to an immutable tree. Every expression and every predicate yields `True`, `False` or `Unknown`; `Unknown` is a normal value that is never silently turned into `True` or `False`. Conversion to a two-valued result happens only at explicit boundaries (`Project`, `Collapse`, `Decision`). The author can optionally expand, compress, simplify or canonicalise the rule, print it with normalised whitespace and delimiters, and gets structured, readable messages when the rule is malformed. All of it is verified exhaustively against a truth-table oracle.

## User Stories

1. As a rule author, I want predicates to be able to answer `Unknown`, so that "the data isn't available" is a legitimate result and not an exception.
2. As a rule author, I want an `Unknown` returned by a predicate to not be recorded as a fault, so that faults only mean real failures.
3. As a rule author, I want a predicate that throws, times out or is cancelled to still evaluate as `Unknown` with a recorded fault, so that outages can never turn into `True` or `False`.
4. As a rule author, I want to write `Unknown` as a literal, so that I can model an indeterminate constant.
5. As a rule author, I want `True`, `False`, `Unknown` and every operator name to be case-insensitive, so that capitalisation never breaks a rule.
6. As a rule author, I want the canonical printed form to be consistent upper camel / upper case, so that equivalent rules print identically.
7. As a rule author, I want to write `&&`, `||`, `!`, `∧`, `∨`, `¬`, `⊕`, `→`, `↔`, `↑`, `↓` and `??`, so that I can use the notation I know.
8. As a rule author, I want symbol notation always to compile to the same tree as the named operator, so that notation is a style choice, not a semantic one.
9. As a rule author, I want `IMPLIES(A, B)` to mean `NOT A OR B`, so that implication follows Strong Kleene material implication.
10. As a rule author, I want `EQUIVALENT` (alias `IFF`, legacy `XNOR`) as the biconditional, so that old rules keep working.
11. As a rule author, I want old persisted rules and JSON/YAML using `xnor` to keep compiling, so that upgrading does not break stored rules.
12. As a rule author, I want `NAND` and `NOR`, so that I can express negated conjunction and disjunction directly.
13. As a rule author, I want binary `XOR` and n-ary `NXOR` (parity), so that each operation has one unambiguous name.
14. As a rule author, I want `XOR` with three or more operands to be an error that points me at `NXOR`, so that I don't get parity when I meant exactly-one.
15. As a rule author, I want `ExactlyOne` to keep meaning "exactly one is true", so that it is never confused with parity.
16. As a rule author, I want `NXOR` to be `Unknown` whenever any operand is `Unknown`, so that parity never guesses.
17. As a rule author, I want `ANY`, `ALL`, `NONE` and `BETWEEN(min, max, ...)`, so that common counting rules read naturally.
18. As a rule author, I want cardinality to use the "definitely true / possibly true" interval, so that `Unknown` operands produce correct bounds.
19. As a rule author, I want `COALESCE` / `??` to replace only `Unknown`, so that known values pass through unchanged.
20. As a rule author, I want `If(condition, whenTrue, whenFalse)` / `? :` to be K3-aware, so that an `Unknown` condition does not guess a branch.
21. As a rule author, I want `IsTrue`, `IsFalse`, `IsUnknown` and `IsKnown`, so that I can test a result's state without collapsing the rest of the rule.
22. As a rule author, I want `Project(expr, True|False)` to replace `Unknown` with a chosen value, so that I can decide how indeterminate results behave inside a rule.
23. As an application developer, I want `Collapse(expr, policy)` at the end of evaluation, so that I explicitly decide how `Unknown` becomes a two-valued answer.
24. As an application developer, I want `UnknownIsError` to produce a clear "rejected: unresolved" outcome and no fault, so that I can distinguish "not known" from "something broke".
25. As an application developer, I want `Decision.IsSatisfied` to remain fail-closed (`True` only), so that the safe default is unchanged.
26. As a rule author, I want to mix operators only with explicit parentheses (other than `NOT` > `AND` > `OR`), so that no one is surprised by implicit precedence.
27. As a rule author, I want `()`, `[]` and `{}` to be interchangeable grouping, so that deeply nested rules are easier to read.
28. As a rule author, I want mismatched or unclosed delimiters reported with a precise location, so that I can fix them quickly.
29. As a rule author, I want to print a rule with every delimiter normalised to parentheses, so that stored text is uniform.
30. As a rule author, I want an optional printer that varies delimiters by nesting depth, so that complex rules are readable.
31. As a rule author, I want whitespace collapsed to single spaces, trimmed, with spaces around operators, so that rule text is tidy.
32. As a rule author, I want to optionally expand a rule to primitive operators, so that I can see or analyse its fundamental form.
33. As a rule author, I want to optionally expand a rule to NAND-only or NOR-only form, so that I can target a universal-gate representation.
34. As a rule author, I want to compress an expanded rule back to readable derived operators where possible, so that the result is shorter.
35. As a rule author, I want simplification to produce an equivalent, cheaper rule, so that evaluation does less work.
36. As a rule author, I want simplification to apply only K3-valid rewrites, so that `A OR NOT A` is never rewritten to `True`.
37. As a rule author, I want canonicalisation to give equivalent rules a deterministic representation, so that I can compare and de-duplicate rules.
38. As a rule author, I want to apply any rewrite and be certain the result evaluates identically for every `{True, False, Unknown}` input, so that I can trust optimisation.
39. As a rule author, I want the analyzer to flag contradictions and tautologies only when they hold in K3, so that warnings are never wrong.
40. As a rule author, I want the analyzer to understand `COALESCE`, `If`, inspection and `Project`, so that analysis works on the full language.
41. As a rule author, I want readable messages when a rule from DSL, JSON or YAML is malformed, so that I can fix it without reading source.
42. As a rule author, I want each message to include a code, a plain explanation, the location (line/column or JSON/YAML path), and what was expected versus found, so that the problem is unambiguous.
43. As a rule author, I want "did you mean" suggestions for unknown operators and aliases, so that typos are quick to fix.
44. As a UI developer, I want the validation result as structured data and as plain text, so that I can render it in an editor or a log.
45. As a rule author, I want a lenient-mode substitution for a failed node to be `Unknown`, not `False`, so that errors can never look like negative answers.
46. As a test author, I want `FakePredicates` to return `Unknown` directly, so that tests don't simulate it by throwing.
47. As a maintainer, I want every operator, alias and transform checked against one truth-table oracle, so that K3 correctness is proven, not asserted.
48. As a maintainer, I want the README, CONTEXT.md and ADRs to describe the new language, so that documentation matches behaviour.
49. As a library consumer, I want a gap list of predicates in the spec's catalogue that don't exist yet, so that I know what is still missing.

## Implementation Decisions

- ADR-0005 holds the language decisions (operators, notation, XOR family, equivalence naming, cardinality, `Project`/`Collapse`, delimiters, mutation, structured diagnostics, predicate return type, `Unknown` constants, K3 analyzer). ADR-0003's operator-set and alias decisions are superseded; its remaining decisions stand.
- **Predicates:** `IPredicate` and all built-in predicate delegates return `TruthValue` (breaking, pre-1.0). The externally-resolved-value helper's test delegate also returns `TruthValue`. `FakePredicates` returns `Unknown` directly and keeps a separate fault simulator for exceptions.
- **Constants:** constants are `TruthValue` end to end (AST, parser, builder, canonical printer, JSON/YAML printers and parsers, JSON schema). Lenient and failed-node substitutions use `Unknown`.
- **Derived operators** (`IMPLIES`, `EQUIVALENT`, `XOR`, `NAND`, `NOR`, `ANY`, `ALL`, `NONE`, `BETWEEN`, `Project`) stay first-class tree nodes with their own evaluation, description and printing; their primitive definitions drive expansion and are the oracle's definitions.
- **Primitive kernel:** `NOT`, `AND`, `OR`, `AtLeast`, `AtMost`, `Exactly`, `COALESCE`.
- **Analyzer:** a dual-rail BDD ("definitely true" / "possibly true"), extended per node by each operator slice. An interim slice relabels the existing classical diagnostics so they are not misleading.
- **Boundaries:** `Project` is an in-tree node yielding a definite `TruthValue`. `Collapse` is the evaluation-API boundary (and an outermost-only DSL function); `UnknownIsError` produces an explicit rejected outcome, not a `Fault`.
- **Notation:** named and symbolic input, any letter case; the canonical printer stays word-only with upper camel names; the AST does not retain the written delimiter.
- **Diagnostics:** structured result with code, message, span (or JSON/YAML path), optional suggestion and expected/found pair, plus a plain-text rendering, built on the existing diagnostic model.
- **Mutation transforms** are opt-in operations on the immutable tree; each preserves K3 semantics. Simplification applies only K3-sound rules.
- Existing node-shape seam is already in place; each operator slice still updates parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

## Testing Decisions

- **One seam.** Tests go through the public pipeline: rule text, JSON or YAML → `RuleCompiler.Compile` → `CompilationResult` (compiled rule plus diagnostics) → `EvaluateAsync` with fake predicates → `Decision`. They assert only observable outputs: decision result, faults, evaluated tree, printed text (canonical DSL, JSON, YAML, Mermaid, plain text), and diagnostics (code, message, span).
- **Oracle.** Slice 01 introduces a test helper that computes the expected K3 result from the primitive definitions (`NOT`, `AND`, `OR`, cardinality interval). Every operator, alias, boundary and transform is compared against it over all `{True, False, Unknown}` inputs (operand counts up to 4). No mocks of internals and no assertions on tree-building details.
- **Rewrite tests** are property-style: for any rule and any assignment, `transform(rule)` evaluates equal to `rule`.
- **Test naming and style:** Arrange / Act / Assert, `{MemberUnderTest}_{Scenario}_{Expectation}_Test`, XML comment per test, one logical behaviour per test, NSubstitute for doubles where a double is needed (CLAUDE.md). TDD: write the failing test first.
- **Prior art:** `KleeneOperatorTests` (truth tables), `XorExactlyOneThresholdTests`, `JsonTreeTests`/`YamlTreeTests` (round-trips), `DslRoundTripPropertyTests`, `AnalyzerTests`, `RuleNodeCompilerDiagnosticsTests`, and `TruthWeaver.Testing` fakes.
- **Required validation before any slice is complete:** `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`, `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info`, `dotnet roslynator analyze`.

## Out of Scope

- Implementing the ~50 predicates in the "Final Semantic Inventory" (only a gap list is produced; the existing `predicate-catalog` track owns them).
- Context-bound predicate arguments, new serialization formats, source-preserving delimiter retention, and `UnknownRequiresResolution`.
- A K3 literal argument kind for predicate arguments.
- Performance work beyond not regressing existing benchmarks.

## Further Notes

- `False < Unknown < True` is an implementation aid for truth functions and cardinality bounds. It must not be exposed as a numeric ordering of truth.
- Reference material in `.tmp/` is partly inconsistent (for example `Project`/`Collapse` and the XOR naming); the decisions in ADR-0005 are authoritative where they differ.
- Proposed vertical slices (to be published as tickets by `/to-tickets` after approval of the breakdown):

| # | Slice | Blocked by |
| - | ----- | ---------- |
| 01 | K3 oracle and conformance audit | none |
| 02 | Predicates return `TruthValue` | 01 |
| 03 | `TruthValue` constants, `Unknown` literal, case-insensitivity | 01 |
| 04 | Lenient/failed substitution uses `Unknown` | 03 |
| 05 | Analyzer diagnostics relabelled (interim) | 01 |
| 06 | K3-aware (dual-rail) analyzer | 03, 05 |
| 07 | Symbol aliases for existing operators | 03 |
| 08 | `IMPLIES` and the no-mixing rule | 03, 06 |
| 09 | `EQUIVALENT`/`IFF`/`XNOR` migration | 08 |
| 10 | `NAND`, `NOR` | 08 |
| 11 | `NXOR` and the `XOR` arity hint | 08 |
| 12 | `ANY`, `ALL`, `NONE` | 03, 06 |
| 13 | `BETWEEN` | 12 |
| 14 | `COALESCE` / `??` | 08 |
| 15 | `If` / `? :` | 08 |
| 16 | Inspection operators | 03, 06 |
| 17 | `Project` | 14 |
| 18 | `Collapse` | 17, 02 |
| 19 | Accept `[]` and `{}` | 03 |
| 20 | Delimiter rendering | 19 |
| 21 | Whitespace normalisation and operator spacing | 08 |
| 22 | Expand to primitives | 09, 10, 11, 12, 13, 16 |
| 23 | Expand to NAND-only / NOR-only | 22 |
| 24 | Compress to non-primitive forms | 22 |
| 25 | Canonicalisation | 22 |
| 26 | Simplification | 25, 06 |
| 27 | Structured diagnostics and DSL messages | 19 |
| 28 | JSON/YAML path-based messages | 27 |
| 29 | Predicate catalog gap list | 02 |
