# Strong K3 specification audit

Audit of the TODO and `.tmp` reference specifications against standard Strong Kleene logic (K3), and of what the implementation inherited from them.

| | |
| --- | --- |
| Date | 2026-10-03 |
| Branch | `StrongK3+Operations` (HEAD `f47e4b4`) |
| Scope | [`.scratch/2026-10-02-TODO.md`](../2026-10-02-TODO.md), every file in `.tmp/` (13 files), [ADR-0005](../../docs/adr/0005-strong-k3-language-surface.md), `Evaluator`, `Analyzer`, `Rewriting/*`, `K3Oracle` |
| Method | Primary-source reading, plus an independent brute-force script (Appendix E) that re-derives every table, identity and rewrite from the min/max/negation definition |
| Code changed | None. Only this file was written. The 390 tests in the K3, rewrite, coalesce and inspection test classes were run read-only and pass |

> [!NOTE]
> `.tmp/` is git-ignored (`.gitignore:60`), so links into `.tmp/` resolve in a local checkout only, not on GitHub.

## How to read this

**Bottom line.** The core of the specs is standard Strong K3: every truth table, the lattice and De Morgan laws, the failure of the complement laws, `IMPLIES`, `EQUIVALENT`, binary and n-ary `XOR`, `NAND`/`NOR`, and the cardinality interval rule all check out. The specs are wrong in a small number of places (consensus theorem, one cardinality-summary row, `Project`/`Collapse` prose, one worked example, a precedence contradiction, the "Major systems" table), and they add several operators that are **not** part of K3 (`COALESCE`, `Project`, `Collapse`, the four inspections) while still calling the whole language "Strong K3". The implementation did not inherit any of the wrong claims. It already avoids the consensus trap and records the information-order point in its own README.

Conventions used below:

| Term | Meaning |
| --- | --- |
| **Standard?** | `yes`: equals Kleene's strong tables or a composition of them. `extension`: well-defined and defensible but not a K3 connective. `no`: the spec text is wrong. |
| **Evidence H** | Brute force agrees **and** I read a text that states or implies the same (Kleene-literature, Priest, Open Logic Project, vendor docs). |
| **Evidence M** | Brute force agrees; supporting sources are secondary or only partly retrieved. |
| **Evidence L** | Naming or convention claim; no authoritative source located. Marked **UNVERIFIED** where relevant. |
| **In code?** | Whether the implementation (ADR, `Evaluator`, `Analyzer`, `Rewriting/*`, `K3Oracle`, README) adopted the spec's claim. |
| `K3 order` | `False < Unknown < True`. |
| `Info order` | `Unknown` below both `True` and `False`. A function is **monotone** when replacing an `Unknown` input by `True`/`False` can never change a definite output. Kleene's strong tables are exactly the monotone ("regular") ones. |

Spec links use short labels: **Logic** is `Strong Kleene K3 Logic.md`, **General** is `Strong Kleene K3 General Specification.md`, **Simplification** is `Strong Kleene K3 Expression Simplification and Optimization Specification.md`, **Catalog** is `Strong Kleene K3 Simplification Rule Catalog.md`, **AlgLaws** is `algebratic laws.md`, **N-ary** is `N-ary Logical Operators.md`. Source tags such as [P08] are listed in section F. Check ids such as `K01` refer to rows in Appendix E.

## Executive summary

| # | Spec claim / operator | Standard? | Evidence | In code? | Recommended action |
| --- | --- | --- | --- | --- | --- |
| 1 | `NOT`, `AND`, `OR` tables; `F<U<T` with `AND`=min, `OR`=max ([Logic L134-L284](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L134-L284), [General L59-L81](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L59-L81)) | yes | H | yes (correct) | None |
| 2 | Commutativity, associativity, idempotence, distributivity, absorption, identity/domination, De Morgan, double negation ([General L181-L267](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L181-L267), [Logic L2001-L2065](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L2001-L2065)) | yes | H | yes (correct) | None |
| 3 | `A OR NOT A` and `A AND NOT A` are not `True`/`False`; K3 has no tautologies ([Logic L2069-L2119](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L2069-L2119)) | yes | H | yes (analyzer) | None |
| 4 | `IMPLIES` = `NOT A OR B`, `U IMPLIES U` = `Unknown` ([Logic L292-L350](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L292-L350)) | yes | H | yes (correct) | None. Keep the "not Lukasiewicz" note |
| 5 | `EQUIVALENT` = `OR(AND(A,B),AND(NOT A,NOT B))`, `U EQUIV U` = `Unknown` ([Logic L352-L414](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L352-L414)) | yes | H | yes (correct) | None. Agrees with Kleene's `(A->B) AND (B->A)` on all 9 rows |
| 6 | Binary `XOR` (both definitions) ([Logic L418-L464](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L418-L464), [xor.md L1-L33](../../.tmp/xor.md#L1-L33)) | yes (derived) | H | yes (correct) | None |
| 7 | n-ary parity, `Unknown` if any operand `Unknown` ([xor.md L37-L91](../../.tmp/xor.md#L37-L91), [N-ary L5-L124](../../.tmp/N-ary%20Logical%20Operators.md#L5-L124)) | yes (strongest extension) | H | yes (correct) | Simplify the "interval parity" wording (B8) |
| 8 | Name `NXOR` for n-ary parity ([TODO L43-L44](../2026-10-02-TODO.md#L43-L44)) | name only | L | yes (ADR d4) | Confirm with owner: `NXOR` conventionally means `XNOR` (D) |
| 9 | Cardinality interval `[T, T+U]` for `AtLeast`/`AtMost`/`Exactly` ([Logic L632-L805](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L632-L805)) | yes (strongest extension) | H | yes (correct) | None. Add the order-statistic remark |
| 10 | Cardinality Summary row `All()`: False when `T<n` ([Logic L896](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L896)) | **no** | H | no (code correct) | Fix to `T+U<n` (B2) |
| 11 | `ANY`/`ALL`/`NONE` = `OR`/`AND`/`NOT OR` ([Logic L808-L846](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L808-L846)) | yes | H | yes (correct) | None |
| 12 | `BETWEEN(m,n)` = `AND(AtLeast(m),AtMost(n))` ([Logic L817](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L817)) | yes for `m<=n`; edge for `m>n` | M | yes, bounds enforced | State `m<=n` in the spec (B6) |
| 13 | `AtLeast`/`AtMost`/`Exactly` are "primitive" ([TODO L67-L75](../2026-10-02-TODO.md#L67-L75)) | classification only | M | yes | Note they are K3-definable from `NOT/AND/OR`, exponentially |
| 14 | `COALESCE` as a "primitive" ([Logic L901-L978](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L901-L978)) | **extension**, not info-monotone | H | yes | Document as external operator (C1) |
| 15 | `If`/`? :` with `If(U,A,A)=A` (consensus term) ([Logic L1134-L1174](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1134-L1174)) | **extension** (the strongest extension of if-then-else) | H | yes (ADR d13) | Keep. Cite rationale; pin with a regression test (C2) |
| 16 | `IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown` ([Logic L1178-L1274](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1178-L1274)) | **extension** (SQL truth tests, Bochvar external operators) | H | yes | Document as outside K3 (C3) |
| 17 | `Project` vs `Collapse` ([ProjCollapse L1-L94](../../.tmp/ProjectAndCollapse.md#L1-L94), [Logic L1276-L1408](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1276-L1408), [General L727-L757](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L727-L757)) | names not standard; definitions contradict each other | H (math), L (terms) | resolved by ADR d12/d14 | Retire `ProjectAndCollapse.md` text (B3, C4) |
| 18 | Consensus removal "valid in K3" ([Simplification L432-L454](../../.tmp/Strong%20Kleene%20K3%20Expression%20Simplification%20and%20Optimization%20Specification.md#L432-L454), [AlgLaws L157-L167](../../.tmp/algebratic%20laws.md#L157-L167)) | **no** | H | no | Delete both passages (B1) |
| 19 | Consensus is forbidden ([Catalog L592-L647](../../.tmp/Strong%20Kleene%20K3%20Simplification%20Rule%20Catalog.md#L592-L647)) | yes (correct) | H | yes (implicitly) | Keep; add a test that `If` survives `Simplify` (B1) |
| 20 | NAND/NOR rewrites; NAND-only is not complete for K3 ([TODO L298-L304](../2026-10-02-TODO.md#L298-L304), [General L606-L630](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L606-L630)) | yes | H | yes (COALESCE boundary) | None |
| 21 | Grammar vs precedence table for `XOR`/`AND` ([Logic L1710-L1770](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1710-L1770)) | **no** (self-contradiction) | H | no (ADR d8 forbids mixing) | Delete the grammar or the table (B5) |
| 22 | `If(IsUnknown(A),B,A)` offered as inverse of `A ?? B` ([Logic L982-L1010](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L982-L1010)) | **no** (it equals `A ?? B`) | H | no | Fix the example (B4) |
| 23 | "Major three-valued systems" table ([Major L1-L9](../../.tmp/Major%20three-values%20sytems.md#L1-L9)) | partly wrong | M | no | Fix rows 3-4, 8 (B7) |
| 24 | `Unknown` as a literal constant ([TODO L33-L38](../2026-10-02-TODO.md#L33-L38)) | extension (SQL has a literal `UNKNOWN`) | M | yes (ADR d16) | None |
| 25 | `??` and `? :` spelled like C# operators ([TODO L116-L117](../2026-10-02-TODO.md#L116-L117)) | spelling only | H | yes | Document that they differ from C# (C8) |
| 26 | `False < Unknown < True` "must not be a numeric ordering" ([TODO L21-L27](../2026-10-02-TODO.md#L21-L27)) | the order is standard; caution is harmless | H | yes (ADR d1) | Note `TruthValue` enum ordinals differ (D) |
| 27 | Logic §4 "six primitive operations" ([Logic L98](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L98)) | editorial | H | n/a | Say seven (B8) |

**Inherited errors in code: none found.** Every table and rewrite in `Evaluator`, `K3Oracle`, `Analyzer`, `PrimitiveExpander`, `Canonicalizer` and `Simplifier` that I re-derived (Appendix E.2, E.4, E.7) matches the standard definition.

## A. Verified-standard claims

| Claim | Where in spec | Source | Brute force |
| --- | --- | --- | --- |
| Three values; `NOT` swaps `T`/`F`, fixes `U`; `AND`=min, `OR`=max under `F<U<T` | [General L59-L113](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L59-L113), [Logic L118-L284](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L118-L284) | [P08] 7.3.2, [OLP] thr.1, [WP-3VL] | E.1: all 16 parsed truth tables PASS |
| Conditional is `NOT A OR B`; `U->U` is `U`; Lukasiewicz differs | [Logic L292-L350](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L292-L350), [Logic L1900-L1908](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1900-L1908) | [P08] 7.3.2 and 7.3.8, [OLP] thr.1, [WP-MVL], [BHT18] Tables 1-2 | E.1, E.3, `I01`, `I05` |
| Biconditional is `(A->B) AND (B->A)`; `U<->U` is `U` (Lukasiewicz gives `T`) | [Logic L352-L414](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L352-L414) | [P08] 7.2.1, [WP-MVL] | `I02`, `I04`, `I06`: the spec's `OR(AND(A,B),AND(NOT A,NOT B))` agrees on all 9 rows |
| `XOR` as `(A AND NOT B) OR (NOT A AND B)` equals `(A OR B) AND NOT(A AND B)`; `Unknown` if either operand is | [Logic L418-L464](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L418-L464), [xor.md L9-L33](../../.tmp/xor.md#L9-L33) | derived connective (not in Kleene's list) | `I03`, E.1 |
| `NAND`, `NOR` tables; De Morgan forms | [Logic L1920-L1945](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1920-L1945), [Simplification L666-L696](../../.tmp/Strong%20Kleene%20K3%20Expression%20Simplification%20and%20Optimization%20Specification.md#L666-L696) | [P08] (derived), [IEP-S] for symbols | E.1, `S04`, `S05` |
| Lattice laws, absorption, identity/annihilator, De Morgan, involution | [General L181-L333](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L181-L333), [Logic L2001-L2065](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L2001-L2065) | [WP-KAI]: Kleene algebra is a De Morgan algebra | `L01`-`L17` |
| Complement laws fail; K3 has no tautologies | [Logic L2069-L2119](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L2069-L2119), [General L269-L313](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L269-L313) | [P08] 7.3.7-7.3.8, [OLP] Prop. thr.3, [WP-3VL] | `C01`, `C02`; 20000 random formulas, all-`Unknown` valuation always yields `Unknown` (E.6) |
| Complement laws hold for definite constants only | [Simplification L408-L428](../../.tmp/Strong%20Kleene%20K3%20Expression%20Simplification%20and%20Optimization%20Specification.md#L408-L428), [Catalog L494-L536](../../.tmp/Strong%20Kleene%20K3%20Simplification%20Rule%20Catalog.md#L494-L536) | trivial | `C03` |
| `U` is not a general short-circuit value; `F` for `AND`, `T` for `OR` are | [Simplification L512-L560](../../.tmp/Strong%20Kleene%20K3%20Expression%20Simplification%20and%20Optimization%20Specification.md#L512-L560), [General L492-L530](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L492-L530) | [OLP] (parallel evaluation motivation) | tables |
| `NOT A = A NAND A`; `A AND B = (A NAND B) NAND (A NAND B)`; `A OR B = (A NAND A) NAND (B NAND B)`; NOR duals | [TODO L298-L304](../2026-10-02-TODO.md#L298-L304) | derived | `U01`-`U06` |
| NAND/NOR alone are **not** functionally complete for K3 | [General L628-L630](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L628-L630) | derived | E.5b: unary clone of NAND has 4 of 27 functions, no constants, no `IsTrue` |
| Binary `XOR` identities that fail (`A XOR A=F`, `A XOR NOT A=T`, distributivity) | [AlgLaws L203-L217](../../.tmp/algebratic%20laws.md#L203-L217) (vague warning) | derived | `I09`, `I10`, `I11` |
| `NXOR`: `Unknown` if any operand `Unknown`, else odd-count parity. Interval rule, binary fold, 3-input DNF and `OR(Exactly(odd k))` all agree | [xor.md L37-L91](../../.tmp/xor.md#L37-L91), [Logic L468-L560](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L468-L560), [N-ary L5-L124](../../.tmp/N-ary%20Logical%20Operators.md#L5-L124) | derived (strongest extension) | `X03a`-`X10` for n=3..5 |
| `XOR(A,B)` = `Exactly(1,A,B)`; `ExactlyOne` differs from parity from n=3 | [N-ary L112-L114](../../.tmp/N-ary%20Logical%20Operators.md#L112-L114), [xor.md L125-L153](../../.tmp/xor.md#L125-L153) | derived | `X11`, `X12`, `X13` |
| `AtLeast`/`AtMost`/`Exactly` interval rule equals the strongest extension of the Boolean cardinality predicate | [Logic L632-L805](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L632-L805) | [FFL18] §2.2 (definition of closure), derived | `N2_*`: 105 (operator, n, k) combinations, n=0..6, PASS |
| `AtLeast(k)` is the k-th largest value under `F<U<T` | [TODO L21-L27](../2026-10-02-TODO.md#L21-L27) claims the order is "useful for cardinality bounds" | derived | `N5` (n<=5). This justifies the claim |
| `AtLeast(k)` equals `OR` over k-subsets of `AND` (monotone, K3-exact); `Exactly(k)` equals `AtLeast(k) AND AtMost(k)` | not in spec; used by `UniversalGateExpander` | derived | `N3_*` (n<=5) |
| `ANY`=`OR`, `ALL`=`AND`, `NONE`=`NOT OR` | [Logic L808-L846](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L808-L846) | SQL `ANY`/`ALL` with NULL behave the same way [PG-SUB] | `N01a`-`N05c` |
| Counting `Unknown` as `False` or as `True` is wrong | [Logic L1478-L1518](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1478-L1518) | derived | `N4_*` |
| `COALESCE` table; associative; not `OR`; not `IMPLIES`; `A??A=A` | [Logic L901-L980](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L901-L980) | [PG-COND] "first of its arguments that is not null" | E.1, `Q01`, `Q02`, `Q06`, `Q07` |
| `If` text of §25 equals the strongest extension of if-then-else | [Logic L1134-L1174](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1134-L1174), [Logic L2516-L2536](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L2516-L2536) | [FFL18] §3 (CMUX), see C2 | `F01`, `F03`, `F06` |
| Inspection and projection tables as printed | [Logic L1178-L1274](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1178-L1274), [Logic L1951-L1989](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1951-L1989) | [PG-CMP] `IS [NOT] TRUE/FALSE/UNKNOWN` | E.8 |
| Catalog "Safe" rules, "Forbidden" X01/X02/X10 and their counterexamples | [Catalog L64-L647](../../.tmp/Strong%20Kleene%20K3%20Simplification%20Rule%20Catalog.md#L64-L647) | derived | `L*`, `C*`, `K02` |
| Documents with no K3 truth claims: grouping delimiters, processing pipeline, recommended spec set, Expression notation | `Grouping Delimiters and Rendering.md`, [Processing L1-L60](../../.tmp/Strong%20K3%20Expression%20Processing%20and%20Evaluation%20Specification.md#L1-L60), [RecSet L1-L20](../../.tmp/Recommended%20specification%20set.md#L1-L20), [ExprSpec L1-L50](../../.tmp/Expression%20specification.md#L1-L50) | n/a | Read in full; nothing to correct. Pipeline order (simplify before evaluate), "`Unknown` is not `false`/`null`/error" ([Processing L770-L781](../../.tmp/Strong%20K3%20Expression%20Processing%20and%20Evaluation%20Specification.md#L770-L781)) and "`AND` is Monotonic: Yes" ([RecSet L430-L445](../../.tmp/Recommended%20specification%20set.md#L430-L445)) are all correct |
| Worked examples (`AtLeast(2,T,U,F)=U`, `XOR(T,T,U)=U`, `If(U,T,T)=T`, ...) | [Logic L721-L725](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L721-L725), [Logic L1505-L1516](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1505-L1516), [Logic L2471-L2536](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L2471-L2536) | derived | E.8: all PASS (only the editorial "six" row fails) |

## B. Deviations and errors in the specs

### B1. Consensus theorem presented as a K3 simplification (HIGH)

**Spec text.** [Simplification L432-L454](../../.tmp/Strong%20Kleene%20K3%20Expression%20Simplification%20and%20Optimization%20Specification.md#L432-L454): "The classical consensus theorem remains valid for the corresponding Strong K3 expression: `(A AND B) OR (NOT A AND C) OR (B AND C)` may be simplified to `(A AND B) OR (NOT A AND C)`". [AlgLaws L157-L167](../../.tmp/algebratic%20laws.md#L157-L167) repeats it ("also has a K3-valid form"). The Catalog says the opposite: [Catalog L592-L647](../../.tmp/Strong%20Kleene%20K3%20Simplification%20Rule%20Catalog.md#L592-L647) (`K3-SIM-X10`, Forbidden, counterexample `A=U, B=T, C=T`). The three documents contradict each other.

**Standard.** K3 is a De Morgan (Kleene) algebra: lattice and involution laws hold, the complement laws `x OR NOT x = 1` and `x AND NOT x = 0` do not [WP-KAI]. The classical proof of the consensus theorem is `yz = yz(x OR NOT x)`, i.e. it consumes excluded middle, so it cannot survive in K3. The Catalog is right.

**Brute force.** `K01`: the identity is INVALID. At `A=U, B=T, C=T` the three-term form is `T` and the two-term form is `U`. `K04`: whenever the two-term form is definite, the three-term form agrees (the reduced form is less informative, never contradictory). `K03`: the three-term form is exactly the strongest extension of `if A then B else C`.

**Impact on the implementation.** Not inherited. `Simplifier` has no consensus rule (only identity, domination, absorption, De Morgan where smaller, threshold shifts; [Simplifier.cs L156-L213](../../src/TruthWeaver/Rewriting/Simplifier.cs#L156-L213)). The consensus term is deliberately part of `If` ([PrimitiveExpander.cs L174-L181](../../src/TruthWeaver/Rewriting/PrimitiveExpander.cs#L174-L181), [Analyzer.cs L282-L294](../../src/TruthWeaver/Analysis/Analyzer.cs#L282-L294), [K3Oracle.cs L221-L234](../../tests/TruthWeaver.Tests/TestSupport/K3Oracle.cs#L221-L234)). This is the one place where a naive reading of Simplification §12 would do damage: "optimising" `(c AND t) OR (NOT c AND f) OR (t AND f)` to the two-term multiplexer turns `If(Unknown,True,True)` from `True` into `Unknown`. `Compressor` recognises the exact three-term shape as `If` ([Compressor.cs L106-L124](../../src/TruthWeaver/Rewriting/Compressor.cs#L106-L124)), so the pair is safe today.

**Suggested fix.** Delete [Simplification L432-L454](../../.tmp/Strong%20Kleene%20K3%20Expression%20Simplification%20and%20Optimization%20Specification.md#L432-L454) and [AlgLaws L157-L167](../../.tmp/algebratic%20laws.md#L157-L167) (or replace with "NOT valid, see K3-SIM-X10"). Add one regression test: `Simplify()` of an `If` whose condition is an `Unknown` term and whose branches are equal definite constants keeps value `True`.

### B2. `All()` row of the Cardinality Summary (MEDIUM)

**Spec text.** [Logic L896](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L896): `All()` is `True` when `T=n and U=0`, `False` when `T<n`.

**Standard / derivation.** `ALL` is `AtLeast(n)` ([Logic L817](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L817)), which is `False` only when `T+U<n`, i.e. at least one operand is `False`. With `T<n` but no `False` operand the result is `Unknown` (`ALL(T,U)=U`).

**Brute force.** E.9: the `All()` row disagrees with the interval rule in 300 of 2004 operand/threshold cases (e.g. operands `UU`: table says `False`, correct is `Unknown`); the other five rows pass. `T19_All_2..4` list counterexamples.

**Impact.** Not inherited: `Evaluator` evaluates `ALL` as `EvaluateThreshold(AtLeast, n, ...)` ([Evaluator.cs L378-L406](../../src/TruthWeaver/Evaluation/Evaluator.cs#L378-L406)); the README and CONTEXT do not reproduce the row. **Fix:** replace `T<n` with `T+U<n`.

### B3. `Project` / `Collapse`: three incompatible definitions, one self-contradiction (MEDIUM)

**Spec text.**

| Document | Project | Collapse |
| --- | --- | --- |
| [ProjCollapse L1-L94](../../.tmp/ProjectAndCollapse.md#L1-L94) | `U -> p` for `p` in `{T,F}` | "truth collapse" `C_T` (`U -> F`), "falsity collapse" `C_F` (`U -> T`); "Projection resolves U; collapse rejects/loses U" |
| [Logic L1276-L1408](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1276-L1408) | `Project(A, UnknownAsFalse/True)` | `Collapse(expr, UnknownAsFalse/True/UnknownIsError)`, "the semantic act of requiring a definite Boolean" |
| [General L727-L757](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L727-L757), [General L840-L841](../../.tmp/Strong%20Kleene%20K3%20General%20Specification.md#L840-L841) | "mapping of a K3 result into another representation or domain" | "explicit reduction of the K3 domain according to a defined policy" |
| [Processing L785-L825](../../.tmp/Strong%20K3%20Expression%20Processing%20and%20Evaluation%20Specification.md#L785-L825) | policies include `T->accept, U->indeterminate, F->reject` | same (an identity relabelling, not a Boolean) |

**Contradiction inside `ProjectAndCollapse.md`.** The formula for falsity collapse gives `C_F(U) = T` ([ProjCollapse L57-L65](../../.tmp/ProjectAndCollapse.md#L57-L65)), but the closing prose says collapse is "reserved for named predicates such as `IsTrue`/`IsFalse`" with `IsFalse(U) = F` ([ProjCollapse L80-L94](../../.tmp/ProjectAndCollapse.md#L80-L94)). `J_P4`: `C_F` is `NOT IsFalse`, the opposite polarity. Also `Project(U->F)` is the same function as `C_T` and `Project(U->T)` the same as `C_F` (`J_P1`, `J_P2`), so "projection resolves, collapse rejects" distinguishes nothing (`J_P5`).

**Standard.** K3 has no `Project`/`Collapse`. The functions themselves are standard: `Project(x, False)` is SQL `IS TRUE` and the `WHERE` rule (keep only `TRUE`); `Project(x, True)` is `IS NOT FALSE` and the `CHECK` rule ("satisfied if the check expression evaluates to true or the null value" [PG-CHK]; [WP-SQL] calls the designated values "True and Unknown"). Bochvar's external connectives include "is undefined" and an external negation [OLP]. I found no source using "Project"/"Collapse" for this. **UNVERIFIED** that no such usage exists; in relational algebra and SQL, "projection" means column selection, which collides with the term (D).

**Impact.** ADR-0005 d12/d14 and `spec.md` already resolved this ("Reference material ... is partly inconsistent (for example `Project`/`Collapse`)" [spec.md L108](spec.md#L108)), and `K3Oracle.Project`/`Collapse` ([K3Oracle.cs L267-L294](../../tests/TruthWeaver.Tests/TestSupport/K3Oracle.cs#L267-L294)) match the standard maps (`J_P3`). No code error.

**Suggested fix.** Delete or banner `ProjectAndCollapse.md`. Keep one statement: `Project(x, v) = COALESCE(x, v)`, `Collapse` adds the `UnknownIsError` outcome.

### B4. "Inverse coalesce" example is `COALESCE` itself (LOW)

**Spec text.** [Logic L982-L1010](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L982-L1010): "An expression that selects one value when `A` is unknown and another when `A` is known can be expressed ... `If(IsUnknown(A), B, A)`", offered in a section titled "Inverse Coalesce".

**Brute force.** `Q05`: `If(IsUnknown(A),B,A)` equals `A ?? B` on all 9 rows. It is the same operator, not an inverse (`COALESCE` is not injective, so it has no inverse). The statement that `NOT(A ?? B)` "is generally different from" `NOT(A) ?? B` is true (`Q03`), but the spec misses the actual identity `NOT(A ?? B) = NOT(A) ?? NOT(B)` (`Q04`).

**Impact.** None in code. **Fix:** retitle the section or give an example that selects when `A` is known (e.g. `If(IsKnown(A), B, A)`).

### B5. Grammar and precedence table disagree on `XOR` vs `AND` (LOW; not inherited)

**Spec text.** The grammar nests `xor-expression` *below* `and-expression` ([Logic L1730-L1736](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1730-L1736)), so `XOR` binds tighter than `AND`. The table lists `AND` (3), `XOR` (4), `OR` (5) ([Logic L1764-L1766](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1764-L1766)), so `AND` binds tighter. The grammar also omits `IMPLIES`/`EQUIVALENT` and makes `??` left-associative ([Logic L1722-L1724](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1722-L1724)) while the table says right ([Logic L1769](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1769)).

**Brute force.** `P01`: `A AND B XOR C` evaluates differently under the two readings on 8 of 27 valuations (e.g. `A=F, B=F, C=T`: `T` vs `F`).

**Standard.** C# orders `&` over `^` over `|` [MS-BOOL] (matches the table, not the grammar), and `??` is right-associative [MS-COAL]. `??` is semantically associative here, so only `XOR` matters.

**Impact.** Not inherited: ADR-0005 d8 forbids mixing `XOR`/`EQUIVALENT`/`NAND`/`NOR`/`IMPLIES`/`??` with other infix operators without parentheses. **Fix:** drop the grammar section or align it to the table.

### B6. `BETWEEN` with `min > max` (LOW)

**Spec text.** [Logic L817](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L817): `BETWEEN(m,n,X...)` = `AND(AtLeast(m,...), AtMost(n,...))`, no bound constraint.

**Brute force.** `N12`: for `m<=n` the composition equals the strongest extension (`N11`, n=5). For `m>n` (empty range) the composition returns `Unknown` where every completion says `False`.

**Impact.** Not reachable: the compiler requires `0<=min<=max<=n` ([ADR-0005 L324-L343](../../docs/adr/0005-strong-k3-language-surface.md#L324-L343)) and `Compressor` only builds `BETWEEN` when `lower.K <= upper.K` ([Compressor.cs L302-L309](../../src/TruthWeaver/Rewriting/Compressor.cs#L302-L309)). **Fix:** add `m<=n` to the spec.

### B7. "Major three-valued systems" table (LOW)

Source: [Major L1-L9](../../.tmp/Major%20three-values%20sytems.md#L1-L9).

| Row | Problem | Evidence |
| --- | --- | --- |
| "Kleene K3" and "Strong Kleene K3" listed as two systems | They are one logic: "usually called the (strong) Kleene 3-valued logic, often written K3" [P08] 7.3.4. The other Kleene logic, **weak** Kleene (`Kw`/K3w), is missing | [P08], [OLP] |
| "Lukasiewicz L3: intermediate truth value" | OK, but state the only difference from K3: `->` (and so `<->`) gives `U->U = T`; `NOT`, `AND`, `OR` are identical | E.3, [P08] 7.3.8, [WP-3VL] |
| "Bochvar B3: meaningless/infectious" | OK. Bochvar's internal connectives are weak Kleene; B3 adds external connectives | [OLP] |
| "McCarthy 3-valued: Unknown, conditional semantics" | McCarthy's third value is "undefined" (non-termination) and his connectives are sequential: `F AND undefined = F` but `undefined AND F = undefined`. His conditional is **strict** in an undefined condition. Neither matches the K3 `If` rule | [MCC63] pp. 8-9; E.3 `AND McCarthy`; `F04` |
| "SQL 3VL" | OK for `AND/OR/NOT`. Note `WHERE` keeps `TRUE` only but `CHECK` keeps `TRUE` and `UNKNOWN` | [WP-SQL], [PG-CHK] |

**Impact.** Reference only; not used by code. The ADR does not cite McCarthy.

### B8. Smaller inconsistencies

| Where | Issue |
| --- | --- |
| [Logic L98](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L98) vs [Logic L100-L108](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L100-L108) | "six primitive operations", seven listed |
| [Logic L503-L534](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L503-L534) | The "every possible count has the same parity" test is correct but vacuous: with any `Unknown`, the count interval has two consecutive integers, so the answer is always `Unknown` (`X03a`). [xor.md L49-L61](../../.tmp/xor.md#L49-L61) states the simpler rule |
| [Logic L66-L92](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L66-L92) | "return a definite value whenever the available information is sufficient" holds per connective, not per formula: `A OR NOT A` is `Unknown` for `A=U` although every completion is `True` (E.5). Say "truth-functional" |
| [TODO L99](../2026-10-02-TODO.md#L99) vs [TODO L43-L44](../2026-10-02-TODO.md#L43-L44) | The arity table marks `XOR` as trinary and n-ary; the naming rule says binary `XOR`, n-ary `NXOR`. ADR d7 resolved it |
| [TODO L80-L81](../2026-10-02-TODO.md#L80-L81) vs [TODO L100-L101](../2026-10-02-TODO.md#L100-L101) | `NAND`/`NOR` written as `NOT(AND(...))` (n-ary) but listed binary-only. ADR d11 chose binary |
| [TODO L5](../2026-10-02-TODO.md#L5), [TODO L94](../2026-10-02-TODO.md#L94) | "trinary" for three values and for arity 3. Standard words: "three-valued" and "ternary" |
| [TODO L15](../2026-10-02-TODO.md#L15), [TODO L40](../2026-10-02-TODO.md#L40), [TODO L61](../2026-10-02-TODO.md#L61) | Typos: "Kleen", "Conical", "Oder" |
| [ExprSpec L40-L50](../../.tmp/Expression%20specification.md#L40-L50) | "K3 tautology: `E == T`" is fine but vacuous for formulas built from variables and `NOT/AND/OR` only (no tautologies). It becomes non-vacuous once constants or `COALESCE`/inspections are present (E.6: `IsKnown(A) OR IsUnknown(A)`) |

## C. Non-standard but defensible extensions (document as such)

> [!IMPORTANT]
> Once `COALESCE`, `Project`, `Collapse` and the four inspections are in the language, the whole language is **no longer Strong K3**. It is K3 plus *external* operators. Consequences: (1) the "no tautologies" theorem no longer holds; (2) refining an `Unknown` input can change a definite output; (3) `NAND`/`NOR` cannot express them (the implementation already documents this boundary: README "`COALESCE` is the one boundary"); (4) each rewrite rule must be verified per operator, which General §22 already requires. The K3 core is unaffected.

E.4 classifies every operator by information-order monotonicity and by whether it equals the strongest extension of its Boolean restriction:

| Group | Operators | Info-monotone | Equals strongest extension |
| --- | --- | --- | --- |
| Kleene connectives and derived | `NOT AND OR IMPLIES EQUIVALENT XOR NAND NOR NXOR ANY ALL NONE AtLeast AtMost Exactly ExactlyOne BETWEEN` | yes | yes |
| `If` with consensus term (spec text and ADR) | `If` | yes | yes |
| `If` as bare multiplexer, McCarthy conditional | | yes | **no** (1 and 2 of 27 valuations differ) |
| External operators | `COALESCE`, `Project`, `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`, SQL `CASE` | **no** | n/a (no Boolean restriction determines them) |
| Not Kleene | Lukasiewicz `->`/`<->` | no | no |

**C1. `COALESCE` / `??`.** SQL `COALESCE` ("the first of its arguments that is not null" [PG-COND]) and C# `??` [MS-COAL] applied to truth values. Not info-monotone (`COALESCE(U,F)=F` but `COALESCE(T,F)=T`). The spec's own words ("technically not a logical connective", [Logic L114](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L114)) are accurate; only the "primitive kernel" label is misleading. It is also the one operator that lets the kernel observe `Unknown`, which is why every inspection expands to it ([PrimitiveExpander.cs L191-L201](../../src/TruthWeaver/Rewriting/PrimitiveExpander.cs#L191-L201), `J_*`). Recommendation: label it an *external* kernel operator.

**C2. `If` with the consensus term.** `If(U,A,B)` returns `A` when `A==B`, else `Unknown` ([Logic L1134-L1174](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1134-L1174)). Not in Kleene or Priest (K3 has no if-then-else). It is not arbitrary:

- It is the strongest extension of Boolean if-then-else (`F01`, `F03`): definite exactly when every classical completion agrees.
- Hardware literature calls it the metastability-containing multiplexer: "A desirable property of a MUX is that if a = b, the output is a, regardless of s", obtained by adding the `a AND b` term; the standard `(NOT s AND a) OR (s AND b)` yields `M` when `s=M` and `a=b=1` [FFL18] §3 (eq. 20-23). §2.2 defines the extension of a gate to metastable inputs as definite exactly when all stabilisations agree and observes "this is equivalent to Kleene's 3-valued logic".
- Competing readings differ: the bare multiplexer is Kleene-composable but less informative (`F02`); McCarthy's conditional is strict in the condition ("If an undefined p occurs before a true p ... then the form is undefined", [MCC63] p. 8; `F04`); SQL `CASE` treats a non-`TRUE` condition as false and sends `Unknown` to `ELSE` [PG-COND] (`F05`, not monotone).

What the alternatives would change: the bare multiplexer and McCarthy's conditional give `If(Unknown,True,True)=Unknown` instead of `True`; SQL `CASE` gives the `ELSE` branch (`If(Unknown,x,y)=y`). ADR d13 and issues-log row 17 chose the first. That choice is standard in the sense above. Recommendation: cite [FFL18] in the ADR and keep the open question closed.

**C3. `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`.** SQL truth tests: "These will always return true or false, never a null value, even when the operand is null" [PG-CMP]. Bochvar's external connectives give `IsUnknown` (his "is undefined") and `NOT IsTrue` (his external negation) [OLP]; checked in `B01`, `B02`. Recommendation: document that results are always definite and that `IsKnown` = SQL `IS NOT UNKNOWN`.

**C4. `Project` / `Collapse`.** See B3. `Project(x, False)` = `IsTrue(x)` and `Project(x, True)` = `NOT IsFalse(x)`: `Project` is derived from the inspections, not a separate concept. `Collapse` is an application boundary and `UnknownIsError` is not a K3 function at all.

**C5. `Unknown` literal.** K3 as defined by Kleene and Priest has no constant for the third value (it is a value, not a formula). Adding one is conservative. SQL has a boolean literal `UNKNOWN` (SQL:1999 feature T031 per [WP-SQL]; PostgreSQL does not accept it [PG-BOOL]). **UNVERIFIED** against the ISO text.

**C6. Cardinality operators.** The three names are standard in constraint programming and SAT ("at-least-k", "at-most-k", "exactly-k"); **UNVERIFIED**, no citation fetched. Their interval semantics under `Unknown` is the strongest extension and coincides with the compositional K3 reading for `AtLeast`/`AtMost` (`N3_dnf_atleast`) and for `Exactly` (`N3_dnf_exactly`). `ANY`/`ALL`/`NONE` add nothing in K3; the README already says so.

**C7. `NXOR`.** Semantics are the strongest extension of parity (B8 row 2). Only the *name* is a risk (D).

**C8. `??` and `? :` as C# lookalikes.** C# `bool?` already follows Strong K3 for `&` and `|` ("`&` returns `false` ... even if another operand evaluates to `null`") and returns `null` for `!` and `^` with a `null` operand; `&&` and `||` are not defined for `bool?` [MS-BOOL]. C# `?:` needs a `bool` ("must evaluate to `true` or `false`", [MS-COND]) and `??` is null-based. So DSL `&&`/`||` mean `AND`/`OR` over three values, `? :` is `If`, and `??` is `COALESCE`; none of them is the C# operator. Recommendation: say so once in the README operator table.

**C9. Faults as `Unknown`.** Kleene's own motivation is a computation that may fail to terminate, read as `U`; strong connectives need parallel evaluation to return `F AND U = F` regardless of order [OLP]. The engine evaluates left to right but keeps going past `Unknown`, and ADR-0001/0002 turn timeouts and cancellation into `Unknown`, which is what makes sequential evaluation equal the strong semantics. Faults are therefore standard-compatible.

## D. Terminology notes

| Term in spec | Elsewhere? | Verdict |
| --- | --- | --- |
| K3, Strong Kleene, Kleene K3 | One logic: "(strong) Kleene 3-valued logic, often written K3" [P08] 7.3.4; weak Kleene is `Kw` [OLP] | Use "Strong Kleene (K3)"; never list K3 and Strong K3 separately |
| Unknown / `U` | Kleene: "undefined" (computations) or "unknown"; Priest: "neither true nor false" `i`; SQL: UNKNOWN [P08] [OLP] [PG-CMP] | Standard |
| Truth order `F<U<T`; "not a numeric ordering" | `AND`/`OR` are min/max under it [WP-3VL] [SEP-MV]. The *information* order is the other relevant order | The caution is harmless; add the information order (C). `TruthValue` is declared `False, True, Unknown` ([TruthValue.cs L8-L22](../../src/TruthWeaver.Abstractions/TruthValue.cs#L8-L22)), so `(int)` order is `F<T<U`; a targeted grep of `src` for casts, `CompareTo` and ordering comparisons found none. `default` is `False`, which suits fail-closed. Informational |
| `IMPLIES`, "material implication", `→` | Kleene `⊃` (strong) [P08] 7.3.2; Lukasiewicz `→` differs | Standard; label as "Kleene strong" |
| `EQUIVALENT`, `IFF`, `↔`, `≡` | Biconditional / equivalence [P08] 7.2.1 | Standard |
| `XNOR` | Complement of `XOR`; "sometimes ENOR, EXNOR, NXOR" [WP-XNOR] | Standard in electronics |
| **`NXOR`** (n-ary parity) | In electronics `NXOR` is a name of `XNOR`, i.e. **negated** XOR [WP-XNOR]. Here `NXOR(a,b)` equals `XOR(a,b)`, the opposite of `XNOR(a,b)` (README: "At two operands it equals `XOR`") | **Naming hazard.** `XNOR` is accepted as an alias of `EQUIVALENT` while `NXOR` means the opposite. The owner asked for the name ([TODO L43-L44](../2026-10-02-TODO.md#L43-L44)); suggest confirming, or at least a prominent warning |
| `NAND` `↑`, `NOR` `↓` | Sheffer stroke is `\|` or `↑`; Peirce arrow is `↓` [IEP-S] | Standard |
| `ANY`, `ALL` | SQL `ANY`/`SOME`/`ALL` over subqueries: `ANY` true if any true, "null, not false" otherwise; `ALL` true if "all rows yield true" [PG-SUB] | Same truth behaviour as `OR`/`AND`; empty-set cases (`ANY` false, `ALL` true) are unspecified in the spec |
| `NONE` | No SQL spelling; `NOT ANY` | TruthWeaver name |
| `BETWEEN(min,max,...)` | SQL `x BETWEEN a AND b` is a value-range test. The same inventory also has numeric `Between(value,n,k)` and a `DateTimeOffset` `Between` ([TODO L222](../2026-10-02-TODO.md#L222), [TODO L261](../2026-10-02-TODO.md#L261)) | Three unrelated `Between`s; the cardinality one counts true operands. Disambiguate in docs |
| `AtLeast`, `AtMost`, `Exactly`, `ExactlyOne` | Cardinality constraints (CP/SAT) | **UNVERIFIED** citation. TruthWeaver spelling |
| `COALESCE`, `??` | SQL `COALESCE` [PG-COND]; C# `??` [MS-COAL] | Borrowed; semantics over `Unknown` are an extension (C1) |
| `If`, `? :` | SQL `CASE`, C# `?:` need a definite condition [MS-COND] | Extension (C2) |
| `IsTrue`... `IsKnown` | SQL `IS [NOT] TRUE/FALSE/UNKNOWN` [PG-CMP]; optional feature F571 per [WP-SQL] | Standard names in SQL; **UNVERIFIED** against ISO text |
| `Project`, `Collapse`, "boundary" | Not found in K3 literature. Relational "projection" = column selection | TruthWeaver terms; collision risk (B3) |
| "Inspection", "semantic kernel", "primitive" | Design vocabulary, not logic terms | Fine; define in `CONTEXT.md` |
| `∃`, `∀` for `ANY`/`ALL` | Quantifiers | The spec itself says not to use them ([Logic L1854](../../.tmp/Strong%20Kleene%20K3%20Logic.md#L1854)) |

## E. Brute-force results appendix

The script is **throwaway and lives outside the repo** (session scratchpad). It imports no repo code and parses the spec truth tables directly from `.tmp/*.md`; Python 3.9+, no dependencies. Run: `python k3audit.py out.md`. Standard definitions used: values `F<U<T`, `NOT x` = reverse, `AND` = min, `OR` = max, `A->B := NOT A OR B`, `A<->B := (A->B) AND (B->A)` [P08] 7.2.1 and 7.3.2; strongest extension = definite iff all classical completions agree [FFL18] §2.2.

Summary of outcomes (details in the tables):

| Check family | Count | Result |
| --- | --- | --- |
| Spec truth tables parsed and recomputed (E.1) | 16 | 16 PASS |
| Identities, rewrites, definitions (E.2) | 278 individual checks | 270 agree with what the spec asserts; 8 do not (6 distinct spec statements) |
| Spec statements shown wrong | 6 | `K01` consensus; `T19_All_2..4` summary row; `J_P4`, `J_P5` Project/Collapse; `P01` precedence; `N12` (edge case). `Q05` passes as a check but shows the "inverse coalesce" label is wrong. Plus the editorial "six" (E.8) |
| Operators info-monotone and equal to strongest extension (E.4) | 19 | all PASS (the bare multiplexer, McCarthy conditional and weak-Kleene `AND` are the contrast rows) |
| Analyzer dual-rail formulas (E.7) | 19 | all PASS |

<details>
<summary>E.0 The script (<code>k3audit.py</code>)</summary>

```python
#!/usr/bin/env python3
"""
k3audit.py - throwaway brute-force audit of the TruthWeaver Strong K3 specs.

Standard Strong Kleene (K3), Kleene 1952 / Priest 2008 ch.7:
  values F < U < T  (written 0 < 1/2 < 1),  NOT x = 1-x, AND = min, OR = max,
  implication  A->B := NOT A OR B,  equivalence A<->B := (A->B) AND (B->A).
Nothing in this file imports or reads repo code; spec tables are parsed from the .tmp markdown files
and every claim is re-computed from the definitions above.
"""
import itertools
import re
import sys
from fractions import Fraction

REPO = r"C:\Users\rheone\code\TruthWeaver"
TMP = REPO + r"\.tmp"

V = ["F", "U", "T"]
R = {v: i for i, v in enumerate(V)}


# ----------------------------------------------------------------------------- standard K3 connectives
def NOT(a):
    return V[2 - R[a]]


def AND(*a):
    return V[min(R[x] for x in a)] if a else "T"


def OR(*a):
    return V[max(R[x] for x in a)] if a else "F"


def IMP(a, b):
    return OR(NOT(a), b)


def EQV(a, b):
    return AND(IMP(a, b), IMP(b, a))


def XOR(a, b):
    return OR(AND(a, NOT(b)), AND(NOT(a), b))


def NAND(a, b):
    return NOT(AND(a, b))


def NOR(a, b):
    return NOT(OR(a, b))


# Lukasiewicz L3 (contrast): x->y = min(1, 1-x+y) with F=0, U=1, T=2 halves
def IMP_L(a, b):
    return V[min(2, 2 - R[a] + R[b])]


def EQV_L(a, b):
    return AND(IMP_L(a, b), IMP_L(b, a))


# Weak Kleene / Bochvar internal: any U input -> U
def AND_W(a, b):
    return "U" if "U" in (a, b) else AND(a, b)


def OR_W(a, b):
    return "U" if "U" in (a, b) else OR(a, b)


# McCarthy sequential (left-to-right): p AND q = (p -> q, T -> F)
def AND_M(a, b):
    if a == "F":
        return "F"
    if a == "U":
        return "U"
    return b  # a == T


def OR_M(a, b):
    if a == "T":
        return "T"
    if a == "U":
        return "U"
    return b


def assignments(n):
    return itertools.product(V, repeat=n)


# ----------------------------------------------------------------------------- strong (maximal-information) extension
def completions(x):
    """All classical completions of a K3 tuple (U -> T or F)."""
    pools = [("T", "F") if v == "U" else (v,) for v in x]
    return itertools.product(*pools)


def ext(fbool):
    """Kleene's strongest (regular) extension of a Boolean function: definite iff every completion agrees."""

    def f(*x):
        outs = {fbool(*c) for c in completions(x)}
        return outs.pop() if len(outs) == 1 else "U"

    return f


def refines(x, y):
    """information order  x [= y : x equals y or x is U"""
    return x == y or x == "U"


def is_info_monotone(f, arity):
    bad = []
    for a in assignments(arity):
        for b in assignments(arity):
            if all(refines(p, q) for p, q in zip(a, b)):
                if not refines(f(*a), f(*b)):
                    bad.append((a, b, f(*a), f(*b)))
    return bad


def equal_fn(f, g, arity):
    return [(a, f(*a), g(*a)) for a in assignments(arity) if f(*a) != g(*a)]


# ----------------------------------------------------------------------------- cardinality
def counts(xs):
    return sum(1 for x in xs if x == "T"), sum(1 for x in xs if x == "U")


def card(sat, xs):
    t, u = counts(xs)
    oks = [sat(c) for c in range(t, t + u + 1)]
    if all(oks):
        return "T"
    return "U" if any(oks) else "F"


def atleast(k, xs):
    return card(lambda c: c >= k, xs)


def atmost(k, xs):
    return card(lambda c: c <= k, xs)


def exactly(k, xs):
    return card(lambda c: c == k, xs)


def between(m, n, xs):
    return AND(atleast(m, xs), atmost(n, xs))


def naive_u_false(sat, xs):  # U counted as False
    t, _ = counts(xs)
    return "T" if sat(t) else "F"


def naive_u_true(sat, xs):  # U counted as True
    t, u = counts(xs)
    return "T" if sat(t + u) else "F"


def ext_card(sat):
    def fb(*c):
        return "T" if sat(sum(1 for v in c if v == "T")) else "F"

    return ext(fb)


def dnf_atleast(k, xs):
    n = len(xs)
    if k <= 0:
        return "T"
    if k > n:
        return "F"
    return OR(*[AND(*[xs[i] for i in S]) for S in itertools.combinations(range(n), k)])


def dnf_exactly(k, xs):
    n = len(xs)
    if k < 0 or k > n:
        return "F"
    terms = []
    for S in itertools.combinations(range(n), k):
        lits = [xs[i] if i in S else NOT(xs[i]) for i in range(n)]
        terms.append(AND(*lits))
    return OR(*terms)


def nxor_interval(xs):
    t, u = counts(xs)
    par = {c % 2 for c in range(t, t + u + 1)}
    return "T" if par == {1} else ("F" if par == {0} else "U")


def nxor_any_u(xs):
    if "U" in xs:
        return "U"
    return "T" if sum(1 for x in xs if x == "T") % 2 else "F"


def nxor_fold(xs):
    r = xs[0]
    for x in xs[1:]:
        r = XOR(r, x)
    return r


def nxor_odd_exactly(xs):  # PrimitiveExpander.ExpandParity
    n = len(xs)
    return OR(*[exactly(k, xs) for k in range(1, n + 1, 2)])


# ----------------------------------------------------------------------------- If / COALESCE / inspection / project
def COAL(*xs):
    for x in xs:
        if x != "U":
            return x
    return "U"


def IF_ext(c, t, f):  # strongest extension of Boolean if-then-else
    return ext(lambda c_, t_, f_: t_ if c_ == "T" else f_)(c, t, f)


def IF_mux(c, t, f):  # bare multiplexer
    return OR(AND(c, t), AND(NOT(c), f))


def IF_cons(c, t, f):  # multiplexer + consensus term (ADR-0005 decision 13)
    return OR(AND(c, t), AND(NOT(c), f), AND(t, f))


def IF_spec(c, t, f):  # Strong K3 Logic.md section 25 as written
    if c == "T":
        return t
    if c == "F":
        return f
    return t if t == f else "U"


def IF_mccarthy(c, t, f):  # McCarthy 1963: conditional strict in undefined p
    if c == "U":
        return "U"
    return t if c == "T" else f


def IF_sql(c, t, f):  # SQL CASE WHEN c THEN t ELSE f END: only TRUE selects THEN
    return t if c == "T" else f


def IsTrue(x):
    return "T" if x == "T" else "F"


def IsFalse(x):
    return "T" if x == "F" else "F"


def IsUnknown(x):
    return "T" if x == "U" else "F"


def IsKnown(x):
    return "F" if x == "U" else "T"


def Proj(x, p):
    return p if x == "U" else x


# ----------------------------------------------------------------------------- reporting helpers
OUT = []


def p(s=""):
    OUT.append(s)
    print(s)


def md_table(headers, rows):
    p("| " + " | ".join(headers) + " |")
    p("| " + " | ".join(["---"] * len(headers)) + " |")
    for r in rows:
        p("| " + " | ".join(str(c).replace("|", "\|").replace("<", "&lt;") for c in r) + " |")
    p()


def fmt(a):
    return "".join(a) if isinstance(a, (tuple, list)) else str(a)


# ============================================================================= PART 1: parse spec truth tables
def read_lines(name):
    with open(TMP + "\\" + name, encoding="utf-8") as fh:
        return fh.read().split("\n")


OPS2 = {
    "∧": AND,
    "∨": OR,
    "→": IMP,
    "↔": EQV,
    "⊕": XOR,
    "↑": NAND,
    "↓": NOR,
    "??": lambda a, b: COAL(a, b),
}
ROW9 = re.compile(r"^\|\s*([TUF])\s*\|\s*([TUF])\s*\|\s*([TUF])\s*\|\s*$")
HDR9 = re.compile(r"^\|\s*A\s*\|\s*B\s*\|\s*`?A\s*(∧|∨|→|↔|⊕|↑|↓|\?\?)\s*B`?\s*\|\s*$")
MATRIX_HDR = re.compile(r"^\|\s*(∧|∨|⊕|→|↔|↑|↓)\s*\|\s*T\s*\|\s*U\s*\|\s*F\s*\|\s*$")
MATRIX_ROW = re.compile(r"^\|\s*\*\*([TUF])\*\*\s*\|\s*([TUF])\s*\|\s*([TUF])\s*\|\s*([TUF])\s*\|\s*$")


def check_tables(fname, short):
    lines = read_lines(fname)
    results = []
    for i, ln in enumerate(lines):
        m = HDR9.match(ln)
        if m:
            sym = m.group(1)
            rows = []
            j = i + 2
            while j < len(lines) and ROW9.match(lines[j]):
                a, b, r = ROW9.match(lines[j]).groups()
                rows.append((a, b, r))
                j += 1
            bad = [(a, b, r, OPS2[sym](a, b)) for a, b, r in rows if OPS2[sym](a, b) != r]
            results.append((f"{short}:{i+1}", f"A {sym} B (9-row list)", len(rows), bad, "list"))
        m = MATRIX_HDR.match(ln)
        if m:
            sym = m.group(1)
            bad = []
            n = 0
            for k in (1, 2, 3):
                # rows start after the separator line
                row = lines[i + 1 + k] if i + 1 + k < len(lines) else ""
                mm = MATRIX_ROW.match(row)
                if not mm:
                    continue
                a = mm.group(1)
                for b, r in zip(["T", "U", "F"], mm.groups()[1:]):
                    n += 1
                    if OPS2[sym](a, b) != r:
                        bad.append((a, b, r, OPS2[sym](a, b)))
            results.append((f"{short}:{i+1}", f"{sym} (3x3 matrix)", n, bad, "matrix"))
    return results


def part1():
    p("### E.1 Spec truth tables parsed from the markdown and recomputed")
    rows = []
    for fname, short in [
        ("Strong Kleene K3 Logic.md", "Logic"),
        ("Strong Kleene K3 General Specification.md", "General"),
        ("xor.md", "xor"),
    ]:
        for loc, name, n, bad, kind in check_tables(fname, short):
            rows.append((loc, name, n, "PASS" if not bad else "FAIL " + str(bad)))
    md_table(["location", "table", "cells", "result vs standard K3 (Kleene strong; Priest 2008 sec 7.3)"], rows)
    p("Note: `Logic:332` (IMPLIES) and `Logic:404` (EQUIVALENT) use ->/<-> as Kleene-strong; Lukasiewicz would differ only at U,U.")
    p()


# ============================================================================= PART 2: identities / claims
CLAIMS = []


def claim(cid, src, text, nvars, lhs, rhs, spec_says_valid):
    """lhs/rhs are lambdas over nvars K3 values. spec_says_valid: True -> spec asserts lhs==rhs for all valuations."""
    cex = []
    for a in assignments(nvars):
        l, r = lhs(*a), rhs(*a)
        if l != r:
            cex.append((a, l, r))
    holds = not cex
    CLAIMS.append((cid, src, text, nvars, holds, cex, spec_says_valid))


def part2():
    A, B, C = None, None, None
    # --- lattice / involution laws
    claim("L01", "Logic:2052,General:104,Catalog:040", "NOT NOT A = A", 1, lambda a: NOT(NOT(a)), lambda a: a, True)
    claim("L02", "General:188", "A AND B = B AND A", 2, lambda a, b: AND(a, b), lambda a, b: AND(b, a), True)
    claim("L03", "General:192", "A OR B = B OR A", 2, lambda a, b: OR(a, b), lambda a, b: OR(b, a), True)
    claim("L04", "General:198", "(A AND B) AND C = A AND (B AND C)", 3, lambda a, b, c: AND(AND(a, b), c), lambda a, b, c: AND(a, AND(b, c)), True)
    claim("L05", "General:202", "(A OR B) OR C = A OR (B OR C)", 3, lambda a, b, c: OR(OR(a, b), c), lambda a, b, c: OR(a, OR(b, c)), True)
    claim("L06", "General:208", "A AND A = A", 1, lambda a: AND(a, a), lambda a: a, True)
    claim("L07", "General:211", "A OR A = A", 1, lambda a: OR(a, a), lambda a: a, True)
    claim("L08", "Logic:2036", "A AND (B OR C) = (A AND B) OR (A AND C)", 3, lambda a, b, c: AND(a, OR(b, c)), lambda a, b, c: OR(AND(a, b), AND(a, c)), True)
    claim("L09", "Logic:2044", "A OR (B AND C) = (A OR B) AND (A OR C)", 3, lambda a, b, c: OR(a, AND(b, c)), lambda a, b, c: AND(OR(a, b), OR(a, c)), True)
    claim("L10", "General:232", "A AND (A OR B) = A (absorption)", 2, lambda a, b: AND(a, OR(a, b)), lambda a, b: a, True)
    claim("L11", "General:236", "A OR (A AND B) = A (absorption)", 2, lambda a, b: OR(a, AND(a, b)), lambda a, b: a, True)
    claim("L12", "General:248", "A AND T = A", 1, lambda a: AND(a, "T"), lambda a: a, True)
    claim("L13", "General:252", "A OR F = A", 1, lambda a: OR(a, "F"), lambda a: a, True)
    claim("L14", "General:258", "A AND F = F", 1, lambda a: AND(a, "F"), lambda a: "F", True)
    claim("L15", "General:262", "A OR T = T", 1, lambda a: OR(a, "T"), lambda a: "T", True)
    claim("L16", "General:322", "NOT(A AND B) = NOT A OR NOT B", 2, lambda a, b: NOT(AND(a, b)), lambda a, b: OR(NOT(a), NOT(b)), True)
    claim("L17", "General:328", "NOT(A OR B) = NOT A AND NOT B", 2, lambda a, b: NOT(OR(a, b)), lambda a, b: AND(NOT(a), NOT(b)), True)
    # --- complement laws (spec says these FAIL)
    claim("C01", "Logic:2078,General:278", "A OR NOT A = T  (excluded middle)", 1, lambda a: OR(a, NOT(a)), lambda a: "T", False)
    claim("C02", "Logic:2100,General:298", "A AND NOT A = F  (non-contradiction)", 1, lambda a: AND(a, NOT(a)), lambda a: "F", False)
    claim("C03", "Catalog:110-113,Simplif:412-426", "T OR NOT T = T; F OR NOT F = T; T AND NOT T = F; F AND NOT F = F (definite constants only)", 0,
          lambda: (OR("T", NOT("T")), OR("F", NOT("F")), AND("T", NOT("T")), AND("F", NOT("F"))), lambda: ("T", "T", "F", "F"), True)
    # --- constant propagation examples
    claim("S01", "Simplif:487", "A AND (B OR F) = A AND B", 2, lambda a, b: AND(a, OR(b, "F")), lambda a, b: AND(a, b), True)
    claim("S02", "Simplif:499", "A OR (B AND F) = A", 2, lambda a, b: OR(a, AND(b, "F")), lambda a, b: a, True)
    claim("S03", "Simplif:595-617", "(A AND T) OR (A AND F) = A", 1, lambda a: OR(AND(a, "T"), AND(a, "F")), lambda a: a, True)
    claim("S04", "Simplif:683", "NAND(A,B) = NOT A OR NOT B", 2, lambda a, b: NAND(a, b), lambda a, b: OR(NOT(a), NOT(b)), True)
    claim("S05", "Simplif:689", "NOR(A,B) = NOT A AND NOT B", 2, lambda a, b: NOR(a, b), lambda a, b: AND(NOT(a), NOT(b)), True)
    # --- consensus: the two .tmp files disagree
    full = lambda a, b, c: OR(AND(a, b), AND(NOT(a), c), AND(b, c))
    red = lambda a, b, c: OR(AND(a, b), AND(NOT(a), c))
    claim("K01", "Simplif:432-454 (says VALID), algebraic laws:157-167 (says VALID)", "(A&B)|(~A&C)|(B&C) = (A&B)|(~A&C)  [consensus removal]", 3, full, red, True)
    claim("K02", "Catalog:590-647 (says FORBIDDEN)", "same identity; Catalog says NOT valid, counterexample A=U,B=T,C=T", 3, full, red, False)
    claim("K03", "(not in spec) link between consensus and If", "consensus form (A&B)|(~A&C)|(B&C) = strongest extension of ite(A,B,C)", 3, full, lambda a, b, c: IF_ext(a, b, c), True)
    claim("K04", "(not in spec) refinement", "reduced form [= full form in the information order (reduced definite => equal)", 3,
          lambda a, b, c: "ok" if (red(a, b, c) == "U" or red(a, b, c) == full(a, b, c)) else "bad", lambda a, b, c: "ok", True)
    claim("N5", "Logic:14 'ordering is useful for ... cardinality bounds'", "AtLeast(k, xs) = k-th largest of xs under F<U<T (order statistic), n<=5", 5,
          lambda *x: [atleast(k, list(x)) for k in range(1, 6)], lambda *x: [sorted(x, key=lambda v: -R[v])[k - 1] for k in range(1, 6)], True)
    claim("B01", "OLP/Bochvar external negation ~ (T->F,U->T,F->T)", "Bochvar ~x = NOT IsTrue(x)  (i.e. SQL IS NOT TRUE)", 1, lambda x: {"T": "F", "U": "T", "F": "T"}[x], lambda x: NOT(IsTrue(x)), True)
    claim("B02", "OLP/Bochvar '+' (is undefined) (T->F,U->T,F->F)", "Bochvar + x = IsUnknown(x)", 1, lambda x: {"T": "F", "U": "T", "F": "F"}[x], IsUnknown, True)
    claim("J_P4", "ProjectAndCollapse:57-65 vs :83-88", "'falsity collapse' C_F (F->F; T,U->T) = IsFalse(x) (the prose says IsFalse is the collapse)", 1, lambda x: Proj(x, "T"), IsFalse, True)
    claim("J_P5", "ProjectAndCollapse:92", "'Projection resolves U; collapse rejects U': Project(U->F) and C_T are different functions", 1, lambda x: Proj(x, "F"), lambda x: IsTrue(x), False)
    claim("I16", "Logic:1767 (EQUIVALENT left-assoc) / ADR no-chaining", "(A<->B)<->C = A<->(B<->C)", 3, lambda a, b, c: EQV(EQV(a, b), c), lambda a, b, c: EQV(a, EQV(b, c)), True)
    claim("I17", "Logic:1766 (IMPLIES right-assoc)", "(A->B)->C = A->(B->C)", 3, lambda a, b, c: IMP(IMP(a, b), c), lambda a, b, c: IMP(a, IMP(b, c)), False)
    claim("I18", "(not in spec) exportation", "(A AND B)->C = A->(B->C)", 3, lambda a, b, c: IMP(AND(a, b), c), lambda a, b, c: IMP(a, IMP(b, c)), True)
    # --- universal gates (TODO:300-304)
    claim("U01", "TODO:301", "NOT A = A NAND A", 1, lambda a: NOT(a), lambda a: NAND(a, a), True)
    claim("U02", "TODO:302", "A AND B = (A NAND B) NAND (A NAND B)", 2, lambda a, b: AND(a, b), lambda a, b: NAND(NAND(a, b), NAND(a, b)), True)
    claim("U03", "TODO:303", "A OR B = (A NAND A) NAND (B NAND B)", 2, lambda a, b: OR(a, b), lambda a, b: NAND(NAND(a, a), NAND(b, b)), True)
    claim("U04", "ADR-0005 d10 (dual)", "NOT A = A NOR A", 1, lambda a: NOT(a), lambda a: NOR(a, a), True)
    claim("U05", "ADR-0005 d10 (dual)", "A OR B = (A NOR B) NOR (A NOR B)", 2, lambda a, b: OR(a, b), lambda a, b: NOR(NOR(a, b), NOR(a, b)), True)
    claim("U06", "ADR-0005 d10 (dual)", "A AND B = (A NOR A) NOR (B NOR B)", 2, lambda a, b: AND(a, b), lambda a, b: NOR(NOR(a, a), NOR(b, b)), True)
    # --- implication / equivalence / xor definitions
    claim("I01", "Logic:306 (A->B = NOT A OR B)", "table vs Kleene strong conditional (Priest 7.3.2: i->i = i)", 2, lambda a, b: IMP(a, b),
          lambda a, b: {("T", "T"): "T", ("T", "U"): "U", ("T", "F"): "F", ("U", "T"): "T", ("U", "U"): "U", ("U", "F"): "U", ("F", "T"): "T", ("F", "U"): "T", ("F", "F"): "T"}[(a, b)], True)
    claim("I02", "Logic:372-385", "(A->B)&(B->A) [Kleene/Priest def] = OR(AND(A,B),AND(NOT A,NOT B)) [spec primitive form]", 2,
          lambda a, b: EQV(a, b), lambda a, b: OR(AND(a, b), AND(NOT(a), NOT(b))), True)
    claim("I03", "Logic:437 vs xor.md:12", "OR(AND(A,~B),AND(~A,B)) = (A OR B) AND NOT(A AND B)", 2, lambda a, b: XOR(a, b), lambda a, b: AND(OR(a, b), NOT(AND(a, b))), True)
    claim("I04", "ADR-0005 d5 (EQUIVALENT replaces XNOR)", "A<->B = NOT(A XOR B)", 2, lambda a, b: EQV(a, b), lambda a, b: NOT(XOR(a, b)), True)
    claim("I05", "Lukasiewicz contrast", "Kleene A->B = Lukasiewicz A->B", 2, lambda a, b: IMP(a, b), lambda a, b: IMP_L(a, b), False)
    claim("I06", "Lukasiewicz contrast", "Kleene A<->B = Lukasiewicz A<->B", 2, lambda a, b: EQV(a, b), lambda a, b: EQV_L(a, b), False)
    claim("I07", "Priest 7.3.8 / Logic (implicit)", "A->A = T (identity law)", 1, lambda a: IMP(a, a), lambda a: "T", False)
    claim("I08", "Logic:2078 analogues", "A<->A = T", 1, lambda a: EQV(a, a), lambda a: "T", False)
    claim("I09", "algebraic laws:203-217", "A XOR A = F", 1, lambda a: XOR(a, a), lambda a: "F", False)
    claim("I10", "algebraic laws:203-217", "A XOR NOT A = T", 1, lambda a: XOR(a, NOT(a)), lambda a: "T", False)
    claim("I11", "XOR ring law (not in spec)", "A AND (B XOR C) = (A AND B) XOR (A AND C)", 3, lambda a, b, c: AND(a, XOR(b, c)), lambda a, b, c: XOR(AND(a, b), AND(a, c)), False)
    claim("I12", "XOR assoc (n-ary XOR relies on it)", "(A XOR B) XOR C = A XOR (B XOR C)", 3, lambda a, b, c: XOR(XOR(a, b), c), lambda a, b, c: XOR(a, XOR(b, c)), True)
    claim("I13", "XOR identity", "A XOR F = A", 1, lambda a: XOR(a, "F"), lambda a: a, True)
    claim("I14", "XOR identity", "A XOR T = NOT A", 1, lambda a: XOR(a, "T"), lambda a: NOT(a), True)
    claim("I15", "Logic:2052 spirit", "contraposition A->B = ~B->~A", 2, lambda a, b: IMP(a, b), lambda a, b: IMP(NOT(b), NOT(a)), True)
    # --- XOR family
    for n in (3, 4, 5):
        claim(f"X0{n}a", "xor.md:75, Logic:480-534, N-ary:61-74", f"NXOR n={n}: interval-parity rule = 'Unknown if any Unknown else odd-count'", n,
              lambda *x: nxor_interval(list(x)), lambda *x: nxor_any_u(list(x)), True)
        claim(f"X0{n}b", "xor.md:75", f"NXOR n={n}: left fold of binary XOR = 'Unknown if any Unknown'", n, lambda *x: nxor_fold(list(x)), lambda *x: nxor_any_u(list(x)), True)
        claim(f"X0{n}c", "N-ary:116-122,Logic:552", f"NXOR n={n}: OR of Exactly(k) over odd k", n, lambda *x: nxor_odd_exactly(list(x)), lambda *x: nxor_any_u(list(x)), True)
    claim("X10", "Logic:493-499,N-ary:26-33", "3-input DNF (A~B~C | ~AB~C | ~A~BC | ABC) = parity", 3,
          lambda a, b, c: OR(AND(a, NOT(b), NOT(c)), AND(NOT(a), b, NOT(c)), AND(NOT(a), NOT(b), c), AND(a, b, c)), lambda a, b, c: nxor_any_u([a, b, c]), True)
    claim("X11", "N-ary:113", "XOR(A,B) = Exactly(1,A,B)", 2, lambda a, b: XOR(a, b), lambda a, b: exactly(1, [a, b]), True)
    claim("X12", "xor.md:149, ADR-0005 d4", "ExactlyOne(A,B,C) = NXOR(A,B,C)  (spec/ADR say they DIFFER for n>=3)", 3, lambda a, b, c: exactly(1, [a, b, c]), lambda a, b, c: nxor_any_u([a, b, c]), False)
    claim("X13", "xor.md:149", "T,T,T: parity = T, ExactlyOne = F", 0, lambda: (nxor_any_u(["T", "T", "T"]), exactly(1, ["T", "T", "T"])), lambda: ("T", "F"), True)
    # --- COALESCE
    claim("Q01", "Logic:966", "A ?? B = A OR B", 2, lambda a, b: COAL(a, b), lambda a, b: OR(a, b), False)
    claim("Q02", "Logic:972", "A ?? B = NOT A OR B", 2, lambda a, b: COAL(a, b), lambda a, b: IMP(a, b), False)
    claim("Q03", "Logic:998-1010", "NOT(A ?? B) = NOT(A) ?? B  (spec: 'generally different')", 2, lambda a, b: NOT(COAL(a, b)), lambda a, b: COAL(NOT(a), b), False)
    claim("Q04", "(not in spec) true identity", "NOT(A ?? B) = NOT(A) ?? NOT(B)", 2, lambda a, b: NOT(COAL(a, b)), lambda a, b: COAL(NOT(a), NOT(b)), True)
    claim("Q05", "Logic:993", "If(IsUnknown(A), B, A) is offered as the 'inverse' of A ?? B; it IS A ?? B", 2, lambda a, b: IF_spec(IsUnknown(a), b, a), lambda a, b: COAL(a, b), True)
    claim("Q06", "ADR-0005 d15 (assoc)", "(A ?? B) ?? C = A ?? (B ?? C)", 3, lambda a, b, c: COAL(COAL(a, b), c), lambda a, b, c: COAL(a, COAL(b, c)), True)
    claim("Q07", "Logic:1011 'Coalesce replaces Unknown but does not otherwise alter K3 values'", "A ?? A = A", 1, lambda a: COAL(a, a), lambda a: a, True)
    # --- If
    claim("F01", "ADR-0005 d13 / Logic:1147-1172", "If(c,t,f) spec-text = strongest extension of Boolean ite", 3, IF_spec, IF_ext, True)
    claim("F02", "issues-log 17 (bare mux rejected)", "bare multiplexer (c&t)|(~c&f) = strongest extension of ite", 3, IF_mux, IF_ext, False)
    claim("F03", "ADR-0005 d13", "mux + consensus (c&t)|(~c&f)|(t&f) = strongest extension of ite", 3, IF_cons, IF_ext, True)
    claim("F04", "McCarthy 1963 p.8", "McCarthy conditional (strict in undefined condition) = strongest extension", 3, IF_mccarthy, IF_ext, False)
    claim("F05", "SQL CASE / PostgreSQL docs", "SQL CASE WHEN c THEN t ELSE f (Unknown cond -> ELSE) = strongest extension", 3, IF_sql, IF_ext, False)
    claim("F06", "Logic:1157", "If(U,A,A) = A", 1, lambda a: IF_spec("U", a, a), lambda a: a, True)
    claim("F07", "ADR-0005 d13 / Simplifier RewriteIf", "If(c,A,A) = A for every c", 2, lambda c, a: IF_cons(c, a, a), lambda c, a: a, True)
    claim("F08", "Simplifier RewriteIf", "If(T,a,b)=a and If(F,a,b)=b  (consensus never changes a definite condition)", 2,
          lambda a, b: (IF_cons("T", a, b), IF_cons("F", a, b)), lambda a, b: (a, b), True)
    # --- cardinality aliases
    for n in (1, 2, 3, 4, 5):
        claim(f"N0{n}a", "Logic:812-820", f"ANY=OR (n={n}): AtLeast(1) = OR", n, lambda *x: atleast(1, list(x)), lambda *x: OR(*x), True)
        claim(f"N0{n}b", "Logic:812-820", f"ALL=AND (n={n}): AtLeast(n) = AND", n, lambda *x: atleast(len(x), list(x)), lambda *x: AND(*x), True)
        claim(f"N0{n}c", "Logic:812-820", f"NONE (n={n}): AtMost(0) = NOT OR", n, lambda *x: atmost(0, list(x)), lambda *x: NOT(OR(*x)), True)
    claim("N10", "Logic:869", "AtLeastFalse(2,A,B,C) = AtLeast(2,~A,~B,~C)  (interval is symmetric)", 3,
          lambda a, b, c: atleast(2, [NOT(a), NOT(b), NOT(c)]), lambda a, b, c: ext_card(lambda k: k >= 2)(NOT(a), NOT(b), NOT(c)), True)
    claim("N11", "Logic:2317 BETWEEN normalisation", "BETWEEN(2,4,A..E)=AND(AtLeast(2),AtMost(4)) vs strongest extension", 5,
          lambda *x: between(2, 4, list(x)), lambda *x: ext_card(lambda k: 2 <= k <= 4)(*x), True)
    claim("N12", "edge: min>max (compiler rejects it)", "BETWEEN(3,2,A,B,C,D) = AND(AtLeast(3),AtMost(2)) vs strongest extension (empty range)", 4,
          lambda *x: between(3, 2, list(x)), lambda *x: ext_card(lambda k: 3 <= k <= 2)(*x), True)
    for n in range(0, 7):
        for k in range(0, n + 2):
            for nm, f_int, sat in (("AtLeast", atleast, lambda c, k=k: c >= k), ("AtMost", atmost, lambda c, k=k: c <= k), ("Exactly", exactly, lambda c, k=k: c == k)):
                claim(f"N2_{nm}_{n}_{k}", "Logic:656-669,894", f"{nm}({k}) interval rule == strongest extension, n={n}", n,
                      lambda *x, f_int=f_int, k=k: f_int(k, list(x)), lambda *x, sat=sat: ext_card(sat)(*x), True)
    for n in range(1, 6):
        for k in range(1, n + 1):
            claim(f"N3_dnf_atleast_{n}_{k}", "UniversalGateExpander / ADR d10", f"AtLeast({k}) = OR of AND over k-subsets (monotone K3 formula), n={n}", n,
                  lambda *x, k=k: dnf_atleast(k, list(x)), lambda *x, k=k: atleast(k, list(x)), True)
        for k in range(0, n + 1):
            claim(f"N3_dnf_exactly_{n}_{k}", "(not claimed) DNF of Exactly", f"Exactly({k}) = OR_S AND(S, NOT rest) compositional K3, n={n}", n,
                  lambda *x, k=k: dnf_exactly(k, list(x)), lambda *x, k=k: exactly(k, list(x)), True)
        claim(f"N3_exact_via_{n}", "UniversalGateExpander", f"Exactly(1) = AtLeast(1) AND AtMost(1), n={n}", n,
              lambda *x: exactly(1, list(x)), lambda *x: AND(atleast(1, list(x)), atmost(1, list(x))), True)
    # naive counting alternatives (what a 'naive count' implementation would do)
    for nm, sat in (("AtLeast(2)", lambda c: c >= 2), ("AtMost(1)", lambda c: c <= 1), ("Exactly(1)", lambda c: c == 1)):
        claim(f"N4_{nm}_U_as_False", "naive alternative", f"{nm} with Unknown counted as False  == interval rule (n=4)", 4,
              lambda *x, sat=sat: naive_u_false(sat, list(x)), lambda *x, nm=nm: {"AtLeast(2)": atleast(2, list(x)), "AtMost(1)": atmost(1, list(x)), "Exactly(1)": exactly(1, list(x))}[nm], False)
        claim(f"N4_{nm}_U_as_True", "naive alternative", f"{nm} with Unknown counted as True  == interval rule (n=4)", 4,
              lambda *x, sat=sat: naive_u_true(sat, list(x)), lambda *x, nm=nm: {"AtLeast(2)": atleast(2, list(x)), "AtMost(1)": atmost(1, list(x)), "Exactly(1)": exactly(1, list(x))}[nm], False)
    # --- inspections / Project / Collapse
    for nm, f, g in (("IsUnknown", IsUnknown, lambda x: AND(COAL(x, "T"), COAL(NOT(x), "T"))),
                     ("IsTrue", IsTrue, lambda x: COAL(x, "F")),
                     ("IsFalse", IsFalse, lambda x: COAL(NOT(x), "F")),
                     ("IsKnown", IsKnown, lambda x: OR(COAL(x, "F"), COAL(NOT(x), "F")))):
        claim(f"J_{nm}", "PrimitiveExpander.ExpandInspection", f"{nm}(x) via COALESCE", 1, f, g, True)
    claim("J_P1", "ProjectAndCollapse:18-22", "Project(x,U->F) = 'truth collapse' C_T = IsTrue(x)", 1, lambda x: Proj(x, "F"), IsTrue, True)
    claim("J_P2", "ProjectAndCollapse:49-65", "Project(x,U->T) = 'falsity collapse' C_F = NOT IsFalse(x)", 1, lambda x: Proj(x, "T"), lambda x: NOT(IsFalse(x)), True)
    claim("J_P3", "ADR-0005 d12", "Project(x,v) = COALESCE(x,v)", 2, lambda x, v: Proj(x, v), lambda x, v: COAL(x, v), True)
    # --- repo rewrite spot checks
    claim("W01", "Simplifier.RewriteNegatedPair", "XOR(NOT l, r) = EQUIVALENT(l, r)", 2, lambda l, r: XOR(NOT(l), r), lambda l, r: EQV(l, r), True)
    claim("W02", "Simplifier.RewriteNegatedPair", "XOR(NOT l, NOT r) = XOR(l, r)", 2, lambda l, r: XOR(NOT(l), NOT(r)), lambda l, r: XOR(l, r), True)
    claim("W03", "Simplifier", "IMPLIES(NOT a, b) = OR(a, b)", 2, lambda a, b: IMP(NOT(a), b), lambda a, b: OR(a, b), True)
    claim("W04", "Simplifier", "NAND(NOT l, NOT r) = OR(l, r)", 2, lambda l, r: NAND(NOT(l), NOT(r)), lambda l, r: OR(l, r), True)
    claim("W05", "Simplifier", "NOT IsKnown(x) = IsUnknown(x)", 1, lambda x: NOT(IsKnown(x)), IsUnknown, True)
    claim("W06", "Simplifier", "IsTrue(NOT x) = IsFalse(x); IsUnknown(NOT x)=IsUnknown(x)", 1, lambda x: (IsTrue(NOT(x)), IsUnknown(NOT(x))), lambda x: (IsFalse(x), IsUnknown(x)), True)
    claim("W07", "Simplifier.RewriteThreshold", "AtLeast(k, T, rest...) = AtLeast(k-1, rest...)  (n=3 rest, k=2)", 3, lambda a, b, c: atleast(2, ["T", a, b, c]), lambda a, b, c: atleast(1, [a, b, c]), True)
    claim("W08", "Simplifier.RewriteThreshold", "Exactly(k, F, rest...) = Exactly(k, rest...)", 3, lambda a, b, c: exactly(2, ["F", a, b, c]), lambda a, b, c: exactly(2, [a, b, c]), True)
    claim("W09", "Simplifier.RewriteThreshold", "all-Unknown operands, valid threshold -> Unknown (AtLeast(2,U,U,U))", 0, lambda: atleast(2, ["U", "U", "U"]), lambda: "U", True)
    claim("W10", "Simplifier NOT threshold flip", "NOT AtLeast(k) = AtMost(k-1) (n=4,k=2)", 4, lambda *x: NOT(atleast(2, list(x))), lambda *x: atmost(1, list(x)), True)
    claim("W11", "Simplifier absorption", "a AND (a OR b) = a, a OR (a AND b) = a (both forms)", 2, lambda a, b: (AND(a, OR(a, b)), OR(a, AND(a, b))), lambda a, b: (a, a), True)
    claim("W12", "Compressor", "OR(NOT a, NOT b) = NAND(a,b); AND(NOT a, NOT b) = NOR(a,b)", 2, lambda a, b: (OR(NOT(a), NOT(b)), AND(NOT(a), NOT(b))), lambda a, b: (NAND(a, b), NOR(a, b)), True)
    # --- precedence contradiction inside Logic.md (section 35 grammar vs section 36 table)
    claim("P01", "Logic:1730-1737 (grammar: XOR binds tighter than AND) vs Logic:1760-1765 (table: AND tighter than XOR)",
          "A AND B XOR C : (A AND B) XOR C  vs  A AND (B XOR C)", 3, lambda a, b, c: XOR(AND(a, b), c), lambda a, b, c: AND(a, XOR(b, c)), True)
    # --- section 19 summary table row 'All(): False when T<n'
    def all_spec(xs):
        n = len(xs)
        t, u = counts(xs)
        if t == n and u == 0:
            return "T"
        if t < n:
            return "F"  # spec text: "False: T<n"
        return "U"

    for n in (2, 3, 4):
        claim(f"T19_All_{n}", "Logic:896", f"All() row of the Cardinality Summary (True: T=n,U=0; False: T<n; else U), n={n}", n,
              lambda *x: all_spec(list(x)), lambda *x: atleast(len(x), list(x)), True)

    # report
    rows = []
    summary = {"ok": 0, "bad": 0}
    detail_bad = []
    # compress families
    fam = {}
    for cid, src, text, nv, holds, cex, spec_valid in CLAIMS:
        key = re.sub(r"_\d+(_\d+)?$", "", cid) if cid.startswith("N2_") or cid.startswith("N3_") else cid
        fam.setdefault(key, []).append((cid, src, text, nv, holds, cex, spec_valid))
    for key, items in fam.items():
        if key.startswith("N2_") or key.startswith("N3_"):
            allmatch = all((h == sv) for _, _, _, _, h, _, sv in items)
            rows.append((key + f" (x{len(items)})", items[0][1], items[0][2].split(", n=")[0] + ", n=0..6" if key.startswith("N2_") else items[0][2].split(", n=")[0] + ", n=1..5", "valid" if all(h for _, _, _, _, h, _, _ in items) else "INVALID", "spec says valid" , "PASS" if allmatch else "FAIL"))
            for cid, src, text, nv, holds, cex, spec_valid in items:
                if holds != spec_valid:
                    detail_bad.append((cid, text, cex[:3]))
            continue
        for cid, src, text, nv, holds, cex, spec_valid in items:
            res = "PASS" if holds == spec_valid else "FAIL"
            verdict = "valid" if holds else "INVALID"
            claimed = "spec says valid" if spec_valid else "spec says NOT valid"
            if holds != spec_valid:
                detail_bad.append((cid, text, cex[:4]))
            rows.append((cid, src, text, verdict, claimed, res))
    p("### E.2 Identity / table / rewrite claims, brute-forced over all 3^n valuations")
    p("`valid` = LHS equals RHS on every valuation. `PASS` = brute force agrees with what the spec asserts (incl. spec assertions that something is NOT valid). `FAIL` = spec claim is wrong.")
    p()
    md_table(["id", "source (file:line)", "claim", "brute force", "spec asserts", "result"], rows)
    p("#### E.2a Counterexamples for every FAIL row")
    cexrows = []
    for cid, text, cex in detail_bad:
        for a, l, r in cex:
            cexrows.append((cid, text, "".join(a) if a else "-", l if not isinstance(l, tuple) else "/".join(l), r if not isinstance(r, tuple) else "/".join(r)))
    md_table(["id", "claim", "valuation", "LHS", "RHS"], cexrows)
    # counterexamples for the 'invalid by design' rows (informative)
    p("#### E.2b Counterexamples that demonstrate the 'not valid in K3' claims the specs make")
    ex = []
    for cid, src, text, nv, holds, cex, spec_valid in CLAIMS:
        if not spec_valid and not holds and not cid.startswith("N4") and cid not in ("K02",):
            a, l, r = cex[0]
            ex.append((cid, text, "".join(a) if a else "-", l if not isinstance(l, tuple) else "/".join(l), r if not isinstance(r, tuple) else "/".join(r), len(cex)))
    md_table(["id", "claim", "first counterexample", "LHS", "RHS", "#failing valuations"], ex)
    return CLAIMS


# ============================================================================= PART 3: other computed tables
def part3():
    p("### E.3 Truth-function contrast: Kleene strong vs Lukasiewicz L3 vs weak Kleene/Bochvar vs McCarthy")
    rows = []
    for a, b in itertools.product(V, V):
        rows.append((a, b, AND(a, b), AND_W(a, b), AND_M(a, b), OR(a, b), OR_W(a, b), OR_M(a, b), IMP(a, b), IMP_L(a, b), EQV(a, b), EQV_L(a, b)))
    md_table(["A", "B", "AND strong", "AND weak/Bochvar", "AND McCarthy", "OR strong", "OR weak", "OR McCarthy", "A->B Kleene", "A->B Luk.", "A<->B Kleene", "A<->B Luk."], rows)

    p("### E.4 Information-order monotonicity (Kleene 'regular' property) and 'equals strongest extension of its Boolean restriction'")
    p("`monotone` = replacing an Unknown input by T or F can never change a definite output (x [= y => f(x) [= f(y), where U [= T and U [= F).")
    p("`= strongest ext.` = f equals the function that is definite exactly when every classical completion agrees.")
    p()

    def boolrest(f, n):
        return lambda *c: f(*c)

    registry = [
        ("NOT", NOT, 1), ("AND", AND, 2), ("OR", OR, 2), ("IMPLIES (Kleene)", IMP, 2), ("EQUIVALENT (Kleene)", EQV, 2),
        ("XOR (binary)", XOR, 2), ("NAND", NAND, 2), ("NOR", NOR, 2),
        ("NXOR n=3 (Unknown if any Unknown)", lambda a, b, c: nxor_any_u([a, b, c]), 3),
        ("AtLeast(2) n=4", lambda *x: atleast(2, list(x)), 4), ("AtMost(1) n=4", lambda *x: atmost(1, list(x)), 4),
        ("Exactly(2) n=4", lambda *x: exactly(2, list(x)), 4), ("ExactlyOne n=4", lambda *x: exactly(1, list(x)), 4),
        ("ANY n=3", lambda *x: atleast(1, list(x)), 3), ("ALL n=3", lambda *x: atleast(3, list(x)), 3), ("NONE n=3", lambda *x: atmost(0, list(x)), 3),
        ("BETWEEN(1,2) n=4", lambda *x: between(1, 2, list(x)), 4),
        ("If = mux + consensus", IF_cons, 3), ("If (spec text, sec 25)", IF_spec, 3),
        ("If = bare multiplexer", IF_mux, 3), ("If = McCarthy (strict in cond.)", IF_mccarthy, 3), ("If = SQL CASE", IF_sql, 3),
        ("Lukasiewicz A->B", IMP_L, 2), ("Lukasiewicz A<->B", EQV_L, 2), ("Weak Kleene AND", AND_W, 2), ("McCarthy AND", AND_M, 2),
        ("COALESCE(a,b)", lambda a, b: COAL(a, b), 2), ("Project(x,F)", lambda x: Proj(x, "F"), 1), ("Project(x,T)", lambda x: Proj(x, "T"), 1),
        ("IsTrue", IsTrue, 1), ("IsFalse", IsFalse, 1), ("IsUnknown", IsUnknown, 1), ("IsKnown", IsKnown, 1),
    ]
    rows = []
    for name, f, n in registry:
        mono = not is_info_monotone(f, n)
        # strongest extension of the Boolean restriction of f (inputs T/F only)
        fb = lambda *c, f=f: f(*c)
        g = ext(fb)
        diffs = equal_fn(f, g, n)
        rows.append((name, n, "yes" if mono else "NO", "yes" if not diffs else f"NO ({len(diffs)} of {3**n} valuations differ)"))
    md_table(["operator", "arity", "info-monotone", "= strongest extension of its Boolean restriction"], rows)

    p("### E.5 Where K3's truth-functional (compositional) evaluation is weaker than the strongest extension (repeated operands)")
    forms = [
        ("A OR NOT A", 1, lambda a: OR(a, NOT(a)), lambda a: "T" if True else None),
        ("A AND NOT A", 1, lambda a: AND(a, NOT(a)), None),
        ("A -> A", 1, lambda a: IMP(a, a), None),
        ("A <-> A", 1, lambda a: EQV(a, a), None),
        ("A XOR A", 1, lambda a: XOR(a, a), None),
        ("A XOR NOT A", 1, lambda a: XOR(a, NOT(a)), None),
        ("(C AND T) OR (NOT C AND F)  [mux, t=T f=F]", 1, lambda c: IF_mux(c, "T", "F"), None),
        ("(C AND T) OR (NOT C AND T)  [mux, t=f=T]", 1, lambda c: IF_mux(c, "T", "T"), None),
    ]
    rows = []
    boolf = {
        "A OR NOT A": lambda a: "T",
        "A AND NOT A": lambda a: "F",
        "A -> A": lambda a: "T",
        "A <-> A": lambda a: "T",
        "A XOR A": lambda a: "F",
        "A XOR NOT A": lambda a: "T",
        "(C AND T) OR (NOT C AND F)  [mux, t=T f=F]": lambda c: c,
        "(C AND T) OR (NOT C AND T)  [mux, t=f=T]": lambda c: "T",
    }
    for name, n, f, _ in forms:
        bf = boolf[name]
        diffs = [(a, f(*a), bf(*a)) for a in assignments(n) if bf(*a) != "U" and f(*a) != bf(*a)]
        # restrict to valuations where classical completion gives a constant answer
        rows.append((name, "; ".join(f"x={fmt(a)}: K3={l}, every completion={r}" for a, l, r in diffs) or "agrees"))
    md_table(["formula", "K3 value vs value forced by every completion"], rows)

    p("### E.5b Clone generated by NAND alone on one variable (General Spec section 20: NAND/NOR 'should not be assumed functionally complete')")
    funcs = {tuple(x for x in V)}  # identity as value table over (F,U,T)
    ident = tuple(V)
    clone = {ident}
    changed = True
    while changed:
        changed = False
        for f, g in itertools.product(list(clone), repeat=2):
            h = tuple(NAND(f[i], g[i]) for i in range(3))
            if h not in clone:
                clone.add(h)
                changed = True
    p(f"- unary functions of x reachable with NAND only (no constants): **{len(clone)} of 27**: " + "; ".join("".join(t) for t in sorted(clone)) + " (value tables over x=F,U,T; e.g. FUT=identity, TUF=NOT)")
    p("- The four are x (FUT), NOT x (TUF), x OR NOT x (TUT) and x AND NOT x (FUF). IsTrue (FFT), IsUnknown (FTF) and every constant (FFF, UUU, TTT) are NOT in the set.")
    p()
    p("### E.6 Priest-style consequence facts used to describe K3 (designated value {T}); valuations are over variables p,q")
    def valid(prem, concl, n):
        for a in assignments(n):
            if all(pr(*a) == "T" for pr in prem) and concl(*a) != "T":
                return False, a
        return True, None

    facts = [
        ("p, p->q |= q (modus ponens)", [lambda a, b: a, lambda a, b: IMP(a, b)], lambda a, b: b),
        ("p OR q, NOT p |= q (disjunctive syllogism)", [lambda a, b: OR(a, b), lambda a, b: NOT(a)], lambda a, b: b),
        ("p AND NOT p |= q (explosion)", [lambda a, b: AND(a, NOT(a))], lambda a, b: b),
        ("p AND q |= p", [lambda a, b: AND(a, b)], lambda a, b: a),
        ("p |= p OR q", [lambda a, b: a], lambda a, b: OR(a, b)),
        ("|= p OR NOT p (excluded middle)", [], lambda a, b: OR(a, NOT(a))),
        ("|= p -> p (identity)", [], lambda a, b: IMP(a, a)),
        ("|= p <-> p", [], lambda a, b: EQV(a, b if False else a)),
        ("p -> q |= NOT q -> NOT p (Priest 7.3.5)", [lambda a, b: IMP(a, b)], lambda a, b: IMP(NOT(b), NOT(a))),
    ]
    rows = []
    for name, prem, concl in facts:
        ok, cex = valid(prem, concl, 2)
        rows.append((name, "valid" if ok else "INVALID", "-" if ok else f"p={cex[0]}, q={cex[1]}"))
    md_table(["inference", "valid in K3", "countermodel"], rows)

    # no tautologies for {NOT,AND,OR,IMP,EQV,XOR,NAND,NOR,cardinality,If_cons,NXOR} formulas over variables only
    p("Pure-connective formulas (variables and NOT/AND/OR/IMPLIES/EQUIVALENT/XOR/NAND/NOR/AtLeast/AtMost/Exactly/If/NXOR; NO constants, NO Coalesce/Is*/Project): all-Unknown valuation yields Unknown?")
    import random

    random.seed(7)
    ops = [
        (1, lambda a: NOT(a)), (2, AND), (2, OR), (2, IMP), (2, EQV), (2, XOR), (2, NAND), (2, NOR),
        (3, IF_cons), (3, lambda a, b, c: atleast(2, [a, b, c])), (3, lambda a, b, c: atmost(1, [a, b, c])), (3, lambda a, b, c: exactly(1, [a, b, c])),
        (3, lambda a, b, c: nxor_any_u([a, b, c])), (3, lambda a, b, c: between(1, 2, [a, b, c])),
    ]
    bad = 0
    total = 20000

    def build(depth):
        if depth == 0 or random.random() < 0.2:
            idx = random.randrange(3)
            return (lambda v, idx=idx: v[idx])
        ar, f = random.choice(ops)
        kids = [build(depth - 1) for _ in range(ar)]
        return lambda v, f=f, kids=kids: f(*[k(v) for k in kids])

    for _ in range(total):
        fm = build(4)
        if fm(("U", "U", "U")) != "U":
            bad += 1
    p(f"- {total} random formulas of depth<=4: formulas whose all-Unknown value is not Unknown = **{bad}**  (Kleene/Priest: K3 has no tautologies; Open Logic Proposition thr.3).")
    p()
    p("With the extra spec operators (constants, COALESCE, Project, IsX) a tautology does exist, e.g. `IsKnown(A) OR IsUnknown(A)`:")
    t = [OR(IsKnown(a), IsUnknown(a)) for a in V]
    p(f"- values for A=F,U,T: {t}")
    p()


# ============================================================================= PART 4: analyzer rail formulas from Analyzer.cs
def part4():
    p("### E.7 Dual-rail formulas used by `Analyzer.cs` re-implemented independently and compared with the K3 semantics")
    p("Rails: D = 'is True', P = 'is True or Unknown'. Result value from rails: T if D, F if not P, else U.")
    D = lambda x: x == "T"
    P = lambda x: x != "F"

    def val(d, pp):
        assert (not d) or pp
        return "T" if d else ("U" if pp else "F")

    def check(name, n, rails_fn, direct_fn):
        bad = 0
        for a in assignments(n):
            d, pp = rails_fn(*a)
            if val(d, pp) != direct_fn(*a):
                bad += 1
        return (name, n, 3 ** n, "PASS" if bad == 0 else f"FAIL ({bad})")

    rows = []
    # COALESCE(x,y): D = Dx | (Px & Dy) ; P = Px & (Dx | Py)
    rows.append(check("COALESCE(x,y)", 2, lambda x, y: (D(x) or (P(x) and D(y)), P(x) and (D(x) or P(y))), lambda x, y: COAL(x, y)))
    # If: Or(Or(And(c,t), And(Not c, f)), And(t,f)) with rails
    def r_not(x):
        return (not P(x), not D(x))

    def rails(x):
        return (D(x), P(x))

    def r_and(a, b):
        return (a[0] and b[0], a[1] and b[1])

    def r_or(a, b):
        return (a[0] or b[0], a[1] or b[1])

    rows.append(check("If (mux+consensus)", 3, lambda c, t, f: r_or(r_or(r_and(rails(c), rails(t)), r_and(r_not(c), rails(f))), r_and(rails(t), rails(f))), IF_ext))
    rows.append(check("IsTrue: (D,D)", 1, lambda x: (D(x), D(x)), IsTrue))
    rows.append(check("IsFalse: (!P,!P)", 1, lambda x: (not P(x), not P(x)), IsFalse))
    rows.append(check("IsUnknown: (P&!D, P&!D)", 1, lambda x: (P(x) and not D(x), P(x) and not D(x)), IsUnknown))
    rows.append(check("IsKnown: (D|!P, D|!P)", 1, lambda x: (D(x) or not P(x), D(x) or not P(x)), IsKnown))
    rows.append(check("Project(x,True): (P,P)", 1, lambda x: (P(x), P(x)), lambda x: Proj(x, "T")))
    rows.append(check("Project(x,False): (D,D)", 1, lambda x: (D(x), D(x)), lambda x: Proj(x, "F")))
    # AtLeast on rails: D = AtLeast over D-bits ; P = AtLeast over P-bits
    for n, k in ((4, 2), (5, 3), (5, 1)):
        rows.append(check(f"AtLeast({k}) n={n}", n, lambda *x, k=k: (sum(D(v) for v in x) >= k, sum(P(v) for v in x) >= k), lambda *x, k=k: atleast(k, list(x))))
        rows.append(check(f"Exactly({k}) n={n} = AtLeast(k) AND NOT AtLeast(k+1)", n,
                          lambda *x, k=k: r_and((sum(D(v) for v in x) >= k, sum(P(v) for v in x) >= k), (not (sum(P(v) for v in x) >= k + 1), not (sum(D(v) for v in x) >= k + 1))),
                          lambda *x, k=k: exactly(k, list(x))))
    # BETWEEN(1,2) n=4 rail from Analyzer note: AtLeast(min) AND NOT AtLeast(max+1); ANY/ALL/NONE rails
    def al(x, k, which):
        return sum((D(v) if which == 0 else P(v)) for v in x) >= k

    rows.append(check("BETWEEN(1,2) n=4 = AtLeast(1) AND NOT AtLeast(3)", 4,
                      lambda *x: (al(x, 1, 0) and not al(x, 3, 1), al(x, 1, 1) and not al(x, 3, 0)), lambda *x: between(1, 2, list(x))))
    rows.append(check("NONE n=3 = NOT AtLeast(1)", 3, lambda *x: (not al(x, 1, 1), not al(x, 1, 0)), lambda *x: atmost(0, list(x))))
    rows.append(check("ALL n=3 = AtLeast(3)", 3, lambda *x: (al(x, 3, 0), al(x, 3, 1)), lambda *x: atleast(3, list(x))))
    # XOR rail & NXOR fold
    def r_xor(a, b):
        return r_or(r_and(a, (not b[1], not b[0])), r_and((not a[1], not a[0]), b))

    rows.append(check("XOR rails", 2, lambda a, b: r_xor(rails(a), rails(b)), XOR))
    rows.append(check("NXOR fold n=4", 4, lambda *x: __import__("functools").reduce(r_xor, [rails(v) for v in x]), lambda *x: nxor_any_u(list(x))))
    md_table(["rail formula", "arity", "valuations", "result"], rows)


# ============================================================================= PART 5: spec text claims with explicit examples
def part5():
    p("### E.8 Worked examples quoted in the specs, recomputed")
    rows = []

    def ex(src, text, got, want):
        rows.append((src, text, got, want, "PASS" if got == want else "FAIL"))

    ex("Logic:721-725", "AtLeast(2,T,T,F)", atleast(2, ["T", "T", "F"]), "T")
    ex("Logic:723", "AtLeast(2,T,U,F)", atleast(2, ["T", "U", "F"]), "U")
    ex("Logic:725", "AtLeast(2,F,F,U)", atleast(2, ["F", "F", "U"]), "F")
    ex("Logic:1505-1516", "AtLeast(2,[T,T,U,F])", atleast(2, list("TTUF")), "T")
    ex("Logic:1506", "AtLeast(3,[T,T,U,F])", atleast(3, list("TTUF")), "U")
    ex("Logic:1507", "AtLeast(4,[T,T,U,F])", atleast(4, list("TTUF")), "F")
    ex("Logic:1513", "AtMost(1,[T,T,U,F])", atmost(1, list("TTUF")), "F")
    ex("Logic:1514", "AtMost(2,[T,T,U,F])", atmost(2, list("TTUF")), "U")
    ex("Logic:1515", "AtMost(3,[T,T,U,F])", atmost(3, list("TTUF")), "T")
    ex("Logic:2471", "AtLeast(2,T,U,F)", atleast(2, ["T", "U", "F"]), "U")
    ex("Logic:2486", "XOR(T,T,U)", nxor_any_u(list("TTU")), "U")
    for ins, want, src in ((list("TFF"), "T", "Logic:540"), (list("TTF"), "F", "Logic:541"), (list("TTT"), "T", "Logic:542"), (list("TUF"), "U", "Logic:543"), (list("FUF"), "U", "Logic:544"), (list("FFF"), "F", "Logic:545"),
                           (list("FUU"), "U", "N-ary:52"), (list("TTU"), "U", "xor.md:65"), (list("FFU"), "U", "xor.md:66"), (list("UUF"), "U", "xor.md:67"), (list("UUU"), "U", "xor.md:68")):
        ex(src, f"XOR{tuple(ins)}", nxor_any_u(ins), want)
    ex("Logic:2519-2524", "If(U,T,T)", IF_spec("U", "T", "T"), "T")
    ex("Logic:2530-2535", "If(U,T,F)", IF_spec("U", "T", "F"), "U")
    ex("Logic:1161", "If(U,F,T)", IF_spec("U", "F", "T"), "U")
    ex("ProjectAndCollapse:33-36", "Project(U,F),Project(U,T),Project(T,F),Project(F,T)", "".join([Proj("U", "F"), Proj("U", "T"), Proj("T", "F"), Proj("F", "T")]), "FTTF")
    # inspection / projection tables exactly as printed (Logic.md 1197-1264, 1951-1955, 1975-1989; ProjectAndCollapse 18-22)
    printed_insp = {"IsTrue": {"T": "T", "U": "F", "F": "F"}, "IsFalse": {"T": "F", "U": "F", "F": "T"}, "IsUnknown": {"T": "F", "U": "T", "F": "F"}, "IsKnown": {"T": "T", "U": "F", "F": "T"}}
    for nm, fn in (("IsTrue", IsTrue), ("IsFalse", IsFalse), ("IsUnknown", IsUnknown), ("IsKnown", IsKnown)):
        ex("Logic:1197-1264,1951", f"{nm} table T/U/F", "".join(fn(v) for v in "TUF"), "".join(printed_insp[nm][v] for v in "TUF"))
    ex("Logic:1292-1296,1975-1981", "Unknown->False projection T/U/F", "".join(Proj(v, "F") for v in "TUF"), "TFF")
    ex("Logic:1312-1316,1983-1989", "Unknown->True projection T/U/F", "".join(Proj(v, "T") for v in "TUF"), "TTF")
    ex("ProjectAndCollapse:18-22", "Project(U->F) / Project(U->T) columns for T,F,U", "".join(Proj(v, "F") for v in "TFU") + "/" + "".join(Proj(v, "T") for v in "TFU"), "TFF/TFT")
    ex("Logic:1998 (§42)", "Unknown->Error: T,F pass; U error", "n/a", "n/a")
    ex("Logic:3-1 §4 count", "§4 says 'six primitive operations' but lists NOT,AND,OR,AT_LEAST,AT_MOST,EXACTLY,COALESCE", "7 listed", "6 claimed")
    rows[-1] = rows[-1][:4] + ("FAIL (editorial: 7 rows listed)",)
    rows[-2] = rows[-2][:4] + ("n/a",)
    md_table(["source", "example", "recomputed", "spec says", "result"], rows)

    p("### E.9 Cardinality Summary table (Logic.md section 19, lines 890-897) vs interval rule")
    n_max = 5
    rows = []
    # encode spec row conditions (True cond, False cond) exactly as printed
    spec_rows = {
        "AtLeast(n)": (lambda t, u, nn, k: t >= k, lambda t, u, nn, k: t + u < k, lambda xs, k: atleast(k, xs)),
        "AtMost(n)": (lambda t, u, nn, k: t + u <= k, lambda t, u, nn, k: t > k, lambda xs, k: atmost(k, xs)),
        "Exactly(n)": (lambda t, u, nn, k: t == k and u == 0, lambda t, u, nn, k: t > k or t + u < k, lambda xs, k: exactly(k, xs)),
        "Any()": (lambda t, u, nn, k: t >= 1, lambda t, u, nn, k: t + u == 0, lambda xs, k: atleast(1, xs)),
        "All()": (lambda t, u, nn, k: t == nn and u == 0, lambda t, u, nn, k: t < nn, lambda xs, k: atleast(len(xs), xs)),
        "None()": (lambda t, u, nn, k: t == 0 and u == 0, lambda t, u, nn, k: t > 0, lambda xs, k: atmost(0, xs)),
    }
    for name, (ct, cf, ref) in spec_rows.items():
        bad = 0
        total = 0
        first = None
        for n in range(1, n_max + 1):
            for xs in assignments(n):
                for k in range(0, n + 1):
                    t, u = counts(list(xs))
                    spec_v = "T" if ct(t, u, n, k) else ("F" if cf(t, u, n, k) else "U")
                    total += 1
                    if spec_v != ref(list(xs), k):
                        bad += 1
                        if first is None:
                            first = (xs, k, spec_v, ref(list(xs), k))
        rows.append((name, total, bad, "PASS" if not bad else f"FAIL e.g. operands={''.join(first[0])}{',k='+str(first[1]) if name in ('AtLeast(n)','AtMost(n)','Exactly(n)') else ''}: table says {first[2]}, interval rule gives {first[3]}"))
    md_table(["row", "cases", "mismatches", "result"], rows)


def main():
    p("# k3audit output")
    p()
    part1()
    part2()
    part3()
    part4()
    part5()
    with open(sys.argv[1] if len(sys.argv) > 1 else "k3audit_out.md", "w", encoding="utf-8") as fh:
        fh.write("\n".join(OUT))


if __name__ == "__main__":
    main()
```

</details>

The script's output follows (it was run against the repo state described above).

### E.1 Spec truth tables parsed from the markdown and recomputed
| location | table | cells | result vs standard K3 (Kleene strong; Priest 2008 sec 7.3) |
| --- | --- | --- | --- |
| Logic:178 | A ∧ B (9-row list) | 9 | PASS |
| Logic:245 | A ∨ B (9-row list) | 9 | PASS |
| Logic:332 | A → B (9-row list) | 9 | PASS |
| Logic:404 | A ↔ B (9-row list) | 9 | PASS |
| Logic:454 | A ⊕ B (9-row list) | 9 | PASS |
| Logic:925 | A ?? B (9-row list) | 9 | PASS |
| Logic:1872 | ∧ (3x3 matrix) | 9 | PASS |
| Logic:1882 | ∨ (3x3 matrix) | 9 | PASS |
| Logic:1892 | ⊕ (3x3 matrix) | 9 | PASS |
| Logic:1902 | → (3x3 matrix) | 9 | PASS |
| Logic:1912 | ↔ (3x3 matrix) | 9 | PASS |
| Logic:1933 | ↑ (3x3 matrix) | 9 | PASS |
| Logic:1941 | ↓ (3x3 matrix) | 9 | PASS |
| General:121 | A ∧ B (9-row list) | 9 | PASS |
| General:153 | A ∨ B (9-row list) | 9 | PASS |
| xor:17 | A ⊕ B (9-row list) | 9 | PASS |

Note: `Logic:332` (IMPLIES) and `Logic:404` (EQUIVALENT) use ->/<-> as Kleene-strong; Lukasiewicz would differ only at U,U.

### E.2 Identity / table / rewrite claims, brute-forced over all 3^n valuations
`valid` = LHS equals RHS on every valuation. `PASS` = brute force agrees with what the spec asserts (incl. spec assertions that something is NOT valid). `FAIL` = spec claim is wrong.

| id | source (file:line) | claim | brute force | spec asserts | result |
| --- | --- | --- | --- | --- | --- |
| L01 | Logic:2052,General:104,Catalog:040 | NOT NOT A = A | valid | spec says valid | PASS |
| L02 | General:188 | A AND B = B AND A | valid | spec says valid | PASS |
| L03 | General:192 | A OR B = B OR A | valid | spec says valid | PASS |
| L04 | General:198 | (A AND B) AND C = A AND (B AND C) | valid | spec says valid | PASS |
| L05 | General:202 | (A OR B) OR C = A OR (B OR C) | valid | spec says valid | PASS |
| L06 | General:208 | A AND A = A | valid | spec says valid | PASS |
| L07 | General:211 | A OR A = A | valid | spec says valid | PASS |
| L08 | Logic:2036 | A AND (B OR C) = (A AND B) OR (A AND C) | valid | spec says valid | PASS |
| L09 | Logic:2044 | A OR (B AND C) = (A OR B) AND (A OR C) | valid | spec says valid | PASS |
| L10 | General:232 | A AND (A OR B) = A (absorption) | valid | spec says valid | PASS |
| L11 | General:236 | A OR (A AND B) = A (absorption) | valid | spec says valid | PASS |
| L12 | General:248 | A AND T = A | valid | spec says valid | PASS |
| L13 | General:252 | A OR F = A | valid | spec says valid | PASS |
| L14 | General:258 | A AND F = F | valid | spec says valid | PASS |
| L15 | General:262 | A OR T = T | valid | spec says valid | PASS |
| L16 | General:322 | NOT(A AND B) = NOT A OR NOT B | valid | spec says valid | PASS |
| L17 | General:328 | NOT(A OR B) = NOT A AND NOT B | valid | spec says valid | PASS |
| C01 | Logic:2078,General:278 | A OR NOT A = T  (excluded middle) | INVALID | spec says NOT valid | PASS |
| C02 | Logic:2100,General:298 | A AND NOT A = F  (non-contradiction) | INVALID | spec says NOT valid | PASS |
| C03 | Catalog:110-113,Simplif:412-426 | T OR NOT T = T; F OR NOT F = T; T AND NOT T = F; F AND NOT F = F (definite constants only) | valid | spec says valid | PASS |
| S01 | Simplif:487 | A AND (B OR F) = A AND B | valid | spec says valid | PASS |
| S02 | Simplif:499 | A OR (B AND F) = A | valid | spec says valid | PASS |
| S03 | Simplif:595-617 | (A AND T) OR (A AND F) = A | valid | spec says valid | PASS |
| S04 | Simplif:683 | NAND(A,B) = NOT A OR NOT B | valid | spec says valid | PASS |
| S05 | Simplif:689 | NOR(A,B) = NOT A AND NOT B | valid | spec says valid | PASS |
| K01 | Simplif:432-454 (says VALID), algebraic laws:157-167 (says VALID) | (A&B)\|(~A&C)\|(B&C) = (A&B)\|(~A&C)  [consensus removal] | INVALID | spec says valid | FAIL |
| K02 | Catalog:590-647 (says FORBIDDEN) | same identity; Catalog says NOT valid, counterexample A=U,B=T,C=T | INVALID | spec says NOT valid | PASS |
| K03 | (not in spec) link between consensus and If | consensus form (A&B)\|(~A&C)\|(B&C) = strongest extension of ite(A,B,C) | valid | spec says valid | PASS |
| K04 | (not in spec) refinement | reduced form [= full form in the information order (reduced definite => equal) | valid | spec says valid | PASS |
| N5 | Logic:14 'ordering is useful for ... cardinality bounds' | AtLeast(k, xs) = k-th largest of xs under F&lt;U&lt;T (order statistic), n&lt;=5 | valid | spec says valid | PASS |
| B01 | OLP/Bochvar external negation ~ (T->F,U->T,F->T) | Bochvar ~x = NOT IsTrue(x)  (i.e. SQL IS NOT TRUE) | valid | spec says valid | PASS |
| B02 | OLP/Bochvar '+' (is undefined) (T->F,U->T,F->F) | Bochvar + x = IsUnknown(x) | valid | spec says valid | PASS |
| J_P4 | ProjectAndCollapse:57-65 vs :83-88 | 'falsity collapse' C_F (F->F; T,U->T) = IsFalse(x) (the prose says IsFalse is the collapse) | INVALID | spec says valid | FAIL |
| J_P5 | ProjectAndCollapse:92 | 'Projection resolves U; collapse rejects U': Project(U->F) and C_T are different functions | valid | spec says NOT valid | FAIL |
| I16 | Logic:1767 (EQUIVALENT left-assoc) / ADR no-chaining | (A&lt;->B)&lt;->C = A&lt;->(B&lt;->C) | valid | spec says valid | PASS |
| I17 | Logic:1766 (IMPLIES right-assoc) | (A->B)->C = A->(B->C) | INVALID | spec says NOT valid | PASS |
| I18 | (not in spec) exportation | (A AND B)->C = A->(B->C) | valid | spec says valid | PASS |
| U01 | TODO:301 | NOT A = A NAND A | valid | spec says valid | PASS |
| U02 | TODO:302 | A AND B = (A NAND B) NAND (A NAND B) | valid | spec says valid | PASS |
| U03 | TODO:303 | A OR B = (A NAND A) NAND (B NAND B) | valid | spec says valid | PASS |
| U04 | ADR-0005 d10 (dual) | NOT A = A NOR A | valid | spec says valid | PASS |
| U05 | ADR-0005 d10 (dual) | A OR B = (A NOR B) NOR (A NOR B) | valid | spec says valid | PASS |
| U06 | ADR-0005 d10 (dual) | A AND B = (A NOR A) NOR (B NOR B) | valid | spec says valid | PASS |
| I01 | Logic:306 (A->B = NOT A OR B) | table vs Kleene strong conditional (Priest 7.3.2: i->i = i) | valid | spec says valid | PASS |
| I02 | Logic:372-385 | (A->B)&(B->A) [Kleene/Priest def] = OR(AND(A,B),AND(NOT A,NOT B)) [spec primitive form] | valid | spec says valid | PASS |
| I03 | Logic:437 vs xor.md:12 | OR(AND(A,~B),AND(~A,B)) = (A OR B) AND NOT(A AND B) | valid | spec says valid | PASS |
| I04 | ADR-0005 d5 (EQUIVALENT replaces XNOR) | A&lt;->B = NOT(A XOR B) | valid | spec says valid | PASS |
| I05 | Lukasiewicz contrast | Kleene A->B = Lukasiewicz A->B | INVALID | spec says NOT valid | PASS |
| I06 | Lukasiewicz contrast | Kleene A&lt;->B = Lukasiewicz A&lt;->B | INVALID | spec says NOT valid | PASS |
| I07 | Priest 7.3.8 / Logic (implicit) | A->A = T (identity law) | INVALID | spec says NOT valid | PASS |
| I08 | Logic:2078 analogues | A&lt;->A = T | INVALID | spec says NOT valid | PASS |
| I09 | algebraic laws:203-217 | A XOR A = F | INVALID | spec says NOT valid | PASS |
| I10 | algebraic laws:203-217 | A XOR NOT A = T | INVALID | spec says NOT valid | PASS |
| I11 | XOR ring law (not in spec) | A AND (B XOR C) = (A AND B) XOR (A AND C) | INVALID | spec says NOT valid | PASS |
| I12 | XOR assoc (n-ary XOR relies on it) | (A XOR B) XOR C = A XOR (B XOR C) | valid | spec says valid | PASS |
| I13 | XOR identity | A XOR F = A | valid | spec says valid | PASS |
| I14 | XOR identity | A XOR T = NOT A | valid | spec says valid | PASS |
| I15 | Logic:2052 spirit | contraposition A->B = ~B->~A | valid | spec says valid | PASS |
| X03a | xor.md:75, Logic:480-534, N-ary:61-74 | NXOR n=3: interval-parity rule = 'Unknown if any Unknown else odd-count' | valid | spec says valid | PASS |
| X03b | xor.md:75 | NXOR n=3: left fold of binary XOR = 'Unknown if any Unknown' | valid | spec says valid | PASS |
| X03c | N-ary:116-122,Logic:552 | NXOR n=3: OR of Exactly(k) over odd k | valid | spec says valid | PASS |
| X04a | xor.md:75, Logic:480-534, N-ary:61-74 | NXOR n=4: interval-parity rule = 'Unknown if any Unknown else odd-count' | valid | spec says valid | PASS |
| X04b | xor.md:75 | NXOR n=4: left fold of binary XOR = 'Unknown if any Unknown' | valid | spec says valid | PASS |
| X04c | N-ary:116-122,Logic:552 | NXOR n=4: OR of Exactly(k) over odd k | valid | spec says valid | PASS |
| X05a | xor.md:75, Logic:480-534, N-ary:61-74 | NXOR n=5: interval-parity rule = 'Unknown if any Unknown else odd-count' | valid | spec says valid | PASS |
| X05b | xor.md:75 | NXOR n=5: left fold of binary XOR = 'Unknown if any Unknown' | valid | spec says valid | PASS |
| X05c | N-ary:116-122,Logic:552 | NXOR n=5: OR of Exactly(k) over odd k | valid | spec says valid | PASS |
| X10 | Logic:493-499,N-ary:26-33 | 3-input DNF (A~B~C \| ~AB~C \| ~A~BC \| ABC) = parity | valid | spec says valid | PASS |
| X11 | N-ary:113 | XOR(A,B) = Exactly(1,A,B) | valid | spec says valid | PASS |
| X12 | xor.md:149, ADR-0005 d4 | ExactlyOne(A,B,C) = NXOR(A,B,C)  (spec/ADR say they DIFFER for n>=3) | INVALID | spec says NOT valid | PASS |
| X13 | xor.md:149 | T,T,T: parity = T, ExactlyOne = F | valid | spec says valid | PASS |
| Q01 | Logic:966 | A ?? B = A OR B | INVALID | spec says NOT valid | PASS |
| Q02 | Logic:972 | A ?? B = NOT A OR B | INVALID | spec says NOT valid | PASS |
| Q03 | Logic:998-1010 | NOT(A ?? B) = NOT(A) ?? B  (spec: 'generally different') | INVALID | spec says NOT valid | PASS |
| Q04 | (not in spec) true identity | NOT(A ?? B) = NOT(A) ?? NOT(B) | valid | spec says valid | PASS |
| Q05 | Logic:993 | If(IsUnknown(A), B, A) is offered as the 'inverse' of A ?? B; it IS A ?? B | valid | spec says valid | PASS |
| Q06 | ADR-0005 d15 (assoc) | (A ?? B) ?? C = A ?? (B ?? C) | valid | spec says valid | PASS |
| Q07 | Logic:1011 'Coalesce replaces Unknown but does not otherwise alter K3 values' | A ?? A = A | valid | spec says valid | PASS |
| F01 | ADR-0005 d13 / Logic:1147-1172 | If(c,t,f) spec-text = strongest extension of Boolean ite | valid | spec says valid | PASS |
| F02 | issues-log 17 (bare mux rejected) | bare multiplexer (c&t)\|(~c&f) = strongest extension of ite | INVALID | spec says NOT valid | PASS |
| F03 | ADR-0005 d13 | mux + consensus (c&t)\|(~c&f)\|(t&f) = strongest extension of ite | valid | spec says valid | PASS |
| F04 | McCarthy 1963 p.8 | McCarthy conditional (strict in undefined condition) = strongest extension | INVALID | spec says NOT valid | PASS |
| F05 | SQL CASE / PostgreSQL docs | SQL CASE WHEN c THEN t ELSE f (Unknown cond -> ELSE) = strongest extension | INVALID | spec says NOT valid | PASS |
| F06 | Logic:1157 | If(U,A,A) = A | valid | spec says valid | PASS |
| F07 | ADR-0005 d13 / Simplifier RewriteIf | If(c,A,A) = A for every c | valid | spec says valid | PASS |
| F08 | Simplifier RewriteIf | If(T,a,b)=a and If(F,a,b)=b  (consensus never changes a definite condition) | valid | spec says valid | PASS |
| N01a | Logic:812-820 | ANY=OR (n=1): AtLeast(1) = OR | valid | spec says valid | PASS |
| N01b | Logic:812-820 | ALL=AND (n=1): AtLeast(n) = AND | valid | spec says valid | PASS |
| N01c | Logic:812-820 | NONE (n=1): AtMost(0) = NOT OR | valid | spec says valid | PASS |
| N02a | Logic:812-820 | ANY=OR (n=2): AtLeast(1) = OR | valid | spec says valid | PASS |
| N02b | Logic:812-820 | ALL=AND (n=2): AtLeast(n) = AND | valid | spec says valid | PASS |
| N02c | Logic:812-820 | NONE (n=2): AtMost(0) = NOT OR | valid | spec says valid | PASS |
| N03a | Logic:812-820 | ANY=OR (n=3): AtLeast(1) = OR | valid | spec says valid | PASS |
| N03b | Logic:812-820 | ALL=AND (n=3): AtLeast(n) = AND | valid | spec says valid | PASS |
| N03c | Logic:812-820 | NONE (n=3): AtMost(0) = NOT OR | valid | spec says valid | PASS |
| N04a | Logic:812-820 | ANY=OR (n=4): AtLeast(1) = OR | valid | spec says valid | PASS |
| N04b | Logic:812-820 | ALL=AND (n=4): AtLeast(n) = AND | valid | spec says valid | PASS |
| N04c | Logic:812-820 | NONE (n=4): AtMost(0) = NOT OR | valid | spec says valid | PASS |
| N05a | Logic:812-820 | ANY=OR (n=5): AtLeast(1) = OR | valid | spec says valid | PASS |
| N05b | Logic:812-820 | ALL=AND (n=5): AtLeast(n) = AND | valid | spec says valid | PASS |
| N05c | Logic:812-820 | NONE (n=5): AtMost(0) = NOT OR | valid | spec says valid | PASS |
| N10 | Logic:869 | AtLeastFalse(2,A,B,C) = AtLeast(2,~A,~B,~C)  (interval is symmetric) | valid | spec says valid | PASS |
| N11 | Logic:2317 BETWEEN normalisation | BETWEEN(2,4,A..E)=AND(AtLeast(2),AtMost(4)) vs strongest extension | valid | spec says valid | PASS |
| N12 | edge: min>max (compiler rejects it) | BETWEEN(3,2,A,B,C,D) = AND(AtLeast(3),AtMost(2)) vs strongest extension (empty range) | INVALID | spec says valid | FAIL |
| N2_AtLeast (x35) | Logic:656-669,894 | AtLeast(0) interval rule == strongest extension, n=0..6 | valid | spec says valid | PASS |
| N2_AtMost (x35) | Logic:656-669,894 | AtMost(0) interval rule == strongest extension, n=0..6 | valid | spec says valid | PASS |
| N2_Exactly (x35) | Logic:656-669,894 | Exactly(0) interval rule == strongest extension, n=0..6 | valid | spec says valid | PASS |
| N3_dnf_atleast (x15) | UniversalGateExpander / ADR d10 | AtLeast(1) = OR of AND over k-subsets (monotone K3 formula), n=1..5 | valid | spec says valid | PASS |
| N3_dnf_exactly (x20) | (not claimed) DNF of Exactly | Exactly(0) = OR_S AND(S, NOT rest) compositional K3, n=1..5 | valid | spec says valid | PASS |
| N3_exact_via (x5) | UniversalGateExpander | Exactly(1) = AtLeast(1) AND AtMost(1), n=1..5 | valid | spec says valid | PASS |
| N4_AtLeast(2)_U_as_False | naive alternative | AtLeast(2) with Unknown counted as False  == interval rule (n=4) | INVALID | spec says NOT valid | PASS |
| N4_AtLeast(2)_U_as_True | naive alternative | AtLeast(2) with Unknown counted as True  == interval rule (n=4) | INVALID | spec says NOT valid | PASS |
| N4_AtMost(1)_U_as_False | naive alternative | AtMost(1) with Unknown counted as False  == interval rule (n=4) | INVALID | spec says NOT valid | PASS |
| N4_AtMost(1)_U_as_True | naive alternative | AtMost(1) with Unknown counted as True  == interval rule (n=4) | INVALID | spec says NOT valid | PASS |
| N4_Exactly(1)_U_as_False | naive alternative | Exactly(1) with Unknown counted as False  == interval rule (n=4) | INVALID | spec says NOT valid | PASS |
| N4_Exactly(1)_U_as_True | naive alternative | Exactly(1) with Unknown counted as True  == interval rule (n=4) | INVALID | spec says NOT valid | PASS |
| J_IsUnknown | PrimitiveExpander.ExpandInspection | IsUnknown(x) via COALESCE | valid | spec says valid | PASS |
| J_IsTrue | PrimitiveExpander.ExpandInspection | IsTrue(x) via COALESCE | valid | spec says valid | PASS |
| J_IsFalse | PrimitiveExpander.ExpandInspection | IsFalse(x) via COALESCE | valid | spec says valid | PASS |
| J_IsKnown | PrimitiveExpander.ExpandInspection | IsKnown(x) via COALESCE | valid | spec says valid | PASS |
| J_P1 | ProjectAndCollapse:18-22 | Project(x,U->F) = 'truth collapse' C_T = IsTrue(x) | valid | spec says valid | PASS |
| J_P2 | ProjectAndCollapse:49-65 | Project(x,U->T) = 'falsity collapse' C_F = NOT IsFalse(x) | valid | spec says valid | PASS |
| J_P3 | ADR-0005 d12 | Project(x,v) = COALESCE(x,v) | valid | spec says valid | PASS |
| W01 | Simplifier.RewriteNegatedPair | XOR(NOT l, r) = EQUIVALENT(l, r) | valid | spec says valid | PASS |
| W02 | Simplifier.RewriteNegatedPair | XOR(NOT l, NOT r) = XOR(l, r) | valid | spec says valid | PASS |
| W03 | Simplifier | IMPLIES(NOT a, b) = OR(a, b) | valid | spec says valid | PASS |
| W04 | Simplifier | NAND(NOT l, NOT r) = OR(l, r) | valid | spec says valid | PASS |
| W05 | Simplifier | NOT IsKnown(x) = IsUnknown(x) | valid | spec says valid | PASS |
| W06 | Simplifier | IsTrue(NOT x) = IsFalse(x); IsUnknown(NOT x)=IsUnknown(x) | valid | spec says valid | PASS |
| W07 | Simplifier.RewriteThreshold | AtLeast(k, T, rest...) = AtLeast(k-1, rest...)  (n=3 rest, k=2) | valid | spec says valid | PASS |
| W08 | Simplifier.RewriteThreshold | Exactly(k, F, rest...) = Exactly(k, rest...) | valid | spec says valid | PASS |
| W09 | Simplifier.RewriteThreshold | all-Unknown operands, valid threshold -> Unknown (AtLeast(2,U,U,U)) | valid | spec says valid | PASS |
| W10 | Simplifier NOT threshold flip | NOT AtLeast(k) = AtMost(k-1) (n=4,k=2) | valid | spec says valid | PASS |
| W11 | Simplifier absorption | a AND (a OR b) = a, a OR (a AND b) = a (both forms) | valid | spec says valid | PASS |
| W12 | Compressor | OR(NOT a, NOT b) = NAND(a,b); AND(NOT a, NOT b) = NOR(a,b) | valid | spec says valid | PASS |
| P01 | Logic:1730-1737 (grammar: XOR binds tighter than AND) vs Logic:1760-1765 (table: AND tighter than XOR) | A AND B XOR C : (A AND B) XOR C  vs  A AND (B XOR C) | INVALID | spec says valid | FAIL |
| T19_All_2 | Logic:896 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=2 | INVALID | spec says valid | FAIL |
| T19_All_3 | Logic:896 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=3 | INVALID | spec says valid | FAIL |
| T19_All_4 | Logic:896 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=4 | INVALID | spec says valid | FAIL |

#### E.2a Counterexamples for every FAIL row
| id | claim | valuation | LHS | RHS |
| --- | --- | --- | --- | --- |
| K01 | (A&B)\|(~A&C)\|(B&C) = (A&B)\|(~A&C)  [consensus removal] | UTT | T | U |
| J_P4 | 'falsity collapse' C_F (F->F; T,U->T) = IsFalse(x) (the prose says IsFalse is the collapse) | F | F | T |
| J_P4 | 'falsity collapse' C_F (F->F; T,U->T) = IsFalse(x) (the prose says IsFalse is the collapse) | U | T | F |
| J_P4 | 'falsity collapse' C_F (F->F; T,U->T) = IsFalse(x) (the prose says IsFalse is the collapse) | T | T | F |
| N12 | BETWEEN(3,2,A,B,C,D) = AND(AtLeast(3),AtMost(2)) vs strongest extension (empty range) | FUUU | U | F |
| N12 | BETWEEN(3,2,A,B,C,D) = AND(AtLeast(3),AtMost(2)) vs strongest extension (empty range) | FUUT | U | F |
| N12 | BETWEEN(3,2,A,B,C,D) = AND(AtLeast(3),AtMost(2)) vs strongest extension (empty range) | FUTU | U | F |
| N12 | BETWEEN(3,2,A,B,C,D) = AND(AtLeast(3),AtMost(2)) vs strongest extension (empty range) | FUTT | U | F |
| P01 | A AND B XOR C : (A AND B) XOR C  vs  A AND (B XOR C) | FFU | U | F |
| P01 | A AND B XOR C : (A AND B) XOR C  vs  A AND (B XOR C) | FFT | T | F |
| P01 | A AND B XOR C : (A AND B) XOR C  vs  A AND (B XOR C) | FUU | U | F |
| P01 | A AND B XOR C : (A AND B) XOR C  vs  A AND (B XOR C) | FUT | T | F |
| T19_All_2 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=2 | UU | F | U |
| T19_All_2 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=2 | UT | F | U |
| T19_All_2 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=2 | TU | F | U |
| T19_All_3 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=3 | UUU | F | U |
| T19_All_3 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=3 | UUT | F | U |
| T19_All_3 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=3 | UTU | F | U |
| T19_All_3 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=3 | UTT | F | U |
| T19_All_4 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=4 | UUUU | F | U |
| T19_All_4 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=4 | UUUT | F | U |
| T19_All_4 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=4 | UUTU | F | U |
| T19_All_4 | All() row of the Cardinality Summary (True: T=n,U=0; False: T&lt;n; else U), n=4 | UUTT | F | U |

#### E.2b Counterexamples that demonstrate the 'not valid in K3' claims the specs make
| id | claim | first counterexample | LHS | RHS | #failing valuations |
| --- | --- | --- | --- | --- | --- |
| C01 | A OR NOT A = T  (excluded middle) | U | U | T | 1 |
| C02 | A AND NOT A = F  (non-contradiction) | U | U | F | 1 |
| I17 | (A->B)->C = A->(B->C) | FFF | F | T | 9 |
| I05 | Kleene A->B = Lukasiewicz A->B | UU | U | T | 1 |
| I06 | Kleene A&lt;->B = Lukasiewicz A&lt;->B | UU | U | T | 1 |
| I07 | A->A = T (identity law) | U | U | T | 1 |
| I08 | A&lt;->A = T | U | U | T | 1 |
| I09 | A XOR A = F | U | U | F | 1 |
| I10 | A XOR NOT A = T | U | U | T | 1 |
| I11 | A AND (B XOR C) = (A AND B) XOR (A AND C) | UTT | F | U | 1 |
| X12 | ExactlyOne(A,B,C) = NXOR(A,B,C)  (spec/ADR say they DIFFER for n>=3) | UTT | F | U | 4 |
| Q01 | A ?? B = A OR B | FU | F | U | 3 |
| Q02 | A ?? B = NOT A OR B | FF | F | T | 6 |
| Q03 | NOT(A ?? B) = NOT(A) ?? B  (spec: 'generally different') | UF | T | F | 2 |
| F02 | bare multiplexer (c&t)\|(~c&f) = strongest extension of ite | UTT | U | T | 1 |
| F04 | McCarthy conditional (strict in undefined condition) = strongest extension | UFF | U | F | 2 |
| F05 | SQL CASE WHEN c THEN t ELSE f (Unknown cond -> ELSE) = strongest extension | UFT | T | U | 4 |

### E.3 Truth-function contrast: Kleene strong vs Lukasiewicz L3 vs weak Kleene/Bochvar vs McCarthy
| A | B | AND strong | AND weak/Bochvar | AND McCarthy | OR strong | OR weak | OR McCarthy | A->B Kleene | A->B Luk. | A<->B Kleene | A<->B Luk. |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| F | F | F | F | F | F | F | F | T | T | T | T |
| F | U | F | U | F | U | U | U | T | T | U | U |
| F | T | F | F | F | T | T | T | T | T | F | F |
| U | F | F | U | U | U | U | U | U | U | U | U |
| U | U | U | U | U | U | U | U | U | T | U | T |
| U | T | U | U | U | T | U | U | T | T | U | U |
| T | F | F | F | F | T | T | T | F | F | F | F |
| T | U | U | U | U | T | U | T | U | U | U | U |
| T | T | T | T | T | T | T | T | T | T | T | T |

### E.4 Information-order monotonicity (Kleene 'regular' property) and 'equals strongest extension of its Boolean restriction'
`monotone` = replacing an Unknown input by T or F can never change a definite output (x [= y => f(x) [= f(y), where U [= T and U [= F).
`= strongest ext.` = f equals the function that is definite exactly when every classical completion agrees.

| operator | arity | info-monotone | = strongest extension of its Boolean restriction |
| --- | --- | --- | --- |
| NOT | 1 | yes | yes |
| AND | 2 | yes | yes |
| OR | 2 | yes | yes |
| IMPLIES (Kleene) | 2 | yes | yes |
| EQUIVALENT (Kleene) | 2 | yes | yes |
| XOR (binary) | 2 | yes | yes |
| NAND | 2 | yes | yes |
| NOR | 2 | yes | yes |
| NXOR n=3 (Unknown if any Unknown) | 3 | yes | yes |
| AtLeast(2) n=4 | 4 | yes | yes |
| AtMost(1) n=4 | 4 | yes | yes |
| Exactly(2) n=4 | 4 | yes | yes |
| ExactlyOne n=4 | 4 | yes | yes |
| ANY n=3 | 3 | yes | yes |
| ALL n=3 | 3 | yes | yes |
| NONE n=3 | 3 | yes | yes |
| BETWEEN(1,2) n=4 | 4 | yes | yes |
| If = mux + consensus | 3 | yes | yes |
| If (spec text, sec 25) | 3 | yes | yes |
| If = bare multiplexer | 3 | yes | NO (1 of 27 valuations differ) |
| If = McCarthy (strict in cond.) | 3 | yes | NO (2 of 27 valuations differ) |
| If = SQL CASE | 3 | NO | NO (4 of 27 valuations differ) |
| Lukasiewicz A->B | 2 | NO | NO (1 of 9 valuations differ) |
| Lukasiewicz A&lt;->B | 2 | NO | NO (1 of 9 valuations differ) |
| Weak Kleene AND | 2 | yes | NO (2 of 9 valuations differ) |
| McCarthy AND | 2 | yes | NO (1 of 9 valuations differ) |
| COALESCE(a,b) | 2 | NO | NO (2 of 9 valuations differ) |
| Project(x,F) | 1 | NO | NO (1 of 3 valuations differ) |
| Project(x,T) | 1 | NO | NO (1 of 3 valuations differ) |
| IsTrue | 1 | NO | NO (1 of 3 valuations differ) |
| IsFalse | 1 | NO | NO (1 of 3 valuations differ) |
| IsUnknown | 1 | NO | NO (1 of 3 valuations differ) |
| IsKnown | 1 | NO | NO (1 of 3 valuations differ) |

### E.5 Where K3's truth-functional (compositional) evaluation is weaker than the strongest extension (repeated operands)
| formula | K3 value vs value forced by every completion |
| --- | --- |
| A OR NOT A | x=U: K3=U, every completion=T |
| A AND NOT A | x=U: K3=U, every completion=F |
| A -> A | x=U: K3=U, every completion=T |
| A &lt;-> A | x=U: K3=U, every completion=T |
| A XOR A | x=U: K3=U, every completion=F |
| A XOR NOT A | x=U: K3=U, every completion=T |
| (C AND T) OR (NOT C AND F)  [mux, t=T f=F] | agrees |
| (C AND T) OR (NOT C AND T)  [mux, t=f=T] | x=U: K3=U, every completion=T |

### E.5b Clone generated by NAND alone on one variable (General Spec section 20: NAND/NOR 'should not be assumed functionally complete')
- unary functions of x reachable with NAND only (no constants): **4 of 27**: FUF; FUT; TUF; TUT (value tables over x=F,U,T; e.g. FUT=identity, TUF=NOT)
- The four are x (FUT), NOT x (TUF), x OR NOT x (TUT) and x AND NOT x (FUF). IsTrue (FFT), IsUnknown (FTF) and every constant (FFF, UUU, TTT) are NOT in the set.

### E.6 Priest-style consequence facts used to describe K3 (designated value {T}); valuations are over variables p,q
| inference | valid in K3 | countermodel |
| --- | --- | --- |
| p, p->q \|= q (modus ponens) | valid | - |
| p OR q, NOT p \|= q (disjunctive syllogism) | valid | - |
| p AND NOT p \|= q (explosion) | valid | - |
| p AND q \|= p | valid | - |
| p \|= p OR q | valid | - |
| \|= p OR NOT p (excluded middle) | INVALID | p=U, q=F |
| \|= p -> p (identity) | INVALID | p=U, q=F |
| \|= p &lt;-> p | INVALID | p=U, q=F |
| p -> q \|= NOT q -> NOT p (Priest 7.3.5) | valid | - |

Pure-connective formulas (variables and NOT/AND/OR/IMPLIES/EQUIVALENT/XOR/NAND/NOR/AtLeast/AtMost/Exactly/If/NXOR; NO constants, NO Coalesce/Is*/Project): all-Unknown valuation yields Unknown?
- 20000 random formulas of depth<=4: formulas whose all-Unknown value is not Unknown = **0**  (Kleene/Priest: K3 has no tautologies; Open Logic Proposition thr.3).

With the extra spec operators (constants, COALESCE, Project, IsX) a tautology does exist, e.g. `IsKnown(A) OR IsUnknown(A)`:
- values for A=F,U,T: ['T', 'T', 'T']

### E.7 Dual-rail formulas used by `Analyzer.cs` re-implemented independently and compared with the K3 semantics
Rails: D = 'is True', P = 'is True or Unknown'. Result value from rails: T if D, F if not P, else U.
| rail formula | arity | valuations | result |
| --- | --- | --- | --- |
| COALESCE(x,y) | 2 | 9 | PASS |
| If (mux+consensus) | 3 | 27 | PASS |
| IsTrue: (D,D) | 1 | 3 | PASS |
| IsFalse: (!P,!P) | 1 | 3 | PASS |
| IsUnknown: (P&!D, P&!D) | 1 | 3 | PASS |
| IsKnown: (D\|!P, D\|!P) | 1 | 3 | PASS |
| Project(x,True): (P,P) | 1 | 3 | PASS |
| Project(x,False): (D,D) | 1 | 3 | PASS |
| AtLeast(2) n=4 | 4 | 81 | PASS |
| Exactly(2) n=4 = AtLeast(k) AND NOT AtLeast(k+1) | 4 | 81 | PASS |
| AtLeast(3) n=5 | 5 | 243 | PASS |
| Exactly(3) n=5 = AtLeast(k) AND NOT AtLeast(k+1) | 5 | 243 | PASS |
| AtLeast(1) n=5 | 5 | 243 | PASS |
| Exactly(1) n=5 = AtLeast(k) AND NOT AtLeast(k+1) | 5 | 243 | PASS |
| BETWEEN(1,2) n=4 = AtLeast(1) AND NOT AtLeast(3) | 4 | 81 | PASS |
| NONE n=3 = NOT AtLeast(1) | 3 | 27 | PASS |
| ALL n=3 = AtLeast(3) | 3 | 27 | PASS |
| XOR rails | 2 | 9 | PASS |
| NXOR fold n=4 | 4 | 81 | PASS |

### E.8 Worked examples quoted in the specs, recomputed
| source | example | recomputed | spec says | result |
| --- | --- | --- | --- | --- |
| Logic:721-725 | AtLeast(2,T,T,F) | T | T | PASS |
| Logic:723 | AtLeast(2,T,U,F) | U | U | PASS |
| Logic:725 | AtLeast(2,F,F,U) | F | F | PASS |
| Logic:1505-1516 | AtLeast(2,[T,T,U,F]) | T | T | PASS |
| Logic:1506 | AtLeast(3,[T,T,U,F]) | U | U | PASS |
| Logic:1507 | AtLeast(4,[T,T,U,F]) | F | F | PASS |
| Logic:1513 | AtMost(1,[T,T,U,F]) | F | F | PASS |
| Logic:1514 | AtMost(2,[T,T,U,F]) | U | U | PASS |
| Logic:1515 | AtMost(3,[T,T,U,F]) | T | T | PASS |
| Logic:2471 | AtLeast(2,T,U,F) | U | U | PASS |
| Logic:2486 | XOR(T,T,U) | U | U | PASS |
| Logic:540 | XOR('T', 'F', 'F') | T | T | PASS |
| Logic:541 | XOR('T', 'T', 'F') | F | F | PASS |
| Logic:542 | XOR('T', 'T', 'T') | T | T | PASS |
| Logic:543 | XOR('T', 'U', 'F') | U | U | PASS |
| Logic:544 | XOR('F', 'U', 'F') | U | U | PASS |
| Logic:545 | XOR('F', 'F', 'F') | F | F | PASS |
| N-ary:52 | XOR('F', 'U', 'U') | U | U | PASS |
| xor.md:65 | XOR('T', 'T', 'U') | U | U | PASS |
| xor.md:66 | XOR('F', 'F', 'U') | U | U | PASS |
| xor.md:67 | XOR('U', 'U', 'F') | U | U | PASS |
| xor.md:68 | XOR('U', 'U', 'U') | U | U | PASS |
| Logic:2519-2524 | If(U,T,T) | T | T | PASS |
| Logic:2530-2535 | If(U,T,F) | U | U | PASS |
| Logic:1161 | If(U,F,T) | U | U | PASS |
| ProjectAndCollapse:33-36 | Project(U,F),Project(U,T),Project(T,F),Project(F,T) | FTTF | FTTF | PASS |
| Logic:1197-1264,1951 | IsTrue table T/U/F | TFF | TFF | PASS |
| Logic:1197-1264,1951 | IsFalse table T/U/F | FFT | FFT | PASS |
| Logic:1197-1264,1951 | IsUnknown table T/U/F | FTF | FTF | PASS |
| Logic:1197-1264,1951 | IsKnown table T/U/F | TFT | TFT | PASS |
| Logic:1292-1296,1975-1981 | Unknown->False projection T/U/F | TFF | TFF | PASS |
| Logic:1312-1316,1983-1989 | Unknown->True projection T/U/F | TTF | TTF | PASS |
| ProjectAndCollapse:18-22 | Project(U->F) / Project(U->T) columns for T,F,U | TFF/TFT | TFF/TFT | PASS |
| Logic:1998 (§42) | Unknown->Error: T,F pass; U error | n/a | n/a | n/a |
| Logic:3-1 §4 count | §4 says 'six primitive operations' but lists NOT,AND,OR,AT_LEAST,AT_MOST,EXACTLY,COALESCE | 7 listed | 6 claimed | FAIL (editorial: 7 rows listed) |

### E.9 Cardinality Summary table (Logic.md section 19, lines 890-897) vs interval rule
| row | cases | mismatches | result |
| --- | --- | --- | --- |
| AtLeast(n) | 2004 | 0 | PASS |
| AtMost(n) | 2004 | 0 | PASS |
| Exactly(n) | 2004 | 0 | PASS |
| Any() | 2004 | 0 | PASS |
| All() | 2004 | 300 | FAIL e.g. operands=U: table says F, interval rule gives U |
| None() | 2004 | 0 | PASS |

## F. Sources

| Tag | Source | How I checked it |
| --- | --- | --- |
| [P08] | Graham Priest, *An Introduction to Non-Classical Logic*, 2nd ed., Cambridge UP, 2008, ch. 7: <https://users.fmi.uni-jena.de/~mundhenk/Webseite/FDE/PriestAuszug.pdf> | Read the extracted text: 7.2.1 (`A ≡ B` defined as `(A ⊃ B) ∧ (B ⊃ A)`), 7.3.2 tables, 7.3.4 "(strong) Kleene 3-valued logic, often written K3", 7.3.7 excluded middle, 7.3.8 "K3 has no logical truths at all", 7.4.1 LP |
| [OLP] | Open Logic Project, "Kleene logics": <https://builds.openlogicproject.org/content/many-valued-logic/three-valued-logics/kleene.pdf> | Read in full: Definitions thr.1-thr.2, "Ks and Kw have no tautologies", parallel-evaluation motivation, Bochvar external connectives |
| [SEP-MV] | Stanford Encyclopedia of Philosophy, "Many-Valued Logic": <https://plato.stanford.edu/entries/logic-manyvalued/> | Via fetch summary only. Order `0 < 1/2 < 1`, min/max, "introduced by Kleene (1938, 1952)" |
| [WP-3VL] | Wikipedia, "Three-valued logic": <https://en.wikipedia.org/wiki/Three-valued_logic> | Via fetch summary. K3 tables, `U->U` differs in Lukasiewicz, "no tautologies", SQL |
| [WP-MVL] | Wikipedia, "Many-valued logic": <https://en.wikipedia.org/wiki/Many-valued_logic> | Via fetch summary. K3 and Lukasiewicz conditional; K3 biconditional table |
| [WP-KAI] | Wikipedia, "Kleene algebra (with involution)": <https://en.wikipedia.org/wiki/Kleene_algebra_(with_involution)> | Via fetch summary. De Morgan algebra, complement laws not guaranteed, K3 first appears in Kleene 1938 |
| [BHT18] | Borja Macías and Hernández-Tello, "Implication and Biconditional in some Three-valued Logics", CEUR-WS Vol. 2264: <https://ceur-ws.org/Vol-2264/paper10.pdf> | Read extracted text: K3 and L3 matrices (Tables 1-2), K3 "absence of tautologies" |
| [MCC63] | John McCarthy, "A Basis for a Mathematical Theory of Computation", 1963: <http://www-formal.stanford.edu/jmc/basis1.pdf> | Read extracted text, pp. 8-9: conditional forms with undefined `p`, non-symmetric connectives |
| [FFL18] | Friedrichs, Függer, Lenzen, "Metastability-Containing Circuits", IEEE Trans. Computers 2018; arXiv:1606.06570v7: <https://arxiv.org/abs/1606.06570> | Read extracted text: §2.2 (closure `f_M`, Kleene), §3 (MUX vs CMUX, consensus term, eq. 20-23) |
| [PG-CMP] | PostgreSQL, "Comparison Functions and Operators": <https://www.postgresql.org/docs/current/functions-comparison.html> | Fetched; quote verbatim |
| [PG-COND] | PostgreSQL, "Conditional Expressions" (CASE, COALESCE): <https://www.postgresql.org/docs/current/functions-conditional.html> | Fetched. The page does not state what a `NULL` `WHEN` condition does; "not true" falls through is my reading of "If the condition's result is not true" |
| [PG-CHK] | PostgreSQL, "Constraints": <https://www.postgresql.org/docs/current/ddl-constraints.html> | Fetched; quote verbatim |
| [PG-SUB] | PostgreSQL, "Subquery Expressions": <https://www.postgresql.org/docs/current/functions-subquery.html> | Fetched |
| [PG-BOOL] | PostgreSQL, "Boolean Type": <https://www.postgresql.org/docs/current/datatype-boolean.html> | Fetched; `UNKNOWN` not an accepted literal |
| [WP-SQL] | Wikipedia, "Null (SQL)": <https://en.wikipedia.org/wiki/Null_(SQL)> | Via fetch summary. WHERE rule, CHECK "designated values are True and Unknown", feature F571, T031 |
| [MS-BOOL] | Microsoft Learn, "Boolean logical operators": <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/boolean-logical-operators> | Fetched in full |
| [MS-COAL] | Microsoft Learn, "`??` and `??=` operators": <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-coalescing-operator> | Fetched in full |
| [MS-COND] | Microsoft Learn, "`?:` operator": <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator> | Fetched in full |
| [WP-XNOR] | Wikipedia, "XNOR gate": <https://en.wikipedia.org/wiki/XNOR_gate> | Search result text only: "sometimes ENOR, EXNOR, NXOR" |
| [IEP-S] | Internet Encyclopedia of Philosophy, "The Sheffer Stroke": <https://iep.utm.edu/sheffers/> | Search result text only |

**UNVERIFIED (not obtainable here):**

- Kleene, *Introduction to Metamathematics* (1952) §64 and Kleene, "On notation for ordinal numbers", *J. Symbolic Logic* 3 (1938) 150-155: the books/papers were not fetched (JSTOR failed). The strong tables, the strong/weak distinction and the "regular" property are confirmed only through [P08], [OLP], [SEP-MV] and [WP-KAI]; a search snippet attributes the "regular" definition to Kleene §64.
- Lukasiewicz (1920) and Bochvar (1938) primary texts: contrast tables come from [P08], [OLP], [BHT18].
- ISO/IEC 9075 text: `IS UNKNOWN` feature F571, boolean literal `UNKNOWN` (T031) and the `WHERE`/`CHECK` rules are taken from vendor documentation and [WP-SQL].
- Use of "at-least-k / at-most-k / exactly-k" in constraint-programming literature, and absence of a K3 usage of "Project"/"Collapse".
- Fetch summaries (SEP, Wikipedia, Null (SQL)) come from a small summarising model, not raw page text; PostgreSQL and Microsoft quotes appeared verbatim.

[P08]: https://users.fmi.uni-jena.de/~mundhenk/Webseite/FDE/PriestAuszug.pdf
[OLP]: https://builds.openlogicproject.org/content/many-valued-logic/three-valued-logics/kleene.pdf
[SEP-MV]: https://plato.stanford.edu/entries/logic-manyvalued/
[WP-3VL]: https://en.wikipedia.org/wiki/Three-valued_logic
[WP-MVL]: https://en.wikipedia.org/wiki/Many-valued_logic
[WP-KAI]: https://en.wikipedia.org/wiki/Kleene_algebra_(with_involution)
[BHT18]: https://ceur-ws.org/Vol-2264/paper10.pdf
[MCC63]: http://www-formal.stanford.edu/jmc/basis1.pdf
[FFL18]: https://arxiv.org/abs/1606.06570
[PG-CMP]: https://www.postgresql.org/docs/current/functions-comparison.html
[PG-COND]: https://www.postgresql.org/docs/current/functions-conditional.html
[PG-CHK]: https://www.postgresql.org/docs/current/ddl-constraints.html
[PG-SUB]: https://www.postgresql.org/docs/current/functions-subquery.html
[PG-BOOL]: https://www.postgresql.org/docs/current/datatype-boolean.html
[WP-SQL]: https://en.wikipedia.org/wiki/Null_(SQL)
[MS-BOOL]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/boolean-logical-operators
[MS-COAL]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-coalescing-operator
[MS-COND]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator
[WP-XNOR]: https://en.wikipedia.org/wiki/XNOR_gate
[IEP-S]: https://iep.utm.edu/sheffers/
