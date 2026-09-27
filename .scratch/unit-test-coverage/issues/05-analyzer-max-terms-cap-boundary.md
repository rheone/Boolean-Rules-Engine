# 05: Test the Analyzer's MaxAnalysisTerms cap boundary (currently untested)

**What to build:** `AnalyzerTests.cs` covers tautology/contradiction detection across every
operator, but nothing exercises `Analyzer.Analyze`'s early-exit branch at
`src/BooleanRulesEngine/Analysis/Analyzer.cs:27-37`: when a compiled rule's distinct-term count
exceeds `CompilerOptions.MaxAnalysisTerms`, analysis is skipped entirely and a single
`DiagnosticCodes.AnalysisSkippedTooManyTerms` info diagnostic is returned instead of running BDD
construction. This is the one Analyzer branch with zero test coverage — `dotnet outdated`-style
grep confirms no existing test references `AnalysisSkippedTooManyTerms`.

Write these first (TDD), confirm they pass against current behavior, and only touch
`Analyzer.cs` if one fails unexpectedly:

- A rule whose distinct-term count is at or under the configured `MaxAnalysisTerms` runs analysis
  normally (no `AnalysisSkippedTooManyTerms` diagnostic) even if it also contains a genuine
  tautology/contradiction
- A rule whose distinct-term count exceeds `MaxAnalysisTerms` (configure a small cap via
  `CompilerOptions` — check `CompilerLimitsAndLenientModeTests.cs` for how existing tests
  configure limits) produces exactly one `AnalysisSkippedTooManyTerms` diagnostic and *no*
  tautology/contradiction diagnostics, even if the rule structurally contains one — proving
  analysis was actually skipped, not just under-reported
- The boundary itself: a term count exactly equal to `MaxAnalysisTerms` does not trigger the skip
  (the check is `distinctTerms.Count > options.MaxAnalysisTerms`, strictly greater-than) — this is
  the kind of off-by-one that's easy to get backwards and cheap to pin down with a test

**Blocked by:** none (uses the existing public `RuleCompiler` surface — no `InternalsVisibleTo`
needed, since `CompilerOptions` and `DiagnosticCodes` are already public/accessible to tests)

**Status:** done

- [x] All three bullets above have corresponding tests in `AnalyzerTests.cs`
- [x] `dotnet test` passes
- [x] No change to `Analyzer.cs` unless a test reveals a real defect — note any such finding in
      this ticket's Comments

## Comments

Added three tests to `AnalyzerTests.cs` covering: term count at/under `MaxAnalysisTerms` runs
analysis normally, term count exceeding the cap produces exactly one
`AnalysisSkippedTooManyTerms` diagnostic with no tautology/contradiction diagnostics, and a term
count exactly equal to the cap does not trigger the skip (confirming the strictly-greater-than
boundary). No defects found; no change to `Analyzer.cs`; `dotnet test` passes (307/307).
