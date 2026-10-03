namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Abstractions;

/// <summary>A generated rule: its DSL text, an oracle-built evaluation over terms a..c, and its operands.</summary>
public sealed record GeneratedRule(
    string Text,
    Func<IReadOnlyList<TruthValue>, TruthValue> Eval,
    IReadOnlyList<GeneratedRule> Children
)
{
    /// <summary>Whether the rule is True (resp. False) in every {True, False, Unknown} assignment of a, b, c.</summary>
    public (bool Tautology, bool Contradiction) Verdict()
    {
        bool always = true;
        bool never = true;
        foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
        {
            TruthValue value = this.Eval(assignment);
            always &= value == TruthValue.True;
            never &= value == TruthValue.False;
        }

        return (always, never);
    }
}
