namespace TruthWeaver.Printing;

/// <summary>
/// How <see cref="PlainTextTreePrinter"/> and <see cref="MermaidTreePrinter"/> render the
/// <c>AND</c>/<c>OR</c>/<c>NOT</c>/<c>XOR</c>/<c>XNOR</c> operator labels of a rendered tree.
/// <see cref="Evaluation.RuleDescription"/> nodes for <c>ExactlyOne</c> and the threshold family (<c>AtLeast</c>,
/// <c>AtMost</c>, <c>GreaterThan</c>, <c>LessThan</c>, <c>Exactly</c>) always keep their
/// word/function-call form (e.g. <c>AtLeast(3)</c>), in every style — they have no symbolic or
/// C-style spelling to switch to.
/// </summary>
public enum OperatorStyle
{
    /// <summary>The default: <c>AND</c>, <c>OR</c>, <c>NOT</c>, <c>XOR</c>, <c>XNOR</c>.</summary>
    Word,

    /// <summary>Mathematical logic notation: <c>∧</c>, <c>∨</c>, <c>¬</c>, <c>⊕</c>, <c>↔</c>.</summary>
    Symbolic,

    /// <summary>C-family operator notation: <c>&amp;&amp;</c>, <c>||</c>, <c>!</c>, <c>^</c>, <c>==</c>.</summary>
    CStyle,
}
