namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Abstractions;

/// <summary>
/// A worked example of the externally-resolved-value predicate pattern's single-sided shape
/// (ticket 04): the rule-text <c>costCenterCode</c> argument (a key, not an identity) is resolved
/// live to a spending limit via <see cref="IBudgetLookupService"/>; the amount it's compared against
/// is a plain value already on <see cref="PurchaseRequestContext"/>, needing no resolution of its own.
/// </summary>
/// <param name="budget">The injected live-lookup dependency, resolved fresh per evaluation (ADR-0002).</param>
public sealed class IsWithinBudget(IBudgetLookupService budget) : IPredicate<PurchaseRequestContext>
{
    private readonly IBudgetLookupService budget = budget;

    /// <inheritdoc />
    public static PredicateSchema Schema =>
        new(
            "isWithinBudget",
            "Is Within Budget",
            "Is the request's amount within the live spending limit resolved for the given cost center code?",
            [
                new PredicateArgumentSchema(
                    "costCenterCode",
                    "The cost center code to look up a live limit for.",
                    LiteralKind.String
                ),
            ]
        );

    /// <inheritdoc />
    public async ValueTask<TruthValue> EvaluateAsync(
        PurchaseRequestContext context,
        PredicateArguments args,
        CancellationToken cancellationToken
    )
    {
        string costCenterCode = args.GetString("costCenterCode");
        decimal limit = await this.budget.ResolveLimitAsync(costCenterCode, cancellationToken);
        return context.Amount <= limit ? TruthValue.True : TruthValue.False;
    }
}
