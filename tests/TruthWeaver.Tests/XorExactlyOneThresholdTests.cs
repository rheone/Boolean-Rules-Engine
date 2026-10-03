namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 05 (and its later extension): XOR / ExactlyOne / the AtLeast-AtMost-GreaterThan-LessThan-Exactly threshold family.</summary>
public sealed class XorExactlyOneThresholdTests
{
    public static TheoryData<bool?, bool?, TruthValue> XorTruthTable =>
        new()
        {
            { true, true, TruthValue.False },
            { true, false, TruthValue.True },
            { true, null, TruthValue.Unknown },
            { false, true, TruthValue.True },
            { false, false, TruthValue.False },
            { false, null, TruthValue.Unknown },
            { null, true, TruthValue.Unknown },
            { null, false, TruthValue.Unknown },
            { null, null, TruthValue.Unknown },
        };

    public static TheoryData<bool?, bool?, TruthValue> XnorTruthTable =>
        new()
        {
            { true, true, TruthValue.True },
            { true, false, TruthValue.False },
            { true, null, TruthValue.Unknown },
            { false, true, TruthValue.False },
            { false, false, TruthValue.True },
            { false, null, TruthValue.Unknown },
            { null, true, TruthValue.Unknown },
            { null, false, TruthValue.Unknown },
            { null, null, TruthValue.Unknown },
        };

    [Theory]
    [MemberData(nameof(XorTruthTable))]
    public async Task Xor_matches_the_kleene_truth_table(bool? left, bool? right, TruthValue expected)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        builder = left is { } l ? builder.AddConstant("a", l) : builder.AddThrowing("a");
        builder = right is { } r ? builder.AddConstant("b", r) : builder.AddThrowing("b");
        RuleCompiler<RuleTestContext> compiler = new(builder.Build());

        Decision decision = await compiler
            .Compile("a XOR b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    [Theory]
    [InlineData("xor")]
    [InlineData("xnor")]
    public void Xor_or_xnor_with_three_operands_via_json_op_is_a_compile_error(string op)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.CompileJson(
            $$"""{"op":"{{op}}","operands":[{"predicate":"a"},{"predicate":"b"},{"predicate":"c"}]}"""
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.InfixArityViolation);
    }

    [Theory]
    [InlineData("a AND b XOR c")]
    [InlineData("a XOR b AND c")]
    [InlineData("a OR b XOR c")]
    [InlineData("a AND b XNOR c")]
    [InlineData("a XNOR b AND c")]
    [InlineData("a OR b XNOR c")]
    [InlineData("a XOR b XNOR c")]
    public void Mixing_xor_or_xnor_with_and_or_each_other_without_parentheses_is_a_compile_error(string dsl)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile(dsl);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    [Fact]
    public void Xor_inside_parentheses_combined_with_and_compiles_successfully()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND (b XOR c)");

        Assert.True(result.Succeeded);
    }

    [Theory]
    [MemberData(nameof(XnorTruthTable))]
    public async Task Xnor_matches_the_negated_xor_truth_table(bool? left, bool? right, TruthValue expected)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        builder = left is { } l ? builder.AddConstant("a", l) : builder.AddThrowing("a");
        builder = right is { } r ? builder.AddConstant("b", r) : builder.AddThrowing("b");
        RuleCompiler<RuleTestContext> compiler = new(builder.Build());

        Decision decision = await compiler
            .Compile("a XNOR b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    [Fact]
    public void Xnor_inside_parentheses_combined_with_and_compiles_successfully()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND (b XNOR c)");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Xnor_round_trips_through_json()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );

        CompilationResult<RuleTestContext> compiled = compiler.Compile("a XNOR b");
        Assert.True(compiled.Succeeded);

        string json = compiled.CompiledRule!.PrintJson();
        CompilationResult<RuleTestContext> reparsed = compiler.CompileJson(json);

        Assert.True(reparsed.Succeeded);
        Assert.Equal("(a EQUIVALENT b)", reparsed.CompiledRule!.CanonicalText);
    }

    [Theory]
    [InlineData(true, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    [InlineData(false, false, false, TruthValue.False)]
    public async Task ExactlyOne_evaluates_to_true_iff_exactly_one_operand_is_true(bool a, bool b, bool c, TruthValue expected)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", a)
                .AddConstant("b", b)
                .AddConstant("c", c)
                .Build()
        );

        Decision decision = await compiler
            .Compile("ExactlyOne(a, b, c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    [Fact]
    public async Task ExactlyOne_with_two_unknowns_and_rest_false_is_unknown()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("a").AddThrowing("b").AddConstant("c", false).Build()
        );

        Decision decision = await compiler
            .Compile("ExactlyOne(a, b, c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    [Theory]
    [InlineData(true, true, false, TruthValue.True)]
    [InlineData(true, false, false, TruthValue.False)]
    public async Task AtLeast_evaluates_to_true_iff_at_least_k_operands_are_true(bool a, bool b, bool c, TruthValue expected)
    {
        Decision decision = await EvaluateThresholdAsync("AtLeast(2, a, b, c)", a, b, c);

        Assert.Equal(expected, decision.Result);
    }

    [Fact]
    public async Task AtLeast_with_unsettled_confirmed_counts_is_unknown()
    {
        // One true, one unknown ("b" faults), one false: confirmed true count (1) < k (2), but
        // true+unknown (2) >= k -> Unknown.
        Decision decision = await EvaluateThresholdWithFaultingBAsync(
            "AtLeast(2, a, b, c)",
            trueOperand: true,
            falseOperand: false
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void AtLeast_with_k_out_of_range_is_a_compile_error(int k)
    {
        AssertThresholdOutOfRange($"AtLeast({k}, a, b, c)");
    }

    [Theory]
    [InlineData(true, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    public async Task AtMost_evaluates_to_true_iff_at_most_k_operands_are_true(bool a, bool b, bool c, TruthValue expected)
    {
        Decision decision = await EvaluateThresholdAsync("AtMost(1, a, b, c)", a, b, c);

        Assert.Equal(expected, decision.Result);
    }

    [Fact]
    public async Task AtMost_with_unsettled_confirmed_counts_is_unknown()
    {
        // Zero confirmed true, one unknown ("b" faults), one false: at-most-0 could still hold
        // (unknown resolves false) or fail (unknown resolves true) -> Unknown.
        Decision decision = await EvaluateThresholdWithFaultingBAsync(
            "AtMost(0, a, b, c)",
            trueOperand: false,
            falseOperand: false
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void AtMost_with_k_out_of_range_is_a_compile_error(int k)
    {
        AssertThresholdOutOfRange($"AtMost({k}, a, b, c)");
    }

    [Theory]
    [InlineData(true, true, false, TruthValue.True)]
    [InlineData(true, false, false, TruthValue.False)]
    public async Task GreaterThan_evaluates_to_true_iff_more_than_k_operands_are_true(
        bool a,
        bool b,
        bool c,
        TruthValue expected
    )
    {
        Decision decision = await EvaluateThresholdAsync("GreaterThan(1, a, b, c)", a, b, c);

        Assert.Equal(expected, decision.Result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void GreaterThan_with_k_out_of_range_is_a_compile_error(int k)
    {
        AssertThresholdOutOfRange($"GreaterThan({k}, a, b, c)");
    }

    [Theory]
    [InlineData(true, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    public async Task LessThan_evaluates_to_true_iff_fewer_than_k_operands_are_true(bool a, bool b, bool c, TruthValue expected)
    {
        Decision decision = await EvaluateThresholdAsync("LessThan(2, a, b, c)", a, b, c);

        Assert.Equal(expected, decision.Result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void LessThan_with_k_out_of_range_is_a_compile_error(int k)
    {
        AssertThresholdOutOfRange($"LessThan({k}, a, b, c)");
    }

    [Theory]
    [InlineData(true, true, false, TruthValue.True)]
    [InlineData(true, false, false, TruthValue.False)]
    public async Task Exactly_evaluates_to_true_iff_the_true_count_matches_k_exactly(
        bool a,
        bool b,
        bool c,
        TruthValue expected
    )
    {
        Decision decision = await EvaluateThresholdAsync("Exactly(2, a, b, c)", a, b, c);

        Assert.Equal(expected, decision.Result);
    }

    [Fact]
    public async Task Exactly_with_k_reachable_but_not_certain_is_unknown()
    {
        // Zero confirmed true, two unknowns, one false: k=1 is reachable (one unknown resolves true)
        // but not certain (both could resolve false, or both true) -> Unknown.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("a").AddThrowing("b").AddConstant("c", false).Build()
        );

        Decision decision = await compiler
            .Compile("Exactly(1, a, b, c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Exactly_with_k_out_of_range_is_a_compile_error(int k)
    {
        AssertThresholdOutOfRange($"Exactly({k}, a, b, c)");
    }

    private static void AssertThresholdOutOfRange(string dsl)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile(dsl);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.InvalidThresholdValue);
    }

    private static Task<Decision> EvaluateThresholdAsync(string dsl, bool a, bool b, bool c)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", a)
                .AddConstant("b", b)
                .AddConstant("c", c)
                .Build()
        );

        return compiler
            .Compile(dsl)
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );
    }

    private static Task<Decision> EvaluateThresholdWithFaultingBAsync(string dsl, bool trueOperand, bool falseOperand)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", trueOperand)
                .AddThrowing("b")
                .AddConstant("c", falseOperand)
                .Build()
        );

        return compiler
            .Compile(dsl)
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );
    }
}
