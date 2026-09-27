namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Diffing;
using BooleanRulesEngine.Evaluation;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Tests.TestSupport;

/// <summary>Ticket 02: human-readable rendering of a <see cref="RuleDiffResult"/>.</summary>
public sealed class RuleDiffPrinterTests
{
    [Fact]
    public void An_empty_diff_renders_as_no_changes()
    {
        RuleDiffResult diff = new([]);

        string text = RuleDiffPrinter.Print(diff);

        Assert.Equal("No changes.", text);
    }

    [Fact]
    public void An_added_operand_renders_using_its_label_and_description_not_the_ast_type_name()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler
            .Compile("isManager AND isDepartmentHead AND hasRole(role: \"Y\")")
            .CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);
        string text = RuleDiffPrinter.Print(diff);

        Assert.Contains("Added", text, StringComparison.Ordinal);
        Assert.Contains("hasRole", text, StringComparison.Ordinal);
        Assert.DoesNotContain("TermExpression", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AndExpression", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_removed_operand_renders_using_its_before_label()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler
            .Compile("isManager AND isDepartmentHead AND hasRole(role: \"Y\")")
            .CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);
        string text = RuleDiffPrinter.Print(diff);

        Assert.Contains("Removed", text, StringComparison.Ordinal);
        Assert.Contains("hasRole", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_changed_operator_renders_both_the_before_and_after_labels()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager OR isDepartmentHead").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);
        string text = RuleDiffPrinter.Print(diff);

        Assert.Contains("AND", text, StringComparison.Ordinal);
        Assert.Contains("OR", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Rendering_produces_one_line_per_diff_entry()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler
            .Compile("isManager AND hasRole(role: \"Z\") AND isDepartmentHead")
            .CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);
        string text = RuleDiffPrinter.Print(diff);

        string[] lines = text.Split('\n');
        Assert.Equal(diff.Entries.Count, lines.Length);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddConstant("isManager", true)
                .AddConstant("isDepartmentHead", true)
                .Build()
        );
    }
}
