# Baseline results

Captured 2026-09-27 on:

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen 5 5600 3.50GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), X64 RyuJIT x86-64-v3

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  LaunchCount=1  WarmupCount=3
```

Reproduce with (see the repo README's "Benchmarks" section for the full command reference):

```powershell
dotnet run -c Release --project benchmarks/BooleanRulesEngine.Benchmarks -- --filter "*" --job Short --inProcess --exporters github --artifacts ./benchmarks/BooleanRulesEngine.Benchmarks/results
```

`--job Short` (~5-8s/case) keeps this baseline capture quick; re-run without `--job` (the BenchmarkDotNet default preset) for higher-confidence numbers before relying on them to judge a real regression.

## Compile-time cost (`CompileBenchmarks.Compile`)

Two representative rule sizes (`RuleFixtures.BuildGroupedRule`): a **Small** rule (10 distinct terms,
5 two-term `OR` groups) and a **Large** rule (200 distinct terms, 50 four-term `OR` groups, ~250 nodes)
compiled with `CompilerOptions.MaxAnalysisTerms` raised so the BDD-based tautology/contradiction
analyzer (`Analysis.Analyzer`/`Analysis.BddManager`) actually runs across the whole tree instead of
being skipped past the default 20-term cap.

| Method  | Size  | Mean         | Error      | StdDev     | Allocated  |
|-------- |------ |-------------:|-----------:|-----------:|-----------:|
| Compile | Small |     8.424 μs |   7.029 μs |  0.3853 μs |   19.09 KB |
| Compile | Large | 1,062.196 μs | 397.256 μs | 21.7749 μs | 1863.57 KB |

## Eval-time memoized term lookup (`EvaluationBenchmarks.EvaluateAsync`)

A rule shaped as an `OR` of `BranchCount` `AND` branches, every branch referencing the same shared term
alongside one branch-unique term (`RuleFixtures.BuildSharedTermFanOut`), evaluated in
`EvaluationMode.Exhaustive` so every branch actually runs. Per-evaluation term memoization (ADR-0002)
means the shared term is invoked at most once per evaluation regardless of `BranchCount`, while each
branch-unique term is invoked exactly once.

| Method        | BranchCount | Mean      | Error     | StdDev    | Allocated |
|-------------- |------------ |----------:|----------:|----------:|----------:|
| EvaluateAsync | 10          |  4.543 μs |  1.543 μs | 0.0846 μs |   7.38 KB |
| EvaluateAsync | 50          | 21.898 μs | 13.455 μs | 0.7375 μs |   33.1 KB |
| EvaluateAsync | 200         | 84.615 μs | 10.447 μs | 0.5727 μs | 133.77 KB |
