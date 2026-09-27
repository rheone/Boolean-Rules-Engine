namespace TruthWeaver.Diffing;

using TruthWeaver.Evaluation;

/// <summary>
/// One structural difference between two compiled rules, located by its <see cref="Path"/> from each
/// tree's root.
/// </summary>
/// <param name="Kind">What kind of change this is.</param>
/// <param name="Path">
/// The zero-based operand-index chain from the root to this node, e.g. <c>[1, 0]</c> for "the first
/// operand of the second operand of the root". Empty for the root itself.
/// </param>
/// <param name="Before">
/// The node's description in the "before" tree, or <see langword="null"/> for <see cref="RuleDiffChangeKind.Added"/>.
/// </param>
/// <param name="After">
/// The node's description in the "after" tree, or <see langword="null"/> for <see cref="RuleDiffChangeKind.Removed"/>.
/// </param>
public sealed record RuleDiffEntry(
    RuleDiffChangeKind Kind,
    IReadOnlyList<int> Path,
    RuleDescription? Before,
    RuleDescription? After
);
