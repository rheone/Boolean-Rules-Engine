namespace TruthWeaver.Tests;

using System.Diagnostics.Metrics;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Metrics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Meter-based counters for evaluation, faults, and compile diagnostics. The library's <c>Meter</c>
/// is process-wide static state (same as any <c>System.Diagnostics.Metrics</c> producer), so these
/// assertions tolerate measurements from other tests running concurrently in the same process —
/// "at least one matching measurement was recorded", never an exact count.
/// </summary>
public sealed class MetricsTests
{
    [Fact]
    public async Task Evaluating_a_rule_increments_the_evaluations_counter()
    {
        using MeasurementCollector collector = new("truthweaver.evaluations");
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("p", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("p").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Contains(1L, collector.Measurements);
    }

    [Fact]
    public async Task A_faulting_predicate_increments_the_faults_counter()
    {
        using MeasurementCollector collector = new("truthweaver.faults");
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("flaky").Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("flaky").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Contains(1L, collector.Measurements);
    }

    [Fact]
    public void Compiling_a_rule_with_a_diagnostic_increments_the_compile_diagnostics_counter_tagged_by_severity()
    {
        using MeasurementCollector collector = new("truthweaver.compile_diagnostics");
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        compiler.Compile("noSuchPredicate");

        Assert.Contains(collector.Tags, t => t.GetValueOrDefault("severity") == nameof(DiagnosticSeverity.Error));
    }

    /// <summary>Subscribes to one named counter on the library's meter and records every measurement raised while it is alive.</summary>
    private sealed class MeasurementCollector : IDisposable
    {
        private readonly MeterListener listener;

        public MeasurementCollector(string instrumentName)
        {
            this.listener = new MeterListener
            {
                InstrumentPublished = (instrument, meterListener) =>
                {
                    if (instrument.Meter.Name == TruthWeaverMetrics.MeterName && instrument.Name == instrumentName)
                    {
                        meterListener.EnableMeasurementEvents(instrument);
                    }
                },
            };
            this.listener.SetMeasurementEventCallback<long>(
                (_, measurement, tags, _) =>
                {
                    this.Measurements.Add(measurement);
                    this.Tags.Add(tags.ToArray().ToDictionary(t => t.Key, t => t.Value?.ToString()));
                }
            );
            this.listener.Start();
        }

        public List<long> Measurements { get; } = [];

        public List<Dictionary<string, string?>> Tags { get; } = [];

        public void Dispose()
        {
            this.listener.Dispose();
        }
    }
}
