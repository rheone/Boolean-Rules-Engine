# 01: AOT/trim analysis verified and CI-gated

**What to build:** Trim/AOT analyzers enabled for `BooleanRulesEngine.Abstractions` and `BooleanRulesEngine`, all resulting warnings resolved, and a CI job that runs the analysis and fails on any warning.

**Blocked by:** none

**Status:** ready-for-agent

- [ ] `IsTrimmable` (and `IsAotCompatible` where applicable) is set for `BooleanRulesEngine.Abstractions` and `BooleanRulesEngine`
- [ ] `dotnet build`/`publish` with trim analysis enabled produces zero trim/AOT warnings for these two projects
- [ ] A CI job runs this analysis on every PR and fails the build on a new warning
- [ ] Reasoning/result recorded in CONTEXT.md or an ADR note near the existing "no assembly scanning" deferred item
