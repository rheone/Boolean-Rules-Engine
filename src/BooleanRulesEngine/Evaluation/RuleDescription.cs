namespace BooleanRulesEngine.Evaluation;

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
public sealed record RuleDescription(string Label, string Description, IReadOnlyList<RuleDescription> Operands);
