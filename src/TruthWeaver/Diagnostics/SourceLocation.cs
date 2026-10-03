namespace TruthWeaver.Diagnostics;

/// <summary>A human-oriented position in rule text: a 1-based line and a 1-based column (in UTF-16 characters).</summary>
/// <param name="Line">The 1-based line number. A line ends at <c>\n</c>, <c>\r</c> or <c>\r\n</c>.</param>
/// <param name="Column">The 1-based column within the line.</param>
public readonly record struct SourceLocation(int Line, int Column);
