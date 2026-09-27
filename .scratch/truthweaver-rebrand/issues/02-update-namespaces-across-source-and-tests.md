# 02: Update namespace declarations and usings across source and tests

**What to build:** Mechanically update every file-scoped `namespace` declaration and every
`using BooleanRulesEngine...;` directive across `src/` and `tests/` to the `TruthWeaver`
equivalent:

- `namespace BooleanRulesEngine` → `namespace TruthWeaver` (and sub-namespaces:
  `BooleanRulesEngine.Analysis`, `.Ast`, `.Building`, `.Compilation`, `.DependencyInjection`,
  `.Diagnostics`, `.Evaluation`, `.Json`, `.Logging`, `.Parsing`, `.Printing`, `.Registry`,
  `.Tree` → `TruthWeaver.*`)
- `namespace BooleanRulesEngine.Abstractions` → `namespace TruthWeaver.Abstractions`
- `namespace BooleanRulesEngine.Yaml` → `namespace TruthWeaver.Yaml`
- `namespace BooleanRulesEngine.Tests` (and its `TestSupport` sub-namespace, if present)
  → `TruthWeaver.Tests`
- Every corresponding `using` directive in every file that references these namespaces

Also rename the type `BooleanRulesEngineServiceCollectionExtensions` (in
`src/BooleanRulesEngine/DependencyInjection/`) to `TruthWeaverServiceCollectionExtensions`,
including its file name.

This ticket depends on ticket 01 landing first so the project/file paths already match;
otherwise apply it in one pass across the whole tree — this is a pure find/replace, not a
per-file judgment call.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] No `.cs` file under `src/` or `tests/` contains the string `BooleanRulesEngine` in a
      `namespace` or `using` statement
- [ ] `BooleanRulesEngineServiceCollectionExtensions` is renamed to
      `TruthWeaverServiceCollectionExtensions` (type and file name), and every call site
      updated
- [ ] `dotnet build` succeeds against `TruthWeaver.slnx`
- [ ] `dotnet test` passes with no failures
- [ ] `dotnet csharpier check .` and `dotnet format --verify-no-changes --severity info`
      report no diffs
