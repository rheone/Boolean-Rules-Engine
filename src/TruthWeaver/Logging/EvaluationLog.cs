namespace TruthWeaver.Logging;

using Microsoft.Extensions.Logging;

/// <summary>Structured log events for absorbed evaluation faults (ticket 13).</summary>
internal static partial class EvaluationLog
{
    /// <summary>Logs one absorbed predicate fault.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="term">The faulting term's canonical identity text.</param>
    /// <param name="message">The exception's message.</param>
    /// <param name="exception">The exception the predicate raised.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Predicate fault for term {Term}: {Message}",
        SkipEnabledCheck = false
    )]
    public static partial void PredicateFaulted(ILogger logger, string term, string message, Exception exception);
}
