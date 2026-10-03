# 05: Analyzer diagnostics relabelled (interim)

**What to build:** The existing classical-logic analyzer diagnostics are relabelled so they no longer claim K3 truths (A AND NOT A is Unknown when A is Unknown). This is a stopgap until the K3-aware analyzer lands.

**Blocked by:** 01

**Status:** done

- [x] Diagnostic codes/messages state they are two-valued (classical) findings
- [x] No message claims a K3 contradiction or tautology
- [x] Existing analyzer tests updated
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Codes `BRE0012`/`BRE0013` and the constant names `StructuralTautology`/`StructuralContradiction` are kept for compatibility (renaming a public constant is a bigger API decision, see issues-log #4); their messages now begin "Two-valued (classical) analysis: ... always True/False when every term is True or False. In Strong K3 it can still be Unknown", and the XML docs, analyzer docs and README say the same. The proper K3 rewrite is ticket 06.
