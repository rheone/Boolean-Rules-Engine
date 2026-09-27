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
}
