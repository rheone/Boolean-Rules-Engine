namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Diffing;
using BooleanRulesEngine.Evaluation;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Tests.TestSupport;

/// <summary>Ticket 01: structural diff between two compiled rules.</summary>
public sealed class RuleDiffTests
{
    [Fact]
    public void Structurally_identical_rules_produce_an_empty_diff()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        Assert.False(diff.HasChanges);
        Assert.Empty(diff.Entries);
    }

    [Fact]
    public void A_changed_operator_produces_a_single_changed_entry_at_the_root()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager OR isDepartmentHead").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Empty(entry.Path);
        Assert.Equal("AND", entry.Before!.Label);
        Assert.Equal("OR", entry.After!.Label);
    }

    [Fact]
    public void Changed_term_arguments_produce_a_changed_entry_at_the_term_position()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager AND hasRole(role: \"Z\")").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Equal([1], entry.Path);
        Assert.Equal("hasRole", entry.Before!.Label);
        Assert.Equal("hasRole", entry.After!.Label);
    }

    [Fact]
    public void An_added_operand_produces_an_added_entry()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler
            .Compile("isManager AND isDepartmentHead AND hasRole(role: \"Y\")")
            .CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Added, entry.Kind);
        Assert.Equal([2], entry.Path);
        Assert.Null(entry.Before);
        Assert.Equal("hasRole", entry.After!.Label);
    }

    [Fact]
    public void A_removed_operand_produces_a_removed_entry()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler
            .Compile("isManager AND isDepartmentHead AND hasRole(role: \"Y\")")
            .CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Removed, entry.Kind);
        Assert.Equal([2], entry.Path);
        Assert.Equal("hasRole", entry.Before!.Label);
        Assert.Null(entry.After);
    }

    [Fact]
    public void A_change_nested_inside_an_unchanged_operator_is_reported_at_its_own_path()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler
            .Compile("isManager AND (isDepartmentHead OR hasRole(role: \"Y\"))")
            .CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler
            .Compile("isManager AND (isDepartmentHead OR hasRole(role: \"Z\"))")
            .CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Equal([1, 1], entry.Path);
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
