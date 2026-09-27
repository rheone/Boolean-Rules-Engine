# 07: Sweep for stale nomenclature after the rename

**What to build:** After tickets 01–05 land, run a repo-wide, case-insensitive grep for
every remaining spelling of the old name across tracked files (exclude `.vs/`, `bin/`,
`obj/`, and `.git/`):

- `BooleanRulesEngine`
- `Boolean-Rules-Engine`
- `Boolean Rules Engine`
- `boolean rules engine` (README prose, ADRs, commit-adjacent docs)

Also check for less obvious leftovers that a literal string search can miss:

- Any abbreviation the team may have started using informally (e.g. "BRE") — grep the
  `.scratch/` ticket specs and `docs/` for one before assuming none exists.
- The `PackageTags` in `Directory.Build.props` and any NuGet package description text for
  wording that assumed the old name's phrasing (e.g. "the BooleanRulesEngine
  compile-once/evaluate-many boolean rule engine").
- Solution/project GUIDs are fine to leave unchanged — only the human-readable name needs
  to move.

Fix anything found; if a hit is intentional (e.g., a changelog entry describing the
project's history under its old name), leave it and note why in this ticket's Comments
rather than silently skipping it.

**Blocked by:** 01, 02, 03, 04, 05, 06

**Status:** ready-for-agent

- [ ] `grep -ri "boolean.?rules.?engine"` across all tracked, non-generated files returns
      zero hits, or every remaining hit is a deliberately-preserved historical reference
      with a note explaining why
- [ ] Full validation suite passes one final time: `dotnet restore --locked-mode`,
      `dotnet build`, `dotnet test`, `dotnet csharpier check .`,
      `dotnet format --verify-no-changes --severity info`, `dotnet roslynator analyze`
