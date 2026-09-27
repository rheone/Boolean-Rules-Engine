# 06: Canonical DSL printer & round-trip

**What to build:** A canonical printer that renders a compiled expression tree back to DSL text — the form a `CompiledRule` round-trips back to. The printer is deterministic (the same tree always prints identically), emits minimal-but-unambiguous parentheses, and always parenthesizes `XOR` explicitly regardless of context. `parse(print(x))` is structurally equal to `x` for every rule shape supported so far (all operators from tickets 03–05, arguments and constants from 02/04).

**Blocked by:** 05

**Status:** done (verified against existing codebase — already implemented prior to this pass)

- [ ] `CompiledRule` exposes a canonical printed string form (e.g. `ToString()` or an explicit `Print()`/`CanonicalText` member)
- [ ] Printing the worked example from ADR-0003 (`hasRole(role: "Y") AND (hasTraining(training: "Q") OR hasTraining(training: "Z") OR (isManager XOR isDepartmentHead))`) reproduces that exact text
- [ ] Printing never omits parentheses around an `XOR` operand, even when the surrounding precedence would otherwise make them unnecessary
- [ ] For a representative set of rules covering every operator (`AND`, `OR`, `NOT`, `XOR`, `ExactlyOne`, `AtLeast`, constants, argument-bearing and zero-arg terms), parsing the printed output produces a tree structurally equal to the original compiled tree
- [ ] Printing the same tree twice produces byte-identical output (determinism)
