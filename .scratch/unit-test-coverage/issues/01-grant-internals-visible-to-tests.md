# 01: Grant the test project InternalsVisibleTo access

**What to build:** Add an `InternalsVisibleTo` attribute to
`src/BooleanRulesEngine/AssemblyInfo.cs` (alongside the existing grant to
`BooleanRulesEngine.Yaml` on line 6) for `BooleanRulesEngine.Tests`. This is a prerequisite for
tickets 02–04: `Lexer`, `DslParser`, `Analyzer`, and `BddManager` are all `internal sealed` and
currently unreachable from the test project by any means other than the public `RuleCompiler`
pipeline.

**Blocked by:** none

**Status:** ready-for-agent

- [ ] `BooleanRulesEngine.Tests` can construct and call `internal` types from
      `BooleanRulesEngine` directly (verify with a throwaway `new Lexer("true")` in a scratch
      test, then remove it — the real tests land in later tickets)
- [ ] `dotnet build` and `dotnet test` still succeed
- [ ] No production (non-test) behavior changes
