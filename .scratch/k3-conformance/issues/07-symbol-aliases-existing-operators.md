# 07: Symbol aliases for existing operators

**What to build:** Rule authors can write &&, ||, !, ∧, ∨, ¬ and ⊕ and get exactly the same tree as the named operators; the canonical printer stays word-only.

**Blocked by:** 03

**Status:** done

- [x] Each symbol compiles to a tree equal to its named operator
- [x] Canonical printer emits named form only
- [x] Precedence is unchanged
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Symbols `&&`, `||`, `!`, `∧`, `∨`, `¬`, `⊕` are lexed as a new `TokenKind.Operator` and mapped by the parser to the named keyword (AND/OR/NOT/XOR), so no node, evaluator, analyzer, printer, JSON/YAML, schema or builder change was needed (those layers never see notation). A lone `&` or `|` is still an "Unexpected character" syntax error. `⊕` follows XOR's no-mixing rule. Out of scope here (later tickets): `→ ↔ ↑ ↓ ??`, and symbols inside JSON/YAML op names.
