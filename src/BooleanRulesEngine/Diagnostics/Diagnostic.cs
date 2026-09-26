namespace BooleanRulesEngine.Diagnostics;

/// <summary>
/// One compile-time diagnostic: a code, a severity, a human-readable message, and a source span an
/// editor can underline (ADR-0002/ADR-0003). <c>RuleCompiler.Compile</c> never throws for an
/// authoring error — it reports diagnostics instead.
/// </summary>
/// <param name="Code">A stable identifier for this diagnostic's kind, e.g. <see cref="DiagnosticCodes.UnknownPredicate"/>.</param>
/// <param name="Severity">The diagnostic's severity.</param>
/// <param name="Message">A human-readable description.</param>
/// <param name="Span">The location in the original source text this diagnostic refers to.</param>
public sealed record Diagnostic(string Code, DiagnosticSeverity Severity, string Message, SourceSpan Span)
{
    /// <summary>Creates an <see cref="DiagnosticSeverity.Error"/>-severity diagnostic.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">A human-readable description.</param>
    /// <param name="span">The location in source text.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Error(string code, string message, SourceSpan span)
    {
        return new(code, DiagnosticSeverity.Error, message, span);
    }

    /// <summary>Creates a <see cref="DiagnosticSeverity.Warning"/>-severity diagnostic.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">A human-readable description.</param>
    /// <param name="span">The location in source text.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Warning(string code, string message, SourceSpan span)
    {
        return new(code, DiagnosticSeverity.Warning, message, span);
    }

    /// <summary>Creates an <see cref="DiagnosticSeverity.Info"/>-severity diagnostic.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">A human-readable description.</param>
    /// <param name="span">The location in source text.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Info(string code, string message, SourceSpan span)
    {
        return new(code, DiagnosticSeverity.Info, message, span);
    }
}
