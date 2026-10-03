namespace TruthWeaver.Parsing;

/// <summary>
/// Builds the paths that locate a JSON or YAML diagnostic: <c>$</c> is the document root, <c>.name</c> selects a key and
/// <c>[n]</c> a 0-based sequence item, as in <c>$.operands[1].op</c>. JSON and YAML use the same syntax so one reading
/// covers both; a key that is not a plain identifier is written <c>['key']</c>.
/// </summary>
internal static class TreePath
{
    /// <summary>The path of the document root.</summary>
    public const string Root = "$";

    /// <summary>Selects a key of the object or mapping at <paramref name="parent"/>.</summary>
    /// <param name="parent">The path of the object or mapping.</param>
    /// <param name="name">The key.</param>
    /// <returns>The key's path.</returns>
    public static string Property(string parent, string name)
    {
        return IsPlainKey(name) ? $"{parent}.{name}" : $"{parent}['{name.Replace("'", "\\'", StringComparison.Ordinal)}']";
    }

    /// <summary>Selects an item of the array or sequence at <paramref name="parent"/>.</summary>
    /// <param name="parent">The path of the array or sequence.</param>
    /// <param name="index">The 0-based index.</param>
    /// <returns>The item's path.</returns>
    public static string Index(string parent, int index)
    {
        return $"{parent}[{index}]";
    }

    // Identifier-like keys read naturally as .name; anything else needs the bracket form to stay unambiguous.
    private static bool IsPlainKey(string name)
    {
        return name.Length > 0
            && (char.IsAsciiLetter(name[0]) || name[0] == '_')
            && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');
    }
}
