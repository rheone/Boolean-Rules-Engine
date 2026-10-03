# 06: K3-aware (dual-rail) analyzer

**What to build:** The analyzer reasons in K3 using a dual-rail BDD (definitely true / possibly true), so contradictions and tautologies are reported only when they hold for every {True, False, Unknown} assignment.

**Blocked by:** 03, 05

**Status:** done

- [x] A AND NOT A and A OR NOT A are not reported as contradiction/tautology; genuine K3 ones are
- [x] Analyzer results agree with the oracle over all assignments
- [x] Max-terms cap behaviour preserved
- [x] Interim relabel from 05 is removed or superseded
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

The analyzer now builds a dual-rail BDD (`DualRail(Definite, Possible)`) per sub-expression. Each term gets two independent BDD variables (is `True`; is `Unknown`) with rails `d` and `d OR q`, so every variable setting is a valid K3 state and no consistency constraint is needed. `NOT x = (not Possible, not Definite)`; `AND`/`OR` act rail-wise; `XOR` uses the primitive `(x AND NOT y) OR (NOT x AND y)`; cardinality is `AtLeast` on each rail with `AtMost`/`LessThan` as negations and `Exactly` as `AtLeast(k) AND NOT AtLeast(k+1)`. A sub-expression is a tautology (`BRE0012`) when its definite rail is constant true and a contradiction (`BRE0013`) when its possible rail is constant false. The `Unknown` literal is the constant (0,1); the interim fresh-variable workaround was removed. `MaxAnalysisTerms` behaviour is unchanged.

Decisions: the `Structural*` constant names and codes are kept (non-breaking; XML docs call the name historical); only the message text changed (issues-log row 6). The interim relabel from ticket 05 is superseded and its two-valued caveat removed. Verified with an oracle property test (`Compile_GeneratedRules_AnalyzerVerdictsAgreeWithK3OracleOverAllAssignments_Test`): 400 seeded random rules, every sub-expression compiled as its own root and compared with `K3Oracle` over all 27 assignments of three terms.
