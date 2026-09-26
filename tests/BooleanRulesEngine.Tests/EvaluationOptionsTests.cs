namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Evaluation;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Tests.TestSupport;

/// <summary>Ticket 11: <see cref="EvaluationOptions"/> — FaultBudget, Exhaustive mode, and timeout.</summary>
public sealed class EvaluationOptionsTests
{
    [Fact]
    public async Task Fault_budget_of_one_tolerates_the_first_fault_and_aborts_on_the_second()
    {
        // Two faulting terms exist; with FaultBudget=1, evaluation aborts on the second.
        Decision decision = await EvaluateWithFaultBudgetAsync();

        Assert.Equal(2, decision.Faults.Count);
    }

    [Fact]
    public async Task Fault_budget_abort_leaves_the_trace_with_unevaluated_entries()
    {
        Decision decision = await EvaluateWithFaultBudgetAsync();

        Assert.Contains(decision.Trace!.Entries, e => e.NotEvaluated);
    }

    [Fact]
    public async Task Unlimited_default_fault_budget_continues_to_completion_with_multiple_faults()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("a").AddThrowing("b").AddConstant("c", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("ExactlyOne(a, b, c)").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(2, decision.Faults.Count);
        Assert.DoesNotContain(decision.Trace!.Entries, e => e.NotEvaluated);
    }

    [Fact]
    public async Task Exhaustive_mode_evaluates_remaining_and_operands_after_a_false_first_operand()
    {
        List<string> invocationLog = [];
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddCountingConstant("a", false, invocationLog)
                .AddCountingConstant("b", true, invocationLog)
                .Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("a AND b").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            new EvaluationOptions(Mode: EvaluationMode.Exhaustive),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
        Assert.Equal(["a", "b"], invocationLog);
    }

    [Theory]
    [InlineData(EvaluationMode.Default)]
    [InlineData(EvaluationMode.Exhaustive)]
    public async Task Result_is_identical_between_default_and_exhaustive_modes(EvaluationMode mode)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", false).AddThrowing("b").Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("a AND b").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            new EvaluationOptions(Mode: mode),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    [Fact]
    public Task Timeout_cancels_an_in_flight_evaluation()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddDelayed("slow", TimeSpan.FromSeconds(5), true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("slow").CompiledRule!;

        return Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            rule.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                new EvaluationOptions(Timeout: TimeSpan.FromMilliseconds(50)),
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task No_timeout_configured_leaves_evaluation_unaffected()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddDelayed("fast", TimeSpan.FromMilliseconds(10), true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("fast").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    private static Task<Decision> EvaluateWithFaultBudgetAsync()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("a").AddThrowing("b").AddConstant("c", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("ExactlyOne(a, b, c)").CompiledRule!;

        return rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            new EvaluationOptions(FaultBudget: 1),
            TestContext.Current.CancellationToken
        );
    }
}
