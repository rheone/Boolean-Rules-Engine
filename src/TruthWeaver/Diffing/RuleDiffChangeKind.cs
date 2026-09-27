namespace TruthWeaver.Diffing;

/// <summary>What kind of structural change a <see cref="RuleDiffEntry"/> describes.</summary>
public enum RuleDiffChangeKind
{
    /// <summary>A node present in the "after" tree has no counterpart in the "before" tree.</summary>
    Added,

    /// <summary>A node present in the "before" tree has no counterpart in the "after" tree.</summary>
    Removed,

    /// <summary>
    /// The node at this position differs between the two trees — a different operator, a different
    /// threshold <c>K</c>, a different constant value, or a different term (predicate name or
    /// arguments). The subtree below a changed node is not diffed further; the whole node is reported
    /// as replaced.
    /// </summary>
    Changed,
}
