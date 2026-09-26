namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// The compile-time-validated schema for one named argument of a predicate.
/// </summary>
/// <param name="Name">The argument name, as it appears in rule text (e.g. <c>role</c> in <c>hasRole(role: "Y")</c>).</param>
/// <param name="Type">The literal kind the argument's value must have.</param>
/// <param name="Required">Whether the argument must be supplied; if <see langword="false"/>, <paramref name="Default"/> is used when omitted.</param>
/// <param name="Default">The value used when the argument is omitted and <paramref name="Required"/> is <see langword="false"/>.</param>
public sealed record PredicateArgumentSchema(string Name, LiteralKind Type, bool Required = true, LiteralValue? Default = null);
