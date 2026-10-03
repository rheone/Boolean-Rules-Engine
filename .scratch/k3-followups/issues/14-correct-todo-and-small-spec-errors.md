# 14: Correct the TODO and small spec errors

**What to build:** Fix the smaller inconsistencies in the source requirements and reference notes: 'six primitive operations' versus seven listed, the arity table that marks XOR as ternary and n-ary versus the binary-only decision, NAND/NOR written n-ary but listed binary, 'trinary' replaced by 'three-valued' or 'ternary', the typos (Kleen, Conical, Oder), the vacuous parity wording, and the 'definite whenever information is sufficient' sentence (true per connective, not per formula). Add a note at the top of the TODO that it is the historical requirements document and where decisions were finalised (ADR-0005, the follow-up decisions). Spec audit section B8.

**Blocked by:** 06

**Status:** ready-for-agent

- [ ] Each B8 row is fixed or explicitly left with a reason
- [ ] The TODO records that NXOR became PARITY and Project/Collapse became methods on the result

Source: [spec audit](../../k3-conformance/spec-audit.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
