namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 21: the default printed form uses parentheses only, and <see cref="GroupingStyle.DepthCycling"/> is an opt-in
/// rendering that varies the delimiter by nesting depth and still re-parses to an equal tree (ADR-0005 decision 9).
/// </summary>
public sealed class DelimiterRenderingTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .AddConstant("d", true)
            .AddConstant("e", true)
            .AddConstant("f", true)
            .AddConstant("g", true)
            .Build()
    );

    /// <summary>However a rule was written, its canonical text uses parentheses and no other grouping delimiter.</summary>
    [Fact]
    public void CanonicalText_RuleWrittenWithMixedDelimiters_UsesParenthesesOnly_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND [b OR {c AND d}]");

        Assert.Equal("a AND (b OR (c AND d))", rule.CanonicalText);
    }

    /// <summary>The parentheses style is the canonical text itself.</summary>
    [Fact]
    public void PrintText_ParenthesesStyle_EqualsCanonicalText_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND [b OR {c AND d}]");

        Assert.Equal(rule.CanonicalText, rule.PrintRuleText(GroupingStyle.Parentheses));
    }

    /// <summary>Depth cycling uses ( at depth 0, [ at depth 1, { at depth 2, then repeats.</summary>
    [Fact]
    public void PrintText_DepthCyclingStyle_CyclesParenthesesBracketsBracesByDepth_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR (c AND (d OR (e AND (f OR g)))))");

        string text = rule.PrintRuleText(GroupingStyle.DepthCycling);

        Assert.Equal("a AND (b OR [c AND {d OR (e AND [f OR g])}])", text);
    }

    /// <summary>Function-call argument lists are call syntax and keep parentheses; groups inside them still start at depth 0.</summary>
    [Fact]
    public void PrintText_DepthCyclingStyle_KeepsCallArgumentListsInParentheses_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("ANY(a AND (b OR c), d XOR (e AND (f OR g)))");

        string text = rule.PrintRuleText(GroupingStyle.DepthCycling);

        Assert.Equal("ANY(a AND (b OR c), (d XOR [e AND {f OR g}]))", text);
    }

    /// <summary>The depth-cycled text re-parses to a tree equal to the original, and prints the same canonical text.</summary>
    [Theory]
    [InlineData("a AND (b OR (c AND (d OR (e AND (f OR g)))))")]
    [InlineData("NOT (a OR (b XOR (c AND (d OR e))))")]
    [InlineData("(a XOR b) EQUIVALENT (c IMPLIES (d NAND (e NOR f)))")]
    [InlineData("a")]
    [InlineData("(a AND b) OR (c AND d) OR NOT (e OR f)")]
    public void PrintText_DepthCyclingStyle_ReparsesToAnEqualTree_Test(string text)
    {
        CompiledRule<RuleTestContext> rule = Compile(text);

        CompiledRule<RuleTestContext> reparsed = Compile(rule.PrintRuleText(GroupingStyle.DepthCycling));

        Assert.Equal(rule.Root, reparsed.Root);
        Assert.Equal(rule.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>A rule with no groups prints identically in every style.</summary>
    [Fact]
    public void PrintText_DepthCyclingStyleWithoutGroups_EqualsCanonicalText_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b AND NOT c");

        Assert.Equal(rule.CanonicalText, rule.PrintRuleText(GroupingStyle.DepthCycling));
    }

    private static CompiledRule<RuleTestContext> Compile(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return result.CompiledRule!;
    }
}
