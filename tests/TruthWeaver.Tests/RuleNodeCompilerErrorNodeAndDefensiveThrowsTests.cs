namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 31: <c>RuleNodeCompiler</c>'s <see cref="ErrorNode"/> placeholder substitution, plus its
/// remaining defensive <c>default</c> throws (<c>Build</c>'s unhandled-rule-node-type branch and
/// <c>ValidThresholdRange</c>'s unhandled-comparison branch). Ticket 14 already covers this
/// compiler's malformed-tree diagnostics and default-argument substitution; this ticket is the
/// distinct <c>ErrorNode</c>/defensive-throw gaps left over from that pass. Reached by calling the
/// internal <c>RuleNodeCompiler.Compile</c> directly with a hand-built raw tree, the same way ticket
/// 14's tests reach it indirectly through <c>RuleCompiler</c> - here going straight to the seam is
/// necessary because the DSL parser never produces an <see cref="ErrorNode"/> without also raising a
/// front-end <see cref="DiagnosticSeverity.Error"/> diagnostic, and <c>RuleCompiler.CompileNode</c>
/// short-circuits before ever calling <c>RuleNodeCompiler.Build</c> when one is present.
/// </summary>
public sealed class RuleNodeCompilerErrorNodeAndDefensiveThrowsTests
{
    private static readonly PredicateRegistry<RuleTestContext> EmptyRegistry = PredicateRegistry<RuleTestContext>
        .CreateBuilder()
        .Build();

    [Fact]
    public void An_error_node_compiles_to_an_unknown_constant_without_throwing_and_the_surrounding_tree_still_compiles()
    {
        SourceSpan span = new(0, 1);
        AndNode tree = new([new ConstantNode(TruthValue.True, span), new ErrorNode(span)], span);

        (Expression? compiled, IReadOnlyList<Diagnostic> diagnostics) = RuleNodeCompiler<RuleTestContext>.Compile(
            tree,
            EmptyRegistry,
            CompilerOptions.Default
        );

        Assert.NotNull(compiled);
        Assert.Empty(diagnostics);
        AndExpression and = Assert.IsType<AndExpression>(compiled);
        Assert.Equal([new ConstantExpression(TruthValue.True), new ConstantExpression(TruthValue.Unknown)], and.Operands);
    }

    [Fact]
    public void Builds_unhandled_rule_node_type_default_branch_throws_naming_the_offending_type()
    {
        // No production front end (DSL/JSON/YAML) ever produces a RuleNode subtype Build's switch
        // doesn't already handle - this defensive throw guards only against that closed set (ADR-0003)
        // growing without the switch being updated. Exercised directly with a hand-built subtype
        // outside that set, the same "exercise it directly" option ticket 15 used for OperatorInfo's
        // ThresholdDescription default branch.
        SourceSpan span = new(0, 1);
        BogusRuleNode bogus = new(span);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            RuleNodeCompiler<RuleTestContext>.Compile(bogus, EmptyRegistry, CompilerOptions.Default)
        );

        Assert.Contains("Unhandled rule node type", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(BogusRuleNode), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidThresholdRanges_unhandled_comparison_default_branch_throws_naming_the_bogus_value()
    {
        // ThresholdComparison's five values (ADR-0004's closed set) are all handled by
        // ValidThresholdRange's switch; unlike OperatorInfo's ThresholdDescription (ticket 15), this
        // switch's discriminant is the Comparison carried directly on a hand-built ThresholdNode, so a
        // bogus value reaches it without needing reflection into a private member.
        SourceSpan span = new(0, 1);
        ThresholdNode bogus = new((ThresholdComparison)999, K: 0, Operands: [new ConstantNode(TruthValue.True, span)], span);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            RuleNodeCompiler<RuleTestContext>.Compile(bogus, EmptyRegistry, CompilerOptions.Default)
        );

        Assert.Contains("Unhandled threshold comparison", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A hand-built <see cref="RuleNode"/> subtype outside the closed set <c>Build</c>'s switch
    /// handles, used only to exercise its otherwise-unreachable <c>default</c> throw.
    /// </summary>
    private sealed record BogusRuleNode(SourceSpan Span) : RuleNode(Span);
}
