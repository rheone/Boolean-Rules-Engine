namespace BooleanRulesEngine.Metrics;

using System.Diagnostics.Metrics;
using BooleanRulesEngine.Diagnostics;

/// <summary>
/// The library's <see cref="System.Diagnostics.Metrics"/> instrumentation. A single static
/// <see cref="Meter"/> named <see cref="MeterName"/>, with counters incremented at the same points
/// structured logging (Logging/*.cs) instruments — evaluations performed, faults recorded, and
/// compile diagnostics raised. A host observes these the same way it observes any other
/// <see cref="System.Diagnostics.Metrics"/> producer (a <see cref="MeterListener"/>, or an
/// OpenTelemetry <c>AddMeter(BooleanRulesEngineMetrics.MeterName)</c> call) — the library takes no
/// dependency on a particular metrics exporter.
/// </summary>
internal static class BooleanRulesEngineMetrics
{
    /// <summary>The library's meter name, for a host to subscribe to (e.g. OpenTelemetry's <c>AddMeter</c>).</summary>
    public const string MeterName = "BooleanRulesEngine";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Evaluations = Meter.CreateCounter<long>(
        "boolean_rules_engine.evaluations",
        unit: "{evaluation}",
        description: "The number of rule evaluations performed (CompiledRule.EvaluateAsync calls)."
    );

    private static readonly Counter<long> Faults = Meter.CreateCounter<long>(
        "boolean_rules_engine.faults",
        unit: "{fault}",
        description: "The number of predicate faults absorbed during evaluation (ADR-0002)."
    );

    private static readonly Counter<long> CompileDiagnostics = Meter.CreateCounter<long>(
        "boolean_rules_engine.compile_diagnostics",
        unit: "{diagnostic}",
        description: "The number of compile diagnostics raised, tagged by severity."
    );

    /// <summary>Records that one rule evaluation completed.</summary>
    public static void EvaluationPerformed()
    {
        Evaluations.Add(1);
    }

    /// <summary>Records that one predicate fault was absorbed.</summary>
    public static void FaultRecorded()
    {
        Faults.Add(1);
    }

    /// <summary>Records that one compile diagnostic was raised.</summary>
    /// <param name="severity">The diagnostic's severity, recorded as a <c>severity</c> tag.</param>
    public static void CompileDiagnosticRaised(DiagnosticSeverity severity)
    {
        CompileDiagnostics.Add(1, new KeyValuePair<string, object?>("severity", severity.ToString()));
    }
}
