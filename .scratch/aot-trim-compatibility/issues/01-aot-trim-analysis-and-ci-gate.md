# 01: AOT/trim analysis verified and CI-gated

**What to build:** Trim/AOT analyzers enabled for `BooleanRulesEngine.Abstractions` and `BooleanRulesEngine`, all resulting warnings resolved, and a CI job that runs the analysis and fails on any warning.

**Blocked by:** none

**Status:** done

- [x] `IsTrimmable` (and `IsAotCompatible` where applicable) is set for `BooleanRulesEngine.Abstractions` and `BooleanRulesEngine` (via `src/Directory.Build.props`, `IsAotCompatible` set for every project under `src/` — all five shipping packages, not just these two)
- [x] `dotnet build`/`publish` with trim analysis enabled produces zero trim/AOT warnings for these two projects (verified zero warnings across all five shipping packages)
- [x] A CI job runs this analysis on every PR and fails the build on a new warning (`.github/workflows/ci.yml`'s `Build` step, gated via `src/Directory.Build.props`'s `TreatWarningsAsErrors` under GitHub Actions' `CI=true`)
- [x] Reasoning/result recorded in CONTEXT.md or an ADR note near the existing "no assembly scanning" deferred item (`CONTEXT.md`, new "AOT / trim compatibility" section)
