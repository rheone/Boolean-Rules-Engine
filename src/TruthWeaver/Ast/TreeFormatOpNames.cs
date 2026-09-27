namespace TruthWeaver.Ast;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// The single operator ⇄ tree-format-string lookup shared by the JSON and YAML tree printers and
/// parsers (ADR-0003's flat, key-discriminated tree shape), instead of each of the four maintaining
/// its own independent copy of the same op-name table. Built on top of the <see cref="NodeShape"/>
/// seam: a canonical op-name here is exactly a <see cref="NodeShape.OpName"/> value.
/// </summary>
internal static class TreeFormatOpNames
{
    private static readonly IReadOnlyDictionary<string, string> CanonicalToTreeFormat = new Dictionary<string, string>(
        StringComparer.Ordinal
    )
    {
        ["Not"] = "not",
        ["And"] = "and",
        ["Or"] = "or",
        ["Xor"] = "xor",
        ["Xnor"] = "xnor",
        ["ExactlyOne"] = "exactlyOne",
        ["AtLeast"] = "atLeast",
        ["AtMost"] = "atMost",
        ["GreaterThan"] = "greaterThan",
        ["LessThan"] = "lessThan",
        ["Exactly"] = "exactly",
    };

    private static readonly IReadOnlyDictionary<string, string> TreeFormatToCanonical = CanonicalToTreeFormat.ToDictionary(
        pair => pair.Value,
        pair => pair.Key,
        StringComparer.OrdinalIgnoreCase
    );

    /// <summary>Gets the tree-format op string for a node's canonical op-name (a <see cref="NodeShape.OpName"/> value).</summary>
    /// <param name="opName">The canonical op-name, e.g. <c>"And"</c> or, for a threshold, <c>"AtLeast"</c>.</param>
    /// <returns>The tree-format op string, e.g. <c>"and"</c> or <c>"atLeast"</c>.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="opName"/> is not a recognized canonical op-name.</exception>
    public static string ToTreeFormat(string opName)
    {
        return CanonicalToTreeFormat.TryGetValue(opName, out string? treeFormatName)
            ? treeFormatName
            : throw new InvalidOperationException($"Unhandled op-name '{opName}'.");
    }

    /// <summary>Attempts to resolve a tree-format op string (case-insensitive) back to its canonical op-name.</summary>
    /// <param name="treeFormatName">The op string as it appears in JSON/YAML tree text, e.g. <c>"atLeast"</c>.</param>
    /// <param name="opName">The canonical op-name (a <see cref="NodeShape.OpName"/> value) when resolved.</param>
    /// <returns><see langword="true"/> if <paramref name="treeFormatName"/> is a recognized op string.</returns>
    public static bool TryFromTreeFormat(string treeFormatName, [NotNullWhen(true)] out string? opName)
    {
        return TreeFormatToCanonical.TryGetValue(treeFormatName, out opName);
    }
}
