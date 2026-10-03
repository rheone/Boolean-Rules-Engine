namespace TruthWeaver.Diagnostics;

/// <summary>The severity of a compile <see cref="Diagnostic"/>.</summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational only — e.g. analysis was skipped because a resource limit was exceeded.</summary>
    Info,

    /// <summary>A likely authoring mistake that does not block compilation (e.g. a Strong K3 tautology or contradiction).</summary>
    Warning,

    /// <summary>
    /// Blocks compilation: <c>CompilationResult.CompiledRule</c> is <see langword="null"/> if any
    /// diagnostic has this severity (ADR-0002/ADR-0003).
    /// </summary>
    Error,
}
