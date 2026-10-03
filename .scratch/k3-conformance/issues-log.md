# k3-conformance: issues log

Problems hit while implementing the tickets, recorded for later discussion. Newest last.

| # | Ticket | Issue | Workaround / status |
| - | ------ | ----- | ------------------- |
| 1 | env | `dotnet csharpier check .` crashes with `DirectoryNotFoundException` on `.claude\SKILLS\humanizer` (broken path under `.claude`). | Run `dotnet csharpier check src tests benchmarks`. Open: fix or ignore the path. |
| 2 | env | `dotnet roslynator analyze` finds no project with no argument, and throws an MSBuild load exception when given `TruthWeaver.slnx`. | Workaround: run per project, e.g. `dotnet roslynator analyze src/TruthWeaver/TruthWeaver.csproj` (works; only pre-existing S1135/SA1512 reported).
| 3 | env | `dotnet format --verify-no-changes --severity info` exits 2 on pre-existing S1135 (TODO in `IPredicate.cs`, `OperatorInfo.cs`) and SA1512 / S6966 warnings. | Only check that no *new* diagnostics appear in touched files. |
