namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Abstractions;

/// <summary>
/// An independent Strong Kleene (K3) truth-table oracle for conformance tests. It computes the
/// expected result of an operator from the primitive definitions only (<c>NOT</c>, <c>AND</c>,
/// <c>OR</c>, and cardinality over the "definitely true / possibly true" interval) and deliberately
/// shares no code with <c>Evaluator</c>, so a defect in the engine cannot also hide in its own
/// expectation. See ADR-0005 and the k3-conformance spec.
/// </summary>
/// <remarks>
/// <c>False &lt; Unknown &lt; True</c> is used here only as an implementation aid (min/max for
/// <c>AND</c>/<c>OR</c>); it is not a numeric ordering of truth.
/// </remarks>
public static class K3Oracle
{
    /// <summary>Gets the three K3 values in a stable enumeration order.</summary>
    public static IReadOnlyList<TruthValue> Values { get; } = [TruthValue.False, TruthValue.Unknown, TruthValue.True];

    /// <summary>Every assignment of K3 values to <paramref name="arity"/> operands (3^arity rows).</summary>
    /// <param name="arity">The number of operands.</param>
    /// <returns>All assignments, first operand varying slowest.</returns>
    public static IEnumerable<TruthValue[]> Assignments(int arity)
    {
        int rows = 1;
        for (int i = 0; i < arity; i++)
        {
            rows *= Values.Count;
        }

        for (int row = 0; row < rows; row++)
        {
            TruthValue[] assignment = new TruthValue[arity];
            int remainder = row;
            for (int i = arity - 1; i >= 0; i--)
            {
                assignment[i] = Values[remainder % Values.Count];
                remainder /= Values.Count;
            }

            yield return assignment;
        }
    }

    /// <summary>Strong Kleene negation: swaps <c>True</c>/<c>False</c>, fixes <c>Unknown</c>.</summary>
    public static TruthValue Not(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => TruthValue.False,
            TruthValue.False => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }

    /// <summary>Strong Kleene conjunction: <c>False</c> dominates, then <c>Unknown</c>, else <c>True</c>.</summary>
    public static TruthValue And(IEnumerable<TruthValue> operands)
    {
        TruthValue result = TruthValue.True;
        foreach (TruthValue operand in operands)
        {
            if (operand == TruthValue.False)
            {
                return TruthValue.False;
            }

            if (operand == TruthValue.Unknown)
            {
                result = TruthValue.Unknown;
            }
        }

        return result;
    }

    /// <summary>Strong Kleene disjunction: <c>True</c> dominates, then <c>Unknown</c>, else <c>False</c>.</summary>
    public static TruthValue Or(IEnumerable<TruthValue> operands)
    {
        TruthValue result = TruthValue.False;
        foreach (TruthValue operand in operands)
        {
            if (operand == TruthValue.True)
            {
                return TruthValue.True;
            }

            if (operand == TruthValue.Unknown)
            {
                result = TruthValue.Unknown;
            }
        }

        return result;
    }

    /// <summary>Binary XOR: <c>Unknown</c> if either operand is <c>Unknown</c>, else exclusive-or of the two.</summary>
    public static TruthValue Xor(TruthValue left, TruthValue right)
    {
        return Or([And([left, Not(right)]), And([Not(left), right])]);
    }

    /// <summary>Binary biconditional: the negation of <see cref="Xor"/>.</summary>
    public static TruthValue Equivalent(TruthValue left, TruthValue right)
    {
        return Not(Xor(left, right));
    }

    /// <summary>
    /// Cardinality over the interval semantics: the true-count lies in
    /// <c>[definitelyTrue, definitelyTrue + unknown]</c>. The result is <c>True</c> if every count in
    /// that interval satisfies <paramref name="satisfies"/>, <c>False</c> if none does, else <c>Unknown</c>.
    /// </summary>
    /// <param name="satisfies">The predicate on the number of true operands.</param>
    /// <param name="operands">The operand values.</param>
    public static TruthValue Cardinality(Func<int, bool> satisfies, IReadOnlyList<TruthValue> operands)
    {
        int definitelyTrue = operands.Count(v => v == TruthValue.True);
        int unknown = operands.Count(v => v == TruthValue.Unknown);

        bool anySatisfies = false;
        bool allSatisfy = true;
        for (int count = definitelyTrue; count <= definitelyTrue + unknown; count++)
        {
            bool ok = satisfies(count);
            anySatisfies |= ok;
            allSatisfy &= ok;
        }

        if (allSatisfy)
        {
            return TruthValue.True;
        }

        return anySatisfies ? TruthValue.Unknown : TruthValue.False;
    }

    /// <summary>Exactly-one-true over the interval semantics (not parity).</summary>
    public static TruthValue ExactlyOne(IReadOnlyList<TruthValue> operands)
    {
        return Cardinality(c => c == 1, operands);
    }
}
