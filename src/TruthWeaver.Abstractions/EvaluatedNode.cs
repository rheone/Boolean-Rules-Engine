namespace TruthWeaver.Abstractions;

/// <summary>
/// A structural mirror of one node in a compiled expression tree, annotated with what happened to it
/// during one evaluation. Unlike <see cref="Trace"/> (a flat, evaluation-order log of leaves and
/// short-circuit-skip points), this preserves the tree shape itself, so every node — leaf or interior
/// operator — carries its own resolved result, letting a consumer (e.g. a diagram renderer) recover
/// the full logic, the path actually taken, and the parts left out by short-circuiting, without
/// re-deriving any of the engine's Kleene combine logic itself.
/// </summary>
/// <param name="NodeDescription">
/// The node's description — a term's identity text for a leaf, or the operator's name (e.g. <c>AND</c>)
/// for an interior node. Matches the text <see cref="TraceEntry.NodeDescription"/> would use for the
/// same node.
/// </param>
/// <param name="Result">
/// This node's resolved value, or <see langword="null"/> if <paramref name="NotEvaluated"/> is
/// <see langword="true"/>.
/// </param>
/// <param name="NotEvaluated">
/// <see langword="true"/> if this node was skipped by short-circuiting rather than evaluated. When
/// <see langword="true"/>, <paramref name="Children"/> is always empty — evaluation never recursed
/// into a skipped subtree, so nothing is known about its descendants beyond the static tree shape.
/// </param>
/// <param name="Children">
/// This node's operand results, in source order — empty for a leaf, or for any node where
/// <paramref name="NotEvaluated"/> is <see langword="true"/>.
/// </param>
public sealed record EvaluatedNode(
    string NodeDescription,
    TruthValue? Result,
    bool NotEvaluated,
    IReadOnlyList<EvaluatedNode> Children
);
