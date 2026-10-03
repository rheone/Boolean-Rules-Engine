namespace TruthWeaver.Diagnostics;

/// <summary>
/// A machine-readable remedy attached to a <see cref="Diagnostic"/>: either the name the author most likely meant
/// ("did you mean") or a short piece of advice. Suggestions are computed deterministically from the rule text and the
/// known operator and predicate names, so the same input always yields the same suggestion.
/// </summary>
/// <param name="Kind">Whether <paramref name="Text"/> is a replacement candidate or advice.</param>
/// <param name="Text">The suggested replacement text, or the advice.</param>
public sealed record DiagnosticSuggestion(DiagnosticSuggestionKind Kind, string Text);
