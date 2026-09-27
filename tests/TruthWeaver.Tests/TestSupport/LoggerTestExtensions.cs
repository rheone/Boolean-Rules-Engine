namespace TruthWeaver.Tests.TestSupport;

using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;

/// <summary>
/// Inspects calls recorded on a substitute <see cref="ILogger"/> without needing to know the
/// compiler-generated <c>TState</c> type each <c>[LoggerMessage]</c> partial method uses internally
/// — NSubstitute records every call via reflection regardless of its generic instantiation, so
/// assertions here work against the structured field values themselves, never the rendered message
/// text (ticket 13).
/// </summary>
public static class LoggerTestExtensions
{
    /// <summary>Gets every logged call's level, event id, structured state fields, and exception.</summary>
    /// <param name="logger">The substitute logger.</param>
    /// <returns>One entry per <c>ILogger.Log</c> invocation actually recorded.</returns>
    public static IReadOnlyList<LoggedCall> GetLoggedCalls(this ILogger logger)
    {
        return [.. logger.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).Select(ToLoggedCall)];
    }

    private static LoggedCall ToLoggedCall(ICall call)
    {
        object?[] arguments = call.GetArguments();
        LogLevel level = (LogLevel)arguments[0]!;
        EventId eventId = (EventId)arguments[1]!;
        object state = arguments[2]!;
        Exception? exception = (Exception?)arguments[3];

        IReadOnlyList<KeyValuePair<string, object?>> fields = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
        return new LoggedCall(level, eventId, fields, exception);
    }

    /// <summary>One recorded structured log call.</summary>
    /// <param name="Level">The log level.</param>
    /// <param name="EventId">The event id.</param>
    /// <param name="Fields">The named structured fields (from the <c>[LoggerMessage]</c> template's placeholders).</param>
    /// <param name="Exception">The logged exception, if any.</param>
    public sealed record LoggedCall(
        LogLevel Level,
        EventId EventId,
        IReadOnlyList<KeyValuePair<string, object?>> Fields,
        Exception? Exception
    )
    {
        /// <summary>Gets a named field's value.</summary>
        /// <param name="name">The field name.</param>
        /// <returns>The field's value.</returns>
        public object? Field(string name)
        {
            return this.Fields.First(f => f.Key == name).Value;
        }
    }
}
