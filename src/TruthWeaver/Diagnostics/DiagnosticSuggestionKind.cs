namespace TruthWeaver.Diagnostics;

/// <summary>What a <see cref="DiagnosticSuggestion"/> asks the author to do.</summary>
public enum DiagnosticSuggestionKind
{
    /// <summary>
    /// <see cref="DiagnosticSuggestion.Text"/> is the name or text the author most likely meant to write (a "did you
    /// mean" answer), for example the nearest known operator or predicate name.
    /// </summary>
    Replacement,

    /// <summary><see cref="DiagnosticSuggestion.Text"/> is plain-language advice, for example to add parentheses.</summary>
    Hint,
}
