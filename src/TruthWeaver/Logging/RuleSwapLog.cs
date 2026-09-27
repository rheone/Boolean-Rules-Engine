namespace TruthWeaver.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
/// Structured log event for a rule-swap notification (ticket 13). The library does not own rule
/// storage or swap scheduling (ADR-0002) — it only logs what the host application tells it happened,
/// via <c>RuleCompiler.NotifyRuleSwapped</c>.
/// </summary>
internal static partial class RuleSwapLog
{
    /// <summary>Logs that a host-managed rule swap occurred.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="ruleIdentifier">A host-supplied identifier for the swapped rule.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Compiled rule '{RuleIdentifier}' was swapped to a new active instance."
    )]
    public static partial void RuleSwapped(ILogger logger, string ruleIdentifier);
}
