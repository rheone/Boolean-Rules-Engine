namespace TruthWeaver.Diagnostics;

/// <summary>
/// One compile-time diagnostic: a stable code, a severity, a plain-language message, where the problem is, and, where
/// they apply, what was expected versus found and a suggested remedy (ADR-0002, ADR-0003, ADR-0005 decision 11).
/// <c>RuleCompiler.Compile</c> never throws for an authoring error — it reports diagnostics instead. The structure is
/// there so a UI can lay a diagnostic out itself; <see cref="DiagnosticFormatter"/> renders the same data as plain text.
/// </summary>
/// <param name="Code">A stable identifier for this diagnostic's kind, e.g. <see cref="DiagnosticCodes.UnknownPredicate"/>.</param>
/// <param name="Severity">The diagnostic's severity.</param>
/// <param name="Message">A plain-language explanation of the problem.</param>
/// <param name="Span">The location in the original source text this diagnostic refers to.</param>
public sealed record Diagnostic(string Code, DiagnosticSeverity Severity, string Message, SourceSpan Span)
{
    /// <summary>
    /// Gets the location of the problem in a JSON or YAML rule as a path from the document root, such as
    /// <c>$.operands[1].op</c> (an RFC 9535 JSONPath singular query; plain identifier keys use <c>.key</c>, other keys use <c>['key']</c>, array items use <c>[n]</c>); <see langword="null"/> for DSL rule text, which is located by <see cref="Span"/>.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>Gets what the compiler needed at this location (for example <c>')'</c> or <c>2 operands</c>), or <see langword="null"/> when that does not apply.</summary>
    public string? Expected { get; init; }

    /// <summary>Gets what was actually there (for example <c>']'</c>, <c>end of rule</c> or <c>3 operands</c>), or <see langword="null"/> when that does not apply.</summary>
    public string? Found { get; init; }

    /// <summary>Gets a "did you mean" replacement or a short piece of advice, or <see langword="null"/> when there is none.</summary>
    public DiagnosticSuggestion? Suggestion { get; init; }

    /// <summary>Creates an <see cref="DiagnosticSeverity.Error"/>-severity diagnostic.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">A plain-language explanation.</param>
    /// <param name="span">The location in source text.</param>
    /// <param name="expected">What was expected, if that applies.</param>
    /// <param name="found">What was found, if that applies.</param>
    /// <param name="suggestion">A suggested remedy, if there is one.</param>
    /// <param name="path">The JSON/YAML path of the problem, for tree front ends.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Error(
        string code,
        string message,
        SourceSpan span,
        string? expected = null,
        string? found = null,
        DiagnosticSuggestion? suggestion = null,
        string? path = null
    )
    {
        return Create(code, DiagnosticSeverity.Error, message, span, expected, found, suggestion, path);
    }

    /// <summary>Creates a <see cref="DiagnosticSeverity.Warning"/>-severity diagnostic.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">A plain-language explanation.</param>
    /// <param name="span">The location in source text.</param>
    /// <param name="expected">What was expected, if that applies.</param>
    /// <param name="found">What was found, if that applies.</param>
    /// <param name="suggestion">A suggested remedy, if there is one.</param>
    /// <param name="path">The JSON/YAML path of the problem, for tree front ends.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Warning(
        string code,
        string message,
        SourceSpan span,
        string? expected = null,
        string? found = null,
        DiagnosticSuggestion? suggestion = null,
        string? path = null
    )
    {
        return Create(code, DiagnosticSeverity.Warning, message, span, expected, found, suggestion, path);
    }

    /// <summary>Creates an <see cref="DiagnosticSeverity.Info"/>-severity diagnostic.</summary>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="message">A plain-language explanation.</param>
    /// <param name="span">The location in source text.</param>
    /// <param name="expected">What was expected, if that applies.</param>
    /// <param name="found">What was found, if that applies.</param>
    /// <param name="suggestion">A suggested remedy, if there is one.</param>
    /// <param name="path">The JSON/YAML path of the problem, for tree front ends.</param>
    /// <returns>The diagnostic.</returns>
    public static Diagnostic Info(
        string code,
        string message,
        SourceSpan span,
        string? expected = null,
        string? found = null,
        DiagnosticSuggestion? suggestion = null,
        string? path = null
    )
    {
        return Create(code, DiagnosticSeverity.Info, message, span, expected, found, suggestion, path);
    }

    private static Diagnostic Create(
        string code,
        DiagnosticSeverity severity,
        string message,
        SourceSpan span,
        string? expected,
        string? found,
        DiagnosticSuggestion? suggestion,
        string? path
    )
    {
        return new Diagnostic(code, severity, message, span)
        {
            Path = path,
            Expected = expected,
            Found = found,
            Suggestion = suggestion,
        };
    }
}
