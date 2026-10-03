namespace TruthWeaver.Abstractions;

/// <summary>
/// How an application turns a three-valued <see cref="TruthValue"/> into a final answer at the evaluation boundary
/// (ADR-0005 decision 14). <see cref="TruthValue.Unknown"/> is a normal Strong Kleene value, not a failure, so this choice
/// is always explicit: the engine never converts <see cref="TruthValue.Unknown"/> to <see cref="TruthValue.True"/> or
/// <see cref="TruthValue.False"/> on its own.
/// </summary>
public enum CollapsePolicy
{
    /// <summary>
    /// <see cref="TruthValue.Unknown"/> becomes <see cref="CollapseOutcome.False"/>: only a definite
    /// <see cref="TruthValue.True"/> is accepted. The fail-closed choice, matching <see cref="Decision.IsSatisfied"/>.
    /// </summary>
    UnknownAsFalse,

    /// <summary>
    /// <see cref="TruthValue.Unknown"/> becomes <see cref="CollapseOutcome.True"/>: only a definite
    /// <see cref="TruthValue.False"/> is refused. Fail-open, so choose it deliberately.
    /// </summary>
    UnknownAsTrue,

    /// <summary>
    /// <see cref="TruthValue.Unknown"/> becomes <see cref="CollapseOutcome.RejectedUnresolved"/>, an explicit
    /// "not known" outcome. It is neither a <see cref="Fault"/> nor an exception, so a caller can tell "the answer is not
    /// known" from "something broke".
    /// </summary>
    UnknownIsError,
}
