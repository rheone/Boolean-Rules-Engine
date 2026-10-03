namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <c>CompiledRule.CompressToDerived</c> (ticket 25, ADR-0005 decision 10) rewrites an expanded rule back into readable
/// derived operators wherever a K3-sound pattern matches, without changing what the rule evaluates to and without growing it.
/// </summary>
public sealed class CompressToDerivedTests
{
    /// <summary>
    /// For many generated rules and every {True, False, Unknown} assignment, compressing the expansion of a rule evaluates
    /// like the rule and the oracle, and is never larger than the expansion.
    /// </summary>
    [Fact]
    public async Task CompressToDerived_ExpandedGeneratedRules_EvaluateEqualToOriginalAndAreNoLargerThanTheExpansion_Test()
    {
        Random random = new(20261101);
        List<string> failures = [];
        int checkedRules = 0;

        for (int i = 0; i < 600; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 3);
            K3Rule? original = K3Rule.TryCreate(generated.Text, 3);
            if (original is null)
            {
                continue;
            }

            K3Rule expanded = original.Rewrite(rule => rule.ExpandToPrimitives());
            K3Rule compressed = expanded.Rewrite(rule => rule.CompressToDerived());
            checkedRules++;
            if (RuleMetrics.NodeCount(compressed.Compiled) > RuleMetrics.NodeCount(expanded.Compiled))
            {
                failures.Add($"{generated.Text}: compressed is larger ({compressed.Compiled.CanonicalText})");
            }

            foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
            {
                Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                Decision after = await compressed.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                TruthValue oracle = generated.Eval(assignment);
                if (before.Result != after.Result || after.Result != oracle)
                {
                    failures.Add(
                        $"{generated.Text} @ [{string.Join(",", assignment)}]: original={before.Result} compressed={after.Result} oracle={oracle}"
                    );
                }
            }
        }

        Assert.Empty(failures);
        Assert.True(checkedRules > 400, $"only {checkedRules} rules were checked");
    }

    /// <summary>Compressing twice gives the same rule as compressing once, whatever the generated rule contains.</summary>
    [Fact]
    public void CompressToDerived_GeneratedRules_IsIdempotent_Test()
    {
        Random random = new(20261102);
        int checkedRules = 0;

        for (int i = 0; i < 600; i++)
        {
            K3Rule? original = K3Rule.TryCreate(K3RuleGenerator.GenerateRule(random, depth: 3).Text, 3);
            if (original is null)
            {
                continue;
            }

            CompiledRule<RuleTestContext> once = original.Compiled.ExpandToPrimitives().CompressToDerived();
            CompiledRule<RuleTestContext> twice = once.CompressToDerived();
            checkedRules++;

            Assert.Equal(once.CanonicalText, twice.CanonicalText);
        }

        Assert.True(checkedRules > 400, $"only {checkedRules} rules were checked");
    }

    /// <summary>
    /// Every pattern the expander produces is recognised again: the expansion of each derived operator, written alone,
    /// compresses to the stated derived form, which evaluates like the original for every assignment.
    /// </summary>
    [Theory]
    [InlineData("a IMPLIES b", 2, "(a IMPLIES b)")]
    [InlineData("a XOR b", 2, "(a XOR b)")]
    [InlineData("a EQUIVALENT b", 2, "(a EQUIVALENT b)")]
    [InlineData("a NAND b", 2, "(a NAND b)")]
    [InlineData("a NOR b", 2, "(a NOR b)")]
    [InlineData("NXOR(a, b, c)", 3, "NXOR(a, b, c)")]
    [InlineData("NXOR(a, b, c, d)", 4, "NXOR(a, b, c, d)")]
    [InlineData("ANY(a, b, c)", 3, "ANY(a, b, c)")]
    [InlineData("ALL(a, b, c)", 3, "ALL(a, b, c)")]
    [InlineData("NONE(a, b, c)", 3, "NONE(a, b, c)")]
    [InlineData("ExactlyOne(a, b, c)", 3, "ExactlyOne(a, b, c)")]
    [InlineData("BETWEEN(1, 2, a, b, c)", 3, "BETWEEN(1, 2, a, b, c)")]
    [InlineData("If(a, b, c)", 3, "If(a, b, c)")]
    [InlineData("Project(a, True)", 1, "Project(a, True)")]
    [InlineData("Project(a, False)", 1, "Project(a, False)")]
    [InlineData("IsFalse(a)", 1, "IsFalse(a)")]
    [InlineData("IsUnknown(a)", 1, "IsUnknown(a)")]
    [InlineData("IsKnown(a)", 1, "IsKnown(a)")]
    public async Task CompressToDerived_ExpandedOperator_IsRecognisedAgain_Test(string ruleText, int arity, string expected)
    {
        K3Rule original = K3Rule.TryCreate(ruleText, arity)!;
        K3Rule compressed = original.Rewrite(rule => rule.ExpandToPrimitives().CompressToDerived());

        Assert.Equal(expected, compressed.Compiled.CanonicalText);
        foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
        {
            Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision after = await compressed.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(before.Result, after.Result);
        }
    }

    /// <summary>
    /// Primitive patterns written by hand (not produced by the expander) compress to the stated derived form and keep their
    /// value under every assignment.
    /// </summary>
    [Theory]
    [InlineData("NOT a OR b", 2, "(a IMPLIES b)")]
    [InlineData("b OR NOT a", 2, "(a IMPLIES b)")]
    [InlineData("NOT a OR NOT b", 2, "(a NAND b)")]
    [InlineData("NOT a AND NOT b", 2, "(a NOR b)")]
    [InlineData("NOT (a AND b)", 2, "(a NAND b)")]
    [InlineData("NOT (a OR b)", 2, "(a NOR b)")]
    [InlineData("AtLeast(1, a, b, c)", 3, "ANY(a, b, c)")]
    [InlineData("AtLeast(3, a, b, c)", 3, "ALL(a, b, c)")]
    [InlineData("AtMost(0, a, b, c)", 3, "NONE(a, b, c)")]
    [InlineData("Exactly(1, a, b, c)", 3, "ExactlyOne(a, b, c)")]
    [InlineData("NOT AtLeast(1, a, b, c)", 3, "NONE(a, b, c)")]
    [InlineData("AtLeast(1, a, b, c) AND AtMost(2, a, b, c)", 3, "BETWEEN(1, 2, a, b, c)")]
    [InlineData("COALESCE(a, True)", 1, "Project(a, True)")]
    [InlineData("COALESCE(a, False)", 1, "Project(a, False)")]
    [InlineData("COALESCE(NOT a, False)", 1, "IsFalse(a)")]
    public async Task CompressToDerived_PrimitivePattern_BecomesTheDerivedForm_Test(string ruleText, int arity, string expected)
    {
        K3Rule original = K3Rule.TryCreate(ruleText, arity)!;
        K3Rule compressed = original.Rewrite(rule => rule.CompressToDerived());

        Assert.Equal(expected, compressed.Compiled.CanonicalText);
        foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
        {
            Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision after = await compressed.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(before.Result, after.Result);
        }
    }

    /// <summary>
    /// Shapes that look like a derived operator but are not one in Strong Kleene logic stay as written (the classical
    /// <c>a OR NOT a</c> is not an implication of anything, and an unmatched threshold pair is not a range).
    /// </summary>
    [Theory]
    [InlineData("a OR b", 2)]
    [InlineData("a AND NOT a", 1)]
    [InlineData("AtLeast(2, a, b, c) AND AtMost(1, a, b, c)", 3)]
    [InlineData("AtLeast(2, a, b, c) AND AtMost(1, a, b, a)", 3)]
    [InlineData("COALESCE(a, b)", 2)]
    [InlineData("COALESCE(a, False, b)", 2)]
    public async Task CompressToDerived_NonMatchingShape_IsLeftEquivalent_Test(string ruleText, int arity)
    {
        K3Rule original = K3Rule.TryCreate(ruleText, arity)!;
        K3Rule compressed = original.Rewrite(rule => rule.CompressToDerived());

        foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
        {
            Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision after = await compressed.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(before.Result, after.Result);
        }

        Assert.Equal(original.Compiled.CanonicalText, compressed.Compiled.CanonicalText);
    }

    /// <summary>The compressed rule is a new rule: the original keeps its text and the declared Collapse policy survives.</summary>
    [Fact]
    public void CompressToDerived_Rule_LeavesTheOriginalUntouchedAndKeepsTheCollapsePolicy_Test()
    {
        K3Rule original = K3Rule.TryCreate("Collapse(NOT a OR b, UnknownAsFalse)", 2)!;
        string textBefore = original.Compiled.CanonicalText;

        CompiledRule<RuleTestContext> compressed = original.Compiled.CompressToDerived();

        Assert.NotSame(original.Compiled, compressed);
        Assert.Equal(textBefore, original.Compiled.CanonicalText);
        Assert.Equal(CollapsePolicy.UnknownAsFalse, compressed.CollapsePolicy);
        Assert.Equal("Collapse((a IMPLIES b), UnknownAsFalse)", compressed.CanonicalText);
    }
}
