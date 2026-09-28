# 23: Cover Evaluator.Describe's XOR/XNOR/ExactlyOne/threshold diagnostic labels

**What to build:** `Evaluator<TContext>.Describe`'s switch over `NodeShape.OpName` (src/TruthWeaver/Evaluation/Evaluator.cs, lines ~59-68) has no coverage for the `"Xor"`, `"Xnor"`, or `"ExactlyOne"` arms, nor for the `default` arm's `$"{shape.OpName}({shape.K})"` threshold formatting. [[22-evaluator-describe-boolean-op-labels]] covers the NOT/AND/OR arms of the same switch.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A fault or trace diagnostic produced for a `Xor` node describes it as `"XOR"`, and for `Xnor` as `"XNOR"`.
- [x] A fault or trace diagnostic produced for an `ExactlyOne` node describes it as `"ExactlyOne"`.
- [x] A fault or trace diagnostic produced for a threshold node not covered by the named arms (e.g. an `AtLeast`/`AtMost` node) is described as `"<OpName>(<K>)"`, matching the `default` arm's formatting.
- [x] Existing `Evaluator` diagnostic/trace tests continue to pass unchanged.
