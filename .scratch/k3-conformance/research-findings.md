# Research findings: k3-conformance open items

Primary-source research for the "Summary for review" block of [`issues-log.md`](issues-log.md), done on branch `StrongK3+Operations` on 2026-10-03. Nothing in the repo was changed except this file.

## How to read this

- **Current behaviour** is read from the repo (relative links). **Primary-source findings** are what a source says: each bullet ends with a [source](#sources) link and a quoted or tightly paraphrased line. **Inference** is mine and is marked as such.
- **UNVERIFIED** means I could not open a primary source for the claim (paywalled book, host unreachable, or only a search snippet). It is never used to hide a guess.
- "Experiment" means I ran a read-only command or a throwaway script (scratchpad, not the repo) and report the observed result.
- Effort: S under half a day, M about a day, L multi-day. Confidence: H/M/L that the recommendation is right, not that it is easy.
- Per [`CLAUDE.md`](../../CLAUDE.md) cardinal rules 1 to 3, every recommendation is a proposal for you to approve, iterate or decline.

> [!IMPORTANT]
> Two findings are not on the review list but block you today: CI on `main` is red because of an `S1135` TODO, and `dotnet csharpier check .` has a second failure (`Directory.Packages.props`) that survives the junction fix. See item 6.

## Executive summary

| # | Item | Decision needed | Recommended fix | Effort | Risk | Confidence |
| - | ---- | --------------- | --------------- | ------ | ---- | ---------- |
| 1a | Collapse vs `IsSatisfied` | Who applies the lenient policy: rule text or host | Keep `Decision.Result` raw; `Outcome` carries the declared collapse; `IsSatisfied` stays `Result == True` (option C) | S | M (public semantics, pre-1.0) | M |
| 1b | `If(Unknown, A, A)` | Confirm consensus term | Keep the consensus definition; document `If(IsTrue(c), t, f)` as the SQL `CASE` equivalent; add 27-triple oracle test | S | L | H |
| 2a | `XnorExpression` rename | Add a shim? | No shim. No package is published and the version is `1.0.0-dev`; a type alias is not possible | S (none) | L | H |
| 2b | `XorArityViolation` (`BRE0006`) | Rename or alias | Keep the string `BRE0006` forever; rename the C# constant to `InfixArityViolation` (alias only if a release ships first) | S | L | M |
| 3a | C-style spellings | Pick `=>` etc. | Keep word forms in `CStyle`; optionally accept `⊼ ⊽ ⊻ ⇒ ⇔` as input aliases; do not use `=>` or `->` | S | L | M |
| 3b | `?:` and `??` rules | Relax ternary mixing? | Keep strict (relaxing later is non-breaking, tightening is breaking); `??` n-ary is sound (associativity checked 27/27) | S (none) | L | H |
| 3c | Prefix `!` spacing | Space it? | Keep `!a` (already implemented and tested; matches CSharpier output) | S (none) | L | H |
| 4 | Operand minimums | Allow 1 or 0 operands? | Keep 2+ in the DSL; keep full-range `BETWEEN` rejected; optional builder helper for dynamic lists | S | L | M |
| 5 | API shape and names | Confirm names | Keep `Outcome`, enums, `NestedCollapse`, `policy`/`unknownAs`, string `Path`, free-text `Expected`/`Found`; consider `PrintText` to `PrintRuleText` | S | L | M |
| 6a | `csharpier check .` crash | Fix path or ignore | Repair the dangling junction (`.csharpierignore` alone does NOT fix it); also add the missing final newline to `Directory.Packages.props` | S | L | H |
| 6b | Roslynator and `.slnx` | Upgrade or loop | Upgrade `roslynator.dotnet.cli` 0.10.1 to 1.0.0 (verified locally on the whole `.slnx`), then drop the per-project loop | S | L | M (CI run unverified) |
| 6c | `dotnet format` exit 2 | Baseline or fix | Fix the 4 code sites (do not suppress); the S1135 also fails the CI build | S | L | H |
| 7 | Predicate catalog | Eight questions | Keep `False` for existing members, `Unknown` for new comparison families, host-level null option; ordinal-only strings; accept `DateTimeOffset` only; clock predicates take `TimeProvider` at registration | M | M | M |

### Headline discoveries

1. **Item 1a:** a declared `UnknownAsTrue` currently turns an `Unknown` caused by a predicate fault into `Result == True` and therefore `IsSatisfied == true` (fail-open on errors). This contradicts the `Decision.Collapse` remark that `IsSatisfied` "stays fail-closed regardless of any policy" and ADR-0001's intent.
2. **Item 6c:** CI on `main` fails at the Build step on `S1135` because `src/Directory.Build.props` sets `TreatWarningsAsErrors` when `CI=true`. The `IPredicate.cs` TODO is gone on this branch but two `S1135` TODOs remain in `OperatorInfo.cs`, so the branch will fail the same way. One of them ("add all operators") is stale: all 21 operators are already covered.
3. **Item 6a:** `.csharpierignore` is not enough. CSharpier 1.3.0 walks the tree once for a MSBuild-version check that ignores ignore files, then a second time that honours them. Both walks crash on the dangling junction (experiment below).
4. **Item 1b:** the consensus `If` equals the Strong Kleene extension of if-then-else on all 27 input triples; the naive multiplexer differs at exactly one triple, `(Unknown, True, True)`. SQL searched `CASE` differs at four. So ADR-0005 decision 13 is right, and the SQL behaviour is expressible as `If(IsTrue(c), t, f)`.
5. **Item 3a:** `OperatorStyle.CStyle` prints `EQUIVALENT` as `==`. In C#, `==` on `bool?` gives `null == null` is `true`; in K3 `Unknown EQUIVALENT Unknown` is `Unknown`. The spelling is a readability hazard for C# readers.

### Experiments run (all read-only)

| Experiment | Result |
| ---------- | ------ |
| `dotnet csharpier check .` (repo, 1.3.0) | `DirectoryNotFoundException` from `HasMismatchedCliAndMsBuildVersions.Check` |
| Same with `--no-msbuild-check` | Crashes later in `CommandLineFormatter...EnumerateNonignoredFiles`; also reports `Directory.Packages.props` unformatted |
| Throwaway dir with a dangling junction (scratchpad, CSharpier 1.3.0): A none; B `.csharpierignore` `.claude/`; C B plus `--no-msbuild-check`; D wrong-case ignore plus flag | A crash; B crash; C OK; D crash |
| `dotnet csharpier check src tests benchmarks docs *.props *.targets *.slnx` | exit 1: only `Directory.Packages.props` ("did not end with a single newline") |
| `roslynator analyze TruthWeaver.slnx` with the globally installed 1.0.0 | Loads all 12 projects in 21 s; reports S1135 x2, SA1512, S6966 |
| `dotnet format TruthWeaver.slnx --verify-no-changes --severity info` | exit 2; excluding `SA1512 S6966` still 2; excluding `S1135 SA1512 S6966` gives 0; argument-less run also works on `.slnx` |
| Python script over all 27 K3 triples | COALESCE associative; consensus `If` equals the strong extension; table in item 1b |
| CSharpier 1.3.0 formatting `! a` and `- x` | rewritten to `!a` and `-x` |
| `gh run view` on CI run 37051044463 | Build failed on `S1135` in `IPredicate.cs(3,4)`; Test, CSharpier, format, Roslynator skipped |

---

## 1. Safety semantics

### 1a. Declared `Collapse` and `Decision.IsSatisfied`

#### Current behaviour

- [`Decision.IsSatisfied`](../../src/TruthWeaver.Abstractions/Decision.cs) is `Result == TruthValue.True`. The call-site `Decision.Collapse(CollapsePolicy)` is pure and its remark says `IsSatisfied` "stays fail-closed regardless of any policy applied here".
- [`CompiledRule.ApplyCollapse`](../../src/TruthWeaver/Evaluation/CompiledRule.cs) (about line 426) does the opposite for a **declared** collapse: it returns `decision with { Result = <collapsed value>, Outcome = ... }`. For `UnknownAsFalse`/`UnknownAsTrue`, `Result` becomes definite, so `IsSatisfied` follows the rule text. For `UnknownIsError` `Result` stays `Unknown` and `Outcome` is `RejectedUnresolved`.
- `Faults` is never touched. A predicate that throws becomes `Unknown` plus a `Fault`; under a declared `UnknownAsTrue` that `Unknown` becomes `True`. [`CollapsePolicy.UnknownAsTrue`](../../src/TruthWeaver.Abstractions/CollapsePolicy.cs) is documented "Fail-open, so choose it deliberately."
- The uncollapsed value survives only as the child of the root `Collapse(...)` node in `EvaluatedTree`.

#### Primary-source findings

- SQL `WHERE` keeps a row only on `true`: "If the result of the condition is true, the row is kept ... otherwise (i.e., if the result is false or null) it is discarded." [PostgreSQL 7.2](https://www.postgresql.org/docs/current/queries-table-expressions.html)
- SQL `CHECK` constraints go the other way: "a check constraint is satisfied if the check expression evaluates to true or the null value." [PostgreSQL 5.5](https://www.postgresql.org/docs/current/ddl-constraints.html). SQL Server agrees: "`CHECK` constraints reject values that evaluate to `FALSE`. Because null values evaluate to UNKNOWN, their presence in expressions might override a constraint." [SQL Server docs](https://learn.microsoft.com/en-us/sql/relational-databases/tables/unique-constraints-and-check-constraints)
- Oracle: a condition that is `UNKNOWN` "acts almost like `FALSE`" in `WHERE`, but "`NOT` `UNKNOWN` evaluates to `UNKNOWN`". [Oracle SQL Language Reference, Nulls](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/Nulls.html)
- SQL has explicit collapse tests that never return null: `IS TRUE`, `IS NOT TRUE` ("yields false or unknown"), `IS FALSE`, `IS NOT FALSE` ("true or unknown"), `IS UNKNOWN`; "A null input is treated as the logical value 'unknown'." [PostgreSQL 9.2](https://www.postgresql.org/docs/current/functions-comparison.html)
- C# `bool?` already uses K3 for `&` and `|` (`null` is the unknown), and there is no implicit `bool?` to `bool`; collapsing is explicit via `??` or a cast. [C# boolean operators](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/boolean-logical-operators), [Nullable value types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types) ("`int m1 = n; // Doesn't compile`").
- XACML separates the decision from its enforcement: the PDP returns Permit, Deny, Indeterminate or NotApplicable; a "deny-biased PEP" or "permit-biased PEP" decides what to do with Indeterminate (sections 5.53 and 7.2). [XACML 3.0 core](https://docs.oasis-open.org/xacml/3.0/xacml-3.0-core-spec-os-en.html) (paraphrased from the fetched summary; I did not read the section text verbatim).
- Cedar treats an evaluation error as "skip-on-error", explicitly rejecting deny-on-error because one broken policy could deny everything. [Cedar authorization](https://docs.cedarpolicy.com/auth/authorization.html) (search snippet only; page not opened, so treat as pointer).
- CEL's `&&`/`||` absorb an error only when the other operand decides the result (`error && false` is `false`, `error && true` is `error`): the same shape as K3 with errors as `Unknown`. [CEL language definition](https://github.com/google/cel-spec/blob/master/doc/langdef.md)
- Kleene's third value means "undefined output of a partial recursive function"; the logic itself has no notion of collapsing to two values. [SEP, Many-Valued Logic 2.2](https://plato.stanford.edu/entries/logic-manyvalued/)

**Inference.** In every system above the *consumer* (the `WHERE` clause, the `CHECK` constraint, the PEP) applies the two-valued policy; the three-valued value itself is not rewritten. The TruthWeaver mapping is: `WHERE` is `UnknownAsFalse`, `CHECK` is `UnknownAsTrue`, `IS TRUE` is `IsTrue`, `IS NOT FALSE` is `Project(x, True)`. Putting the policy in rule text is closer to a `CHECK` written by the rule author than to a `WHERE` chosen by the host, which is the authority-boundary issue.

#### Options

| Option | `Result` | `IsSatisfied` | For | Against |
| ------ | -------- | ------------- | --- | ------- |
| A (current) | Collapsed (definite) | `Result == True` | Self-contained rule text; simplest | Lossy; rule text can flip the host's fail-closed default to fail-open; contradicts `Decision.Collapse` remark; faults become `True` |
| B | Raw | Consults `Outcome` when present | Lossless; same authorisation effect as A | Still lets rule text flip fail-closed; `IsSatisfied` now depends on two members |
| C (recommended) | Raw | Unchanged (`Result == True`) | Keeps the invariant "satisfied only if the rule is definitely True"; matches `Decision.Collapse` docs and XACML/SQL layering | A lenient policy has no effect on `IsSatisfied` consumers; they must read `Outcome` deliberately |

#### Recommendation

Option C. Reason: ADR-0001 and the `IsSatisfied` docs call fail-closed "the correct default for an authorization consumer", and the rule text may be authored by a different party than the host developer. The author's declared policy stays fully available as `Decision.Outcome`. Choose B instead only if rule authors are trusted by the host and you want the rule text to be authoritative. This changes the wording of ADR-0005 decision 14 ("so `IsSatisfied` follows the author's explicit choice"), so it needs your approval. Confidence M: it is a product-intent decision, not a fact.

#### Proposed change (if option C is accepted)

- [ ] [`CompiledRule.ApplyCollapse`](../../src/TruthWeaver/Evaluation/CompiledRule.cs): return `decision with { EvaluatedTree = tree, Outcome = outcome }` (do not overwrite `Result`); keep the root `Collapse(...)` node's own result as the collapsed value for display.
- [ ] Update the XML docs of `Decision.Outcome` ("`Result` is already the collapsed value" becomes "`Result` stays the raw three-valued result"), `CollapsePolicy.UnknownAsFalse` ("matching `IsSatisfied`") and `CompiledRule.CollapsePolicy`.
- [ ] Tests: [`CollapseTests`](../../tests/TruthWeaver.Tests/CollapseTests.cs) and `K3Oracle` expectations (for `UnknownAsTrue` over `Unknown`: `Result == Unknown`, `IsSatisfied == false`, `Outcome == True`); keep the `UnknownIsError` tests.
- [ ] Add a test that a faulting predicate under `UnknownAsTrue` leaves `IsSatisfied == false`.
- [ ] Docs: ADR-0005 decision 14 and open-decision text, README collapse section, [`issues-log.md`](issues-log.md) row 22, CONTEXT.md glossary.

#### Open risks

- Hosts that read only `Outcome` still convert a fault into `True` under `UnknownAsTrue`. Optional guard (not recommended without your approval): when `Faults.Count > 0` and the raw result is `Unknown`, report `RejectedUnresolved` instead of `True`. Trade-off: it also fires when the fault is unrelated to the final `Unknown`.
- Pre-1.0 and unpublished, so no consumer migration is needed.

### 1b. `If(Unknown, A, A)`: consensus term versus naive multiplexer

#### Current behaviour

[ADR-0005 decision 13](../../docs/adr/0005-strong-k3-language-surface.md) defines `If(c, t, f)` as `(c AND t) OR (NOT c AND f) OR (t AND f)`. A definite condition takes its branch; an `Unknown` condition gives the branch value only when both branches are the same definite value.

#### Primary-source findings

- Searched `CASE`: "When a condition is not true, any subsequent `WHEN` clauses are examined ... If no `WHEN` condition yields true, the value of the `CASE` expression is the result of the `ELSE` clause." So an `Unknown` condition falls to `ELSE`. [PostgreSQL 9.18](https://www.postgresql.org/docs/current/functions-conditional.html); same in T-SQL: "If no *Boolean_expression* evaluates to `TRUE`, the Database Engine returns the *else_result_expression*." [SQL Server CASE](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/case-transact-sql)
- Kleene's strong tables as presented by the SEP are just minimum, maximum and `1 - x` over `{0, 1/2, 1}`; there is no if-then-else connective among them. [SEP 2.2](https://plato.stanford.edu/entries/logic-manyvalued/)
- I found **no** standard K3 if-then-else definition in the sources I could open. Kleene 1952 section 64 and Priest's textbook are books I could not read (UNVERIFIED). McCarthy's 1963 conditional for partial functions is the usual reference for a *lazy* conditional; I could not extract its text (UNVERIFIED).

#### Experiment (all 27 triples, script in scratchpad)

Strong extension means: the value is definite exactly when every classical completion of the `Unknown` inputs agrees.

| `c` | `t` | `f` | naive mux | consensus (ADR) | strong extension | SQL searched `CASE` |
| --- | --- | --- | --------- | --------------- | ---------------- | ------------------- |
| U | T | T | **U** | T | T | T |
| U | F | T | U | U | U | **T** |
| U | U | F | U | U | U | **F** |
| U | U | T | U | U | U | **T** |
| U | T | F | U | U | U | **F** |

All other triples agree across the four columns. Consensus equals the strong extension on 27 of 27; the naive mux differs only at `(U, T, T)`; SQL `CASE` differs at four triples (all with `c = Unknown`).

**Inference.** SQL `CASE` is `If(IsTrue(c), t, f)`: with a definite condition the consensus `If` reduces to exactly the branch selection SQL does. The two readings are different by design, not one of them wrong.

#### Options and recommendation

| Option | Result for `If(U, A, A)` | Verdict |
| ------ | ------------------------ | ------- |
| Consensus (current) | `A` | Keep. Matches the Strong Kleene principle on all 27 triples |
| Naive mux | `U` when `A` is `True` | Rejects a tautology (`If(c, x, x)` is not `x`) |
| SQL-style (`Unknown` goes to else) | `f` | Not K3; available as `If(IsTrue(c), t, f)` |

Keep the consensus definition (confidence H for the semantics; the Kleene text itself is UNVERIFIED). Effort S: documentation and a test.

#### Proposed change

- [ ] CONTEXT.md and README `If` section: add "SQL searched `CASE` is `If(IsTrue(c), t, f)`" with the table above.
- [ ] Test: enumerate all 27 triples against a strong-extension oracle (helper in `K3Oracle`) for the evaluator and for `ExpandToPrimitives`.
- [ ] Mark [`issues-log.md`](issues-log.md) row 17 confirmed.

#### Open risks

None identified. Note the evaluator already evaluates both branches when `c` is `Unknown`, which the consensus term requires.

---

## 2. API names

### 2a. `XnorExpression` to `EquivalentExpression`, no shim

#### Current behaviour

[`EquivalentExpression`](../../src/TruthWeaver/Ast/Expression.cs) is a `sealed record`; `RuleBuilder.Xnor` forwards to `RuleBuilder.Equivalent`; DSL `XNOR`/`IFF` and JSON `xnor`/`iff` still read. The package has **not** been published: NuGet's flat-container index for `truthweaver` returns `BlobNotFound`, the repo has no git tags, and `Directory.Build.props` sets `1.0.0-dev`.

#### Primary-source findings

- "Major version zero (0.y.z) is for initial development. Anything MAY change at any time." [SemVer 2.0.0 item 4](https://semver.org/spec/v2.0.0.html). Item 4 is about `0.y.z`; `1.0.0-dev` is a pre-release identifier (item 9), and the items I fetched (4 to 9) make no compatibility promise for pre-releases, though I did not read the rest of the spec for one (UNVERIFIED).
- ".NET libraries: DO include a prerelease suffix when releasing a nonstable package." [Library guidance, versioning](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning)
- A source-breaking change "is the least disruptive breaking change. Developers can fix their own broken source code easily." The guidance for APIs you intend to remove is to put `ObsoleteAttribute` on them and consider keeping them indefinitely in low and middle-level libraries. [Breaking changes and .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes)
- `ObsoleteAttribute` targets include class, struct, enum, interface, field, method, property, constructor, delegate, event. [ObsoleteAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.obsoleteattribute)

**Inference.** Type forwarding (`TypeForwardedTo`) moves a type between assemblies; it cannot make a second name for the same type, and C# has no exported type alias (`using` aliases are file or global to a project). A `sealed record` cannot be subclassed. If you made `EquivalentExpression` non-sealed and added `[Obsolete] XnorExpression : EquivalentExpression`, the compiler would still build `EquivalentExpression`, so a consumer's `is XnorExpression` would **silently never match**. A compile-time break is safer than that silent mismatch.

#### Recommendation

No shim (confidence H). Effort: none. Keep `RuleBuilder.Xnor` as the only forwarding member; optionally mark it `[Obsolete("Use Equivalent")]` since it is cheap. Revisit only if a package is published before a consumer migrates.

#### Proposed change

- [ ] None required. Optional: `[Obsolete]` on `RuleBuilder.Xnor`; add a one-line note to the ADR-0005 consequences that `XnorExpression` pattern matches must move to `EquivalentExpression`.

### 2b. Shared `XorArityViolation` (`BRE0006`)

#### Current behaviour

[`DiagnosticCodes.XorArityViolation`](../../src/TruthWeaver/Diagnostics/DiagnosticCodes.cs) `= "BRE0006"` is used for XOR, EQUIVALENT, IMPLIES, NAND, NOR. The constant is referenced by `RuleNodeCompiler` and roughly ten test files.

#### Primary-source findings

- SARIF: a reporting descriptor's `id` is a "stable value", "more likely to remain stable if it is a symbolic or numeric value, as opposed to a descriptive string"; a result's `ruleId` is "the stable value which an analysis tool associates with a rule" (sections 3.49.3, 3.27.5). [SARIF 2.1.0](https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html)
- Roslyn release tracking models a rule rename as "Removed Rules" plus a new rule, and tracks "Changed Rules" only for category, default severity or enabled-by-default. There is no "renamed" category. [Release tracking help](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md)
- I did **not** find a Microsoft Learn page that states "diagnostic IDs must never be reused or renamed". The Roslyn analyzer docs I opened do not say it (UNVERIFIED as a written rule; the SARIF text above is the closest primary statement).
- Constants "are propagated by compilers, so other code compiled with your libraries needs to be recompiled to see the changes". [const keyword](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/const) So the string value must not change, while renaming the C# identifier is source-breaking only.

#### Options

| Option | Cost | Note |
| ------ | ---- | ---- |
| Leave as is | 0 | Name misleading; `BRE0006` stays stable |
| Rename the constant, keep the string | S, mechanical across about 12 files | No consumer can be broken because nothing is published |
| Add `InfixArityViolation = "BRE0006"` plus `[Obsolete]` old name | S | Needed only after a release ships |
| New code (for example `BRE0017`) and obsolete `BRE0006` | S | Changes what existing log/suppression filters match; the message already names the operator, so little gained |

#### Recommendation

Rename the constant to `InfixArityViolation`, keep the value `"BRE0006"` (confidence M; the name is taste, the stable string is the part sources support). Do not mint a new code.

#### Proposed change

- [ ] `DiagnosticCodes.cs`: rename; XML doc "XOR, EQUIVALENT (XNOR), IMPLIES, NAND or NOR given other than exactly two operands".
- [ ] Update `RuleNodeCompiler` and the test files that reference it (`grep XorArityViolation`).
- [ ] If preferred instead: keep both, with `[Obsolete("Use InfixArityViolation")] public const string XorArityViolation = InfixArityViolation;`.
- [ ] Document the diagnostic code list as a stable contract (strings never change) in README or CONTEXT.md.

---

## 3. Syntax

### 3a. C-style spellings for `IMPLIES`, `NAND`, `NOR`

#### Current behaviour

[`OperatorStyle.CStyle`](../../src/TruthWeaver/Printing/OperatorStyle.cs) prints `&&`, `||`, `!`, `^` (XOR), `==` (EQUIVALENT), `??` (COALESCE); `IMPLIES`, `NAND`, `NOR` keep their words. The DSL already reads `→ ↔ ↑ ↓ ⊕`.

#### Primary-source findings

| System | Implication | Equivalence | XOR | NAND / NOR | Source |
| ------ | ----------- | ----------- | --- | ---------- | ------ |
| C# | none | `==` | `^` (same as `!=` for `bool`) | none | [boolean operators](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/boolean-logical-operators) |
| SQL (PostgreSQL) | none; `NOT a OR b` | `=` (null-propagating), `IS NOT DISTINCT FROM` (null-safe) | none | none | [PostgreSQL 9.2](https://www.postgresql.org/docs/current/functions-comparison.html) |
| Python | none | `==` | `^` is bitwise | none | [Python expressions](https://docs.python.org/3/reference/expressions.html) |
| SMT-LIB 2.7 Core | `=>` (right-assoc) | `=` (chainable) | `xor` (left-assoc, n-ary) | none | [SMT-LIB Core](https://smt-lib.org/theories-Core.shtml) |
| Wolfram Language | `Implies`, infix `=>` or `\[Implies]` | not checked | not checked | `Nand` infix `⊼`, `Nor` infix `⊽` | [Implies](https://reference.wolfram.com/language/ref/Implies.html), [Nand](https://reference.wolfram.com/language/ref/Nand.html), [Nor](https://reference.wolfram.com/language/ref/Nor.html) |
| Lean 4 | `→` or `->` | `↔` or `<->` | not in the connectives table I saw | none | [Lean reference](https://lean-lang.org/doc/reference/latest/Basic-Propositions/Logical-Connectives/) (search snippet) |
| Prolog (SWI) | `->` is *if-then control*, not implication | none | none | none | [SWI-Prolog control](https://www.swi-prolog.org/pldoc/man?section=control) |
| Unicode | `→` U+2192, `⇒` U+21D2 | `↔` U+2194, `⇔` U+21D4 | `⊻` U+22BB XOR, `⊕` U+2295 | `⊼` U+22BC NAND, `⊽` U+22BD NOR | Unicode Character Database 13.0 via Python `unicodedata` (chart URL not opened) |

- Dafny (`==>`, `<==>`), Boogie, Verilog (`~&`, `~|`), Haskell, Coq/Rocq, Isabelle, Alloy: **UNVERIFIED**. The Dafny reference host was unreachable and the others are paywalled or not opened.
- Peirce's arrow `↓` is NOR and the Sheffer stroke is NAND in the usual convention, matching the DSL's `↓`/`↑` (UNVERIFIED primary; Unicode names `↑` "UPWARDS ARROW", so it is not a dedicated logic symbol).
- In C# `^` on `bool?` returns `null` when an operand is `null`, matching K3 XOR, but `==` does not: "if both operands are `null`, the result is `true`." [Nullable value types, Lifted operators](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types). K3 `Unknown EQUIVALENT Unknown` is `Unknown`.

#### Options and recommendation

| Option | Pros | Cons |
| ------ | ---- | ---- |
| Keep words in `CStyle` (current) | Unambiguous; no collision | Mixed-style output |
| Add `=>` / `->` / `==>` | Familiar from proofs | `=>` is the lambda arrow in C#/JS, `->` is member access in C/C++ and if-then in Prolog; `==>` only attested in sources I could not verify |
| Accept `⊼ ⊽ ⊻ ⇒ ⇔` as extra input aliases | Dedicated Unicode logic characters, Wolfram precedent for `⊼ ⊽`; cheap in the lexer | More spellings (ADR-0005 decision 2 already accepts several) |

Recommend: keep word forms in `CStyle`; optionally add the five Unicode input aliases (confidence M); do not use `=>` or `->`. Separately, decide whether `CStyle` should keep `==` for `EQUIVALENT` given the C# `null == null` hazard; a one-line XML-doc warning is the minimum.

#### Proposed change

- [ ] No change required for the current rows 8 and 10. Optional: lexer aliases `⊼`, `⊽`, `⊻`, `⇒`, `⇔` in [`Token.cs`](../../src/TruthWeaver/Parsing/Token.cs) and the lexer, with `RuleText` and docs; add round-trip tests.
- [ ] `OperatorStyle.CStyle` XML doc: note that `==` and `^` are spellings, not C# `bool?` semantics.

### 3b. Strict no-mixing for `?:`, and `??` chains

#### Current behaviour

`?:` mixed with `AND`/`OR`, another infix operator or a nested ternary is `AmbiguousOperatorMixing` ([row 18](issues-log.md)). `a ?? b ?? c` is one n-ary `CoalesceExpression` ([row 15](issues-log.md)).

#### Primary-source findings

- C#: "The conditional operator is right-associative, that is, `a ? b : c ? d : e` is evaluated as `a ? b : (c ? d : e)`." [conditional operator](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator) The precedence table lists `&&`, `||`, `??`, then `?:` (lowest before assignment), and says the null-coalescing and conditional operators are right-associative. [C# operators](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/)
- C# `??`: "`a ?? b ?? c` ... evaluated as `a ?? (b ?? c)`", and the right operand is not evaluated when the left is non-null. [null-coalescing operator](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-coalescing-operator)
- ECMAScript grammar: `ConditionalExpression : ShortCircuitExpression ? AssignmentExpression : AssignmentExpression`, and `CoalesceExpression : CoalesceExpressionHead ?? BitwiseORExpression`, `CoalesceExpressionHead : CoalesceExpression | BitwiseORExpression`. So in JavaScript `??` is **left**-recursive (left-associative), its operands are `BitwiseORExpression`s (a `||`/`&&` operand needs parentheses), and the ternary condition is a `ShortCircuitExpression`. [ECMA-262 binary logical operators](https://tc39.es/ecma262/#sec-binary-logical-operators), [conditional operator](https://tc39.es/ecma262/#sec-conditional-operator) (read from the `spec.html` source on GitHub)
- Python: `x if C else y` sits below `or` in the precedence table (lowest to highest: `lambda`, `if`-`else`, `or`, `and`, `not`). [Python expressions](https://docs.python.org/3/reference/expressions.html)
- SQL: `NOT` right, `AND` left, `OR` left, in that precedence order. [PostgreSQL 4.1.6](https://www.postgresql.org/docs/current/sql-syntax-lexical.html)
- SQL `COALESCE`: n-ary, "only evaluates the arguments that are needed ... arguments to the right of the first non-null argument are not evaluated." [PostgreSQL 9.18](https://www.postgresql.org/docs/current/functions-conditional.html)

**Experiment: `COALESCE` associativity.** I checked `co(co(a,b),c) == co(a,co(b,c))` for all 27 triples over `{F, U, T}` with `co(a,b) = b if a is U else a`. Zero violations. So left fold, right fold and the n-ary node are the same function (proof sketch: both return the first non-`Unknown` operand, or the last operand's value if all are `Unknown`, which is `Unknown`).

**Order of evaluation and faults.** C# right fold `a ?? (b ?? c)` and JS left fold `(a ?? b) ?? c` both evaluate `a`, then `b` only if needed, then `c` only if needed; the sequence of predicate invocations, and hence the order faults are recorded, is the same. Only the tree shape differs: a flat n-ary node gives one `EvaluatedNode` with skipped children, a fold gives nesting. The repo's evaluator already evaluates left to right and stops at the first non-`Unknown` ([ADR-0005 decision 13, ticket 15](../../docs/adr/0005-strong-k3-language-surface.md)).

#### Options and recommendation

| Question | Option | Verdict |
| -------- | ------ | ------- |
| Ternary mixing | Keep strict (current) | **Recommended.** Accepting more later is source-compatible; rejecting later is breaking. JavaScript's own grammar also refuses to guess for `??` mixed with `||`/`&&` |
| | Relax to "ternary is lowest precedence" | Consistent with C#, JS and Python (condition is the `or` level, branches are full expressions); do it only if authors complain |
| `??` chains | n-ary node (current) | Keep; associativity verified |

Confidence H. Effort: none for "keep"; S for the relaxation (parser plus tests) if chosen. If relaxed, copy Python/ES: condition parsed at `OR` level, branches full expressions, unparenthesised nested ternary stays an error or becomes right-associative as in C#.

#### Proposed change

- [ ] None for "keep". Optionally add the associativity table check as a test of `Coalesce` (all 27 triples, n-ary versus nested binary).
- [ ] Optional: add the C#/ES/Python citations to ADR-0005 decision 8 as the rationale.

### 3c. Prefix `!` spacing in the whitespace normaliser

#### Current behaviour

[`RuleText.NeedsSpace`](../../src/TruthWeaver/Printing/RuleText.cs) returns no space after `!` or `¬`; [`WhitespaceNormalisationTests`](../../tests/TruthWeaver.Tests/WhitespaceNormalisationTests.cs) asserts `"a  &&   !  b"` to `"a && !b"`. So the question in [row 27](issues-log.md) is already answered in code.

#### Primary-source findings

- Experiment: CSharpier 1.3.0 rewrites `var c = ! a; var e = - x; var f = ~ x;` to `!a`, `-x`, `~x`.
- The documented `csharp_space_*` options for `dotnet format` / IDE0055 include `csharp_space_after_cast` and `csharp_space_around_binary_operators` but **no** option for unary operators, so `dotnet format` does not enforce either spelling. [C# formatting options](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options) (list read in full).
- gofmt, rustfmt, Prettier behaviour: **UNVERIFIED** (not checked against their docs).

#### Recommendation

Keep `!a` (confidence H). Effort: none. Close row 27.

---

## 4. Operand minimums

### Current behaviour

`AND`, `OR`, `ExactlyOne`, `NXOR`, `ANY`, `ALL`, `NONE`, `BETWEEN` require two or more operands (`MalformedTree`); the threshold family (`AtLeast`, `AtMost`, ...) allows one. `BETWEEN(min, max, ...)` requires `0 <= min <= max <= n` and rejects `0..n` as an always-true constant (`InvalidThresholdValue`, [`RuleNodeCompiler.BuildBetween`](../../src/TruthWeaver/Compilation/RuleNodeCompiler.cs)). The analyzer already reports a K3 tautology as warning `BRE0012`. [`RuleBuilder`](../../src/TruthWeaver/Building/RuleBuilder.cs) does not special-case short operand lists.

### Primary-source findings

| System | Empty | Single | Source |
| ------ | ----- | ------ | ------ |
| Python `all` / `any` | `True` / `False` | the element's truth | "`all`: Return True if all elements ... are true (or if the iterable is empty)"; "`any`: If the iterable is empty, return False" [Python built-ins](https://docs.python.org/3/library/functions.html) |
| LINQ `All` | `true` | the predicate | "`true` if every element ... passes ..., or if the sequence is empty" [Enumerable.All](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.all) |
| SQL `ANY` / `ALL` over an array | `false` / `true` | the comparison | "ANY ... Empty set: false"; "ALL ... Empty set: true" (fetched summary) [PostgreSQL 9.24](https://www.postgresql.org/docs/current/functions-comparisons.html) |
| SQL aggregate `bool_and` / `every` | **null** (no rows) | the value | "Returns true if all non-null input values are true"; "return a null value when no rows are selected"; nulls are ignored [PostgreSQL 9.21](https://www.postgresql.org/docs/current/functions-aggregate.html) |
| Wolfram `Nor[]` | `True` (since `Or[] = False`) | | [Nor](https://reference.wolfram.com/language/ref/Nor.html) |

- Note the SQL aggregate **ignores** `NULL`, while K3 propagates `Unknown`: aggregates are not a K3 precedent for the single and empty cases.
- Linter practice on always-constant conditions: ESLint `no-constant-condition` is in the recommended set at **error** level [ESLint](https://eslint.org/docs/latest/rules/no-constant-condition); .NET's CA1508 "Avoid dead conditional code" is **not** enabled by default [CA1508](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1508). Practice is split.
- Kleene does not define n-ary operators; the empty conjunction `True` and empty disjunction `False` are the identities of the lattice meet and join on `{F < U < T}` (**inference**; no primary citation).

**Inference, operator by operator:**

| Operator | Empty | Single operand `x` |
| -------- | ----- | ------------------ |
| `AND` / `ALL` | `True` | `x` |
| `OR` / `ANY` | `False` | `x` |
| `NONE` | `True` | `NOT x` |
| `NXOR` (parity, `Unknown` if any `Unknown`) | `False` (even count) | `x` |
| `BETWEEN(0,0)` / `BETWEEN(1,1)` | n/a | `NOT x` / `x` |
| `BETWEEN(0,n)` | constant `True`, even with `Unknown` operands | |

### Options and recommendation

| Option | Pros | Cons |
| ------ | ---- | ---- |
| Keep 2+ everywhere (current) | Uniform with `AND`/`OR`; a one-operand `ANY` is a typo or a redundant wrapper | Dynamic operand lists need caller-side handling |
| Allow 1 operand | Matches threshold family | Redundant node; adds nothing to the K3 semantics |
| Allow 0 operands with identities | Matches Python/LINQ/SQL ANY/ALL | Silent constants hide mistakes (an empty list of role checks becoming `True`); conflicts with the analyzer's constant warnings |
| `BETWEEN` full range: error (current) / warning | Consistent with `AtLeast(0)` | ESLint-style error vs CA1508-style off-by-default: no consensus |

Recommend: keep 2+ in the DSL and builder AST; keep the full-range rejection (it matches the threshold family's rule and `ExpandToPrimitives` relies on it); if dynamic lists matter, add builder helpers that fold short lists at build time (`Any()` is `False`, `All()` is `True`, one operand is the operand). The helpers are an **addition beyond what you asked**; I list them for your decision (cardinal rule 2). Confidence M. Effort S.

### Proposed change

- [ ] None for "keep" beyond closing rows 12 to 14.
- [ ] Optional: `RuleBuilder` overloads taking `IEnumerable<RuleBuilder>` that return `Constant` or the single operand for 0 or 1 items, with tests and README.
- [ ] Optional: downgrade the `BETWEEN(0..n)` error to warning `BRE0012`-style only if you decide consistency with ESLint-style warnings outweighs consistency with `AtLeast(0)`; this also requires changing `PrimitiveExpander` which currently relies on the rejection.

---

## 5. API shape and naming

### Current behaviour

`Decision.Outcome` is a trailing nullable `CollapseOutcome?`; enums [`CollapsePolicy`](../../src/TruthWeaver.Abstractions/CollapsePolicy.cs) (`UnknownAsFalse = 0`, `UnknownAsTrue`, `UnknownIsError`) and [`CollapseOutcome`](../../src/TruthWeaver.Abstractions/CollapseOutcome.cs) (`False = 0`, `True`, `RejectedUnresolved`). JSON fields in [`rule-tree.schema.json`](../../src/TruthWeaver/Json/rule-tree.schema.json): `op`, `operands`, `const`, `predicate`, `args`, `k`, `min`, `max`, `policy`, `unknownAs`; op names are lowerCamel (`isTrue`, `atLeast`, `exactlyOne`); policy values are lowerCamel (`unknownAsFalse`). `CompiledRule` exposes `CanonicalText`, `PrintJson()`, `PrintMermaid()`, `PrintPlainText()` (a tree) and `PrintText(GroupingStyle)` (DSL text). [`Diagnostic`](../../src/TruthWeaver/Diagnostics/Diagnostic.cs) has `Code`, `Span`, `Path` (string), `Expected`, `Found` (free text), `Suggestion`.

### Primary-source findings

- Enums: "DO use a singular type name for an enumeration", "DO NOT use an 'Enum' suffix", "DO provide a value of zero on simple enums ... the most common default value for the enum should be assigned ... zero." [Framework Design Guidelines, naming](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces), [enum design](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/enum). `Style` is a normal suffix; both enums satisfy these, and `default(CollapsePolicy)` is the fail-closed `UnknownAsFalse` (a useful property worth keeping).
- JSON naming: "Property names must be camel-cased, ascii strings." [Google JSON Style Guide](https://google.github.io/styleguide/jsoncstyleguide.xml) (search snippet). `JsonNamingPolicy.CamelCase` is the built-in .NET policy. [JsonNamingPolicy.CamelCase](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonnamingpolicy.camelcase) JSON Schema's own keywords are camelCase (`additionalProperties`, `minItems`). The existing field set is already uniformly lowerCamel, so `policy` and `unknownAs` fit. Enum *value* spelling conventions: UNVERIFIED.
- Roslyn `Diagnostic` carries `Id`, one `Location`, `AdditionalLocations`, a `Properties` string dictionary for "diagnostic specific information you want to pass around ... for example, to corresponding fixer", and message arguments (`Create(descriptor, location, messageArgs)`). [Diagnostic](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnostic)
- SARIF 2.1.0 `result`: `ruleId`, `message` with `arguments` and `{0}` placeholders (3.11.5), `locations`, `relatedLocations`, `fixes`, and a `properties` bag; `region` has 1-based `startLine`/`startColumn`; a `logicalLocation` has `fullyQualifiedName`, `kind`, `name` (3.33). [SARIF 2.1.0](https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html) Neither Roslyn nor SARIF has typed "expected/found"; machine-readable data goes in arguments or a property bag.
- JSON Schema validation output uses `instanceLocation`, "A JSON Pointer". [JSON Schema 2020-12 output](https://json-schema.org/draft/2020-12/json-schema-core#name-output-formatting)
- JSON Pointer: "`json-pointer = *( "/" reference-token )`", `~` encoded `~0`, `/` as `~1`; it can name a nonexistent array slot only with `-`. [RFC 6901](https://www.rfc-editor.org/rfc/rfc6901.html)
- JSONPath: a Normalized Path is "a unique representation of the location of a node in a value", bracket notation only with single quotes (`$['book'][3]`). A query such as `$.foo.bar` is valid shorthand for `$['foo']['bar']` (`member-name-shorthand`, section 2.5.1.1), and `singular-query` allows `name-segment`/`index-segment`. [RFC 9535](https://www.rfc-editor.org/rfc/rfc9535.html)
- `Utf8JsonReader.TokenStartIndex` is a public property: "the index that the last processed JSON token starts at (within the given UTF-8 encoded input text)", and `BytesConsumed` is public. [TokenStartIndex](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonreader.tokenstartindex), [Utf8JsonReader](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonreader). `JsonException.LineNumber` and `BytePositionInLine` are "zero-based". [JsonException](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonexception)
- The `JsonDocument`/`JsonElement` DOM docs describe a read-only DOM built from `Utf8JsonReader`; I found no member exposing a source position (absence in the pages I read, not an exhaustive member check). Related open runtime issues: [dotnet/runtime #28482](https://github.com/dotnet/runtime/issues/28482) (expose line number and byte position on `Utf8JsonReader`, open) and [#31068](https://github.com/dotnet/runtime/issues/31068) (JsonPath support for `JsonDocument`/`JsonElement`, open). [DOM article](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-dom)

### Analysis and recommendations

| Topic | Finding | Recommendation |
| ----- | ------- | -------------- |
| `Decision.Outcome` + enums | Names and zero values follow the guidelines. If 1a option C is taken, `Outcome` is the sole carrier of the declared answer, and "Result vs Outcome" reads clearly enough | Keep. Document the distinction in one sentence on both members |
| `NestedCollapse` (`BRE0016`) | Named by condition like `AmbiguousOperatorMixing`; separate code is justified by the distinct span and fix | Keep |
| `policy` / `unknownAs` | Match existing lowerCamel fields. `Project` uses a boolean `unknownAs`, `Collapse` a string `policy`: different shapes for related concepts, but each mirrors its in-language argument | Keep |
| `GroupingStyle` | Matches `OperatorStyle` | Keep |
| `PrintText(GroupingStyle)` | Easy to confuse with `PrintPlainText()` (which prints a tree) | Consider `PrintRuleText(GroupingStyle)` while pre-1.0 (S); alternative: keep and cross-reference the XML docs |
| `Diagnostic.Path` | The `$`-rooted `.key` / `[n]` / `['key']` syntax **is already a valid RFC 9535 singular query** when keys are plain identifiers, but not a *normalized* path (`$['operands'][1]['op']`) | Keep the string; document it as an RFC 9535 singular query. Switch to JSON Pointer only for JSON Schema/validator interop (add `ToJsonPointer()` then); normalized paths add verbosity with no consumer need |
| `Expected` / `Found` typed? | No comparable system types them; machine data is `Properties` (Roslyn) or `properties`/`message.arguments` (SARIF) | Keep free text. If a UI must branch, add an optional string `Properties` map rather than a typed expectation (M) |
| JSON spans | Feasible with one `Utf8JsonReader` pass that records `TokenStartIndex` per node. Complications: byte offsets versus the `SourceSpan` unit (UTF-16 chars), BOM and multibyte text; line/column must be counted manually | Defer until an editor integration needs it (M to L). Not available from `JsonElement` |

Confidence M on naming (taste), H on the factual statements. Effort S for any rename; M for `Properties`; M to L for JSON spans.

### Proposed change

- [ ] Optional rename `CompiledRule.PrintText` to `PrintRuleText` (tests, README, ADR-0005 decision 9, `PrintText` call sites).
- [ ] `Diagnostic.Path` XML doc: "RFC 9535 singular query".
- [ ] Row 38 stays open; note the three constraints above.

---

## 6. Tooling

### 6a. `dotnet csharpier check .` crash

#### Current behaviour and root cause

`.claude/SKILLS/humanizer` is a Windows **junction** (`cmd /AL`) to `C:\Users\rheone\code\Boolean-Rules-Engine\.agents\skills\humanizer`. `Boolean-Rules-Engine` is the pre-rebrand folder and no longer exists, so the junction dangles. The folder `.claude/SKILLS` is untracked, so CI (Linux checkout) never has it; the crash is local only. [`.husky/task-runner.json`](../../.husky/task-runner.json) runs `dotnet csharpier check .`, so the local pre-commit hook fails too. `dotnet-tools.json` pins CSharpier **1.3.0**, which is also the latest release (2026-06-07), so an upgrade is not available.

#### Primary-source findings

- ".csharpierignore ... uses gitignore syntax" and "CSharpier will read the contents of `.gitignore` files and use them in addition to a `.csharpierignore`"; the docs say nothing about symlinks, junctions or enumeration. [CSharpier ignore](https://csharpier.com/docs/Ignore)
- `--no-msbuild-check`: "Bypass the check to determine if a csproj files references a different version of CSharpier.MsBuild." [CSharpier CLI](https://csharpier.com/docs/CLI)
- Source at tag 1.3.0: `HasMismatchedCliAndMsBuildVersions.Check` recurses with `Directory.EnumerateFiles/EnumerateDirectories` and skips only `node_modules`, `.git`, `bin`, `obj` ("using optionsProvider is slower so just hard coding the ones that could cause performance issues"); it never consults ignore files. [source](https://github.com/belav/csharpier/blob/1.3.0/Src/CSharpier.Cli/HasMismatchedCliAndMsBuildVersions.cs) The main walk, `EnumerateNonignoredFiles`, **does** call `IsDirectoryIgnoredAsync(subdirectory)` before descending. [source](https://github.com/belav/csharpier/blob/1.3.0/Src/CSharpier.Cli/CommandLineFormatter.cs)
- Tracker search in `belav/csharpier` for `DirectoryNotFoundException`, symlink/junction and `no-msbuild-check` found no issue matching this crash (related: [#1781](https://github.com/belav/csharpier/issues/1781), Windows `node_modules` performance). Completeness of the search is UNVERIFIED.
- Experiment (throwaway tree, CSharpier 1.3.0): ignore file alone still crashes (first walk); ignore file plus `--no-msbuild-check` passes; an ignore entry with the wrong case (`.claude/skills/`) does not match `.claude/SKILLS` (case-sensitive). The repo's `.gitignore` has `.claude/skills/`, which git (case-insensitive on Windows) honours but CSharpier does not.

#### Options

| Option | Fixes the crash? | Notes |
| ------ | ---------------- | ----- |
| Remove or repair the junction | Yes (both walks) | One local action; `rmdir` on a junction does not delete the target. Recreate it as `.claude\SKILLS\humanizer` pointing to `.agents\skills\humanizer` (exists here; `.agents/` is untracked) |
| `.csharpierignore` with `.claude/` and `.agents/` | **No** by itself | Must be paired with `--no-msbuild-check` in Husky and CI; the flag also hides a real version-mismatch check (you do not use `CSharpier.MsBuild`, so nothing is lost today) |
| Run on `src tests benchmarks` only (current workaround) | Yes | Misses root files such as `Directory.Packages.props`, which is exactly where the second problem is |

#### Second blocker found (not in the review list)

`dotnet csharpier check .` also exits 1 on `./Directory.Packages.props`: "The file did not end with a single newline." The file is CRLF with no final newline (`.editorconfig` has `insert_final_newline = true`). CI runs `dotnet csharpier check .` after Build and Test, so once the Build step is green this step will fail too. `stylecop.json` only gives a warning ("unsupported file type").

#### Recommendation

Repair the junction locally (confidence H) and add the missing final newline to `Directory.Packages.props` via `dotnet csharpier format Directory.Packages.props`. Optionally add a `.csharpierignore` with `.claude/` and `.agents/` plus fix the `.gitignore` case, but only as defence in depth and only with `--no-msbuild-check` if a dangling link might reappear. Effort S, risk L.

#### Proposed change

- [ ] Local: `cmd /c rmdir .claude\SKILLS\humanizer` then optionally `mklink /J .claude\SKILLS\humanizer ..\..\.agents\skills\humanizer` (adjust relative target).
- [ ] `dotnet csharpier format Directory.Packages.props` and commit.
- [ ] Optional: `.csharpierignore` (`.claude/`, `.agents/`, `.scratch/`, `.tmp/`) and `.gitignore` entry `.claude/SKILLS/` (or lowercase the directory).
- [ ] Re-run `dotnet csharpier check .` and expect exit 0.

### 6b. Roslynator CLI and `.slnx`

#### Current behaviour

`dotnet-tools.json` pins `roslynator.dotnet.cli` **0.10.1** (published 2025-02-09). With it, `roslynator analyze TruthWeaver.slnx` fails: "the file extension '.slnx' is not associated with a language" (reproduced). [`ci.yml`](../../.github/workflows/ci.yml) works around this with a loop over **four** projects (`Abstractions`, `TruthWeaver`, `Yaml`, `Tests`); the solution has 12, so eight are never analysed in CI.

#### Primary-source findings

- Roslynator **v4.14.1** (2025-10-05): "[CLI] Add support for `slnx` files ([PR #1662](https://github.com/dotnet/roslynator/pull/1662)) ... Bump Roslyn to 4.14.0 ... Drop support for .NET 7 SDK". [release notes](https://github.com/dotnet/roslynator/releases/tag/v4.14.1) NuGet shows the matching CLI package is **0.11.0** (2025-10-05); later CLI packages are 0.12.0 (2025-12-14), 0.13.0 (2026-08-08), 0.13.1 (2026-08-16), 1.0.0 (2026-08-21). [NuGet registration](https://api.nuget.org/v3/registration5-gz-semver2/roslynator.dotnet.cli/index.json)
- PR [#1783](https://github.com/dotnet/roslynator/pull/1783) (merged 2026-08-07) fixes a `TypeLoadException: Could not load type 'Microsoft.Build.Framework.FileUtilities'` on SDK 10 with MSBuild 18 when parsing `.slnx` (issues #1729, #1748, #1716), by loading MSBuild from the SDK. It supersedes closed PR [#1762](https://github.com/dotnet/roslynator/pull/1762). So 0.11.0/0.12.0 may fail on newer SDKs; use 0.13.0 or later.
- Roslyn's `MSBuildWorkspace` only loads project extensions registered to a language; that is the cause of the 0.10.1 message (stack in `ProjectFileExtensionRegistry.TryGetLanguageNameFromProjectPath`).
- Experiment: the globally installed **1.0.0** analysed `TruthWeaver.slnx` ("Analyze solution ... 12 projects", 21.0 s, 4 diagnostics: S1135 x2, SA1512, S6966).

#### Recommendation

Bump `roslynator.dotnet.cli` to **1.0.0** in `dotnet-tools.json` and replace the CI loop with `dotnet roslynator analyze TruthWeaver.slnx`. Confidence M: verified locally, **not** verified on the CI runner (Ubuntu, SDK 11 RC via `setup-dotnet`); the tool's runtime roll-forward on a machine with only the .NET 11 runtime is UNVERIFIED, so try it on a branch first. Effort S; risk L (it may surface findings in the 8 projects not covered today; locally the whole solution has only the 4 known diagnostics).

#### Proposed change

- [ ] [`dotnet-tools.json`](../../dotnet-tools.json): `roslynator.dotnet.cli` to `1.0.0`; `dotnet tool restore`.
- [ ] [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml) Roslynator step: single `dotnet roslynator analyze TruthWeaver.slnx`; delete the explanatory comment about `.slnx`.
- [ ] Update the "Required validation" block in CLAUDE.md only if you want it (it already says `dotnet roslynator analyze`).
- [ ] If the CI runner fails to start the tool, fall back to the loop (all 12 projects) and keep the upgrade.

### 6c. `dotnet format --verify-no-changes --severity info` exits 2

#### Current behaviour

CI step: `dotnet format TruthWeaver.slnx --no-restore --verify-no-changes --severity info`; Husky: the same without the solution argument (it finds `TruthWeaver.slnx`; verified). Both exit 2 today.

#### Primary-source findings

- `--verify-no-changes`: "Terminates with a non zero exit code if any files would have been formatted." `--severity` default is `warn`; `--diagnostics` and `--exclude-diagnostics` take space-separated diagnostic IDs; `--exclude` takes paths. [dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)
- Experiment: the four findings each make the run fail. `--report` lists each as a "FileChange": `S6966` at `benchmarks/.../Program.cs(12,1)`; `S1135` at `OperatorInfo.cs(14,8)` and `(40,16)`; `SA1512` at `(40,13)`. `--exclude-diagnostics SA1512 S6966` still exits 2; `--exclude-diagnostics S1135 SA1512 S6966` exits 0.
- CI evidence: run 37051044463 on `main` failed at the **Build** step with `error S1135` in `IPredicate.cs(3,4)`, because [`src/Directory.Build.props`](../../src/Directory.Build.props) sets `TreatWarningsAsErrors` when `CI=true`. Test, CSharpier, format and Roslynator were skipped.
- Sonar's `S1135` is a deliberate "track TODO tags" rule; [CLAUDE.md](../../CLAUDE.md) says "Do not suppress analyzers merely to make a build pass."

#### The code sites

| Diagnostic | File | Cause | Fix |
| ---------- | ---- | ----- | --- |
| S1135 | [`OperatorInfo.cs:40`](../../src/TruthWeaver/Ast/OperatorInfo.cs) | `// TODO add all operators` is **stale**: the switch covers Not, And, Or, Xor, Equivalent, Implies, Nand, Nor, Nxor, Any, All, None, Between, Coalesce, IsTrue, IsFalse, IsUnknown, IsKnown, Project, If, ExactlyOne, plus the threshold family | Delete the comment and the blank line after it (also clears SA1512) |
| SA1512 | `OperatorInfo.cs:40` | Blank line after a single-line comment | Same edit |
| S1135 | `OperatorInfo.cs:14` | `// TODO operator descriptions should be enriched with context ...` | Move to a ticket under `.scratch/` and delete the comment, or do the enrichment |
| S6966 | [`benchmarks/TruthWeaver.Benchmarks/Program.cs:12`](../../benchmarks/TruthWeaver.Benchmarks/Program.cs) | `BenchmarkSwitcher...Run(args)` where an async twin exists | `await ... RunAsync(args)` (the string `RunAsync` exists in BenchmarkDotNet 0.16.0-preview.2; that it is on `BenchmarkSwitcher` is UNVERIFIED), or a documented suppression |

#### Recommendation

Fix the four sites (confidence H); do not baseline or suppress. A baseline via `.editorconfig` severity or `--exclude-diagnostics` is mechanically possible (tested above) but would also stop `S1135` from protecting `src/`. Effort S; risk L. The `IPredicate.cs` TODO from the log is already gone on this branch.

#### Proposed change

- [ ] Edit `OperatorInfo.cs` (both TODOs, blank line) and `Program.cs`.
- [ ] Re-run: `dotnet build -p:CI=true`, `dotnet format --verify-no-changes --severity info` (expect 0), `dotnet roslynator analyze` (expect 0 diagnostics).
- [ ] Update [`issues-log.md`](issues-log.md) rows 1 to 3 to "resolved".

---

## 7. Predicate catalog

Grounding: [`k3-gap-list.md`](../predicate-catalog/k3-gap-list.md); [`StringPredicates`](../../src/TruthWeaver.Predicates/StringPredicates.cs) returns `False` for a null selected value through `PredicateResult.FromBoolAsync`; `EqualsConfigurable` uses `CultureInfo.InvariantCulture.CompareInfo` by default (a *linguistic* comparison, not ordinal); [CONTEXT.md](../../CONTEXT.md) states only that "ambient state (clocks, timezones) is the predicate's problem" and that `IsToday` "just a predicate that happens to read `TimeProvider` internally"; it does **not** record a null rule or a culture rule.

### 7.1 Null selected value: `False` or `Unknown` (question 1) and `NotX` (question 2)

#### Primary-source findings

- "Ordinary comparison operators yield null (signifying 'unknown'), not true or false, when either input is null. For example, `7 = NULL` yields null, as does `7 <> NULL`." [PostgreSQL 9.2](https://www.postgresql.org/docs/current/functions-comparison.html) Oracle: "If you use any other condition with nulls and the result depends on the value of the null, then the result is `UNKNOWN`." [Oracle Nulls](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/Nulls.html) That sentence also covers `LIKE`; PostgreSQL's pattern-matching page has no explicit null sentence (UNVERIFIED there).
- The null-safe, definite tests are `IS NULL` / `IS NOT NULL` and `IS [NOT] DISTINCT FROM`: "`NULL IS NOT DISTINCT FROM NULL` yields true". [PostgreSQL 9.2](https://www.postgresql.org/docs/current/functions-comparison.html)
- `IN`/`NOT IN`: "If the left-hand expression yields null, or if there are no equal right-hand values and at least one right-hand expression yields null, the result is null." [PostgreSQL 9.24](https://www.postgresql.org/docs/current/functions-comparisons.html) (fetched summary)
- C# chose the other convention for relational operators on nullable values: "if one or both operands are `null`, the result is `false`" for `<`, `>`, `<=`, `>=`, and `null == null` is `true`. [Nullable value types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types) So both conventions have mainstream precedent; K3 sides with SQL.
- Kleene's third value is "undefined", which is the natural reading of a missing selected value. [SEP 2.2](https://plato.stanford.edu/entries/logic-manyvalued/)

**Inference.** With `False`, `NOT Equals(x, "a")` is `True` for null `x`; with `Unknown` it stays `Unknown`, which is fail-closed under `IsSatisfied`. The gap-list recommendation is sound.

#### Options and recommendation

| Option | Effect | Cost |
| ------ | ------ | ---- |
| Keep `False` for the 7 existing members | No behaviour change | The `NOT` hazard stays |
| Switch them to `Unknown` | K3-pure | Breaking behaviour change; moves null from "never a fault" to "never definite" |
| New families return `Unknown`; existing keep `False`; null tests (`IsNull`, `IsNullOrEmpty`, ...) return definite | SQL-aligned going forward | Two conventions to document |
| Registration-time option `NullBehavior` (`False` default or `Unknown`) on the factory, chosen by the host | Host decides, not the rule author; existing behaviour is the default | Slight API growth |

Recommend: keep `False` for existing members, make the host-level `NullBehavior` option available for them and default new comparison, range and count families to `Unknown`; define `NotX` as rule-level `NOT` (K3 complement, `Unknown` maps to `Unknown`) rather than first-class predicates unless a host UX needs them. Record the rule in CONTEXT.md (row 41). Confidence M, effort M.

### 7.2 Culture (question 3)

#### Primary-source findings

- "Use `StringComparison.Ordinal` or `StringComparison.OrdinalIgnoreCase` for comparisons as your safe default for culture-agnostic string matching"; "Use the non-linguistic Ordinal ... values instead of ... `CultureInfo.InvariantCulture` when the comparison is linguistically irrelevant (symbolic, for example)." [Best practices for comparing strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings)
- The Turkish-I example: `IsFileURI("file:")` "returns `true` if the current culture is U.S. English, but `false` if the current culture is Turkish", and this "could cause significant problems if the culture is inadvertently used in security-sensitive settings". Same page. Also: "On balance, the invariant culture has few properties that make it useful for comparison."
- CA1310 flags overloads that default to a culture-sensitive comparison, CA1307 flags any overload lacking a `StringComparison`; both are off by default in .NET 10 and are "safe to suppress ... when the library or application is not intended to be localized". [CA1310](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1310), [CA1307](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1307)
- When strings must be normalised for comparison: "Use the `String.ToUpperInvariant` method instead of the `String.ToLowerInvariant` method" (same page).

#### Recommendation

Option (a) of the gap list: every string predicate uses ordinal comparison; `ignoreCase` maps to `OrdinalIgnoreCase`; `trim` is `string.Trim()` (culture-independent); the `Culture` option is dropped or limited to the empty string. Also note that the **existing** `EqualsConfigurable` default (empty culture, invariant `CompareInfo`) is *linguistic*, so it already breaks the "never culture-sensitive" docs; switch it to `CompareOptions.OrdinalIgnoreCase` when `ignoreCase` and ordinal otherwise, and drop `culture` or make it a faulting error when non-empty. Record the rule in CONTEXT.md. Confidence H on the .NET guidance, M on dropping an argument that exists. Effort S to M.

### 7.3 `DateTime` literal (question 5)

- "Consider `DateTimeOffset` as the default date and time type for application development"; a `DateTime` with `Kind` `Unspecified` "is ambiguous within the same time zone"; only UTC `DateTime` is unambiguous. [Compare date and time types](https://learn.microsoft.com/en-us/dotnet/standard/datetime/choosing-between-datetime)

Recommend: no `DateTime` `LiteralKind` (confidence H). Accept `DateTimeOffset` literals only (JSON/YAML/DSL as ISO 8601 strings with an offset); a host that has a `DateTime` converts it in its selector. Adding a kind is a breaking change for exhaustive switches over the closed set. Effort: none.

### 7.4 Clock predicates `AfterNow` / `BeforeNow` (question 6)

- `TimeProvider` is the .NET 8+ abstraction of time ("By using `TimeProvider`, you ensure that your code is testable and predictable"); `TimeProvider.System` is the default and `FakeTimeProvider` (Microsoft.Extensions.TimeProvider.Testing) is the test double. [TimeProvider overview](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview)
- The repo already blesses this: CONTEXT.md says ambient state is the predicate's problem and names `IsToday` reading `TimeProvider`. The catalog issue 01 rejected clock predicates for the *shared catalog*, to keep it host-agnostic.

**Inference.** Evaluation stays pure with respect to the engine if the clock is a registration-time dependency: `AfterNow(selector, TimeProvider timeProvider)` captures the provider once and reads `GetUtcNow()` when invoked. The engine memoizes one term's answer per evaluation, so a given `AfterNow` term reads the clock once per evaluation, but two distinct clock terms in one rule can read slightly different instants; if a single "now" per evaluation matters, the host should expose it through the context (a `Now` property) and use a normal comparison predicate against a selector.

Recommend: ship `AfterNow`/`BeforeNow` with a mandatory `TimeProvider` parameter and no default (so a clock is never silently ambient), and document the per-term read. Confidence M; effort S once question 7 (bounds) is settled.

### 7.5 Collection `In` / `NotIn` (question 4) and bounds (question 7)

- SQL `IN` is scalar membership ("`expression IN (value [, ...])`") with null propagation as in 7.1; array comparisons are the `ANY`/`ALL` forms with the empty-set rules from item 4. [PostgreSQL 9.24](https://www.postgresql.org/docs/current/functions-comparisons.html)
- `BETWEEN`: "`a BETWEEN x AND y` is equivalent to `a >= x AND a <= y`", "treats the endpoint values as included in the range", and `BETWEEN SYMMETRIC` swaps reversed bounds. [PostgreSQL 9.2](https://www.postgresql.org/docs/current/functions-comparison.html)

Recommend: `In`/`NotIn` over a **scalar** selector are membership with literal candidates (candidates cannot be null, so `NotIn` has no SQL null trap). For a **collection** selector use explicit names instead of overloading `In`: `ContainsAny`, `ContainsAll`, `IsSubsetOf` (gap-list question 4 asked for a one-line definition). `Between` inclusive on both ends; reversed bounds either a compile-time/argument error or `False`; do not swap silently. Confidence M, effort S.

Question 8 (selector shapes) is a design choice with no primary source; not researched.

### Proposed change (catalog)

- [ ] CONTEXT.md: record the null rule, the ordinal-only string rule and the clock rule (row 41).
- [ ] `NullBehavior` option on `StringPredicates`/`RegexPredicates`/`CollectionPredicates` factories (default `False`).
- [ ] `EqualsConfigurable`: ordinal comparisons; remove or restrict `culture`.
- [ ] Settle bounds and `In` meaning, then implement the gap-list slices in its suggested order.

---

## Suggested sequencing

1. **Unblock CI and local gates (6c, 6a, 6b).** Fix the `OperatorInfo.cs` TODOs and `Program.cs`; repair the junction and the `Directory.Packages.props` newline; try Roslynator 1.0.0 on a branch.
2. **Decide 1a (Collapse versus `IsSatisfied`)**, because it changes public semantics and ADR-0005 decision 14; then 1b documentation and tests.
3. **Small names and codes (2a, 2b, 5)** in one pass: constant rename, optional `PrintRuleText`, `Path` doc.
4. **Close syntax rows (3a, 3b, 3c) and operand rows (4)** as "keep", with the optional Unicode aliases and builder helpers only if you want them.
5. **Predicate catalog (7)**: record CONTEXT.md rules first, then the null and culture decisions, then bounds, then implementation slices.
6. **Deferred:** JSON spans, typed diagnostic `Properties`, and the Collapse fault guard, until a consumer asks.

## Sources

### Logic and standards

- Stanford Encyclopedia of Philosophy, Many-Valued Logic: <https://plato.stanford.edu/entries/logic-manyvalued/>
- Fitting, Kleene's Three Valued Logics and Their Children (abstract only; PDF not read): <https://journals.sagepub.com/doi/10.3233/FI-1994-201234>
- Kleene, Introduction to Metamathematics (1952) section 64; Priest, An Introduction to Non-Classical Logic; McCarthy 1963, A Basis for a Mathematical Theory of Computation: books and papers I could not read (UNVERIFIED)
- OASIS XACML 3.0 core: <https://docs.oasis-open.org/xacml/3.0/xacml-3.0-core-spec-os-en.html>
- OASIS SARIF 2.1.0: <https://docs.oasis-open.org/sarif/sarif/v2.1.0/os/sarif-v2.1.0-os.html>
- RFC 6901 JSON Pointer: <https://www.rfc-editor.org/rfc/rfc6901.html>
- RFC 9535 JSONPath: <https://www.rfc-editor.org/rfc/rfc9535.html>
- JSON Schema 2020-12 core, output formatting: <https://json-schema.org/draft/2020-12/json-schema-core#name-output-formatting>
- SemVer 2.0.0: <https://semver.org/spec/v2.0.0.html>
- ECMA-262 binary logical operators and conditional operator: <https://tc39.es/ecma262/#sec-binary-logical-operators>, <https://tc39.es/ecma262/#sec-conditional-operator>
- SMT-LIB Core theory: <https://smt-lib.org/theories-Core.shtml>
- CEL language definition: <https://github.com/google/cel-spec/blob/master/doc/langdef.md>
- Cedar authorization (snippet only): <https://docs.cedarpolicy.com/auth/authorization.html>
- Lean 4 logical connectives (snippet only): <https://lean-lang.org/doc/reference/latest/Basic-Propositions/Logical-Connectives/>
- SWI-Prolog control predicates: <https://www.swi-prolog.org/pldoc/man?section=control>
- Wolfram Language: <https://reference.wolfram.com/language/ref/Implies.html>, <https://reference.wolfram.com/language/ref/Nand.html>, <https://reference.wolfram.com/language/ref/Nor.html>
- Unicode Character Database 13.0 (via Python `unicodedata`); chart: <https://www.unicode.org/charts/PDF/U22C0.pdf> (not opened)

### SQL vendor documentation

- PostgreSQL: [comparison functions](https://www.postgresql.org/docs/current/functions-comparison.html), [row and array comparisons](https://www.postgresql.org/docs/current/functions-comparisons.html), [conditional expressions](https://www.postgresql.org/docs/current/functions-conditional.html), [aggregates](https://www.postgresql.org/docs/current/functions-aggregate.html), [check constraints](https://www.postgresql.org/docs/current/ddl-constraints.html), [WHERE](https://www.postgresql.org/docs/current/queries-table-expressions.html), [operator precedence](https://www.postgresql.org/docs/current/sql-syntax-lexical.html), [pattern matching](https://www.postgresql.org/docs/current/functions-matching.html)
- SQL Server: [check constraints](https://learn.microsoft.com/en-us/sql/relational-databases/tables/unique-constraints-and-check-constraints), [CASE](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/case-transact-sql)
- Oracle: [Nulls](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/Nulls.html). The ISO/IEC 9075 text itself is paywalled (UNVERIFIED).

### Languages and .NET

- C#: [conditional operator](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator), [null-coalescing](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-coalescing-operator), [operators and precedence](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/), [boolean logical operators](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/boolean-logical-operators), [nullable value types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types), [const](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/const)
- Python: [expressions](https://docs.python.org/3/reference/expressions.html), [built-in functions](https://docs.python.org/3/library/functions.html)
- ESLint: <https://eslint.org/docs/latest/rules/no-constant-condition>; .NET [CA1508](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1508)
- LINQ: <https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.all>
- Framework Design Guidelines: [names](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-classes-structs-and-interfaces), [enums](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/enum)
- Library guidance: [breaking changes](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes), [versioning](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/versioning); [ObsoleteAttribute](https://learn.microsoft.com/en-us/dotnet/api/system.obsoleteattribute)
- Roslyn: [Diagnostic](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnostic), [release tracking help](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md)
- System.Text.Json: [Utf8JsonReader](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonreader), [TokenStartIndex](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.utf8jsonreader.tokenstartindex), [JsonException](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonexception), [DOM](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-dom), [CamelCase policy](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonnamingpolicy.camelcase), [dotnet/runtime #28482](https://github.com/dotnet/runtime/issues/28482), [#31068](https://github.com/dotnet/runtime/issues/31068)
- Google JSON Style Guide (snippet only): <https://google.github.io/styleguide/jsoncstyleguide.xml>
- Strings, dates, time: [string best practices](https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings), [CA1307](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1307), [CA1310](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1310), [DateTime vs DateTimeOffset](https://learn.microsoft.com/en-us/dotnet/standard/datetime/choosing-between-datetime), [TimeProvider](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview)
- C# formatting options: <https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options>

### Tools

- `dotnet format`: <https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format>
- CSharpier: [ignore](https://csharpier.com/docs/Ignore), [CLI](https://csharpier.com/docs/CLI), source at tag 1.3.0: [`HasMismatchedCliAndMsBuildVersions.cs`](https://github.com/belav/csharpier/blob/1.3.0/Src/CSharpier.Cli/HasMismatchedCliAndMsBuildVersions.cs), [`CommandLineFormatter.cs`](https://github.com/belav/csharpier/blob/1.3.0/Src/CSharpier.Cli/CommandLineFormatter.cs), [issue #1781](https://github.com/belav/csharpier/issues/1781)
- Roslynator: [v4.14.1 release](https://github.com/dotnet/roslynator/releases/tag/v4.14.1), [PR #1662](https://github.com/dotnet/roslynator/pull/1662), [PR #1783](https://github.com/dotnet/roslynator/pull/1783), [PR #1762](https://github.com/dotnet/roslynator/pull/1762), [NuGet `roslynator.dotnet.cli`](https://api.nuget.org/v3/registration5-gz-semver2/roslynator.dotnet.cli/index.json)
- GitHub Actions run evidence: `gh run view 37051044463` on `rheone/TruthWeaver`
