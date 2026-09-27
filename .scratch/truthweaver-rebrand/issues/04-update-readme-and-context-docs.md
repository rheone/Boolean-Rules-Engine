# 04: Update README, CONTEXT.md, and ADRs to the TruthWeaver name

**What to build:** Update prose documentation that describes the project by its old name:

- `README.md`: title, the "A general-purpose boolean expression engine for .NET" intro,
  every code sample's `using` directives and `<ProjectReference>` snippet, and the CI
  badge URL (which encodes the old GitHub owner/repo path — update once ticket 06 lands).
- `CONTEXT.md`: project name and any prose referring to "BooleanRulesEngine."
- `docs/adr/0003-rule-syntax-and-serialization.md` and
  `docs/adr/0004-package-boundaries-and-extensibility.md`: update references to the old
  name; do not alter the substance of either ADR's decision — this is a naming pass only.

Do not touch the *content* of any code example beyond the namespace/using lines — the
predicate/rule examples in the README should still demonstrate the same behavior, just
under the new namespace.

**Blocked by:** 02, 06

**Status:** ready-for-agent

- [ ] `README.md` contains no `BooleanRulesEngine` string; all code samples compile
      conceptually against the renamed namespaces (spot-check by pasting one sample into a
      scratch file and building)
- [ ] README's CI badge URL points at the renamed repository
- [ ] `CONTEXT.md` refers to the project as TruthWeaver throughout
- [ ] Both touched ADRs refer to the project as TruthWeaver, with no change to their
      recorded decisions
