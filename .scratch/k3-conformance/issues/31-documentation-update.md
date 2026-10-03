# 31: Documentation update

**What to build:** README, CONTEXT.md and ADRs describe the new K3 language surface, notation, boundaries, transforms and diagnostics.

**Blocked by:** 19, 27, 29

**Status:** done

- [x] README documents operators, notations, Project/Collapse, transforms and diagnostics
- [x] CONTEXT.md glossary updated
- [x] ADR cross-references are consistent
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Documentation sweep; no C# changed.

- README: reframed as a Strong K3 engine; added a complete EBNF grammar and precedence summary, a features list covering the full language, boundaries, rewriting and diagnostics, a full table of contents, rewriting-rules tour entries, fixed infix-only operators written as call forms, a broken whitespace example, a stale JSON-syntax diagnostic sample, glossary gaps and the missing ADR-0005 link.
- CONTEXT.md: K3 framing, symbol notation, new vocabulary (Inspection, Project, Rewrite, Diagnostic), `TruthValue` rules, corrected class diagram names, the abstract grammar now includes `Unknown`, five-package summary, ADR-0005 link.
- ADR-0001/0002/0003/0004: short "Superseded by ADR-0005" or "Extended" notes in place; history not rewritten.
- spec.md Status set to done with deviations pointing at issues-log.md; CLAUDE.md "In-flight work" now says the effort is complete; issues-log.md gained a "Summary for review" block (open questions grouped, most important first) and rows 40-41.
- Verification: README examples (whitespace, grouping styles, delimiter and diagnostic messages, rewrites, simplifications, compressions, canonical/JSON/YAML/Mermaid/plain-text output of the worked example, builder equivalence, Collapse evaluation, operator acceptance and rejection cases) were run with a throwaway console project in the scratchpad (not committed). All kept claims matched. All 31 tickets are Status: done with boxes ticked; ticket 30 and 31 validation boxes are satisfied by the unchanged build and tests below.
- Validation: `dotnet build` and `dotnet test` on the whole solution (see commit); no C# touched so csharpier/format are unaffected.
