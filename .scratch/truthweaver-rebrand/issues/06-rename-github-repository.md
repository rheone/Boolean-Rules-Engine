# 06: Rename the GitHub repository

**What to build:** Rename the GitHub repository from `rheone/Boolean-Rules-Engine` to
`rheone/TruthWeaver` (exact casing TBD by the user — GitHub repo names are case-preserving
but not case-sensitive for URLs).

**This is an external, hard-to-reverse action against shared infrastructure — it is not
something an agent should do unattended.** GitHub does redirect the old URL after a
rename, but every clone with the old remote URL, every external link (badges, package
metadata, other people's bookmarks), and any branch-protection/webhook config scoped to
the repo name needs to be re-verified afterward. Confirm with the user before running
this, and have the user (or an explicitly authorized `gh repo rename` call) perform it
directly rather than scripting it as part of a larger batch.

After the rename, update the local `origin` remote URL (`git remote set-url origin
<new-url>`) and confirm `git fetch`/`git push` still work.

**Blocked by:** none (this can happen any time, but tickets 03/04 are blocked on it so
they don't guess at the new URL)

**Status:** ready-for-agent

- [ ] User has explicitly confirmed the rename (repo name, casing, and timing) before it
      happens
- [ ] GitHub repository is renamed
- [ ] Local `origin` remote URL updated and verified with `git fetch`
- [ ] Old URL redirect confirmed working (GitHub does this automatically, but verify)
