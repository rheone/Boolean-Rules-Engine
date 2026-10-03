namespace TruthWeaver.Tests;

using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>Ticket 04: direct unit tests for the DSL recursive-descent parser.</summary>
public sealed class DslParserTests
{
    [Fact]
    public void Trailing_garbage_after_a_complete_expression_raises_a_diagnostic_and_still_parses_the_valid_prefix()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("a AND b c");

        Assert.IsType<AndNode>(root);
        Assert.Contains(
            diagnostics,
            d => d.Message.Contains("Unexpected token") && d.Message.Contains("after end of expression")
        );
    }

    [Fact]
    public void Mixing_xor_with_and_or_without_parentheses_raises_ambiguous_operator_mixing()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("a XOR b AND c");

        Assert.Contains(diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    [Fact]
    public void Parenthesizing_the_xor_chain_avoids_ambiguous_operator_mixing()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("(a XOR b) AND c");

        Assert.DoesNotContain(diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    [Fact]
    public void Mixing_xor_and_xnor_at_the_same_chain_level_raises_the_specific_message()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("a XOR b XNOR c");

        Assert.Contains(diagnostics, d => d.Message.Contains("Mixing XOR with EQUIVALENT"));
    }

    [Theory]
    [InlineData("a XOR b XOR c")]
    [InlineData("a XNOR b XNOR c")]
    public void A_chain_using_only_xor_or_only_xnor_does_not_raise_the_mixing_message(string source)
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(source);

        Assert.DoesNotContain(diagnostics, d => d.Message.Contains("Mixing XOR with EQUIVALENT"));
    }

    [Theory]
    [InlineData(",")]
    [InlineData(")")]
    public void An_input_starting_with_neither_a_term_constant_nor_open_paren_raises_the_expected_term_diagnostic(string source)
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(source);

        Assert.IsType<ErrorNode>(root);

        // Exactly one diagnostic (rather than several) proves parsing reached Eof and stopped
        // instead of looping back into the same fallback error path.
        Diagnostic diagnostic = Assert.Single(diagnostics);
        Assert.Contains("Expected a term, constant, or '('", diagnostic.Message);
    }

    [Fact]
    public void Threshold_omitting_the_integer_first_argument_raises_a_diagnostic_and_defaults_k_to_zero()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("AtLeast(a, b)");

        ThresholdNode threshold = Assert.IsType<ThresholdNode>(root);
        Assert.Equal(0, threshold.K);
        Assert.Contains(diagnostics, d => d.Message.Contains("Expected an integer threshold as AtLeast's first argument"));
    }

    [Fact]
    public void A_term_argument_with_a_missing_colon_raises_expected_colon()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("f(x 1)");

        Assert.Contains(diagnostics, d => d.Message.Contains("Expected ':'"));
    }

    [Fact]
    public void A_literal_position_holding_a_bare_identifier_raises_expected_a_literal_value_and_recovers_with_false()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("f(x: y)");

        TermNode term = Assert.IsType<TermNode>(root);
        RawLiteral value = term.Arguments[0].Value;
        Assert.Equal(RawLiteralForm.Boolean, value.Form);
        Assert.False(value.BooleanValue);
        Assert.Contains(diagnostics, d => d.Message.Contains("Expected a literal value"));
    }

    [Fact]
    public void An_empty_array_literal_parses_to_a_zero_element_array()
    {
        (RuleNode root, IReadOnlyList<Diagnostic> _) = DslParser.Parse("f(x: [])");

        TermNode term = Assert.IsType<TermNode>(root);
        RawLiteral value = term.Arguments[0].Value;
        Assert.Equal(RawLiteralForm.Array, value.Form);
        Assert.Empty(value.Elements!);
    }

    [Fact]
    public void An_array_literal_missing_the_closing_bracket_raises_expected_bracket()
    {
        (RuleNode _, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse("f(x: [1, 2)");

        Assert.Contains(diagnostics, d => d.Message.Contains("Expected ']'"));
    }

    [Theory]
    [InlineData("and")]
    [InlineData("And")]
    [InlineData("AND")]
    [InlineData("xnor")]
    [InlineData("EXACTLYONE")]
    public void IsReservedWord_returns_true_for_every_keyword_case_insensitively(string keyword)
    {
        Assert.True(DslParser.IsReservedWord(keyword));
    }

    [Fact]
    public void IsReservedWord_returns_false_for_an_arbitrary_predicate_name()
    {
        Assert.False(DslParser.IsReservedWord("hasRole"));
    }
}
