# 01: Taxonomy, template and structure proposal

**What to build:** A proposal document, for the owner's approval, that fixes the authoritative model before any operation document is written. It defines the values (T, F, U), the umbrella term Operation and the categories (Gates / Operators, Predicates, Functions, Cardinality Functions, Derived / Composite Operations, Result Transformations) with exactly one primary category per operation, the primitive-versus-derived rule, the proposed individual-operation Markdown template (required sections: Name, Classification, Kind, Arity, Input Domain, Output Domain, Definition, Syntax, Aliases, Formal Semantics; conditional sections: Formula, Truth Table, Evaluation Table, Canonical Form, Equivalent Forms, Examples, Edge Cases, Mermaid Diagram, Implementation Notes, Related Operations; Truth Table for finite fixed-arity operations, Evaluation Table for parameterized, variadic or cardinality operations), the directory tree under docs/strong-k3/ and an inventory of every operation in the engine's final design (see k3-followups: PARITY instead of NXOR, Project and Collapse as methods on the result) with its proposed category, primitive/derived status, canonical form if one is established, and any classification ambiguity flagged rather than forced (for example COALESCE as gate-like but not a Strong K3 connective, the inspections, Project and Collapse). Sources: the spec audit's terminology and extensions sections and the research findings.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Taxonomy, template and directory tree are presented in one document at docs/strong-k3/PROPOSAL.md (or the agreed equivalent), clearly marked as awaiting approval
- [ ] Every operation in the inventory has exactly one primary category and an explicit Primitive or Derived kind; ambiguities are listed as open questions, none silently resolved
- [ ] Derived operations list a canonical form only where it is established (and verified under Strong K3 by brute force); otherwise the proposal says no canonical reduction is established
- [ ] The owner has approved or amended the proposal (record the decision in the ticket Comments) before any other ticket starts
- [ ] No operation documents are written in this ticket

Source: [spec audit](../../k3-conformance/spec-audit.md) and [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md).
