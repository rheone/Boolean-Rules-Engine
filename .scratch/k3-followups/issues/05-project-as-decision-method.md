# 05: Project is a method on the result, not an operator

**What to build:** Project stops being an in-tree operator. Add Decision.Project(unknownAs) returning a definite value (True and False pass through, Unknown becomes the chosen value), and remove the Project node from the DSL, JSON, YAML, builder, schema, compiler, evaluator, analyzer, node-shape and operator-info tables, printers, rule diff, and the expand, compress, canonicalise and simplify rewrites (the expansion and compression patterns that produced or consumed Project now use COALESCE with a constant, which remains a rule-level operator). A rule that needs the effect inside an expression uses COALESCE(x, True|False). Owner decision recorded 2026-10-03. Amend ADR-0005 decision 12, README and CONTEXT.md.

**Blocked by:** 04

**Status:** ready-for-agent

- [ ] `Project(...)` in rule text, JSON or YAML is rejected with a diagnostic suggesting COALESCE or Decision.Project
- [ ] Decision.Project matches the oracle for all inputs and never changes Result
- [ ] Rewrites no longer emit or recognise a Project node and still preserve evaluation over all assignments (property tests pass)
- [ ] All parity and exhaustiveness tests updated deliberately
- [ ] Docs and ADR amendments written
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
