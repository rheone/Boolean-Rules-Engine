# 24: Expand to NAND-only / NOR-only

**What to build:** Opt-in transforms that rewrite a rule into NAND-only or NOR-only form (e.g. NOT A = A NAND A).

**Blocked by:** 23

**Status:** ready-for-agent

- [ ] Evaluation is identical to the original for all assignments
- [ ] Output contains only the target gate
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
