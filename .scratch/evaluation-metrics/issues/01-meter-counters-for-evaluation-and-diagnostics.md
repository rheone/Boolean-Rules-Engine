# 01: Meter-based counters for evaluation, faults, and compile diagnostics

**What to build:** A `Meter` (e.g. `"BooleanRulesEngine"`) exposing `Counter<long>` instruments for evaluations performed, faults recorded, and compile diagnostics raised (tagged by `DiagnosticSeverity`), incremented at the appropriate points in `EvaluateAsync` and `Compile`/`CompileJson`/`CompileYaml`.

**Blocked by:** none

**Status:** ready-for-agent

- [ ] A `Meter` is exposed with counters for: evaluations performed, faults recorded, compile diagnostics raised (tagged by `DiagnosticSeverity`)
- [ ] Counters increment correctly under a test using a `MeterListener`, verified for at least one evaluation and one compile that raises a diagnostic
- [ ] No new third-party dependency is introduced
- [ ] Documented in the README's Feature highlights section alongside the existing structured-logging bullet
