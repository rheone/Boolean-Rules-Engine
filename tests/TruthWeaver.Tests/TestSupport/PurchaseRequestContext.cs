namespace TruthWeaver.Tests.TestSupport;

/// <summary>
/// A minimal purchase-request context used to prove the externally-resolved-value predicate
/// pattern's single-sided shape (ticket 04): <see cref="Amount"/> is a plain value already on the
/// context, needing no resolution of its own, unlike the argument it's compared against.
/// </summary>
/// <param name="Amount">The requested purchase amount.</param>
public sealed record PurchaseRequestContext(decimal Amount);
