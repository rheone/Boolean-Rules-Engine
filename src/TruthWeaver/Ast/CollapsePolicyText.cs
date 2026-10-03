namespace TruthWeaver.Ast;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;

/// <summary>
/// The single spelling table for <see cref="CollapsePolicy"/> names, shared by the DSL parser and canonical printer, the
/// tree descriptions and the JSON/YAML printers and parsers so every layer agrees on how a policy is written and read
/// (the same role <see cref="TruthValueText"/> plays for the constants).
/// </summary>
internal static class CollapsePolicyText
{
    /// <summary>Gets the policy names as the DSL and diagnostics spell them, in declaration order.</summary>
    public static IReadOnlyList<string> Names { get; } =
    [nameof(CollapsePolicy.UnknownAsFalse), nameof(CollapsePolicy.UnknownAsTrue), nameof(CollapsePolicy.UnknownIsError)];

    /// <summary>Gets the canonical (upper camel) spelling used by the DSL printer and descriptions.</summary>
    /// <param name="policy">The policy.</param>
    /// <returns><c>UnknownAsFalse</c>, <c>UnknownAsTrue</c> or <c>UnknownIsError</c>.</returns>
    public static string Canonical(CollapsePolicy policy)
    {
        return policy switch
        {
            CollapsePolicy.UnknownAsFalse => "UnknownAsFalse",
            CollapsePolicy.UnknownAsTrue => "UnknownAsTrue",
            CollapsePolicy.UnknownIsError => "UnknownIsError",
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unhandled collapse policy."),
        };
    }

    /// <summary>Gets the lower camel spelling written to JSON/YAML trees (matching the op names, e.g. <c>isTrue</c>).</summary>
    /// <param name="policy">The policy.</param>
    /// <returns><c>unknownAsFalse</c>, <c>unknownAsTrue</c> or <c>unknownIsError</c>.</returns>
    public static string TreeFormat(CollapsePolicy policy)
    {
        string canonical = Canonical(policy);
        return char.ToLowerInvariant(canonical[0]) + canonical[1..];
    }

    /// <summary>
    /// Attempts to read a policy name in any letter case. Only the three names match: a numeric string that
    /// <c>Enum.TryParse</c> would accept (<c>"1"</c>) does not.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="policy">The policy when recognised.</param>
    /// <returns><see langword="true"/> if <paramref name="text"/> names a policy.</returns>
    public static bool TryParse([NotNullWhen(true)] string? text, out CollapsePolicy policy)
    {
        CollapsePolicy[] all = Enum.GetValues<CollapsePolicy>();
        int index = Array.FindIndex(all, c => string.Equals(text, Canonical(c), StringComparison.OrdinalIgnoreCase));
        policy = index >= 0 ? all[index] : default;
        return index >= 0;
    }
}
