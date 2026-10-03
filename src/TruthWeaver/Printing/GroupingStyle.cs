namespace TruthWeaver.Printing;

/// <summary>
/// Which grouping delimiters rule text is printed with (ADR-0005 decision 9). The DSL reads <c>()</c>, <c>[]</c> and <c>{}</c>
/// as the same thing, so this is purely presentation: every style re-parses to a tree equal to the original.
/// Function-call argument lists (<c>ANY(...)</c>) are call syntax, not grouping, and are always written with parentheses.
/// </summary>
public enum GroupingStyle
{
    /// <summary>The default and the canonical persisted form: every group is written with parentheses.</summary>
    Parentheses,

    /// <summary>
    /// A readability aid for deeply nested rules: the delimiter is chosen by how many groups enclose it, cycling
    /// <c>(</c> at the outermost level, then <c>[</c>, then <c>{</c>, then repeating, e.g.
    /// <c>a AND (b OR [c AND {d OR (e AND [f OR g])}])</c>.
    /// </summary>
    DepthCycling,
}
