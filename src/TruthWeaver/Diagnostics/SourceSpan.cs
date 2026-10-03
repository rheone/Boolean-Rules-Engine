namespace TruthWeaver.Diagnostics;

/// <summary>
/// A location within the original rule source text (DSL, JSON, or YAML), so an editor can underline
/// the offending token. Positions are 0-based character offsets into the source string that was
/// passed to the parser.
/// </summary>
/// <param name="Start">The 0-based offset of the first character in the span.</param>
/// <param name="Length">The number of characters the span covers.</param>
public readonly record struct SourceSpan(int Start, int Length)
{
    /// <summary>Gets a span representing "no specific location" (e.g. a whole-tree diagnostic).</summary>
    public static SourceSpan None { get; } = new(0, 0);

    /// <summary>Gets the offset one past the last character in this span.</summary>
    public int End => this.Start + this.Length;

    /// <summary>
    /// Converts the start of this span to a 1-based line and column in <paramref name="source"/>. The offsets in a span
    /// are into the exact string that was compiled, so pass that same string; an offset past the end of the text is
    /// clamped to the end.
    /// </summary>
    /// <param name="source">The rule text this span points into.</param>
    /// <returns>The line and column of <see cref="Start"/>.</returns>
    public SourceLocation GetLocation(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        int offset = Math.Clamp(this.Start, 0, source.Length);
        int line = 1;
        int lineStart = 0;
        for (int i = 0; i < offset; i++)
        {
            // "\r\n" is one break: the '\r' is skipped and the '\n' that follows it ends the line.
            if (source[i] == '\n' || (source[i] == '\r' && !(i + 1 < source.Length && source[i + 1] == '\n')))
            {
                line++;
                lineStart = i + 1;
            }
        }

        return new SourceLocation(line, offset - lineStart + 1);
    }
}
