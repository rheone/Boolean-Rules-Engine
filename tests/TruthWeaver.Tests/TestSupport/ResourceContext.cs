namespace TruthWeaver.Tests.TestSupport;

/// <summary>
/// A minimal resource-scoped application context — deliberately has no user field at all, so tests
/// using it can't accidentally lean on "the context is the current user" (externally-resolved-value
/// predicates ticket 03).
/// </summary>
/// <param name="ResourceId">The resource's identity — itself a key to be resolved, not a party.</param>
public sealed record ResourceContext(Guid ResourceId);
