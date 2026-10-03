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
        ["Equivalent"] = "equivalent",
        ["Implies"] = "implies",
        ["Nand"] = "nand",
        ["Nor"] = "nor",
        ["Nxor"] = "nxor",
        ["Any"] = "any",
        ["All"] = "all",
        ["None"] = "none",
        ["ExactlyOne"] = "exactlyOne",
        ["AtLeast"] = "atLeast",
        ["AtMost"] = "atMost",
        ["GreaterThan"] = "greaterThan",
        ["LessThan"] = "lessThan",
        ["Exactly"] = "exactly",
        ["Between"] = "between",
        ["Coalesce"] = "coalesce",
        ["If"] = "if",
        ["IsTrue"] = "isTrue",
        ["IsFalse"] = "isFalse",
        ["IsUnknown"] = "isUnknown",
        ["IsKnown"] = "isKnown",
        ["Project"] = "project",
        ["Collapse"] = "collapse",
    };

    private static readonly IReadOnlyDictionary<string, string> TreeFormatToCanonical = BuildReadTable();

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

    /// <summary>
    /// Builds the read-side table: every canonical tree-format string plus the input-only aliases. <c>xnor</c> (the
    /// pre-ADR-0005 name, kept so persisted rules still compile) and <c>iff</c> both read back as
    /// <c>Equivalent</c>; the printers only ever write <c>equivalent</c> (ADR-0005 decision 5).
    /// </summary>
    private static Dictionary<string, string> BuildReadTable()
    {
        Dictionary<string, string> table = CanonicalToTreeFormat.ToDictionary(
            pair => pair.Value,
            pair => pair.Key,
            StringComparer.OrdinalIgnoreCase
        );
        table["xnor"] = "Equivalent";
        table["iff"] = "Equivalent";
        return table;
    }
}
