# 02: Direct unit tests for BddManager (highest-risk, zero direct coverage)

**What to build:** A new `BddManagerTests.cs` exercising `src/BooleanRulesEngine/Analysis/BddManager.cs`
directly (needs ticket 01). This is a hand-rolled ROBDD engine with zero direct tests today — every
existing test reaches it only indirectly through `Analyzer`/`RuleCompiler`, which can prove the
final tautology/contradiction diagnostic came out right for the cases tried, but can't cheaply
enumerate the engine's own reduction rules.

Follow TDD: write each test first, run it, and only change `BddManager` if a test reveals an
actual defect (none are currently known — the goal is pinning down behavior, not fixing bugs).

Cover, using `Variable`, `And`, `Or`, `Not`, `Xor`, and `Ite` directly:

- `Variable(0)` returns a stable node id on repeated calls (uniquification: `MakeNode` returns the
  existing entry from `uniqueTable` rather than allocating a new node)
- `And(x, Not(x))` reduces to `BddManager.False` and `Or(x, Not(x))` reduces to `BddManager.True`
  for a variable `x` — the `Ite` terminal short-circuits at lines 80–98 (`i == True`/`i ==
  False`/`t == True && e == False`/`t == e`)
- `And(x, x)` and `Or(x, x)` both reduce to `x` itself (idempotence via the `t == e` short-circuit)
- Two structurally-equal BDDs built independently (e.g. `And(Variable(0), Variable(1))` computed
  twice) return the *same* node id — proves `uniqueTable` correctly shares structurally identical
  sub-BDDs rather than duplicating them
- `Xor(x, y)` matches its truth table for all four combinations of `x`/`y` being
  `BddManager.True`/`BddManager.False`
- A three-variable `Ite(i, t, e)` where `i`, `t`, and `e` each depend on a different variable
  exercises `TopVariable` picking the minimum variable index across all three and `Restrict`
  correctly substituting `true`/`false` for that variable in each — assert the result matches
  brute-force truth-table evaluation across all 8 assignments of the three variables
- Repeated calls to `Ite` with the same `(i, t, e)` triple hit `iteCache` (indirectly verifiable:
  the returned node id is identical across calls, and — if a call counter isn't practical — at
  minimum assert correctness holds under heavy reuse, e.g. building the same sub-expression many
  times inside a larger BDD)

**Blocked by:** 01

**Status:** done

- [x] New `BddManagerTests.cs` under `tests/BooleanRulesEngine.Tests/`
- [x] Every bullet above has a corresponding test (or `[Theory]` cases covering the truth table)
- [x] `dotnet test` passes
- [x] No change to `BddManager.cs` unless a test reveals a real defect — note any such finding in
      this ticket's Comments

## Comments

Added `BddManagerTests.cs` covering `Variable` uniquification, the `Ite` terminal short-circuits
for `And`/`Or` self-negation and idempotence, unique-table structural sharing, the `Xor` truth
table, a three-variable `Ite` cross-checked against brute-force truth-table evaluation, and
`iteCache` reuse under repeated calls. No defects found; no change to `BddManager.cs`; `dotnet
test` passes (307/307).
