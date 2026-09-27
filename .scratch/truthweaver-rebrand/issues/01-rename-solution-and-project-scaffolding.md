# 01: Rename solution, project files, and directories to TruthWeaver

**What to build:** Rename the on-disk scaffolding from `BooleanRulesEngine` to
`TruthWeaver`, using `git mv` so history follows each move:

- `BooleanRulesEngine.slnx` → `TruthWeaver.slnx`
- `src/BooleanRulesEngine/` → `src/TruthWeaver/` (and its `.csproj`)
- `src/BooleanRulesEngine.Abstractions/` → `src/TruthWeaver.Abstractions/` (and its
  `.csproj`)
- `src/BooleanRulesEngine.Yaml/` → `src/TruthWeaver.Yaml/` (and its `.csproj`)
- `tests/BooleanRulesEngine.Tests/` → `tests/TruthWeaver.Tests/` (and its `.csproj`)

Update every `<ProjectReference>` path inside each `.csproj` to point at the new project
locations/file names, and update the `.slnx` file's project entries to match. Update
`RootNamespace` in each `.csproj` (`BooleanRulesEngine` → `TruthWeaver`,
`BooleanRulesEngine.Abstractions` → `TruthWeaver.Abstractions`, etc.) and the
`<Description>` text in `src/TruthWeaver/TruthWeaver.csproj`, which currently reads
"BooleanRulesEngine: the DSL parser, ...".

Leave namespace declarations *inside* `.cs` files untouched — that's ticket 02.

**Blocked by:** none

**Status:** done

- [x] `TruthWeaver.slnx` exists at repo root; `BooleanRulesEngine.slnx` no longer does
- [x] All four project directories are renamed under `src/`/`tests/` with matching
      `.csproj` filenames (plus `src/BooleanRulesEngine.Predicates`, `src/BooleanRulesEngine.Testing`,
      `tests/BooleanRulesEngine.Predicates.Tests`, `tests/BooleanRulesEngine.Testing.Tests`, and
      `benchmarks/BooleanRulesEngine.Benchmarks`, which were added to the repo after this ticket
      was written; renamed for consistency using the same convention)
- [x] Every `<ProjectReference>` in every `.csproj` resolves to the new paths
- [x] `<RootNamespace>` in each `.csproj` matches the new project name
- [x] `dotnet restore --locked-mode` and `dotnet build` succeed against
      `TruthWeaver.slnx`
- [x] `git status`/`git log --follow` shows renames, not delete+add, for every moved file
