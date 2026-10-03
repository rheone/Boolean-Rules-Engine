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
   Implemented (k3-conformance 20): the lexer has `{`/`}` tokens and every
   grouping site accepts any of the three pairs, but the argument list of a
   function-call operator or term (`ANY(...)`, `Role(name: "x")`) is still
   `(` only, because the decision says "grouping" and the reference material
   only shows delimiters around sub-expressions. A closer must match its
   opener: a wrong closer is a `SyntaxError` at that closer ("Expected ')' to
   close '(' at offset 4 but found ']'."), end of input is reported at the
   opener ("Unclosed '(' ..."), and a closer with nothing open is reported as
   "Unexpected closing ..." at the closer.
   Implemented (k3-conformance 21): `CompiledRule.PrintText(GroupingStyle)`
   with `GroupingStyle.Parentheses` (identical to `CanonicalText`, the default
   and persisted form) and `GroupingStyle.DepthCycling`. The cycle is by group
   depth: outermost group `(`, then `[`, then `{`, then repeating (the
   reference example `A AND (B OR [C AND {D OR (E AND [F OR G])}])`). Only
   groups the printer wraps count as depth; function-call argument lists stay
   `(` and do not deepen it. The reference material's "option to convert all
   delimiter pairs to parens" is the default, so it needs no flag.
   Whitespace normalization is implemented (k3-conformance 22) as the
   text-level `RuleText.NormalizeWhitespace`, which works on text as written
   (no compile, no registry, keeps operators, case and delimiters) and is
   idempotent; the canonical printer already prints single spaces around
   infix operators and after commas, so printed text needs nothing more.
10. **Expression mutation** (primitive/NAND/NOR expansion, compression,
    simplification, canonicalization, whitespace normalization) all ship in
    this effort. Every rewrite must be K3-sound, verified exhaustively against
    the truth-table oracle; classical laws that fail in K3 (for example
    `A OR NOT A = True`) are not applied.

    Implemented in k3-conformance 23 (primitive expansion):
    `CompiledRule<TContext>.ExpandToPrimitives()` returns a new rule (the original
    is immutable and untouched; the declared `CollapsePolicy` is carried over)
    whose tree holds only the kernel (`NOT`, `AND`, `OR`, `AtLeast`, `AtMost`,
    `Exactly`, `COALESCE`), constants and terms. Definitions, each verified
    exhaustively against the oracle (and a random-rule property test over every
    assignment): `IMPLIES` = `OR(NOT a, b)`; `XOR` = `OR(AND(a, NOT b), AND(NOT a,
    b))`; `EQUIVALENT` = `OR(AND(a, b), AND(NOT a, NOT b))`; `NAND`/`NOR` = `NOT`
    of `AND`/`OR`; `ExactlyOne` = `Exactly(1, ...)`; `ANY`/`ALL`/`NONE` =
    `AtLeast(1)`/`AtLeast(n)`/`AtMost(0)`; `BETWEEN` = `AND(AtLeast(min),
    AtMost(max))` with a vacuous bound (`min = 0` or `max = n`) dropped, since the
    compiler rejects those thresholds as constants; `GreaterThan(k)` =
    `AtLeast(k + 1)` and `LessThan(k)` = `AtMost(k - 1)` (the compiler's valid
    ranges map exactly onto the targets' valid ranges); `If` = the multiplexer
    plus consensus term of decision 13; `Project(x, v)` = `COALESCE(x, v)`.
    **`NXOR` (parity)** is `OR(Exactly(1, ...), Exactly(3, ...), ...)` over every
    odd count rather than a fold of the `XOR` expansion: with no `Unknown`
    operand the interval is a single count and the disjunction is `True` iff it is
    odd; with an `Unknown` operand the interval has two or more consecutive
    counts, so every matching `Exactly(k)` is `Unknown` and at least one odd count
    lies inside it, which gives `Unknown`. That is exactly parity's "Unknown if
    any operand is Unknown", and the size is linear (a fold repeats its
    accumulator twice per step, growing exponentially). **The inspections need no
    semantic boundary:** `COALESCE` is the one primitive that can observe
    `Unknown`, so `IsTrue(x)` = `COALESCE(x, False)`, `IsFalse(x)` =
    `COALESCE(NOT x, False)`, `IsUnknown(x)` = `AND(COALESCE(x, True), COALESCE(NOT
    x, True))` and `IsKnown(x)` = `OR(COALESCE(x, False), COALESCE(NOT x, False))`.
    Operands a definition repeats are one shared, already-expanded node (a DAG in
    memory; the printed text repeats them), so a deeply nested expansion can
    exceed the default compile node limit when recompiled.
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

    Implemented in k3-conformance 16: `If(condition, whenTrue, whenFalse)` and
    the ternary `condition ? whenTrue : whenFalse` build one
    `IfExpression(Condition, WhenTrue, WhenFalse)`. **Semantics:** a `True`
    condition yields `whenTrue`, a `False` one `whenFalse`; an `Unknown`
    condition does not guess a branch, so the result is the branch value only
    when both branches are the same definite value, else `Unknown`
    (`.tmp/Strong Kleene K3 Logic.md` section 25). The primitive definition,
    used by the analyzer rail and the test oracle, is therefore the
    multiplexer plus its consensus term: `(c AND t) OR (NOT c AND f) OR
    (t AND f)`. The bare multiplexer `(c AND t) OR (NOT c AND f)` was
    rejected because it yields `Unknown` for `If(Unknown, True, True)`, which
    contradicts the "does not guess a branch" intent and the reference
    specification; the consensus term never changes the result of a definite
    condition. **Evaluation** skips the branch a definite condition does not
    need (recorded as `NotEvaluated`, like `AND`/`OR`/`COALESCE`); an `Unknown`
    condition evaluates both; `EvaluationMode.Exhaustive` evaluates both. The
    analyzer rail is the primitive definition over the dual rails, so
    `If(a, b OR True, c OR True)` is a tautology even for an `Unknown` `a`.
    **Syntax:** `If` is a reserved function-call word (any case) taking
    exactly three operands (`MalformedTree` otherwise). The lone `?` and `:`
    form the ternary, the lowest-precedence construct, accepted wherever a full
    expression is (the root, parentheses, call arguments). Under decision 8 the
    condition and each branch must each be a single operand or a parenthesized
    group: a bare `AND`/`OR` chain, a bare infix expression (`XOR`, `??`, ...)
    or an unparenthesized nested ternary in any of the three positions is
    `AmbiguousOperatorMixing`. The canonical printer writes the function-call
    form `If(a, b, c)`; tree printers and the evaluated/description label are
    `If` in every `OperatorStyle` (no symbolic or C-style spelling). JSON/YAML:
    `{"op": "if", "operands": [condition, whenTrue, whenFalse]}` (op name
    case-insensitive on read; `rule-tree.schema.json` lists `if`).
    `RuleBuilder.If` is new.

    Implemented in k3-conformance 17: `IsTrue(x)`, `IsFalse(x)`, `IsUnknown(x)`
    and `IsKnown(x)` are **one** `InspectionExpression(Kind, Operand)` node with
    an `InspectionKind` (the smallest design consistent with the `NodeShape`
    seam: like the threshold family they differ only in what they test, and
    `NodeShape.OpName` is the kind's name). They are function calls (reserved
    words, any case) taking exactly one operand (`MalformedTree` otherwise) and
    always yield a definite `True` or `False`: `IsTrue` is "is `True`",
    `IsFalse` is "is `False`", `IsUnknown` is "is `Unknown`", `IsKnown` is "is
    not `Unknown`". The operand's own faults are recorded as usual but the
    inspection adds none, and a definite result never collapses the rest of
    the enclosing rule. The operand is always evaluated. The analyzer rail has
    equal definite and possible rails: with `(D, P)` the rails of `x`, `IsTrue`
    is `D`, `IsFalse` is `NOT P`, `IsUnknown` is `P AND NOT D`, `IsKnown` is `D
    OR NOT P`, so `IsUnknown(a) OR IsKnown(a)` is a genuine tautology. The
    canonical printer writes `IsTrue(a)` etc.; the evaluated/description label
    is the same word in every `OperatorStyle`. JSON/YAML: `{"op": "isTrue" |
    "isFalse" | "isUnknown" | "isKnown", "operands": [x]}` (case-insensitive on
    read, one operand checked by the compiler; the schema lists them in the
    unary node). `RuleBuilder.IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown` are new.

    Implemented in k3-conformance 18: `Project(expr, True|False)` is a
    first-class `ProjectExpression(Operand, UnknownAs)` node (`UnknownAs` is a
    `bool`, so an `Unknown` replacement cannot even be represented). It keeps
    `True`/`False` and replaces `Unknown` with the chosen constant, so it is
    always definite and is the same value as `COALESCE(expr, value)`; the
    operand's own faults are recorded as usual (a faulting predicate is
    `Unknown` plus a `Fault`, the projection makes the *value* definite but does
    not hide the fault). **Syntax:** `Project` is a reserved function-call word
    (any case) taking exactly two arguments, an expression and the literal
    constant `True` or `False` (any case). The second argument is parsed as a
    full expression and then checked, so every bad shape is a `SyntaxError` over
    the offending argument's span: `Unknown` gets a dedicated message ("Project
    replaces Unknown, so it needs the constant True or False to replace it
    with"), any other non-constant (`Project(a, b)`, `Project(a, NOT True)`)
    says it must be the constant `True` or `False`, and a missing or third
    argument names the expected shape. **JSON/YAML:** `{"op": "project",
    "unknownAs": true, "operands": [x]}` (YAML `op: project`, `unknownAs:
    true`): the policy rides in an `unknownAs` field next to the single operand,
    as `BETWEEN` carries `min`/`max` and the threshold family `k`, so it is not
    an operand. It is written as a plain boolean (like a `True`/`False` `const`)
    and read as a boolean or the string `"true"`/`"false"` in any letter case; a
    missing, `"unknown"` or non-boolean value is `MalformedTree`, and the operand
    count (exactly one) is a compiler `MalformedTree` like the inspections.
    `rule-tree.schema.json` has a `projectOperatorNode`. `NodeShape` gained an
    optional `UnknownAs`. **Analyzer:** both rails are the same BDD, `P` (the
    operand's possible rail) for `Project(x, True)` and `D` for `Project(x,
    False)`, so `Project(a, True) OR NOT Project(a, True)` is a genuine
    tautology. The canonical printer writes `Project(a, True)`; the evaluated and
    description label is `Project(True)` / `Project(False)`, kept as a word in
    every `OperatorStyle`. `RuleBuilder.Project(operand, unknownAs)` is new.
    Unlike `Collapse` (decision 14) it may appear anywhere in a rule.

14. **`Collapse(expr, policy)`** is the final boundary that produces a
    two-valued application result. Policies: `UnknownAsFalse`,
    `UnknownAsTrue`, `UnknownIsError`. `Unknown` is a normal K3 value, not a
    failure: `UnknownIsError` yields an explicit "rejected: unresolved"
    outcome and never a `Fault` or exception; `Decision.Faults` is reserved
    for real predicate exceptions, timeouts and cancellation. It lives on the
    evaluation API and is accepted in the DSL only as the outermost function
    (never nested). `UnknownRequiresResolution` is out of scope.

    Implemented in k3-conformance 19. **Public API** (in
    `TruthWeaver.Abstractions`): the `CollapsePolicy` enum, the `CollapseOutcome`
    enum (`False`, `True`, `RejectedUnresolved`), `Decision.Collapse(CollapsePolicy)`
    (a pure function of `Decision.Result`: `True`/`False` map to themselves and only
    `Unknown` depends on the policy) and a trailing optional `Decision.Outcome`
    (`CollapseOutcome?`, `null` when the rule declared no collapse, so existing
    callers see no change). `RejectedUnresolved` is a normal outcome: it is not a
    `Fault`, nothing is thrown, `Decision.Faults` is not touched (a faulting
    predicate still records its fault, which is how "not known" and "something
    broke" stay distinguishable), and `Decision.IsSatisfied` is unchanged
    (`Result == True`, fail-closed). **Where a DSL-declared policy lives:**
    `Collapse(expr, policy)` is not an `Expression` node. The parsers build a raw
    `CollapseNode` wherever the text puts it; `RuleNodeCompiler` peels a *root*
    one into the outer policy and reports every other occurrence (inside an
    operator, a `Project`, an `If` branch, another `Collapse`, a builder operand)
    as the new error `NestedCollapse` (`BRE0016`) whose span is exactly the nested
    collapse expression. `CompiledRule.CollapsePolicy` (`CollapsePolicy?`) holds the
    outer policy, so the analyzer, `ExpressionShape`, `RuleDiff` and every operator
    see only the inner expression and the analyzer analyzes it unchanged.
    **Evaluation:** `EvaluateAsync` evaluates the inner expression, then applies the
    declared policy: `Decision.Outcome` is set; for `UnknownAsFalse`/`UnknownAsTrue`
    `Decision.Result` becomes the collapsed definite value (so `IsSatisfied` follows
    the explicit choice the author wrote), and for `UnknownIsError` it stays the
    three-valued result (`Unknown` when rejected, so `IsSatisfied` is `false`). The
    uncollapsed value remains the single child of the evaluated tree. **Printing and
    rendering:** the canonical text is `Collapse(inner, UnknownAsFalse)` (policy in
    canonical case, parsed in any case), recompiling to the same rule;
    `CompiledRule.Describe()` and the evaluated tree both gain a root labelled
    `Collapse(UnknownAsFalse)` etc. wrapping the inner tree (so the plain-text and
    Mermaid renderers need no change and the description/evaluated trees stay
    aligned), with no symbolic or C-style spelling in any `OperatorStyle`.
    **JSON/YAML:** the outermost collapse is supported as a node of its own,
    `{"op": "collapse", "policy": "unknownAsFalse", "operands": [rule]}` (YAML
    `op: collapse`, `policy: unknownAsFalse`; op and policy names case-insensitive on
    read, a missing or unknown policy is `MalformedTree`, exactly one operand), so a
    rule that declares a collapse round-trips through every format; a nested
    collapse node is `NestedCollapse` like in the DSL. `rule-tree.schema.json` has
    a `collapseOperatorNode` (it validates a nested one structurally; the
    outermost-only rule is the compiler's). `RuleBuilder.Collapse(operand, policy)`
    is new. `RuleDiff` reports a changed, added or removed collapse policy as one
    `Changed` entry at the root, and diffs the inner expression otherwise.
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
