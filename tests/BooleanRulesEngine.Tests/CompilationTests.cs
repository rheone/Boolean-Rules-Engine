namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Tests.TestSupport;

/// <summary>Ticket 02: the tracer bullet — compile and evaluate the smallest possible rule shapes.</summary>
public sealed class CompilationTests
{
    [Fact]
    public async Task Registered_zero_arg_predicate_evaluates_to_its_registered_value()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("isManager", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        CompilationResult<RuleTestContext> result = compiler.Compile("isManager");

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
        Assert.True(decision.IsSatisfied);
    }

    [Fact]
    public async Task Registered_zero_arg_predicate_evaluates_to_false_when_registered_false()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("isManager", false)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("isManager")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.False, decision.Result);
        Assert.False(decision.IsSatisfied);
    }

    [Fact]
    public void Unregistered_predicate_produces_error_diagnostic_with_span_and_null_compiled_rule()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>.CreateBuilder().Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        CompilationResult<RuleTestContext> result = compiler.Compile("noSuchPredicate");

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(DiagnosticCodes.UnknownPredicate, diagnostic.Code);
        Assert.Equal(new SourceSpan(0, "noSuchPredicate".Length), diagnostic.Span);
    }

    [Theory]
    [InlineData("true", TruthValue.True)]
    [InlineData("false", TruthValue.False)]
    public async Task Constant_literal_rule_evaluates_to_the_matching_constant(string dsl, TruthValue expected)
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        Decision decision = await compiler
            .Compile(dsl)
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(expected, decision.Result);
    }

    [Fact]
    public async Task Throwing_predicate_is_absorbed_as_a_fault_and_evaluation_completes()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddThrowing("flaky")
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("flaky")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal("flaky", fault.Term.PredicateName);
        Assert.IsType<InvalidOperationException>(fault.Exception);
    }
}
