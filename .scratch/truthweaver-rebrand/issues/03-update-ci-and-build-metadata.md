# 03: Update CI workflow and build metadata for the new project names

**What to build:** Update every reference to the old solution/project file paths and
repository URL in build/CI configuration:

- `.github/workflows/ci.yml`: every `BooleanRulesEngine.slnx`, and the four
  `.csproj` paths formatted for `dotnet csharpier`/format in the format-check step, to the
  new `TruthWeaver*` paths.
- `Directory.Build.props`: `<RepositoryUrl>` and `<PackageProjectUrl>` currently point at
  `https://github.com/rheone/Boolean-Rules-Engine` — update both once the GitHub repo
  rename (ticket 06) has actually happened, so this ticket is blocked on that one rather
  than guessing the URL in advance. `<PackageTags>` (`rules engine;boolean;kleene;dsl`)
  can optionally gain a `truthweaver`-flavored tag but doesn't need the product name
  itself.
- Any `.editorconfig`/`stylecop.json`/`Directory.Build.targets` references to the old
  name (grep to confirm — none were found in the initial survey, but re-check since this
  ticket runs after 01/02 land).

**Blocked by:** 02, 06

**Status:** done

- [x] `.github/workflows/ci.yml` references only `TruthWeaver*` paths; a CI run (or local
      dry run of the same commands) succeeds
- [x] `Directory.Build.props` `<RepositoryUrl>`/`<PackageProjectUrl>` point at the renamed
      GitHub repository
- [x] `dotnet restore --locked-mode`, `dotnet build`, `dotnet test`,
      `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info`, and
      `dotnet roslynator analyze` all succeed using the commands as CI would invoke them
