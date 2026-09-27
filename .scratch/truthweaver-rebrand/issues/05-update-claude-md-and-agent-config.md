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

**Status:** ready-for-agent

- [ ] `CLAUDE.md` refers to the project as TruthWeaver
- [ ] `AGENTS.md` refers to the project as TruthWeaver wherever it names the project
- [ ] `.mcp.json`/`.cursor/mcp.json`/`.vscode/mcp.json`/`opencode.jsonc` contain no
      leftover `BooleanRulesEngine`/`Boolean-Rules-Engine` strings tied to this project
