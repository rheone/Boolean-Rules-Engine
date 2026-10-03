# k3-conformance: issues log

Problems hit while implementing the tickets, recorded for later discussion. Newest last.

| # | Ticket | Issue | Workaround / status |
| - | ------ | ----- | ------------------- |
| 1 | env | `dotnet csharpier check .` crashes with `DirectoryNotFoundException` on `.claude\SKILLS\humanizer` (broken path under `.claude`). | Run `dotnet csharpier check src tests benchmarks`. Open: fix or ignore the path. |
| 2 | env | `dotnet roslynator analyze` finds no project with no argument, and throws an MSBuild load exception when given `TruthWeaver.slnx`. | Workaround: run per project, e.g. `dotnet roslynator analyze src/TruthWeaver/TruthWeaver.csproj` (works; only pre-existing S1135/SA1512 reported).
| 3 | env | `dotnet format --verify-no-changes --severity info` exits 2 on pre-existing S1135 (TODO in `IPredicate.cs`, `OperatorInfo.cs`) and SA1512 / S6966 warnings. | Only check that no *new* diagnostics appear in touched files. |
| 4 | 05 | `StructuralTautology` / `StructuralContradiction` constant names now describe classical (two-valued) findings but keep their names and codes `BRE0012`/`BRE0013` to avoid a breaking public rename. | Messages and XML docs relabelled. Open: rename (or add `Classical*` aliases) when ticket 06 replaces the pass. |
| 5 | 04 | The failed-node placeholder is never observable through `Compile` (a tree with an error diagnostic is discarded), so "no False substitution" can only be tested at the internal seam. | Tested via `FailedNode.Placeholder` and `RuleNodeCompilerErrorNodeAndDefensiveThrowsTests`. |
