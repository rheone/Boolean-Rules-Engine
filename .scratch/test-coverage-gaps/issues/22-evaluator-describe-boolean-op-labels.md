# 22: Cover Evaluator.Describe's NOT/AND/OR diagnostic labels

**What to build:** `Evaluator<TContext>.Describe`'s switch over `NodeShape.OpName` (src/TruthWeaver/Evaluation/Evaluator.cs, lines ~59-68) has no coverage for the `"Not"`, `"And"`, or `"Or"` arms — only the `ConstantExpression`/`TermExpression` branches above the switch and the `default` arm are exercised today. `Describe` produces the human-readable node labels used in evaluation trace/fault diagnostics, so an untested label mapping means a fault involving a NOT/AND/OR node could silently print the wrong diagnostic text. [[23-evaluator-describe-xor-family-labels]] covers the remaining arms of the same switch.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A fault or trace diagnostic produced for a `Not` node describes it as `"NOT"`.
- [x] A fault or trace diagnostic produced for an `And` node describes it as `"AND"`.
- [x] A fault or trace diagnostic produced for an `Or` node describes it as `"OR"`.
- [x] Existing `Evaluator` diagnostic/trace tests continue to pass unchanged.
