# k3-reference: Strong K3 operation reference library

**Status:** ready-for-agent

Create a complete, navigable Markdown reference library for Strong Kleene K3 operations at `docs/strong-k3/`, serving as a human-readable semantic reference and an implementation reference for a Strong K3 parser, AST, evaluator, simplifier, truth-table generator and related tooling. One Markdown file per operation, an index README in every meaningful directory, relative links throughout, and Mermaid only where it materially helps. Use the `github-markdown` and `mermaid-diagram-generator` skills.

Owner decisions (2026-10-03):

- Location: `docs/strong-k3/`.
- Describe the **final design** agreed in `.scratch/k3-followups` (PARITY instead of NXOR; Project and Collapse as methods on the result); dependent tickets stay blocked until those follow-ups land.
- **Predicates are on hold** until they are implemented; no predicate documentation is written before then (ticket 13 is deferred).
- Nothing is written before the taxonomy and template are approved (ticket 01 is the approval gate).

Evidence: [spec audit](../k3-conformance/spec-audit.md), [research findings](../k3-conformance/research-findings.md).
