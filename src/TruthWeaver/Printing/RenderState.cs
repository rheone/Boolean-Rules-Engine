namespace TruthWeaver.Printing;

using TruthWeaver.Abstractions;

/// <summary>
/// A node's rendering state, independent of output format: whether it ran, what it resolved to, or
/// whether it (or an ancestor) was skipped by short-circuiting.
/// </summary>
internal enum RenderState
{
    /// <summary>No evaluation data was supplied — a purely structural render.</summary>
    NoData,

    /// <summary>Evaluated and resolved to <see cref="TruthValue.True"/>.</summary>
    True,

    /// <summary>Evaluated and resolved to <see cref="TruthValue.False"/>.</summary>
    False,

    /// <summary>Evaluated and resolved to <see cref="TruthValue.Unknown"/>.</summary>
    Indeterminate,

    /// <summary>Skipped by short-circuiting — either this node directly, or an ancestor of it.</summary>
    Skipped,
}
