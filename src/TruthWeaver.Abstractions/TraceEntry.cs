namespace TruthWeaver.Abstractions;

/// <summary>
/// One node visited (or explicitly skipped by short-circuiting) during an evaluation.
/// </summary>
/// <param name="NodeDescription">
/// The canonical text of the node — a term's identity text for a leaf, or the operator's name
/// (e.g. <c>AND</c>) for an interior node.
/// </param>
/// <param name="Result">
/// This node's resolved value, or <see langword="null"/> if <paramref name="NotEvaluated"/> is
/// <see langword="true"/>.
/// </param>
/// <param name="NotEvaluated">
/// <see langword="true"/> if this node was skipped by short-circuiting rather than evaluated — recorded
/// explicitly so a "why was I denied" trace has no unexplained holes (ADR-0002).
/// </param>
public sealed record TraceEntry(string NodeDescription, TruthValue? Result, bool NotEvaluated);
