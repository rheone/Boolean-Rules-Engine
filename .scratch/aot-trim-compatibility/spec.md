# AOT/trimming compatibility

**Status:** ready-for-agent

## Problem Statement

The repo targets a preview .NET 11 SDK and follows an explicit "no assembly
scanning" design decision partly justified by AOT/trimming friendliness
(CONTEXT.md's deferred "Attribute-based / assembly-scanned predicate
registration" item), but this has never been verified — there is no trim/AOT
analysis enabled or checked in CI for `BooleanRulesEngine.Abstractions` or
`BooleanRulesEngine`.

## Solution

Enable trimming and NativeAOT compatibility analysis (`IsTrimmable`/
`IsAotCompatible`) for the Abstractions and core packages, fix any warnings
surfaced, and add a CI job that fails on regressions.

## User Stories

1. As a consumer building a trimmed or NativeAOT application, I want
   `BooleanRulesEngine.Abstractions` and `BooleanRulesEngine` to be verified
   trim/AOT-safe, so that referencing them doesn't silently break my publish.
2. As a maintainer, I want CI to fail if a future change introduces a
   trim/AOT warning, so that this guarantee doesn't quietly rot.
