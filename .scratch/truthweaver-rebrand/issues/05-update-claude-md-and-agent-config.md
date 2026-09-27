# 05: Update CLAUDE.md and other agent-facing config for the new name

**What to build:** Update repository-level agent guidance and local tool config that names
the project:

- `CLAUDE.md` (repo root): "This repository contains the **BooleanRulesEngine** C#
  library" and any other name references in the "Project" section.
- `AGENTS.md` (repo root), if it names the project anywhere beyond the CodeGraph
  boilerplate block.
- `.mcp.json`, `.cursor/mcp.json`, `.vscode/mcp.json`, `opencode.jsonc` — check for any
  server/task labels or working-directory strings that embed the old name; update only
  what actually references the project name, leave unrelated config untouched.

Do not touch `.vs/` — it's a generated Visual Studio cache directory and will regenerate
correctly against the renamed `.slnx` on next load.

**Blocked by:** 01

**Status:** done

- [x] `CLAUDE.md` refers to the project as TruthWeaver
- [x] `AGENTS.md` refers to the project as TruthWeaver wherever it names the project
      (it had no project-name references beyond the CodeGraph boilerplate block, so no
      change was needed)
- [x] `.mcp.json`/`.cursor/mcp.json`/`.vscode/mcp.json`/`opencode.jsonc` contain no
      leftover `BooleanRulesEngine`/`Boolean-Rules-Engine` strings tied to this project
      (`.mcp.json` and `opencode.jsonc` had none; `.cursor/mcp.json` and `.vscode/mcp.json`
      are gitignored/local-only and only contain the local repo folder's absolute
      filesystem path, `...\Boolean-Rules-Engine`, which is left as-is since the local
      directory itself isn't being renamed by this effort)
