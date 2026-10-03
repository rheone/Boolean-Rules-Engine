# 08: No-mixing operator rule

**What to build:** Mixing different operators in one group requires explicit parentheses, except the NOT > AND > OR precedence, so no one is surprised by implicit precedence.

**Blocked by:** 03

**Status:** done

- [x] Ambiguous mixing is a diagnostic with a precise location and a suggestion to add parentheses
- [x] NOT > AND > OR continues to parse without parentheses
- [x] Existing valid rules and round-trips are unaffected
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

The parser's XOR-only chain was generalised: `DslParser` now keeps a list of infix operators outside the NOT > AND > OR chain (`InfixOperators`, currently `XOR`, `XNOR`; each later infix operator slice appends its keyword and a node in `ParseInfixChain`). Two ambiguities are reported as `AmbiguousOperatorMixing` (BRE0007), both with an "Add parentheses" suggestion in the message (the `Diagnostic` record has no separate suggestion field until ticket 27):

- two different infix operators in one chain are reported at the span of the second operator's token (so `a XOR b XNOR c` points at `XNOR`; `⊕` is one character wide);
- a bare infix expression that is an operand of an `AND`/`OR` chain is reported at that operand's own span, once per offending operand (the flag no longer propagates up to enclosing AND/OR levels, so `a XOR b AND c OR d` reports once rather than twice).

Parentheses and function-call operand lists (`ExactlyOne(a XOR b, c)`) start a new level, so they never trigger it. NOT > AND > OR is unchanged. A repeated *same* infix operator (`a XOR b XOR c`) is still not a mixing error; the compiler's arity check rejects it. Messages keep the "Mixing X with Y" wording the existing tests rely on.
