namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>The fluent <see cref="RuleBuilder"/> API for assembling a rule without hand-written DSL/JSON/YAML text.</summary>
public sealed class RuleBuilderTests
{
    [Fact]
    public async Task Predicate_builder_compiles_and_evaluates_like_the_equivalent_dsl_text()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("isManager").Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public void And_or_not_builders_produce_the_same_compiled_rule_as_equivalent_dsl_text()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("isManager", true)
                .AddConstant("isSuspended", false)
                .Build()
        );

        CompiledRule<RuleTestContext> viaBuilder = RuleBuilder
            .And(RuleBuilder.Predicate("isManager"), RuleBuilder.Not(RuleBuilder.Predicate("isSuspended")))
            .Compile(compiler)
            .CompiledRule!;
        CompiledRule<RuleTestContext> viaDsl = compiler.Compile("isManager AND NOT isSuspended").CompiledRule!;

        Assert.Equal(viaDsl.CanonicalText, viaBuilder.CanonicalText);
    }

    [Fact]
    public void Predicate_builder_with_named_string_argument_compiles_successfully()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("hasRole", ("role", "Y")).Compile(compiler);

        Assert.True(result.Succeeded);
        Assert.Equal("hasRole(role: \"Y\")", result.CompiledRule!.CanonicalText);
    }

    [Fact]
    public void Threshold_builder_compiles_to_the_same_canonical_text_as_dsl_text()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompiledRule<RuleTestContext> viaBuilder = RuleBuilder
            .AtLeast(2, RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c"))
            .Compile(compiler)
            .CompiledRule!;
        CompiledRule<RuleTestContext> viaDsl = compiler.Compile("AtLeast(2, a, b, c)").CompiledRule!;

        Assert.Equal(viaDsl.CanonicalText, viaBuilder.CanonicalText);
    }

    [Fact]
    public void Xor_and_xnor_builders_compile_to_the_same_canonical_text_as_dsl_text()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );

        CompiledRule<RuleTestContext> viaXorBuilder = RuleBuilder
            .Xor(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .Compile(compiler)
            .CompiledRule!;
        CompiledRule<RuleTestContext> viaXnorBuilder = RuleBuilder
            .Xnor(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .Compile(compiler)
            .CompiledRule!;

        Assert.Equal("(a XOR b)", viaXorBuilder.CanonicalText);
        Assert.Equal("(a EQUIVALENT b)", viaXnorBuilder.CanonicalText);
    }

    [Fact]
    public void An_unknown_predicate_from_the_builder_is_a_compile_error_not_an_exception()
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("neverRegistered").Compile(compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.UnknownPredicate);
    }

    [Fact]
    public void An_out_of_range_threshold_from_the_builder_is_a_compile_error_not_an_exception()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder
            .AtLeast(0, RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .Compile(compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.InvalidThresholdValue);
    }

    [Fact]
    public void Constant_builder_round_trips_through_the_same_pipeline_as_dsl_text()
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompiledRule<RuleTestContext> rule = RuleBuilder.Constant(true).Compile(compiler).CompiledRule!;

        Assert.Equal("True", rule.CanonicalText);
    }

    [Theory]
    [InlineData(true, false, TruthValue.True)]
    [InlineData(false, false, TruthValue.False)]
    public Task Or_builder_evaluates_true_with_at_least_one_true_operand(bool a, bool b, TruthValue expected)
    {
        return EvaluateAsync([a, b], operands => RuleBuilder.Or(operands), expected);
    }

    [Theory]
    [InlineData(false, false, false, TruthValue.False)]
    [InlineData(true, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    public Task ExactlyOne_builder_evaluates_true_iff_exactly_one_operand_is_true(bool a, bool b, bool c, TruthValue expected)
    {
        return EvaluateAsync([a, b, c], operands => RuleBuilder.ExactlyOne(operands), expected);
    }

    [Theory]
    [InlineData(true, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    public Task AtMost_builder_evaluates_correctly_near_the_boundary(bool a, bool b, bool c, TruthValue expected)
    {
        return EvaluateAsync([a, b, c], operands => RuleBuilder.AtMost(1, operands), expected);
    }

    [Theory]
    [InlineData(true, true, false, TruthValue.True)]
    [InlineData(true, false, false, TruthValue.False)]
    public Task GreaterThan_builder_evaluates_correctly_near_the_boundary(bool a, bool b, bool c, TruthValue expected)
    {
        return EvaluateAsync([a, b, c], operands => RuleBuilder.GreaterThan(1, operands), expected);
    }

    [Theory]
    [InlineData(false, false, false, TruthValue.True)]
    [InlineData(true, true, false, TruthValue.False)]
    public Task LessThan_builder_evaluates_correctly_near_the_boundary(bool a, bool b, bool c, TruthValue expected)
    {
        return EvaluateAsync([a, b, c], operands => RuleBuilder.LessThan(2, operands), expected);
    }

    [Theory]
    [InlineData(true, true, false, TruthValue.True)]
    [InlineData(true, true, true, TruthValue.False)]
    public Task Exactly_builder_evaluates_correctly_near_the_boundary(bool a, bool b, bool c, TruthValue expected)
    {
        return EvaluateAsync([a, b, c], operands => RuleBuilder.Exactly(2, operands), expected);
    }

    [Fact]
    public void ToJson_round_trips_to_a_rule_that_compiles_and_evaluates_identically()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("isManager", true)
                .AddConstant("isSuspended", false)
                .Build()
        );
        RuleBuilder original = RuleBuilder.And(
            RuleBuilder.Predicate("isManager"),
            RuleBuilder.Not(RuleBuilder.Predicate("isSuspended"))
        );

        string json = original.ToJson();
        CompilationResult<RuleTestContext> reparsed = compiler.CompileJson(json);
        CompiledRule<RuleTestContext> viaOriginal = original.Compile(compiler).CompiledRule!;

        Assert.True(reparsed.Succeeded);
        Assert.Equal(viaOriginal.CanonicalText, reparsed.CompiledRule!.CanonicalText);
    }

    [Fact]
    public async Task Predicate_builder_with_a_bool_argument_value_compiles_and_evaluates_correctly()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddBooleanArgPredicate("hasFlag", "flag", true).Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("hasFlag", ("flag", true)).Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task Predicate_builder_with_an_int_argument_value_compiles_and_evaluates_correctly()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasCode",
                        "hasCode",
                        "True iff 'code' equals 7.",
                        [new PredicateArgumentSchema("code", "The code to compare against 7.", LiteralKind.Int64)]
                    ),
                    (_, args, _) => ValueTask.FromResult(args.GetInt64("code") == 7 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("hasCode", ("code", 7)).Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task Predicate_builder_with_a_long_argument_value_compiles_and_evaluates_correctly()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasCode",
                        "hasCode",
                        "True iff 'code' equals 7.",
                        [new PredicateArgumentSchema("code", "The code to compare against 7.", LiteralKind.Int64)]
                    ),
                    (_, args, _) => ValueTask.FromResult(args.GetInt64("code") == 7 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("hasCode", ("code", 7L)).Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task Predicate_builder_with_a_double_argument_value_compiles_and_evaluates_correctly()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddDecimalArgPredicate("exceedsThreshold", "threshold", 12.5m)
                .Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder
            .Predicate("exceedsThreshold", ("threshold", 12.5d))
            .Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task Predicate_builder_with_a_decimal_argument_value_compiles_and_evaluates_correctly()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddDecimalArgPredicate("exceedsThreshold", "threshold", 12.5m)
                .Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder
            .Predicate("exceedsThreshold", ("threshold", 12.5m))
            .Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task Predicate_builder_with_a_datetimeoffset_argument_value_compiles_and_evaluates_correctly()
    {
        DateTimeOffset when = new(2024, 6, 1, 12, 30, 0, TimeSpan.Zero);
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddDateTimeOffsetArgPredicate("occurredAt", "when", when).Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("occurredAt", ("when", when)).Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task Predicate_builder_with_an_array_argument_value_compiles_and_evaluates_correctly_via_ArrayToNode()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasAnyCode",
                        "hasAnyCode",
                        "True iff any of 'codes' matches.",
                        [new PredicateArgumentSchema("codes", "The codes to check for.", LiteralKind.Int64Array)]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(args.GetInt64Array("codes").Count > 0 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );

        CompilationResult<RuleTestContext> result = RuleBuilder
            .Predicate("hasAnyCode", ("codes", new object[] { 1, 2, 3 }))
            .Compile(compiler);

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public void An_unsupported_argument_value_type_throws_ArgumentException_naming_the_type()
    {
        object unsupported = new StringBuilder("not a supported literal type");

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            RuleBuilder.Predicate("anything", ("arg", unsupported)).ToJson()
        );

        Assert.Contains(unsupported.GetType().ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Equal("value", exception.ParamName);
    }

    private static async Task EvaluateAsync(bool[] operandValues, Func<RuleBuilder[], RuleBuilder> build, TruthValue expected)
    {
        PredicateRegistryBuilder<RuleTestContext> registryBuilder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        RuleBuilder[] operands = new RuleBuilder[operandValues.Length];
        for (int i = 0; i < operandValues.Length; i++)
        {
            string name = ((char)('a' + i)).ToString();
            registryBuilder = registryBuilder.AddConstant(name, operandValues[i]);
            operands[i] = RuleBuilder.Predicate(name);
        }

        RuleCompiler<RuleTestContext> compiler = new(registryBuilder.Build());
        CompiledRule<RuleTestContext> rule = build(operands).Compile(compiler).CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(expected, decision.Result);
    }
}
