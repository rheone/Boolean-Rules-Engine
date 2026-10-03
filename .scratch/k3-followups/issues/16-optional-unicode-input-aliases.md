# 16: Optional: Unicode input aliases

**What to build:** Accept the Unicode logic symbols for NAND, NOR, XOR, IMPLIES and EQUIVALENT (⊼ ⊽ ⊻ ⇒ ⇔) as input aliases only; the canonical printer stays word-only. Low priority. Research item 3a.

**Blocked by:** 06

**Status:** ready-for-agent

- [ ] Each symbol compiles to the same tree as the named operator
- [ ] Canonical output remains word-only
- [ ] Lexer and README symbol table updated
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
