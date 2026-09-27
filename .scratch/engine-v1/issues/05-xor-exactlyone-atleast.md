# 05: XOR / ExactlyOne / AtLeast operators

**What to build:** The remaining operators from ADR-0003's closed operator set: `XOR` (binary only — a compile error if given more than two operands), `ExactlyOne(...)` (n-ary, true iff exactly one operand is `True`), and `AtLeast(k, ...)` (n-ary threshold, true iff at least `k` operands are `True`). All three follow the general Kleene principle: the result is determinate only when it's determinate regardless of what any `Unknown` operand would have resolved to; otherwise the result is `Unknown`.

Mixing `XOR` with `AND`/`OR` in the same expression without parentheses is a compile error (not resolved by a precedence rule) — this is a deliberate ambiguity rejection, not an oversight, per ADR-0003.

**Blocked by:** 04

**Status:** done (verified against existing codebase — already implemented prior to this pass)

- [ ] `XOR(a, b)` parses and evaluates correctly for all nine combinations of `True`/`False`/`Unknown` on `a` and `b`
- [ ] `XOR` given three or more operands is a compile `Error` diagnostic, not silently accepted with parity semantics
- [ ] `a AND b XOR c` (or any `XOR` mixed with `AND`/`OR` at the same nesting level without parentheses) is a compile `Error` diagnostic; `a AND (b XOR c)` compiles successfully
- [ ] `ExactlyOne(a, b, c)` evaluates to `True` iff exactly one operand is `True` and the rest are `False`; involves `Unknown` correctly (e.g. two `Unknown` operands where the rest are `False` yields `Unknown`, since either or neither could resolve to make the count exactly one)
- [ ] `AtLeast(2, a, b, c)` evaluates to `True` iff at least 2 of the 3 operands are `True`, and to `Unknown` rather than a wrong determinate answer when the count of confirmed `True`/`False` operands doesn't yet settle the threshold
- [ ] `AtLeast(k, ...)` with `k` greater than the operand count, or with `k <= 0`, is a compile `Error` diagnostic
