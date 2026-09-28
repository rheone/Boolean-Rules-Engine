# 06: Cover heterogeneous-array literal guessing

**What to build:** Verify literal-kind guessing behaves correctly when a rule under lenient compilation mode supplies an array literal with mixed element kinds against an unregistered predicate, exercising `LiteralConversion.Guess`'s array-mismatch fallback path.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] Compiling, under `CompilationMode.Lenient`, a rule whose array literal has heterogeneous element kinds against an unregistered predicate produces the expected guessed literal (or diagnostic) instead of throwing unexpectedly.
- [ ] The `catch (ArgumentException)` fallback path in `Guess` is exercised by at least one test case.
- [ ] Existing lenient-mode compilation tests continue to pass unchanged.
