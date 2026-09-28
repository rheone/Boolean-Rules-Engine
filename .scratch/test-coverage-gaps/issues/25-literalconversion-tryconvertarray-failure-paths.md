# 25: Cover LiteralConversion.TryConvertArray's failure paths

**What to build:** `LiteralConversion.TryConvertArray` (src/TruthWeaver/Compilation/LiteralConversion.cs, lines ~112-135) has no coverage for either failure path: the non-array-form/null-elements guard (lines ~114-118) or a per-element conversion failure partway through the array (lines ~124-128) — only the all-elements-succeed path is exercised today. This runs on every array-typed literal in every compiled rule; an untested failure path risks a malformed array literal being silently accepted (or a valid one rejected) instead of surfacing the intended compile error.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `TryConvert` against an array-typed `LiteralKind` with a non-array raw literal (or a null `Elements`) returns `false` without throwing.
- [x] `TryConvert` against an array literal where one element doesn't match the expected element kind (e.g. a string mixed into an expected `Int64Array`) returns `false`, and no partial `LiteralValue` is exposed to the caller.
- [x] Existing array-literal compile tests continue to pass unchanged.
