namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Tests.TestSupport;

/// <summary>Ticket 05: XOR / ExactlyOne / AtLeast operators.</summary>
public sealed class XorExactlyOneAtLeastTests
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

    [Fact]
    public void Xor_with_three_operands_via_json_op_is_a_compile_error()
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
            """{"op":"xor","operands":[{"predicate":"a"},{"predicate":"b"},{"predicate":"c"}]}"""
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.XorArityViolation);
    }

    [Theory]
    [InlineData("a AND b XOR c")]
    [InlineData("a XOR b AND c")]
    [InlineData("a OR b XOR c")]
    public void Mixing_xor_with_and_or_without_parentheses_is_a_compile_error(string dsl)
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
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", a)
                .AddConstant("b", b)
                .AddConstant("c", c)
                .Build()
        );

        Decision decision = await compiler
            .Compile("AtLeast(2, a, b, c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    [Fact]
    public async Task AtLeast_with_unsettled_confirmed_counts_is_unknown()
    {
        // One true, one unknown, one false: confirmed true count (1) < k (2), but true+unknown (2) >= k -> Unknown.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddThrowing("b")
                .AddConstant("c", false)
                .Build()
        );

        Decision decision = await compiler
            .Compile("AtLeast(2, a, b, c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void AtLeast_with_k_out_of_range_is_a_compile_error(int k)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile($"AtLeast({k}, a, b, c)");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.InvalidAtLeastThreshold);
    }
}
