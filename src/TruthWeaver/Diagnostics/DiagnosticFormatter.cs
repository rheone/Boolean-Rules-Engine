namespace TruthWeaver.Diagnostics;

using System.Text;

/// <summary>
/// Renders <see cref="Diagnostic"/>s as plain text for logs, consoles and editors. The same members a UI would read
/// from the structured <see cref="Diagnostic"/> are laid out as a header line, the offending source line with a caret
/// underline, then the expected/found pair and any suggestion:
/// <code>
/// BRE0001 error at line 1, column 3: Unexpected token 'ANDD' after end of expression.
///   a ANDD b
///     ^^^^
///   Expected: an operator or the end of the rule
///   Found: 'ANDD'
///   Did you mean: AND
/// </code>
/// Lines are separated by <c>\n</c>. A diagnostic located by a JSON/YAML path reads <c>at $.operands[1].op</c> instead.
/// </summary>
public static class DiagnosticFormatter
{
    /// <summary>Renders one diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to render.</param>
    /// <param name="source">
    /// The rule text that was compiled, or <see langword="null"/> if it is not to hand. With it the header gains a line
    /// and column and the source line is shown with a caret underline; without it the header shows the character offset.
    /// </param>
    /// <returns>The rendered text, without a trailing line break.</returns>
    public static string Format(Diagnostic diagnostic, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        StringBuilder text = new();
        bool hasSpan = HasLocation(diagnostic, source);
        text.Append(diagnostic.Code).Append(' ').Append(SeverityWord(diagnostic.Severity));

        string? where = Where(diagnostic, source, hasSpan);
        if (where is not null)
        {
            text.Append(" at ").Append(where);
        }

        text.Append(": ").Append(diagnostic.Message);

        if (hasSpan && source is not null)
        {
            AppendSourceLine(text, diagnostic.Span, source);
        }

        if (diagnostic.Expected is not null)
        {
            text.Append("\n  Expected: ").Append(diagnostic.Expected);
        }

        if (diagnostic.Found is not null)
        {
            text.Append("\n  Found: ").Append(diagnostic.Found);
        }

        if (diagnostic.Suggestion is { } suggestion)
        {
            text.Append(suggestion.Kind == DiagnosticSuggestionKind.Replacement ? "\n  Did you mean: " : "\n  Hint: ")
                .Append(suggestion.Text);
        }

        return text.ToString();
    }

    /// <summary>Renders several diagnostics, one block after another, in the order given.</summary>
    /// <param name="diagnostics">The diagnostics to render.</param>
    /// <param name="source">The rule text that was compiled, or <see langword="null"/>; see <see cref="Format(Diagnostic, string?)"/>.</param>
    /// <returns>The rendered text; empty when there are no diagnostics.</returns>
    public static string Format(IEnumerable<Diagnostic> diagnostics, string? source = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return string.Join("\n", diagnostics.Select(d => Format(d, source)));
    }

    private static string SeverityWord(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            _ => "info",
        };
    }

    // A zero-length span at offset 0 is SourceSpan.None, used for whole-rule findings, so it carries no location unless
    // the rule text is empty (where "line 1, column 1" is the honest answer for a missing operand).
    private static bool HasLocation(Diagnostic diagnostic, string? source)
    {
        return diagnostic.Span != SourceSpan.None || (source is { Length: 0 } && diagnostic.Path is null);
    }

    private static string? Where(Diagnostic diagnostic, string? source, bool hasSpan)
    {
        string? position = null;
        if (hasSpan)
        {
            position = source is null
                ? $"offset {diagnostic.Span.Start}"
                : $"line {diagnostic.Span.GetLocation(source).Line}, column {diagnostic.Span.GetLocation(source).Column}";
        }

        if (diagnostic.Path is null)
        {
            return position;
        }

        return position is null ? diagnostic.Path : $"{diagnostic.Path} ({position})";
    }

    private static void AppendSourceLine(StringBuilder text, SourceSpan span, string source)
    {
        SourceLocation location = span.GetLocation(source);
        int lineStart = Math.Clamp(span.Start, 0, source.Length) - (location.Column - 1);
        int lineEnd = lineStart;
        while (lineEnd < source.Length && source[lineEnd] is not ('\n' or '\r'))
        {
            lineEnd++;
        }

        // Tabs become single spaces so the caret column lines up whatever width the viewer gives a tab.
        string line = source[lineStart..lineEnd].Replace('\t', ' ');
        int carets = Math.Max(1, Math.Min(span.Length, lineEnd - (lineStart + location.Column - 1)));
        text.Append("\n  ").Append(line).Append("\n  ").Append(' ', location.Column - 1).Append('^', carets);
    }
}
