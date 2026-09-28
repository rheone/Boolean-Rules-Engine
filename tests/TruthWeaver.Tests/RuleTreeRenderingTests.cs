namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintMermaid(bool)"/>/<see cref="CompiledRule{TContext}.PrintPlainText(bool)"/>
/// — structure-only and evaluation-colored tree rendering, for delivery to a diagram UI or a log.
/// </summary>
public sealed class RuleTreeRenderingTests
{
    [Fact]
    public void Structural_mermaid_output_has_no_evaluation_coloring()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );

        string mermaid = rule.PrintMermaid();

        Assert.StartsWith("flowchart TD", mermaid);
        Assert.Contains("AND", mermaid);
        Assert.Contains("-->", mermaid);
        Assert.DoesNotContain("classDef", mermaid);
    }

    [Fact]
    public void Mermaid_output_always_includes_a_start_marker_pointing_at_the_root()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );

        string mermaid = rule.PrintMermaid();

        Assert.Contains("Start([\"Start\"]) --> n0", mermaid);
    }

    [Fact]
    public void Mermaid_output_includes_a_terms_argument_values_by_default()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "hasCrust(crust: \"thin\")",
            registry => registry.AddStringArgPredicate("hasCrust", "crust", "thin")
        );

        string mermaid = rule.PrintMermaid();

        Assert.Contains("crust: #quot;thin#quot;", mermaid);
    }

    [Fact]
    public void Mermaid_output_omits_argument_values_when_disabled()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "hasCrust(crust: \"thin\")",
            registry => registry.AddStringArgPredicate("hasCrust", "crust", "thin")
        );

        string mermaid = rule.PrintMermaid(showArgumentValues: false);

        Assert.DoesNotContain("crust", mermaid);
    }

    [Fact]
    public void PlainText_output_includes_a_terms_argument_values_by_default()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "hasCrust(crust: \"thin\")",
            registry => registry.AddStringArgPredicate("hasCrust", "crust", "thin")
        );

        string text = rule.PrintPlainText();

        Assert.Contains("crust: \"thin\"", text);
    }

    [Fact]
    public void PlainText_output_omits_argument_values_when_disabled()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "hasCrust(crust: \"thin\")",
            registry => registry.AddStringArgPredicate("hasCrust", "crust", "thin")
        );

        string text = rule.PrintPlainText(showArgumentValues: false);

        Assert.DoesNotContain("crust: \"thin\"", text);
    }

    [Fact]
    public void Argument_values_are_absent_for_zero_argument_terms()
    {
        CompiledRule<RuleTestContext> rule = Compile("a", registry => registry.AddConstant("a", true));

        string mermaid = rule.PrintMermaid();
        string text = rule.PrintPlainText();

        Assert.DoesNotContain("(", mermaid.Replace("Start([\"Start\"])", string.Empty));
        Assert.DoesNotContain("(", text);
    }

    [Fact]
    public async Task Evaluated_mermaid_output_colors_true_false_and_skipped_nodes()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", false).AddConstant("b", true)
        );

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string mermaid = rule.PrintMermaid(decision);

        Assert.Contains("classDef brTrue", mermaid);
        Assert.Contains("classDef brFalse", mermaid);
        Assert.Contains("classDef brSkipped", mermaid);
        Assert.Contains("brFalse", mermaid);
        Assert.Contains("brSkipped", mermaid);
    }

    [Fact]
    public void PrintMermaid_rejects_a_decision_with_no_evaluated_tree()
    {
        CompiledRule<RuleTestContext> rule = Compile("a", registry => registry.AddConstant("a", true));
        Decision decisionWithoutTree = new(TruthValue.True, []);

        Assert.Throws<ArgumentException>(() => rule.PrintMermaid(decisionWithoutTree));
    }

    [Fact]
    public void Structural_plain_text_output_has_no_evaluation_suffixes()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );

        string text = rule.PrintPlainText();

        Assert.Contains("AND", text);
        Assert.DoesNotContain("[true]", text);
        Assert.DoesNotContain("[skipped]", text);
    }

    [Fact]
    public async Task Evaluated_plain_text_output_marks_the_short_circuited_operand_as_skipped()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", false).AddConstant("b", true)
        );

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string text = rule.PrintPlainText(decision);

        Assert.Contains("[false]", text);
        Assert.Contains("[skipped]", text);
        Assert.DoesNotContain("[true]", text);
    }

    [Theory]
    [InlineData(OperatorStyle.Word, "AND", "OR", "NOT", "XOR", "XNOR")]
    [InlineData(OperatorStyle.Symbolic, "∧", "∨", "¬", "⊕", "↔")]
    [InlineData(OperatorStyle.CStyle, "&&", "||", "!", "^", "==")]
    public void PlainText_renders_operators_in_the_requested_style(
        OperatorStyle style,
        string and,
        string or,
        string not,
        string xor,
        string xnor
    )
    {
        Assert.Contains(and, PlainTextTreePrinter.Print(BinaryNode("AND"), style));
        Assert.Contains(or, PlainTextTreePrinter.Print(BinaryNode("OR"), style));
        Assert.Contains(not, PlainTextTreePrinter.Print(UnaryNode("NOT"), style));
        Assert.Contains(xor, PlainTextTreePrinter.Print(BinaryNode("XOR"), style));
        Assert.Contains(xnor, PlainTextTreePrinter.Print(BinaryNode("XNOR"), style));
    }

    [Theory]
    [InlineData(OperatorStyle.Word, "AND", "OR", "NOT", "XOR", "XNOR")]
    [InlineData(OperatorStyle.Symbolic, "∧", "∨", "¬", "⊕", "↔")]
    [InlineData(OperatorStyle.CStyle, "&&", "||", "!", "^", "==")]
    public void Mermaid_renders_operators_in_the_requested_style(
        OperatorStyle style,
        string and,
        string or,
        string not,
        string xor,
        string xnor
    )
    {
        Assert.Contains(and, MermaidTreePrinter.Print(BinaryNode("AND"), style));
        Assert.Contains(or, MermaidTreePrinter.Print(BinaryNode("OR"), style));
        Assert.Contains(not, MermaidTreePrinter.Print(UnaryNode("NOT"), style));
        Assert.Contains(xor, MermaidTreePrinter.Print(BinaryNode("XOR"), style));
        Assert.Contains(xnor, MermaidTreePrinter.Print(BinaryNode("XNOR"), style));
    }

    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void PlainText_keeps_ExactlyOne_and_threshold_labels_in_word_form_in_every_style(OperatorStyle style)
    {
        RuleDescription exactlyOne = new("ExactlyOne", "desc", [Leaf("a"), Leaf("b")]);
        RuleDescription atLeast = new("AtLeast(3)", "desc", [Leaf("a"), Leaf("b"), Leaf("c")]);

        Assert.Contains("ExactlyOne", PlainTextTreePrinter.Print(exactlyOne, style));
        Assert.Contains("AtLeast(3)", PlainTextTreePrinter.Print(atLeast, style));
    }

    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void Mermaid_keeps_ExactlyOne_and_threshold_labels_in_word_form_in_every_style(OperatorStyle style)
    {
        RuleDescription exactlyOne = new("ExactlyOne", "desc", [Leaf("a"), Leaf("b")]);
        RuleDescription atLeast = new("AtLeast(3)", "desc", [Leaf("a"), Leaf("b"), Leaf("c")]);

        Assert.Contains("ExactlyOne", MermaidTreePrinter.Print(exactlyOne, style));
        Assert.Contains("AtLeast(3)", MermaidTreePrinter.Print(atLeast, style));
    }

    [Fact]
    public void PlainText_default_style_is_Word_with_no_style_argument()
    {
        string text = PlainTextTreePrinter.Print(BinaryNode("AND"));

        Assert.Contains("AND", text);
    }

    [Fact]
    public void Mermaid_default_style_is_Word_with_no_style_argument()
    {
        string mermaid = MermaidTreePrinter.Print(BinaryNode("AND"));

        Assert.Contains("AND", mermaid);
    }

    [Fact]
    public async Task PlainText_evaluated_overload_accepts_an_operator_style()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string text = PlainTextTreePrinter.Print(rule.Describe(), decision.EvaluatedTree!, OperatorStyle.Symbolic);

        Assert.Contains("∧", text);
    }

    [Fact]
    public async Task Mermaid_evaluated_overload_accepts_an_operator_style()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string mermaid = MermaidTreePrinter.Print(rule.Describe(), decision.EvaluatedTree!, OperatorStyle.CStyle);

        Assert.Contains("&&", mermaid);
    }

    [Fact]
    public void Mermaid_output_escapes_double_quotes_in_labels()
    {
        RuleDescription node = Leaf("has \"quotes\" inside");

        string mermaid = MermaidTreePrinter.Print(node);

        Assert.Contains("#quot;", mermaid);
        Assert.Contains("n0[\"has #quot;quotes#quot; inside\"]", mermaid);
        Assert.DoesNotContain("[\"has \"quotes\" inside\"]", mermaid);
    }

    [Theory]
    [InlineData("line one\r\nline two")]
    [InlineData("line one\rline two")]
    [InlineData("line one\nline two")]
    public void Mermaid_output_sanitizes_carriage_returns_and_newlines_in_labels(string label)
    {
        RuleDescription node = Leaf(label);

        string mermaid = MermaidTreePrinter.Print(node);

        Assert.DoesNotContain('\r', mermaid);
        Assert.DoesNotContain("line one\nline two", mermaid);
        Assert.Matches(@"n0\[""line one +line two""\]", mermaid);
    }

    private static RuleDescription BinaryNode(string label)
    {
        return new RuleDescription(label, "desc", [Leaf("a"), Leaf("b")]);
    }

    private static RuleDescription UnaryNode(string label)
    {
        return new RuleDescription(label, "desc", [Leaf("a")]);
    }

    private static RuleDescription Leaf(string label)
    {
        return new RuleDescription(label, "desc", []);
    }

    private static CompiledRule<RuleTestContext> Compile(
        string dsl,
        Func<PredicateRegistryBuilder<RuleTestContext>, PredicateRegistryBuilder<RuleTestContext>> configure
    )
    {
        PredicateRegistry<RuleTestContext> registry = configure(PredicateRegistry<RuleTestContext>.CreateBuilder()).Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        return compiler.Compile(dsl).CompiledRule!;
    }
}
