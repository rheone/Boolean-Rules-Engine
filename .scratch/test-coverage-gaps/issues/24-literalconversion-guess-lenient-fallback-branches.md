# 24: Cover LiteralConversion.Guess's boolean/decimal branches and GuessArray's empty-array branch

**What to build:** `LiteralConversion.Guess` (src/TruthWeaver/Compilation/LiteralConversion.cs) has no coverage for its `RawLiteralForm.Boolean` branch (line ~85) or the decimal-parsing branch of `RawLiteralForm.Number` (lines ~90-91, the `text.Contains('.')` path) — only the integer-parsing branch is exercised. `GuessArray`'s empty-elements branch (line ~141) is also untested. `Guess`/`GuessArray` are the lenient-mode fallback used for a term bound to an unregistered predicate (per the method's own doc comment) — the term is never evaluated, but still needs a stable inferred identity for memoization, so a wrong inference here would silently misidentify lenient-mode terms.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `Guess` on a boolean-form raw literal returns the corresponding `LiteralValue.OfBoolean`.
- [x] `Guess` on a number-form raw literal containing a `.` (e.g. `"1.5"`) returns a `LiteralValue.OfDecimal` with the parsed value.
- [x] `Guess` on an array-form raw literal with zero elements returns an empty `LiteralValue.OfArray(LiteralKind.String, [])`.
- [x] Existing lenient-mode/unresolved-predicate tests continue to pass unchanged.
