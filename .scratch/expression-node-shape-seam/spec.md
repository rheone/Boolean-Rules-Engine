# Shared node-shape seam for Expression

**Status:** ready-for-agent

## Problem Statement

`Expression` (`src/BooleanRulesEngine/Ast/Expression.cs`) is a closed discriminated
union with no shared traversal seam. Six separate places each hand-roll their own
`switch (node) { case ConstantExpression … case ThresholdExpression th … }` to answer
"what is this node's op-name, its threshold `K` (if any), and its operand list":
`Ast/OperatorInfo.cs` (label/description), `Printing/CanonicalPrinter.cs`,
`Json/JsonTreePrinter.cs`, `BooleanRulesEngine.Yaml/YamlTreePrinter.cs`,
`Evaluation/Evaluator.cs`, and `Evaluation/CompiledRule.cs` (`DescribeNode`'s operand
extraction).

Touching one operator family costs far more than these six files alone — tracing the
threshold family end to end (parse, AST, compile, evaluate, describe, print, analyze)
touches roughly 11 files and 14+ switch sites — but this ticket scopes to the
node-shape duplication specifically: the six places above that all re-derive the same
structural fact from an `Expression` node.

ADR-0004 explicitly anticipated some of this ("a new operator needs parser, compiler,
evaluator, and analyzer to know about it — the operator set is small and closed by
design"), but the six-switch node-shape duplication found here grew in afterward
(the printers and `OperatorInfo` postdate ADR-0004) and is roughly 3x the touch-point
count that decision originally scoped.

## Solution

Introduce one internal node-shape helper for `Expression` — given any node, it
returns the node's op-name, its threshold `K` (when applicable), and its operand
list in one shot. `OperatorInfo`, `CanonicalPrinter`, `JsonTreePrinter`,
`YamlTreePrinter`, `Evaluator`, and `CompiledRule` delegate to it instead of
re-deriving the same structural fact independently. Each of the six keeps its own
format-specific rendering/evaluation logic — only the "what shape is this node"
question moves behind the new seam.

Record an amendment to ADR-0004 noting the real current touch-point count for a new
operator, since the fan-out has grown past what that ADR originally described. This
is a record for future maintainers, not a reversal of the closed-operator-set
decision.

## User Stories

1. As a maintainer adding or changing an operator, I want one seam that answers
   "what are this node's operands and op-name," so that I'm not hunting down and
   keeping six independent switches in sync.
2. As a maintainer reading ADR-0004, I want the amendment to reflect the actual
   number of places a new operator touches today, so the historical decision isn't
   read as understating the real cost.
