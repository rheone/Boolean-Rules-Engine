namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Tests.TestSupport;

/// <summary>Ticket 03: AND / OR / NOT with Kleene semantics, precedence, and short-circuiting.</summary>
public sealed class KleeneOperatorTests
{
    public static TheoryData<bool?, bool?, TruthValue> AndTruthTable =>
        new()
        {
            { true, true, TruthValue.True },
            { true, false, TruthValue.False },
            { true, null, TruthValue.Unknown },
            { false, true, TruthValue.False },
            { false, false, TruthValue.False },
            { false, null, TruthValue.False },
            { null, true, TruthValue.Unknown },
            { null, false, TruthValue.False },
            { null, null, TruthValue.Unknown },
        };

    public static TheoryData<bool?, bool?, TruthValue> OrTruthTable =>
        new()
        {
            { true, true, TruthValue.True },
            { true, false, TruthValue.True },
            { true, null, TruthValue.True },
            { false, true, TruthValue.True },
            { false, false, TruthValue.False },
            { false, null, TruthValue.Unknown },
            { null, true, TruthValue.True },
            { null, false, TruthValue.Unknown },
            { null, null, TruthValue.Unknown },
        };

    [Theory]
    [MemberData(nameof(AndTruthTable))]
    public async Task And_matches_the_kleene_truth_table(bool? left, bool? right, TruthValue expected)
    {
        Decision decision = await EvaluateBinaryAsync("a AND b", left, right);
        Assert.Equal(expected, decision.Result);
    }

    [Theory]
    [MemberData(nameof(OrTruthTable))]
    public async Task Or_matches_the_kleene_truth_table(bool? left, bool? right, TruthValue expected)
    {
        Decision decision = await EvaluateBinaryAsync("a OR b", left, right);
        Assert.Equal(expected, decision.Result);
    }

    [Theory]
    [InlineData(true, TruthValue.False)]
    [InlineData(false, TruthValue.True)]
    public async Task Not_matches_the_kleene_truth_table(bool value, TruthValue expected)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", value)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("NOT a")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    [Fact]
    public async Task Not_of_a_faulting_term_is_unknown_not_true()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddThrowing("a")
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("NOT a")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    [Fact]
    public async Task Not_binds_tighter_than_and_which_binds_tighter_than_or()
    {
        // "NOT a AND b" must parse as "(NOT a) AND b": a=true -> NOT a=false -> AND anything=false.
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("NOT a AND b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    [Fact]
    public async Task Or_with_faulting_operand_and_true_operand_is_true()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddThrowing("a")
            .AddConstant("b", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a OR b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task And_with_faulting_operand_and_false_operand_is_false()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddThrowing("a")
            .AddConstant("b", false)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    [Fact]
    public async Task And_short_circuits_after_first_false_and_records_not_evaluated_in_trace()
    {
        List<string> invocationLog = [];
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddCountingConstant("a", false, invocationLog)
            .AddCountingConstant("b", true, invocationLog)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(["a"], invocationLog);
        Assert.NotNull(decision.Trace);
        Assert.Contains(decision.Trace.Entries, e => e.NodeDescription == "b" && e.NotEvaluated);
    }

    [Fact]
    public async Task Or_short_circuits_after_first_true()
    {
        List<string> invocationLog = [];
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddCountingConstant("a", true, invocationLog)
            .AddCountingConstant("b", false, invocationLog)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a OR b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Equal(["a"], invocationLog);
    }

    [Fact]
    public async Task Sibling_operands_are_evaluated_in_written_order()
    {
        List<string> invocationLog = [];
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddCountingConstant("a", true, invocationLog)
            .AddCountingConstant("b", true, invocationLog)
            .AddCountingConstant("c", true, invocationLog)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        await compiler
            .Compile("a AND b AND c")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(["a", "b", "c"], invocationLog);
    }

    private static Task<Decision> EvaluateBinaryAsync(string dsl, bool? left, bool? right)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        builder = left is { } l ? builder.AddConstant("a", l) : builder.AddThrowing("a");
        builder = right is { } r ? builder.AddConstant("b", r) : builder.AddThrowing("b");
        RuleCompiler<RuleTestContext> compiler = new(builder.Build());

        return compiler
            .Compile(dsl)
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );
    }
}
