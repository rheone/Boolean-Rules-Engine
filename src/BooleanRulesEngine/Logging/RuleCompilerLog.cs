namespace BooleanRulesEngine.Logging;

using BooleanRulesEngine.Diagnostics;
using Microsoft.Extensions.Logging;

/// <summary>
/// Structured log events for compile diagnostics (ticket 13) — named fields, not just an
/// interpolated message, so a host's chosen provider (Serilog or otherwise) can query on them.
/// </summary>
internal static partial class RuleCompilerLog
{
    /// <summary>Logs one compile diagnostic.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="level">The log level, derived from the diagnostic's severity.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="diagnosticMessage">The diagnostic's human-readable message.</param>
    /// <param name="spanStart">The diagnostic's source span start offset.</param>
    /// <param name="spanLength">The diagnostic's source span length.</param>
    [LoggerMessage(
        EventId = 1,
        Message = "Rule compilation diagnostic {Code} ({Severity}) at [{SpanStart},{SpanLength}): {DiagnosticMessage}"
    )]
    public static partial void DiagnosticProduced(
        ILogger logger,
        LogLevel level,
        string code,
        DiagnosticSeverity severity,
        string diagnosticMessage,
        int spanStart,
        int spanLength
    );

    /// <summary>Maps a diagnostic severity to its logging level.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <returns>The corresponding log level.</returns>
    public static LogLevel ToLogLevel(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => LogLevel.Error,
            DiagnosticSeverity.Warning => LogLevel.Warning,
            _ => LogLevel.Information,
        };
    }
}
