# 06: Line-ending guard

**What to build:** The repository is eol=crlf, and two separate scripted edits wrote LF into the working tree and had to be repaired by hand. Add a cheap guard (a .gitattributes/.editorconfig check, a Husky pre-commit step or a CI step) that fails when a tracked text file has the wrong line endings, and document the rule for scripted edits.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A file with LF line endings in a CRLF-configured path fails the check
- [ ] The check is part of the local pre-commit tasks and CI, or the ticket records why one is enough
- [ ] CLAUDE.md or the contributing notes mention the rule
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).
