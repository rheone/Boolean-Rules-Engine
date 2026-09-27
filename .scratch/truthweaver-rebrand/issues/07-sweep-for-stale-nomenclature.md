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

**Status:** done

- [x] `grep -ri "boolean.?rules.?engine"` across all tracked, non-generated files returns
      zero hits, or every remaining hit is a deliberately-preserved historical reference
      with a note explaining why
- [x] Full validation suite passes one final time: `dotnet restore --locked-mode`,
      `dotnet build`, `dotnet test`, `dotnet csharpier check .`,
      `dotnet format --verify-no-changes --severity info`, `dotnet roslynator analyze`

## Comments

Fixed: the internal `BooleanRulesEngineMetrics` class/const/meter-name string (renamed to
`TruthWeaverMetrics`, `MeterName = "TruthWeaver"`, instrument names
`truthweaver.evaluations`/`truthweaver.faults`/`truthweaver.compile_diagnostics`), the
`AddBooleanRulesEngine` public DI extension method (renamed to `AddTruthWeaver`, with all
callers in README.md/CONTEXT.md/tests updated), doc-comment prose referencing the old
package/namespace names across `src/`, the JSON schema's `$id`/`title`/`description`, the
stray "BRE" abbreviation in a `LoggingTests.cs` comment, and a leftover
`benchmarks/BooleanRulesEngine.Benchmarks` path in `benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md`.

Both the extension method rename and the metrics class rename are technically
API-observable changes (a public method rename, and an internal class whose `MeterName`
constant is externally subscribed-to by OpenTelemetry-style consumers), but per the repo's
own README ("There is no published NuGet package yet ... build from source") this project
has no released consumers to break, so no `[Obsolete]` shim was added — a straight rename
is the correct move at this pre-release stage, consistent with tickets 01/02's approach to
the namespace rename itself.

Deliberately left unchanged (all under `.scratch/`, none reachable from `src/`, `tests/`,
`docs/`, or root-level prose):

- `.scratch/aot-trim-compatibility/**`, `.scratch/engine-v1/**`,
  `.scratch/evaluated-node-rule-description-alignment/**`,
  `.scratch/evaluation-metrics/**`, `.scratch/expression-node-shape-seam/**`,
  `.scratch/predicates-package/**`, `.scratch/rule-tree-json-schema/**`,
  `.scratch/rules-testing-package/**`, `.scratch/unit-test-coverage/**` — completed
  tickets from before the rebrand; they document work done under the project's old name at
  the time, and rewriting them would misrepresent the historical record of what was
  actually planned/shipped under that name.
- `.scratch/truthweaver-rebrand/issues/01,02,03,05,06,07.md` and
  `.scratch/truthweaver-rebrand/spec.md` — this rebrand effort's own planning documents;
  they necessarily reference the old name to describe the rename being performed and are
  not themselves stale nomenclature.

No NuGet package ID migration note was needed (per ticket 03/07 guidance and the README's
own "no published NuGet package yet" disclaimer — nothing has shipped under the old
package ID to redirect from).
