# k3-conformance: issues log

Problems hit while implementing the tickets, recorded for later discussion. Newest last.

| # | Ticket | Issue | Workaround / status |
| - | ------ | ----- | ------------------- |
| 1 | env | `dotnet csharpier check .` crashes with `DirectoryNotFoundException` on `.claude\SKILLS\humanizer` (broken path under `.claude`). | Run `dotnet csharpier check src tests benchmarks`. Open: fix or ignore the path. |
| 2 | env | `dotnet roslynator analyze` with no argument finds no project (the repo has only `TruthWeaver.slnx`). | Run `dotnet roslynator analyze TruthWeaver.slnx`. |
| 3 | env | `dotnet format --verify-no-changes --severity info` exits 2 on pre-existing S1135 (TODO in `IPredicate.cs`, `OperatorInfo.cs`) and SA1512 / S6966 warnings. | Only check that no *new* diagnostics appear in touched files. |
