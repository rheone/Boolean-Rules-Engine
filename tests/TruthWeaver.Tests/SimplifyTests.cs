namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <c>CompiledRule.Simplify</c> (ticket 27, ADR-0005 decision 10) replaces a rule with an equivalent, never larger one using
/// only Strong Kleene-sound rewrites. Classical-only laws such as <c>a OR NOT a = True</c> are deliberately not applied.
/// </summary>
public sealed class SimplifyTests
{
    /// <summary>
    /// For many generated rules (half the leaves are constants) and every {True, False, Unknown} assignment, the simplified
    /// rule evaluates like the rule and the oracle, is never larger, and simplifying it again changes nothing.
    /// </summary>
    [Fact]
    public async Task Simplify_GeneratedRules_EvaluateEqualToOriginalAreNoLargerAndIdempotent_Test()
    {
        Random random = new(20261301);
        List<string> failures = [];
        int checkedRules = 0;
        int shrunk = 0;

        for (int i = 0; i < 900; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 3);
            K3Rule? original = K3Rule.TryCreate(generated.Text, 3);
            if (original is null)
            {
                continue;
            }

            K3Rule simplified = original.Rewrite(rule => rule.Simplify());
            checkedRules++;
            int before = RuleMetrics.NodeCount(original.Compiled);
            int after = RuleMetrics.NodeCount(simplified.Compiled);
            shrunk += after < before ? 1 : 0;
            if (after > before)
            {
                failures.Add($"{generated.Text}: simplified is larger ({simplified.Compiled.CanonicalText})");
            }

            if (simplified.Compiled.CanonicalText != simplified.Compiled.Simplify().CanonicalText)
            {
                failures.Add($"{generated.Text}: not idempotent ({simplified.Compiled.CanonicalText})");
            }

            foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
            {
                Decision original3 = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                Decision simplified3 = await simplified.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                TruthValue oracle = generated.Eval(assignment);
                if (original3.Result != simplified3.Result || simplified3.Result != oracle)
                {
                    failures.Add(
                        $"{generated.Text} @ [{string.Join(",", assignment)}]: original={original3.Result} simplified={simplified3.Result} oracle={oracle} ({simplified.Compiled.CanonicalText})"
                    );
                }
            }
        }

        Assert.Empty(failures);
        Assert.True(checkedRules > 600, $"only {checkedRules} rules were checked");
        Assert.True(shrunk > 100, $"only {shrunk} rules actually shrank");
    }

    /// <summary>
    /// Simplifying an expanded rule (every derived operator written in primitives, constants everywhere) still evaluates like
    /// the original and the oracle and is never larger than the expansion.
    /// </summary>
    [Fact]
    public async Task Simplify_ExpandedGeneratedRules_EvaluateEqualToOriginalAndAreNoLargerThanTheExpansion_Test()
    {
        Random random = new(20261302);
        List<string> failures = [];
        int checkedRules = 0;

        for (int i = 0; i < 500; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 3);
            K3Rule? original = K3Rule.TryCreate(generated.Text, 3);
            if (original is null)
            {
                continue;
            }

            K3Rule expanded = original.Rewrite(rule => rule.ExpandToPrimitives());
            K3Rule simplified = expanded.Rewrite(rule => rule.Simplify());
            checkedRules++;
            if (RuleMetrics.NodeCount(simplified.Compiled) > RuleMetrics.NodeCount(expanded.Compiled))
            {
                failures.Add($"{generated.Text}: simplified expansion is larger");
            }

            foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
            {
                Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                Decision after = await simplified.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                TruthValue oracle = generated.Eval(assignment);
                if (before.Result != after.Result || after.Result != oracle)
                {
                    failures.Add(
                        $"{generated.Text} @ [{string.Join(",", assignment)}]: original={before.Result} simplified={after.Result} oracle={oracle}"
                    );
                }
            }
        }

        Assert.Empty(failures);
        Assert.True(checkedRules > 350, $"only {checkedRules} rules were checked");
    }

    /// <summary>Each K3-sound rewrite, applied to a small rule, gives the stated simplified text and the same value.</summary>
    [Theory]
    [InlineData("a AND True", 1, "a")]
    [InlineData("a AND False", 1, "False")]
    [InlineData("a OR False", 1, "a")]
    [InlineData("a OR True", 1, "True")]
    [InlineData("a AND b AND True", 2, "a AND b")]
    [InlineData("a AND Unknown", 1, "Unknown AND a")]
    [InlineData("a OR Unknown", 1, "Unknown OR a")]
    [InlineData("Unknown AND False", 0, "False")]
    [InlineData("True AND Unknown", 0, "Unknown")]
    [InlineData("Unknown OR Unknown", 0, "Unknown")]
    [InlineData("NOT True", 0, "False")]
    [InlineData("NOT Unknown", 0, "Unknown")]
    [InlineData("a AND a", 1, "a")]
    [InlineData("a AND (b AND a)", 2, "a AND b")]
    [InlineData("NOT NOT a", 1, "a")]
    [InlineData("a AND (a OR b)", 2, "a")]
    [InlineData("a OR (a AND b)", 2, "a")]
    [InlineData("NOT (NOT a AND NOT b)", 2, "a OR b")]
    [InlineData("NOT (NOT a OR NOT b)", 2, "a AND b")]
    [InlineData("NOT a NAND NOT b", 2, "a OR b")]
    [InlineData("NOT a NOR NOT b", 2, "a AND b")]
    [InlineData("NOT a IMPLIES b", 2, "a OR b")]
    [InlineData("COALESCE(True, a)", 1, "True")]
    [InlineData("COALESCE(Unknown, a)", 1, "a")]
    [InlineData("COALESCE(a, Unknown)", 1, "a")]
    [InlineData("COALESCE(a, True, b)", 2, "COALESCE(a, True)")]
    [InlineData("COALESCE(IsKnown(a), b)", 2, "IsKnown(a)")]
    [InlineData("Project(True, False)", 0, "True")]
    [InlineData("Project(Unknown, True)", 0, "True")]
    [InlineData("Project(IsTrue(a), False)", 1, "IsTrue(a)")]
    [InlineData("IsKnown(True)", 0, "True")]
    [InlineData("IsUnknown(Unknown)", 0, "True")]
    [InlineData("IsTrue(Unknown)", 0, "False")]
    [InlineData("IsFalse(False)", 0, "True")]
    [InlineData("IsKnown(IsTrue(a))", 1, "True")]
    [InlineData("IsUnknown(IsTrue(a))", 1, "False")]
    [InlineData("IsTrue(IsFalse(a))", 1, "IsFalse(a)")]
    [InlineData("IsTrue(NOT a)", 1, "IsFalse(a)")]
    [InlineData("NOT IsKnown(a)", 1, "IsUnknown(a)")]
    [InlineData("If(True, a, b)", 2, "a")]
    [InlineData("If(False, a, b)", 2, "b")]
    [InlineData("If(c, a, a)", 3, "a")]
    [InlineData("If(c, True, a)", 3, "a OR c")]
    [InlineData("True IMPLIES a", 1, "a")]
    [InlineData("a IMPLIES True", 1, "True")]
    [InlineData("False IMPLIES a", 1, "True")]
    [InlineData("a IMPLIES False", 1, "NOT a")]
    [InlineData("a XOR False", 1, "a")]
    [InlineData("a XOR True", 1, "NOT a")]
    [InlineData("a EQUIVALENT True", 1, "a")]
    [InlineData("a EQUIVALENT False", 1, "NOT a")]
    [InlineData("a NAND True", 1, "NOT a")]
    [InlineData("a NAND False", 1, "True")]
    [InlineData("a NOR False", 1, "NOT a")]
    [InlineData("a NOR True", 1, "False")]
    [InlineData("NOT a XOR NOT b", 2, "(a XOR b)")]
    [InlineData("NOT a XOR b", 2, "(a EQUIVALENT b)")]
    [InlineData("NOT a EQUIVALENT b", 2, "(a XOR b)")]
    [InlineData("AtLeast(2, True, a, b)", 2, "a OR b")]
    [InlineData("AtLeast(1, True, a, b)", 2, "True")]
    [InlineData("AtLeast(3, False, a, b)", 2, "False")]
    [InlineData("AtMost(1, True, a, b)", 2, "AtMost(0, a, b)")]
    [InlineData("AtMost(0, True, a, b)", 2, "False")]
    [InlineData("Exactly(1, False, a, b)", 2, "Exactly(1, a, b)")]
    [InlineData("Exactly(2, True, True, a)", 1, "NOT a")]
    [InlineData("AtLeast(1, Unknown, Unknown)", 0, "Unknown")]
    [InlineData("AtLeast(1, a)", 1, "a")]
    [InlineData("Exactly(0, a)", 1, "NOT a")]
    [InlineData("a AND b", 2, "a AND b")]
    public async Task Simplify_SmallRule_GivesTheStatedSimplifiedText_Test(string ruleText, int arity, string expected)
    {
        K3Rule original = K3Rule.TryCreate(ruleText, Math.Max(arity, 1))!;
        K3Rule simplified = original.Rewrite(rule => rule.Simplify());

        Assert.Equal(expected, simplified.Compiled.CanonicalText);
        foreach (TruthValue[] assignment in K3Oracle.Assignments(Math.Max(arity, 1)))
        {
            Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision after = await simplified.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(before.Result, after.Result);
        }
    }

    /// <summary>
    /// Classical-only laws are never applied: each of these is a tautology, contradiction or reduction in two-valued logic but
    /// is not in Strong Kleene logic. The simplified rule keeps its value (including <c>Unknown</c> where the operands are),
    /// agrees with the oracle everywhere, and is not collapsed to a constant.
    /// </summary>
    [Theory]
    [InlineData("a OR NOT a", 1)]
    [InlineData("NOT a OR a OR b", 2)]
    [InlineData("a AND NOT a", 1)]
    [InlineData("a IMPLIES a", 1)]
    [InlineData("a EQUIVALENT a", 1)]
    [InlineData("a XOR a", 1)]
    [InlineData("a AND (NOT a OR b)", 2)]
    [InlineData("a OR (NOT a AND b)", 2)]
    [InlineData("AtLeast(1, a, NOT a)", 1)]
    [InlineData("If(a, NOT a, a)", 1)]
    public async Task Simplify_ClassicalOnlyLaw_IsNotAppliedAndTheOracleStillAgrees_Test(string ruleText, int arity)
    {
        K3Rule original = K3Rule.TryCreate(ruleText, arity)!;
        K3Rule simplified = original.Rewrite(rule => rule.Simplify());

        Assert.NotEqual("True", simplified.Compiled.CanonicalText);
        Assert.NotEqual("False", simplified.Compiled.CanonicalText);
        foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
        {
            Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision after = await simplified.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(before.Result, after.Result);
        }
    }

    /// <summary>
    /// The textbook classical reductions that Strong Kleene logic rejects are not applied: <c>a AND (NOT a OR b)</c> is not
    /// <c>a AND b</c> and <c>a OR (NOT a AND b)</c> is not <c>a OR b</c> (they differ when <c>a</c> is <c>Unknown</c>).
    /// </summary>
    [Theory]
    [InlineData("a AND (NOT a OR b)", "a AND b")]
    [InlineData("a OR (NOT a AND b)", "a OR b")]
    public void Simplify_ComplementAbsorption_IsNotRewrittenToTheClassicalForm_Test(string ruleText, string classical)
    {
        K3Rule original = K3Rule.TryCreate(ruleText, 2)!;

        string simplified = original.Compiled.Simplify().CanonicalText;

        Assert.NotEqual(K3Rule.TryCreate(classical, 2)!.Compiled.Canonicalize().CanonicalText, simplified);
    }

    /// <summary>The simplified rule is a new rule: the original keeps its text and the declared Collapse policy survives.</summary>
    [Fact]
    public void Simplify_Rule_LeavesTheOriginalUntouchedAndKeepsTheCollapsePolicy_Test()
    {
        K3Rule original = K3Rule.TryCreate("Collapse(a AND True, UnknownAsFalse)", 1)!;
        string textBefore = original.Compiled.CanonicalText;

        CompiledRule<RuleTestContext> simplified = original.Compiled.Simplify();

        Assert.NotSame(original.Compiled, simplified);
        Assert.Equal(textBefore, original.Compiled.CanonicalText);
        Assert.Equal(CollapsePolicy.UnknownAsFalse, simplified.CollapsePolicy);
        Assert.Equal("Collapse(a, UnknownAsFalse)", simplified.CanonicalText);
    }
}
