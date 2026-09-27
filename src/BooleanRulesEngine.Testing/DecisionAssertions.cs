namespace BooleanRulesEngine.Testing;

using BooleanRulesEngine.Abstractions;

/// <summary>
/// Fluent assertions over a <see cref="Decision"/>, so a test reads "the decision should be
/// satisfied" instead of poking at <see cref="Decision.IsSatisfied"/> / <see cref="Decision.Faults"/>
/// directly. Every method returns <see langword="this"/> for chaining and throws
/// <see cref="DecisionAssertionException"/> on failure.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="DecisionAssertions"/> class.</remarks>
/// <param name="decision">The decision under test.</param>
public sealed class DecisionAssertions(Decision decision)
{
    /// <summary>Gets the decision under test.</summary>
    public Decision Subject { get; } = decision;

    /// <summary>Asserts that <see cref="Decision.IsSatisfied"/> is <see langword="true"/>.</summary>
    /// <returns>This instance, for chaining.</returns>
    public DecisionAssertions BeSatisfied()
    {
        if (!this.Subject.IsSatisfied)
        {
            throw new DecisionAssertionException(
                $"Expected the decision to be satisfied, but its result was '{this.Subject.Result}'."
            );
        }

        return this;
    }

    /// <summary>Asserts that <see cref="Decision.IsSatisfied"/> is <see langword="false"/>.</summary>
    /// <returns>This instance, for chaining.</returns>
    public DecisionAssertions NotBeSatisfied()
    {
        if (this.Subject.IsSatisfied)
        {
            throw new DecisionAssertionException(
                $"Expected the decision not to be satisfied, but its result was '{this.Subject.Result}'."
            );
        }

        return this;
    }

    /// <summary>Asserts that <see cref="Decision.Result"/> equals <paramref name="expected"/>.</summary>
    /// <param name="expected">The expected three-valued result.</param>
    /// <returns>This instance, for chaining.</returns>
    public DecisionAssertions HaveResult(TruthValue expected)
    {
        if (this.Subject.Result != expected)
        {
            throw new DecisionAssertionException(
                $"Expected the decision's result to be '{expected}', but it was '{this.Subject.Result}'."
            );
        }

        return this;
    }

    /// <summary>Asserts that <see cref="Decision.Faults"/> is empty.</summary>
    /// <returns>This instance, for chaining.</returns>
    public DecisionAssertions HaveNoFaults()
    {
        if (this.Subject.Faults.Count > 0)
        {
            throw new DecisionAssertionException(
                $"Expected the decision to have no faults, but it had {this.Subject.Faults.Count}: "
                    + string.Join(", ", this.Subject.Faults.Select(f => f.Term))
                    + "."
            );
        }

        return this;
    }

    /// <summary>Asserts that <see cref="Decision.Faults"/> is non-empty.</summary>
    /// <returns>This instance, for chaining.</returns>
    public DecisionAssertions HaveFault()
    {
        if (this.Subject.Faults.Count == 0)
        {
            throw new DecisionAssertionException("Expected the decision to have at least one fault, but it had none.");
        }

        return this;
    }

    /// <summary>Asserts that <see cref="Decision.Faults"/> has exactly <paramref name="expected"/> entries.</summary>
    /// <param name="expected">The expected fault count.</param>
    /// <returns>This instance, for chaining.</returns>
    public DecisionAssertions HaveFaultCount(int expected)
    {
        if (this.Subject.Faults.Count != expected)
        {
            throw new DecisionAssertionException(
                $"Expected the decision to have {expected} fault(s), but it had {this.Subject.Faults.Count}."
            );
        }

        return this;
    }

    /// <summary>Asserts that <see cref="Decision.Faults"/> contains a fault for the given predicate name.</summary>
    /// <param name="predicateName">The faulted term's predicate name, as registered.</param>
    /// <returns>This instance, for chaining.</returns>
    public DecisionAssertions HaveFaultForTerm(string predicateName)
    {
        if (!this.Subject.Faults.Any(f => string.Equals(f.Term.PredicateName, predicateName, StringComparison.Ordinal)))
        {
            throw new DecisionAssertionException(
                $"Expected the decision to have a fault for predicate '{predicateName}', but none was found among: "
                    + string.Join(", ", this.Subject.Faults.Select(f => f.Term))
                    + "."
            );
        }

        return this;
    }
}
