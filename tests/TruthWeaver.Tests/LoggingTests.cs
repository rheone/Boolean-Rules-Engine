namespace TruthWeaver.Tests;

using Microsoft.Extensions.Logging;
using NSubstitute;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 13: structured logging via <see cref="ILogger{TCategoryName}"/>.</summary>
public sealed class LoggingTests
{
    [Fact]
    public async Task Faulting_predicate_logs_a_structured_warning_with_term_and_exception()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("flaky").Build(),
            logger: logger
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("flaky").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls(), c => c.Level == LogLevel.Warning);
        Assert.Equal("flaky", call.Field("Term"));
        Assert.NotNull(call.Exception);
        Assert.IsType<InvalidOperationException>(call.Exception);
    }

    [Fact]
    public void Compiling_a_rule_with_a_diagnostic_logs_one_structured_event_per_diagnostic()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            logger: logger
        );

        compiler.Compile("noSuchPredicate");

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Error, call.Level);
        Assert.Equal(DiagnosticCodes.UnknownPredicate, call.Field("Code"));
        Assert.Equal(DiagnosticSeverity.Error, call.Field("Severity"));
    }

    [Fact]
    public void Compiling_a_rule_with_a_warning_diagnostic_logs_at_warning_level()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).Build(),
            logger: logger
        );

        compiler.Compile("a AND FALSE");

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Warning, call.Level);
        Assert.Equal(DiagnosticCodes.StructuralContradiction, call.Field("Code"));
        Assert.Equal(DiagnosticSeverity.Warning, call.Field("Severity"));
    }

    [Fact]
    public void Compiling_a_rule_with_an_info_diagnostic_logs_at_information_level()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build(),
            new CompilerOptions(MaxAnalysisTerms: 1),
            logger: logger
        );

        compiler.Compile("a AND NOT a AND b");

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Information, call.Level);
        Assert.Equal(DiagnosticCodes.AnalysisSkippedTooManyTerms, call.Field("Code"));
        Assert.Equal(DiagnosticSeverity.Info, call.Field("Severity"));
    }

    [Fact]
    public void Rule_swap_notification_logs_a_distinct_structured_event()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            logger: logger
        );

        compiler.NotifyRuleSwapped("my-rule");

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Information, call.Level);
        Assert.Equal("my-rule", call.Field("RuleIdentifier"));

        // Distinct event id from both the compile-diagnostic and fault log events.
        Assert.NotEqual(0, call.EventId.Id);
    }

    [Fact]
    public void Rule_swap_notification_without_an_identifier_falls_back_to_unnamed()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = CreateEnabledLogger();
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            logger: logger
        );

        compiler.NotifyRuleSwapped();

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Information, call.Level);
        Assert.Equal("(unnamed)", call.Field("RuleIdentifier"));
    }

    private static ILogger<RuleCompiler<RuleTestContext>> CreateEnabledLogger()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = Substitute.For<ILogger<RuleCompiler<RuleTestContext>>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return logger;
    }
}
