namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The analyzer's Strong K3 constant/contradiction diagnostics (ticket 10; made K3-aware by k3-conformance 06).
/// </summary>
public sealed class AnalyzerTests
{
    private static readonly (string Name, Func<int, int, bool> Satisfies)[] Thresholds =
    [
        ("AtLeast", (count, k) => count >= k),
        ("AtMost", (count, k) => count <= k),
        ("GreaterThan", (count, k) => count > k),
        ("LessThan", (count, k) => count < k),
        ("Exactly", (count, k) => count == k),
    ];

    /// <summary>
    /// <c>A AND NOT A</c> is <c>Unknown</c> when <c>A</c> is <c>Unknown</c>, so it is not a Strong K3
    /// contradiction and must not be reported as one.
    /// </summary>
    [Fact]
    public void Compile_TermAndItsNegation_IsNotReportedAsContradiction_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: \"Y\") AND NOT hasRole(role: \"Y\")");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralContradiction);
    }

    /// <summary>
    /// <c>A OR NOT A</c> is <c>Unknown</c> when <c>A</c> is <c>Unknown</c>, so it is not a Strong K3
    /// tautology and must not be reported as one.
    /// </summary>
    [Fact]
    public void Compile_TermOrItsNegation_IsNotReportedAsTautology_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: \"Y\") OR NOT hasRole(role: \"Y\")");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralTautology);
    }

    /// <summary>A conjunction with <c>False</c> is <c>False</c> for every input, which is a genuine K3 contradiction.</summary>
    [Fact]
    public void Compile_TermAndFalse_ReportsContradictionAsWarning_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: \"Y\") AND FALSE");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralContradiction);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    /// <summary>A disjunction with <c>True</c> is <c>True</c> for every input, which is a genuine K3 tautology.</summary>
    [Fact]
    public void Compile_TermOrTrue_ReportsTautologyAsWarning_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: \"Y\") OR TRUE");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralTautology);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    /// <summary>
    /// The contradiction message states the Strong K3 claim (False for every True/False/Unknown input) and
    /// no longer carries the interim two-valued caveat.
    /// </summary>
    [Fact]
    public void Compile_K3Contradiction_MessageStatesStrongK3Claim_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND FALSE");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralContradiction);
        Assert.Contains("Strong K3", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("every", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("two-valued", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The tautology message states the Strong K3 claim (True for every True/False/Unknown input) and
    /// no longer carries the interim two-valued caveat.
    /// </summary>
    [Fact]
    public void Compile_K3Tautology_MessageStatesStrongK3Claim_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("a OR TRUE");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralTautology);
        Assert.Contains("Strong K3", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("every", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("two-valued", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rule_with_no_constant_or_contradictory_subexpression_produces_no_analysis_diagnostics()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND (b OR c)");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>
    /// Every operator the analyzer supports reports its genuine K3 tautology or contradiction at the
    /// operator itself (the root), using operands that are constant <c>False</c> (or <c>True</c>) under any input.
    /// </summary>
    [Theory]
    [InlineData("(a AND FALSE) XOR (b AND FALSE)", false)]
    [InlineData("(a AND FALSE) EQUIVALENT (b AND FALSE)", true)]
    [InlineData("(a AND FALSE) IMPLIES b", true)]
    [InlineData("a IMPLIES (b OR TRUE)", true)]
    [InlineData("(a OR TRUE) IMPLIES (b AND FALSE)", false)]
    [InlineData("(a AND FALSE) NAND b", true)]
    [InlineData("(a OR TRUE) NAND (b OR TRUE)", false)]
    [InlineData("(a OR TRUE) NOR b", false)]
    [InlineData("(a AND FALSE) NOR (b AND FALSE)", true)]
    [InlineData("NXOR((a AND FALSE), (b AND FALSE), (c AND FALSE))", false)]
    [InlineData("NXOR((a OR TRUE), (b AND FALSE), (c AND FALSE))", true)]
    [InlineData("ANY((a AND FALSE), (b AND FALSE))", false)]
    [InlineData("ANY((a OR TRUE), (b AND FALSE))", true)]
    [InlineData("ALL((a AND FALSE), (b OR TRUE))", false)]
    [InlineData("ALL((a OR TRUE), (b OR TRUE))", true)]
    [InlineData("NONE((a OR TRUE), (b AND FALSE))", false)]
    [InlineData("NONE((a AND FALSE), (b AND FALSE))", true)]
    [InlineData("BETWEEN(1, 1, (a AND FALSE), (b AND FALSE))", false)]
    [InlineData("BETWEEN(0, 1, (a AND FALSE), (b AND FALSE))", true)]
    [InlineData("COALESCE((a AND FALSE), b)", false)]
    [InlineData("COALESCE((a OR TRUE), b)", true)]
    [InlineData("ExactlyOne((a AND FALSE), (b AND FALSE))", false)]
    [InlineData("ExactlyOne((a OR TRUE), (b AND FALSE))", true)]
    [InlineData("AtLeast(1, (a AND FALSE), (b AND FALSE))", false)]
    [InlineData("AtMost(1, (a AND FALSE), (b AND FALSE))", true)]
    [InlineData("GreaterThan(0, (a AND FALSE), (b AND FALSE))", false)]
    [InlineData("LessThan(2, (a AND FALSE), (b AND FALSE))", true)]
    [InlineData("Exactly(1, (a AND FALSE), (b AND FALSE))", false)]
    public void Compile_OperatorWithConstantOperands_ReportsK3VerdictAtTheOperator_Test(string rule, bool tautology)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile(rule);

        Assert.True(result.Succeeded);
        string code = tautology ? DiagnosticCodes.StructuralTautology : DiagnosticCodes.StructuralContradiction;
        Assert.True(IsRootFlagged(result, code), $"expected {code} at the root of '{rule}'");
    }

    /// <summary>
    /// Operators over a term and its negation are <c>Unknown</c> when the term is <c>Unknown</c>, so none of
    /// them is reported.
    /// </summary>
    [Theory]
    [InlineData("a XOR NOT a")]
    [InlineData("a EQUIVALENT NOT a")]
    [InlineData("a EQUIVALENT a")]
    [InlineData("a IMPLIES a")]
    [InlineData("a IMPLIES NOT a")]
    [InlineData("a NAND NOT a")]
    [InlineData("a NOR NOT a")]
    [InlineData("a NAND a")]
    [InlineData("a NOR a")]
    [InlineData("NXOR(a, NOT a)")]
    [InlineData("NXOR(a, b, NOT b)")]
    [InlineData("ANY(a, NOT a)")]
    [InlineData("ALL(a, NOT a)")]
    [InlineData("NONE(a, NOT a)")]
    [InlineData("BETWEEN(1, 1, a, NOT a)")]
    [InlineData("COALESCE(a, NOT a)")]
    [InlineData("a ?? NOT a")]
    [InlineData("ExactlyOne(a, NOT a)")]
    [InlineData("AtLeast(1, a, NOT a)")]
    [InlineData("AtMost(1, a, NOT a)")]
    [InlineData("GreaterThan(0, a, NOT a)")]
    [InlineData("LessThan(2, a, NOT a)")]
    [InlineData("Exactly(1, a, NOT a)")]
    public void Compile_OperatorOverTermAndItsNegation_IsNotReported_Test(string rule)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile(rule);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(
            result.Diagnostics,
            d => d.Code is DiagnosticCodes.StructuralTautology or DiagnosticCodes.StructuralContradiction
        );
    }

    /// <summary>
    /// A K3 verdict is found even when the constant is combined with a sub-expression that can stay unknown:
    /// <c>(A OR NOT A) AND FALSE</c> is <c>False</c> whatever <c>A</c> is.
    /// </summary>
    [Fact]
    public void Compile_UnknownCapableSubexpressionAndFalse_ReportsContradictionAtTheRoot_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("(a OR NOT a) AND FALSE");

        Assert.True(IsRootFlagged(result, DiagnosticCodes.StructuralContradiction));
    }

    /// <summary>
    /// Over many generated rules and every {True, False, Unknown} assignment of three terms, a sub-expression
    /// is reported as a tautology exactly when the independent <see cref="K3Oracle"/> finds it True in every
    /// row, and as a contradiction exactly when it is False in every row. Every sub-expression is checked as
    /// the root of its own compilation.
    /// </summary>
    [Fact]
    public void Compile_GeneratedRules_AnalyzerVerdictsAgreeWithK3OracleOverAllAssignments_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        Random random = new(20261002);
        List<string> disagreements = [];
        int checkedRules = 0;
        int flagged = 0;

        for (int i = 0; i < 400; i++)
        {
            foreach (GeneratedRule node in Subtrees(GenerateRule(random, depth: 3)))
            {
                if (node.Children.Count == 0)
                {
                    // The analyzer reports operators only, never a bare term or literal.
                    continue;
                }

                CompilationResult<RuleTestContext> result = compiler.Compile(node.Text);
                if (result.CompiledRule is null)
                {
                    // Out-of-range thresholds (BRE0008) are authoring errors, not analyzer input.
                    continue;
                }

                checkedRules++;
                (bool tautology, bool contradiction) = node.Verdict();
                flagged += tautology || contradiction ? 1 : 0;
                if (IsRootFlagged(result, DiagnosticCodes.StructuralTautology) != tautology)
                {
                    disagreements.Add($"{node.Text}: oracle tautology={tautology}");
                }

                if (IsRootFlagged(result, DiagnosticCodes.StructuralContradiction) != contradiction)
                {
                    disagreements.Add($"{node.Text}: oracle contradiction={contradiction}");
                }
            }
        }

        Assert.Empty(disagreements);

        // Guard against a vacuous pass: the sample must contain real verdicts and plenty of rules.
        Assert.True(checkedRules > 1000, $"only {checkedRules} rules were checked");
        Assert.True(flagged > 20, $"only {flagged} rules had a K3 verdict");
    }

    [Fact]
    public void Diagnostics_are_warning_or_info_never_error()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: \"Y\") AND FALSE");

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Term_count_at_or_under_the_analysis_cap_runs_analysis_normally()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompilerWithMaxAnalysisTerms(2);

        CompilationResult<RuleTestContext> result = compiler.Compile("(a AND FALSE) OR b");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.AnalysisSkippedTooManyTerms);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralContradiction);
    }

    [Fact]
    public void Term_count_exceeding_the_analysis_cap_skips_analysis_and_suppresses_the_contradiction()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompilerWithMaxAnalysisTerms(1);

        CompilationResult<RuleTestContext> result = compiler.Compile("(a AND FALSE) OR b");

        Assert.True(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.AnalysisSkippedTooManyTerms, diagnostic.Code);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralContradiction);
    }

    [Fact]
    public void Term_count_exactly_equal_to_the_analysis_cap_does_not_trigger_the_skip()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompilerWithMaxAnalysisTerms(1);

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND a");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.AnalysisSkippedTooManyTerms);
    }

    /// <summary>
    /// Whether <paramref name="code"/> was reported for the whole compiled rule rather than only for one of its
    /// sub-expressions: diagnostics end with the canonical text of the node they describe.
    /// </summary>
    private static bool IsRootFlagged(CompilationResult<RuleTestContext> result, string code)
    {
        string root = result.CompiledRule?.ToString() ?? throw new InvalidOperationException("The rule did not compile.");
        return result.Diagnostics.Any(d => d.Code == code && d.Message.EndsWith($": {root}", StringComparison.Ordinal));
    }

    private static IEnumerable<GeneratedRule> Subtrees(GeneratedRule rule)
    {
        yield return rule;
        foreach (GeneratedRule child in rule.Children)
        {
            foreach (GeneratedRule descendant in Subtrees(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Generates a random rule over terms <c>a</c>, <c>b</c>, <c>c</c> and the three constants, together with an
    /// evaluation built only from <see cref="K3Oracle"/>. Compound nodes are always parenthesised so the
    /// no-implicit-mixing rule cannot reject them.
    /// </summary>
    private static GeneratedRule GenerateRule(Random random, int depth)
    {
        if (depth == 0 || random.Next(5) == 0)
        {
            return GenerateLeaf(random);
        }

        GeneratedRule Child()
        {
            return GenerateRule(random, depth - 1);
        }

        switch (random.Next(17))
        {
            case 0:
                GeneratedRule operand = Child();
                return new GeneratedRule($"NOT ({operand.Text})", v => K3Oracle.Not(operand.Eval(v)), [operand]);
            case 1:
                GeneratedRule[] ands = [.. Enumerable.Range(0, random.Next(2, 4)).Select(_ => Child())];
                return new GeneratedRule(
                    $"({string.Join(" AND ", ands.Select(o => o.Text))})",
                    v => K3Oracle.And(ands.Select(o => o.Eval(v))),
                    ands
                );
            case 2:
                GeneratedRule[] ors = [.. Enumerable.Range(0, random.Next(2, 4)).Select(_ => Child())];
                return new GeneratedRule(
                    $"({string.Join(" OR ", ors.Select(o => o.Text))})",
                    v => K3Oracle.Or(ors.Select(o => o.Eval(v))),
                    ors
                );
            case 3:
                GeneratedRule xl = Child();
                GeneratedRule xr = Child();
                return new GeneratedRule($"({xl.Text} XOR {xr.Text})", v => K3Oracle.Xor(xl.Eval(v), xr.Eval(v)), [xl, xr]);
            case 4:
                GeneratedRule el = Child();
                GeneratedRule er = Child();
                return new GeneratedRule(
                    $"({el.Text} EQUIVALENT {er.Text})",
                    v => K3Oracle.Equivalent(el.Eval(v), er.Eval(v)),
                    [el, er]
                );
            case 5:
                GeneratedRule il = Child();
                GeneratedRule ir = Child();
                return new GeneratedRule(
                    $"({il.Text} IMPLIES {ir.Text})",
                    v => K3Oracle.Implies(il.Eval(v), ir.Eval(v)),
                    [il, ir]
                );
            case 9:
                GeneratedRule nl = Child();
                GeneratedRule nr = Child();
                return new GeneratedRule($"({nl.Text} NAND {nr.Text})", v => K3Oracle.Nand(nl.Eval(v), nr.Eval(v)), [nl, nr]);
            case 10:
                GeneratedRule rl = Child();
                GeneratedRule rr = Child();
                return new GeneratedRule($"({rl.Text} NOR {rr.Text})", v => K3Oracle.Nor(rl.Eval(v), rr.Eval(v)), [rl, rr]);
            case 11:
                GeneratedRule[] nxor = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"NXOR({string.Join(", ", nxor.Select(o => o.Text))})",
                    v => K3Oracle.Nxor([.. nxor.Select(o => o.Eval(v))]),
                    nxor
                );
            case 12:
                GeneratedRule[] anys = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"ANY({string.Join(", ", anys.Select(o => o.Text))})",
                    v => K3Oracle.Any([.. anys.Select(o => o.Eval(v))]),
                    anys
                );
            case 13:
                GeneratedRule[] alls = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"ALL({string.Join(", ", alls.Select(o => o.Text))})",
                    v => K3Oracle.All([.. alls.Select(o => o.Eval(v))]),
                    alls
                );
            case 14:
                GeneratedRule[] nones = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"NONE({string.Join(", ", nones.Select(o => o.Text))})",
                    v => K3Oracle.None([.. nones.Select(o => o.Eval(v))]),
                    nones
                );
            case 15:
                GeneratedRule[] betweenOperands = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                int min = random.Next(0, betweenOperands.Length + 1);
                int max = random.Next(min, betweenOperands.Length + 1);
                if (min == 0 && max == betweenOperands.Length)
                {
                    min = 1; // The full range is rejected as an always-true structural constant.
                }

                return new GeneratedRule(
                    $"BETWEEN({min}, {max}, {string.Join(", ", betweenOperands.Select(o => o.Text))})",
                    v => K3Oracle.Between(min, max, [.. betweenOperands.Select(o => o.Eval(v))]),
                    betweenOperands
                );
            case 16:
                GeneratedRule[] coalesced = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"COALESCE({string.Join(", ", coalesced.Select(o => o.Text))})",
                    v => K3Oracle.Coalesce([.. coalesced.Select(o => o.Eval(v))]),
                    coalesced
                );
            case 6:
                GeneratedRule[] exactlyOne = [.. Enumerable.Range(0, random.Next(2, 4)).Select(_ => Child())];
                return new GeneratedRule(
                    $"ExactlyOne({string.Join(", ", exactlyOne.Select(o => o.Text))})",
                    v => K3Oracle.ExactlyOne([.. exactlyOne.Select(o => o.Eval(v))]),
                    exactlyOne
                );
            default:
                (string name, Func<int, int, bool> satisfies) = Thresholds[random.Next(Thresholds.Length)];
                GeneratedRule[] operands = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                int k = random.Next(0, operands.Length + 1);
                return new GeneratedRule(
                    $"{name}({k}, {string.Join(", ", operands.Select(o => o.Text))})",
                    v => K3Oracle.Cardinality(count => satisfies(count, k), [.. operands.Select(o => o.Eval(v))]),
                    operands
                );
        }
    }

    private static GeneratedRule GenerateLeaf(Random random)
    {
        switch (random.Next(6))
        {
            case 0:
                return new GeneratedRule("TRUE", _ => TruthValue.True, []);
            case 1:
                return new GeneratedRule("FALSE", _ => TruthValue.False, []);
            case 2:
                return new GeneratedRule("UNKNOWN", _ => TruthValue.Unknown, []);
            default:
                int index = random.Next(3);
                return new GeneratedRule(((char)('a' + index)).ToString(), v => v[index], []);
        }
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );
    }

    private static RuleCompiler<RuleTestContext> CreateCompilerWithMaxAnalysisTerms(int maxAnalysisTerms)
    {
        return new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build(),
            new CompilerOptions(MaxAnalysisTerms: maxAnalysisTerms)
        );
    }

    /// <summary>A generated rule: its DSL text, an oracle-built evaluation over terms a..c, and its operands.</summary>
    private sealed record GeneratedRule(
        string Text,
        Func<IReadOnlyList<TruthValue>, TruthValue> Eval,
        IReadOnlyList<GeneratedRule> Children
    )
    {
        /// <summary>Whether the rule is True (resp. False) in every {True, False, Unknown} assignment of a, b, c.</summary>
        public (bool Tautology, bool Contradiction) Verdict()
        {
            bool always = true;
            bool never = true;
            foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
            {
                TruthValue value = this.Eval(assignment);
                always &= value == TruthValue.True;
                never &= value == TruthValue.False;
            }

            return (always, never);
        }
    }
}
