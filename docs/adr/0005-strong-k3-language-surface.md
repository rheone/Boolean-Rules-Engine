# ADR-0005: Strong K3 language surface

## Status

Accepted. Supersedes the operator-set, alias, `IMPLIES`, `XOR`/`XNOR`
and "word operators only" decisions in
[ADR-0003](0003-rule-syntax-and-serialization.md). Source requirements: `.scratch/2026-10-02-TODO.md`; planning
spec: `.scratch/k3-conformance/spec.md`.

## Context

[ADR-0001](0001-kleene-failure-model.md) made Strong Kleene (K3) the engine's
internal logic. ADR-0003 then deliberately kept the *authoring surface* small:
no `IMPLIES`, no symbol aliases, binary-only `XOR`/`XNOR`, and `ExactlyOne` as
the n-ary "exactly one" operator. The library is now being positioned as a
general-purpose Strong K3 expression engine, and the reference specifications
(`.tmp/`) define a complete K3 language: a primitive kernel, derived operators,
cardinality aliases, K3 value operations (`COALESCE`, `If`), inspection and
boundary functions, and alternate notations. The reasons ADR-0003 declined
those conveniences (two spellings, rule authors getting truth tables wrong)
are outweighed by the goal of conforming to a published K3 operator set, and
the aliases are cheap once the canonical form stays single.

## Decision

1. **Every expression, including every predicate result, is a `TruthValue`**
   (`True`, `False`, `Unknown`). `Unknown` is never implicitly converted to
   `True` or `False`; conversion to a two-valued result happens only at an
   explicit boundary (`Project`, `Collapse`, or the `Decision` API).
   `False < Unknown < True` is an implementation aid for truth functions and
   cardinality bounds, not a numeric ordering of truth.
2. **Operators are accepted in several notations but have one canonical form.**
   Named operators are case-insensitive on input; canonical output is upper
   camel/upper case (`AND`, `OR`, `AtLeast`). Symbol notation (`&&`, `||`,
   `!`, and the logic symbols `∧ ∨ ¬ ⊕ → ↔`) is accepted on input and maps to
   the canonical named operator. The canonical printer and persisted DSL text
   remain word-only. `True`/`False`/`Unknown` literals are case-insensitive,
   and `Unknown` becomes a valid literal.
3. **Primitive kernel and derived operators.** Primitives: `NOT`, `AND`, `OR`,
   `AtLeast`, `AtMost`, `Exactly`, `COALESCE`. Derived: `IMPLIES` (`¬A ∨ B`,
   Strong Kleene material implication), `EQUIVALENT` (alias `IFF`), `XOR`,
   `NAND`, `NOR`, `ANY`, `ALL`, `NONE`, `BETWEEN`. Inspection: `IsTrue`,
   `IsFalse`, `IsUnknown`, `IsKnown`. Conditional: `If` / `? :`. Boundaries:
   `Project`, `Collapse`.
3a. **Derived operators remain first-class AST nodes** with their own
   evaluation, label/description and printing, rather than being desugared at
   parse time. Their primitive definitions are used by the optional
   expand-to-primitives transform, by the analyzer, and as the conformance
   oracle in tests. This preserves the author's operator on round-trip.
4. **XOR family.** `XOR(a, b)` is binary. `NXOR(a, b, ...)` is n-ary **parity**
   (odd number of `True`); any `Unknown` operand yields `Unknown`
   (`.tmp/xor.md`). `ExactlyOne` is retained and is a different operation
   (exactly one `True`, evaluated with the cardinality interval semantics), as
   is `Exactly(1, ...)`. ADR-0003's concern that n-ary XOR is ambiguous is
   resolved by the distinct name `NXOR`.
5. **`EQUIVALENT` replaces `XNOR` as the canonical biconditional.** `IFF` and
   `XNOR` are accepted on input as aliases producing the same node. JSON/YAML
   canonical op name is `equivalent`; `xnor` and `iff` are accepted on read.
   Persisted rules written with `xnor` continue to compile.
6. **Cardinality uses the `[definitely true, possibly true]` interval.**
   `ANY` = `AtLeast(1, ...)`, `ALL` = `AtLeast(n, ...)`, `NONE` = `AtMost(0, ...)`,
   `BETWEEN(min, max, ...)` = `AND(AtLeast(min, ...), AtMost(max, ...))`.
   This supersedes CONTEXT.md's "there are no `All`/`None` operators".

## Amendments (2026-10-02 grilling, round 2)

7. **`XOR` with more than two operands remains a compile error**
   (`XorArityViolation`); the diagnostic message hints at `NXOR` for parity.
8. **Precedence.** `NOT` > `AND` > `OR` is unchanged. Every other infix
   operator (`XOR`, `EQUIVALENT`, `NAND`, `NOR`, `IMPLIES`, `??`) must not be
   mixed with another infix operator at the same nesting level without
   parentheses (compile error, extending the existing `XOR`/`XNOR` rule).
   Function-call forms (`NXOR(...)`, `ANY(...)`, `If(...)`) have no precedence.
9. **Delimiters.** `()`, `[]`, `{}` are interchangeable grouping; the AST does
   not retain which was written. Printing normalizes to parentheses by
   default, with an optional deterministic depth-cycling renderer.
10. **Expression mutation** (primitive/NAND/NOR expansion, compression,
    simplification, canonicalization, whitespace normalization) all ship in
    this effort. Every rewrite must be K3-sound, verified exhaustively against
    the truth-table oracle; classical laws that fail in K3 (for example
    `A OR NOT A = True`) are not applied.
11. **Validation messages are structured**: code, message, span (or
    JSON/YAML path), optional "did you mean" suggestion, and an
    expected-vs-found pair, with a plain-text rendering.

12. **`Project(expr, unknown)`** is an in-tree node that keeps `True`/`False`
    and replaces `Unknown` with the chosen `True` or `False`
    (`.tmp/ProjectAndCollapse.md`). It always yields a definite `TruthValue`
    and equals `COALESCE(expr, unknown)`; it exists as a named alias for intent.
13. **JSON/YAML node shapes** for `If`, inspection, boundaries and the
    `Unknown` literal follow the existing `{"op": ..., "operands": [...]}`
    pattern (literal: `{"op": "unknown"}`) and are recorded here when
    implemented. Implemented so far (k3-conformance 09): `IMPLIES` is
    `{"op": "implies", "operands": [antecedent, consequent]}` (exactly two
    operands, compile-time checked like `XOR`; op name case-insensitive on
    read) in both JSON and YAML (`op: implies`), and `rule-tree.schema.json`
    lists `implies` among the operator ops. The DSL accepts `IMPLIES` and `→`;
    the canonical printer writes `(a IMPLIES b)`; the symbolic tree-printer
    style renders `→`, while the C-style has no spelling for it and keeps
    `IMPLIES`.

    Implemented in k3-conformance 10: `EQUIVALENT` is
    `{"op": "equivalent", "operands": [left, right]}` (exactly two operands,
    compile-time checked with the shared infix arity code); `xnor` and `iff`
    are read-only aliases in both formats (the shared op-name table has a
    separate read table) and the printers always write `equivalent`;
    `rule-tree.schema.json` lists `equivalent`, `iff` and `xnor`. The DSL
    accepts `EQUIVALENT`, `IFF`, `XNOR` and `↔` (all reserved words, any
    case) and the canonical printer writes `(a EQUIVALENT b)`; the tree
    printers use `EQUIVALENT` / `↔` / `==` for the word / symbolic / C-style
    operator styles. **Public API break (pre-1.0):** the AST record
    `XnorExpression` is renamed `EquivalentExpression` (a record cannot be
    type-aliased), its `NodeShape` op-name is `Equivalent`, and the
    description/evaluated-node label is `EQUIVALENT` instead of `XNOR`.
    `RuleBuilder.Xnor` is kept as a forwarding member of the new
    `RuleBuilder.Equivalent`.

    Implemented in k3-conformance 11: `NAND` and `NOR` are strictly binary
    (the spec's operator table says binary, and a chain is ambiguous for a
    non-associative operator), first-class `NandExpression` /
    `NorExpression` nodes with `{"op": "nand" | "nor", "operands": [left,
    right]}` in JSON and YAML (case-insensitive on read, exactly two operands,
    compile-time checked with the shared infix arity code and a parentheses
    hint). The DSL accepts `NAND`, `NOR`, `↑` and `↓` (reserved words, any
    case) as infix operators subject to the no-mixing rule; the canonical
    printer writes `(a NAND b)` / `(a NOR b)`; the symbolic tree-printer style
    renders `↑` / `↓`, while the C-style has no spelling and keeps the words.
    Evaluation and the analyzer rail are the negated primitive
    (`NOT (a AND b)`, `NOT (a OR b)`); both operands are always evaluated.
    `rule-tree.schema.json` lists `nand` and `nor`. `RuleBuilder.Nand` and
    `RuleBuilder.Nor` are new.

    Implemented in k3-conformance 12: `NXOR` is a first-class
    `NxorExpression` function-call node (no precedence, so it needs no
    parentheses next to infix operators) with `{"op": "nxor", "operands":
    [...]}` in JSON and YAML (case-insensitive on read). It takes **two or more**
    operands (fewer is `MalformedTree`, like `AND`/`OR`/`ExactlyOne`), and the
    DSL spelling is `NXOR(a, b, ...)` (reserved word, any case). It is
    `Unknown` whenever any operand is `Unknown`, otherwise `True` for an odd
    number of `True` operands; evaluation folds binary XOR and the analyzer rail
    is the same fold of the XOR rail. The canonical printer writes
    `NXOR(a, b, ...)`; every tree-printer style keeps the word (no symbol or
    C-family spelling). The `XOR` arity message (still `XorArityViolation`) now
    names `NXOR` and `ExactlyOne`. `ExactlyOne` is unchanged and differs from
    `NXOR` from three operands on. `rule-tree.schema.json` lists `nxor`.
    `RuleBuilder.Nxor` is new.

    Implemented in k3-conformance 13: `ANY`, `ALL` and `NONE` are first-class
    `AnyExpression` / `AllExpression` / `NoneExpression` function-call nodes (no
    precedence) with `{"op": "any" | "all" | "none", "operands": [...]}` in JSON
    and YAML (case-insensitive on read) and the DSL spellings `ANY(...)`,
    `ALL(...)`, `NONE(...)` (reserved words, any case). They take **two or more**
    operands, the same minimum as `AND`/`OR`/`ExactlyOne`/`NXOR` (fewer is
    `MalformedTree`); the threshold family's one-operand allowance is not
    inherited because a single-operand `ANY`/`ALL`/`NONE` is just the operand
    or its negation. Semantics are the cardinality interval over the
    definitely-true / possibly-true counts: `ANY` = `AtLeast(1, ...)`, `ALL` =
    `AtLeast(n, ...)`, `NONE` = `AtMost(0, ...)` (evaluation reuses the
    threshold evaluator; the analyzer rail reuses `AtLeast`, with `NONE` as its
    negation). The canonical printer writes `ANY(a, b, ...)` etc.; every
    tree-printer style keeps the word (no symbol or C-family spelling) and the
    evaluated/description label is `ANY`/`ALL`/`NONE`. `rule-tree.schema.json`
    lists `any`, `all` and `none`. `RuleBuilder.Any`, `All` and `None` are new.

    Implemented in k3-conformance 14: `BETWEEN(min, max, op1, op2, ...)` is a
    first-class `BetweenExpression(Min, Max, Operands)` function-call node (no
    precedence): the first two arguments are integer bounds, parsed like the
    threshold family's `k` (a missing or non-integer bound is a `SyntaxError`
    naming the minimum or maximum), then the operands. It is
    `AND(AtLeast(min, ...), AtMost(max, ...))` over the definitely-true /
    possibly-true interval (the evaluator ANDs the two threshold results; the
    analyzer rail is `AtLeast(min)` AND NOT `AtLeast(max + 1)`). Operand
    minimum: **two or more**, as `ANY`/`ALL` (`MalformedTree`). Bound range:
    `0 <= min <= max <= n` for `n` operands, and the whole range `0..n` is
    rejected because the node would be the constant `True`, the same
    structural-constant rationale as the threshold family (all of these are
    `InvalidThresholdValue`, with a message naming `min=`, `max=` and the
    allowed range). JSON/YAML: `{"op": "between", "min": 1, "max": 2,
    "operands": [...]}` (case-insensitive op, integer `min`/`max` required
    else `MalformedTree`); `rule-tree.schema.json` has a `betweenOperatorNode`.
    `NodeShape` gained an optional `Max` (its `K` carries `min`). The
    canonical printer writes `BETWEEN(1, 2, a, b, c)`; the evaluated and
    description label is `BETWEEN(min, max)`, kept as a word in every
    `OperatorStyle`. `RuleBuilder.Between(min, max, operands)` is new.

    Implemented in k3-conformance 15: `COALESCE(a, b, ...)` and the infix `??`
    build one `CoalesceExpression(Operands)`: the first operand that is not
    `Unknown` (`True`/`False` pass through; `Unknown` only if all are).
    `??` is an infix operator under decision 8: it cannot share a level with
    `AND`/`OR` or another infix operator without parentheses. **Chains are
    accepted**: `a ?? b ?? c` is one three-operand node, because coalescing is
    associative (unlike the binary-only `IMPLIES`/`NAND`/`NOR`, whose chains
    stay errors); its operands are `NOT`-level expressions. Only the `??`
    token is infix; the word `COALESCE` is a function call only (a lone `?` is
    a lexical error). Two or more operands are required (`MalformedTree`).
    Evaluation is left to right and stops at the first non-`Unknown` operand
    (skipped operands appear as `NotEvaluated` in the evaluated tree and trace,
    like `AND`/`OR`; `EvaluationMode.Exhaustive` evaluates all). The analyzer
    rail folds from the right: with `(D, P)` the definite/possible rails of
    `x`, `COALESCE(x, y)` is `(D_x OR (P_x AND D_y), P_x AND (D_x OR P_y))`.
    The canonical printer writes the function-call form `COALESCE(a, b)`; the
    tree printers spell the label `COALESCE` / `??` / `??` for the word /
    symbolic / C-style styles. JSON/YAML op `coalesce`; `RuleBuilder.Coalesce`
    is new.

14. **`Collapse(expr, policy)`** is the final boundary that produces a
    two-valued application result. Policies: `UnknownAsFalse`,
    `UnknownAsTrue`, `UnknownIsError`. `Unknown` is a normal K3 value, not a
    failure: `UnknownIsError` yields an explicit "rejected: unresolved"
    outcome and never a `Fault` or exception; `Decision.Faults` is reserved
    for real predicate exceptions, timeouts and cancellation. It lives on the
    evaluation API and is accepted in the DSL only as the outermost function
    (never nested). `UnknownRequiresResolution` is out of scope.
15. **Predicates return `TruthValue`.** `IPredicate` and every predicate
    delegate return `TruthValue` (breaking change, pre-1.0). A returned
    `Unknown` records no `Fault`; an exception, timeout or cancellation still
    becomes `Unknown` plus a `Fault` (ADR-0001).
16. **`Unknown` is a first-class constant and substitution value.**
    Constants are `TruthValue`s end to end. Lenient-mode and failed-node
    substitutions use `Unknown`, not `false`. `True`, `False`, `Unknown` and
    every operator name are case-insensitive; the canonical printer writes
    `True`, `False`, `Unknown` and upper-case operators. Serialized form
    (implemented in k3-conformance 03): JSON `{"const": true|false}` is kept,
    and `Unknown` is `{"const": "unknown"}` (a string constant in any letter
    case is also accepted for all three values); YAML is `const: unknown`.
17. **The analyzer is K3-aware.** A dual-rail BDD tracking "definitely true"
    and "possibly true" replaces classical two-valued analysis, so
    `A AND NOT A` is a K3 contradiction only when it is, and `A OR NOT A` is
    not a tautology. Implemented in k3-conformance 06 (the interim relabel
    from ticket 05 is superseded). Each term contributes two independent BDD
    variables (is `True`; is `Unknown`), so every variable setting is a valid
    K3 state. A sub-expression is reported as a tautology (`BRE0012`) when its
    definitely-true rail is constant true and as a contradiction (`BRE0013`)
    when its possibly-true rail is constant false. The `Structural*` constant
    names and codes are kept for stability. Each later operator slice extends
    the analyzer with its own rail definition.

## Open decisions

None.

## Consequences

- ADR-0003's "closed operator set" and "no aliases" statements no longer hold;
  its remaining decisions (named arguments, literal-only arguments, pipeline,
  precedence of `NOT > AND > OR`) stand.
- Existing public API names (`XNOR`, `RuleBuilder.Xnor`) need aliases or
  deprecation, tracked in the planning tickets.
- The equivalency table in CONTEXT.md becomes a description of derived-operator
  definitions rather than a list of non-existent operators.
