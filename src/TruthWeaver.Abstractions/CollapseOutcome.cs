namespace TruthWeaver.Abstractions;

/// <summary>
/// The result of collapsing a three-valued <see cref="TruthValue"/> under a <see cref="CollapsePolicy"/>
/// (ADR-0005 decision 14): the two definite answers, plus <see cref="RejectedUnresolved"/> for the one policy
/// (<see cref="CollapsePolicy.UnknownIsError"/>) that refuses to guess.
/// </summary>
public enum CollapseOutcome
{
    /// <summary>The collapsed answer is false.</summary>
    False,

    /// <summary>The collapsed answer is true.</summary>
    True,

    /// <summary>
    /// The result was <see cref="TruthValue.Unknown"/> and the policy was <see cref="CollapsePolicy.UnknownIsError"/>:
    /// the rule could not be resolved either way. This is a normal outcome, not a <see cref="Fault"/>; it is not recorded
    /// in <see cref="Decision.Faults"/> and nothing is thrown.
    /// </summary>
    RejectedUnresolved,
}
