namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

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

    /// <summary>A different <c>Project</c> policy is a change of the node, not an identical rule.</summary>
    [Fact]
    public void A_changed_project_policy_produces_a_changed_entry_at_the_root()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("Project(isManager, True)").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("Project(isManager, False)").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Empty(entry.Path);
        Assert.Equal("Project(True)", entry.Before!.Label);
        Assert.Equal("Project(False)", entry.After!.Label);
    }

    /// <summary>A different inspection kind is a change of the node, not an identical rule.</summary>
    [Fact]
    public void A_changed_inspection_kind_produces_a_changed_entry_at_the_root()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("IsTrue(isManager)").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("IsFalse(isManager)").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Empty(entry.Path);
    }

    /// <summary>Changing only the declared collapse policy is a change of the rule, reported at the root.</summary>
    [Fact]
    public void A_changed_collapse_policy_produces_a_changed_entry_at_the_root()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("Collapse(isManager, UnknownAsFalse)").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler.Compile("Collapse(isManager, UnknownAsTrue)").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Empty(entry.Path);
        Assert.Equal("Collapse(UnknownAsFalse)", entry.Before!.Label);
        Assert.Equal("Collapse(UnknownAsTrue)", entry.After!.Label);
    }

    /// <summary>Adding a collapse around an otherwise identical rule is reported once, at the root.</summary>
    [Fact]
    public void An_added_collapse_produces_a_changed_entry_at_the_root()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler.Compile("isManager AND isDepartmentHead").CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler
            .Compile("Collapse(isManager AND isDepartmentHead, UnknownIsError)")
            .CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Empty(entry.Path);
        Assert.Equal("AND", entry.Before!.Label);
        Assert.Equal("Collapse(UnknownIsError)", entry.After!.Label);
    }

    /// <summary>
    /// With the same collapse on both sides, operand changes inside are located relative to the inner expression, exactly as
    /// without a collapse, and the descriptions line up with the expression nodes.
    /// </summary>
    [Fact]
    public void A_changed_term_inside_the_same_collapse_is_located_relative_to_the_inner_expression()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> before = compiler
            .Compile("Collapse(isManager AND hasRole(role: \"Y\"), UnknownAsFalse)")
            .CompiledRule!;
        CompiledRule<RuleTestContext> after = compiler
            .Compile("Collapse(isManager AND hasRole(role: \"Z\"), UnknownAsFalse)")
            .CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal(RuleDiffChangeKind.Changed, entry.Kind);
        Assert.Equal([1], entry.Path);
        Assert.Equal("hasRole", entry.Before!.Label);
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
