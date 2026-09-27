namespace TruthWeaver.Ast;

/// <summary>
/// A short, human-friendly display label plus a human-readable description for one operator — the
/// operator-side counterpart to <see cref="Abstractions.PredicateSchema"/>'s required
/// <c>Label</c>/<c>Description</c>, so a rule-authoring UI or generated documentation can describe
/// every node of a compiled expression tree, not just its predicate leaves.
/// </summary>
/// <param name="Label">A short, human-friendly display name for the operator (e.g. "AND").</param>
/// <param name="Description">A human-readable description of what the operator means.</param>
public sealed record OperatorDescriptor(string Label, string Description);
