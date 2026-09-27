# Rebrand: BooleanRulesEngine → TruthWeaver

**Status:** ready-for-agent

## Problem Statement

The project's current name, "BooleanRulesEngine," is a placeholder chosen before the
library had a distinct identity. It is generic — a search for prior art turned up an
existing NuGet package (`Verdict.Rich`) and several unrelated "rules engine" projects
crowding the space, and "BooleanRulesEngine" itself gives no hint of the library's actual
differentiators: DSL/JSON/YAML round-tripping, an explainable evaluated-node tree, and
Mermaid/plain-text rendering.

The chosen replacement name is **TruthWeaver**: "Truth" names the domain (boolean
evaluation) directly, and "Weaver" reflects the library's core mechanic — composing
individual predicates (threads) via boolean operators into a single compiled tree
(fabric) that yields one evaluated result. A web search turned up no existing GitHub
repository or NuGet package under this exact name.

The rename touches nearly every file in the repository: two-part namespaces
(`BooleanRulesEngine`, `BooleanRulesEngine.Abstractions`, `BooleanRulesEngine.Yaml`,
`BooleanRulesEngine.Tests`) are declared per-file via file-scoped namespace declarations,
project/solution file names embed the old name, `Directory.Build.props` embeds GitHub URLs
under the old repo name, the CI workflow references file paths by the old name, and
prose docs (README, CONTEXT.md, ADRs, CLAUDE.md) describe the project by its old name
throughout.

## Solution

Rename in dependency order so the tree keeps building at each step: solution/project
scaffolding and directory names first, then namespaces (mechanical, file-scoped
declarations only — no behavior change), then CI/tooling config that references paths by
name, then prose documentation, then the GitHub repository itself (external, requires the
user's own action), then a final sweep for stragglers.

## Constraints

- This is a pure rename: no behavior, API shape, or logic changes. Any diff that isn't a
  literal `BooleanRulesEngine` → `TruthWeaver` (or namespace-shaped variant) substitution
  is out of scope for this effort.
- `git mv` for renamed files/directories, not delete+recreate, so history follows the
  move.
- Run the full validation suite (`dotnet restore --locked-mode`, `dotnet build`,
  `dotnet test`, `dotnet csharpier check .`, `dotnet format --verify-no-changes
  --severity info`, `dotnet roslynator analyze`) after each ticket that touches buildable
  files — a rename this broad is exactly the kind of change a single missed reference
  silently breaks.
- Generated/local-only files (`.vs/`, `bin/`, `obj/`) are not part of this effort — they
  regenerate from a clean build and editor reload.
- `Directory.Packages.props` central package versions are unaffected — no dependency
  changes.

## User Stories

1. As a maintainer, I want the solution, project, and directory names to say
   "TruthWeaver," so that `dotnet build`/`dotnet test` output and the repo layout match
   the library's real name.
2. As a library consumer, I want to `using TruthWeaver;` (and `TruthWeaver.Abstractions`,
   `TruthWeaver.Yaml`) instead of the old namespace, so that the public API surface
   reflects the new name everywhere, not just in the package metadata.
3. As a maintainer, I want the README, CONTEXT.md, ADRs, and CLAUDE.md to refer to the
   project as "TruthWeaver" with no leftover "BooleanRulesEngine" references, so that
   documentation is internally consistent.
4. As a maintainer, I want the GitHub repository itself renamed (with the old
   `Boolean-Rules-Engine` URL redirecting, per GitHub's default behavior), so that the
   repo URL matches the project's new identity.
5. As a maintainer, I want a final grep sweep confirming zero remaining case-sensitive or
   kebab-case references to the old name anywhere in tracked files, so that nothing was
   missed.
