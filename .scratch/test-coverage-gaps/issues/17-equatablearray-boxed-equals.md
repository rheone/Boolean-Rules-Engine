# 17: Cover EquatableArray's boxed `Equals(object?)`

**What to build:** `EquatableArray<T>.Equals(object?)` (src/TruthWeaver.Abstractions/EquatableArray.cs, line ~55) has 0% branch coverage per the ticket 11 Cobertura report — neither the `obj is EquatableArray<T>` success path nor its false path (wrong type, or `null`) is exercised. This is the override that `object.Equals` dispatch, boxing scenarios, and any `EqualityComparer<T>.Default` usage over `EquatableArray<T>` fall back to.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `Equals((object)other)` returns `true` when `other` is an `EquatableArray<T>` boxed as `object` with equal elements.
- [x] `Equals((object)other)` returns `false` when `other` is a boxed `EquatableArray<T>` with different elements.
- [x] `Equals(someUnrelatedObject)` returns `false` for a non-`EquatableArray<T>` object, and `Equals(null)` returns `false`.
- [x] Existing `EquatableArray` tests continue to pass unchanged.
