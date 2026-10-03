namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 06: canonical DSL printer and round-trip.</summary>
public sealed class CanonicalPrinterTests
{
    [Fact]
    public void Prints_the_adr_0003_worked_example_verbatim()
    {
        const string source =
            "hasRole(role: \"Y\") AND (hasTraining(training: \"Q\") OR hasTraining(training: \"Z\") OR (isManager XOR isDepartmentHead))";
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile(source).CompiledRule!;

        Assert.Equal(source, rule.CanonicalText);
    }

    [Fact]
    public void Xor_operand_is_always_parenthesized_even_when_unnecessary_for_precedence()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager XOR isDepartmentHead").CompiledRule!;

        Assert.Equal("(isManager XOR isDepartmentHead)", rule.CanonicalText);
    }

    [Theory]
    [InlineData("isManager ↑ isDepartmentHead", "(isManager NAND isDepartmentHead)")]
    [InlineData("isManager nor isDepartmentHead", "(isManager NOR isDepartmentHead)")]
    public void Nand_and_nor_are_always_parenthesized_and_printed_with_the_word_operator(string text, string expected)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile(text).CompiledRule!;

        Assert.Equal(expected, rule.CanonicalText);
    }

    [Fact]
    public void Implies_is_always_parenthesized_and_printed_with_the_word_operator()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager → isDepartmentHead").CompiledRule!;

        Assert.Equal("(isManager IMPLIES isDepartmentHead)", rule.CanonicalText);
    }

    [Fact]
    public void Equivalent_operand_is_always_parenthesized_even_when_unnecessary_for_precedence()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager XNOR isDepartmentHead").CompiledRule!;

        Assert.Equal("(isManager EQUIVALENT isDepartmentHead)", rule.CanonicalText);
    }

    [Fact]
    public void And_operand_of_or_is_parenthesized_for_clarity_even_though_precedence_makes_it_unambiguous()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler
            .Compile("isManager AND isDepartmentHead OR hasRole(role: \"Y\")")
            .CompiledRule!;

        Assert.Equal("(isManager AND isDepartmentHead) OR hasRole(role: \"Y\")", rule.CanonicalText);
    }

    [Theory]
    [InlineData("isManager AND isDepartmentHead")]
    [InlineData("isManager OR isDepartmentHead")]
    [InlineData("NOT isManager")]
    [InlineData("(isManager XOR isDepartmentHead)")]
    [InlineData("NXOR(isManager, isDepartmentHead, isManager)")]
    [InlineData("ANY(isManager, isDepartmentHead, isManager)")]
    [InlineData("ALL(isManager, isDepartmentHead, isManager)")]
    [InlineData("NONE(isManager, isDepartmentHead, isManager)")]
    [InlineData("BETWEEN(1, 2, isManager, isDepartmentHead, isManager)")]
    [InlineData("COALESCE(isManager, isDepartmentHead, isManager)")]
    [InlineData("If(isManager, isDepartmentHead, isManager)")]
    [InlineData("IsTrue(isManager)")]
    [InlineData("IsFalse(isManager AND isDepartmentHead)")]
    [InlineData("IsUnknown(isManager)")]
    [InlineData("IsKnown(NOT isManager)")]
    [InlineData("Project(isManager, True)")]
    [InlineData("Project(isManager AND isDepartmentHead, False)")]
    [InlineData("ExactlyOne(isManager, isDepartmentHead, isManager)")]
    [InlineData("AtLeast(2, isManager, isDepartmentHead, isManager)")]
    [InlineData("AtMost(1, isManager, isDepartmentHead, isManager)")]
    [InlineData("GreaterThan(1, isManager, isDepartmentHead, isManager)")]
    [InlineData("LessThan(2, isManager, isDepartmentHead, isManager)")]
    [InlineData("Exactly(2, isManager, isDepartmentHead, isManager)")]
    [InlineData("(isManager EQUIVALENT isDepartmentHead)")]
    [InlineData("hasRole(role: \"Y\")")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("isManager AND (isDepartmentHead OR hasRole(role: \"Y\"))")]
    [InlineData("(isManager AND isDepartmentHead) OR hasRole(role: \"Y\")")]
    public void Parse_print_round_trips_to_a_structurally_equal_tree(string source)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> original = compiler.Compile(source).CompiledRule!;
        CompiledRule<RuleTestContext> reparsed = compiler.Compile(original.CanonicalText).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void A_string_argument_with_an_embedded_quote_prints_with_the_quote_escaped()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile("hasRole(role: \"a\\\"b\")").CompiledRule!;

        Assert.Equal("hasRole(role: \"a\\\"b\")", rule.CanonicalText);
    }

    [Fact]
    public void A_string_argument_with_an_embedded_backslash_prints_with_the_backslash_escaped()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> rule = compiler.Compile("hasRole(role: \"a\\\\b\")").CompiledRule!;

        Assert.Equal("hasRole(role: \"a\\\\b\")", rule.CanonicalText);
    }

    [Fact]
    public void A_string_argument_with_both_a_quote_and_a_backslash_round_trips_to_a_structurally_equal_tree()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> original = compiler.Compile("hasRole(role: \"a\\\"\\\\b\")").CompiledRule!;
        CompiledRule<RuleTestContext> reparsed = compiler.Compile(original.CanonicalText).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void A_datetimeoffset_argument_prints_as_a_quoted_string_and_round_trips_to_a_structurally_equal_tree()
    {
        DateTimeOffset matchValue = new(2024, 3, 17, 9, 30, 0, TimeSpan.FromHours(-5));
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddDateTimeOffsetArgPredicate("startedAt", "when", matchValue)
                .Build()
        );

        CompiledRule<RuleTestContext> original = compiler.Compile($"startedAt(when: \"{matchValue:O}\")").CompiledRule!;

        Assert.Equal($"startedAt(when: \"{matchValue:O}\")", original.CanonicalText);

        CompiledRule<RuleTestContext> reparsed = compiler.Compile(original.CanonicalText).CompiledRule!;
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void Printing_the_same_tree_twice_is_byte_identical()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;

        Assert.Equal(rule.CanonicalText, rule.CanonicalText);
    }

    [Fact]
    public void ToString_returns_the_same_text_as_canonical_text()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;

        Assert.Equal(rule.CanonicalText, rule.ToString());
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddStringArgPredicate("hasTraining", "training", "Q")
                .AddConstant("isManager", true)
                .AddConstant("isDepartmentHead", true)
                .Build()
        );
    }
}
