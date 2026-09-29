namespace TruthWeaver.Evaluation;

/// <summary>
/// A human-readable description of one node in a compiled expression tree — an operator's or a
/// predicate's <c>Label</c>/<c>Description</c>, plus its operands' own descriptions, recursively.
/// Produced by <see cref="CompiledRule{TContext}.Describe"/> so a rule-authoring UI or generated
/// documentation can render an entire compiled rule without needing access to the closed-set AST
/// types themselves (ADR-0004).
/// </summary>
/// <param name="Label">A short, human-friendly display name for this node.</param>
/// <param name="Description">A human-readable description of what this node means.</param>
/// <param name="Operands">This node's operands, described the same way, or empty for a leaf node.</param>
/// <param name="ArgumentText">
/// A term's rule-text arguments, rendered as comma-joined <c>name: value</c> pairs, or
/// <see langword="null"/> for an operator, a constant, or a zero-argument term.
/// </param>
public sealed record RuleDescription(
    string Label,
    string Description,
    IReadOnlyList<RuleDescription> Operands,
    string? ArgumentText = null
);
