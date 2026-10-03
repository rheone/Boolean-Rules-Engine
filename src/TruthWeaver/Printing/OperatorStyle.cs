namespace TruthWeaver.Printing;

/// <summary>
/// How <see cref="PlainTextTreePrinter"/> and <see cref="MermaidTreePrinter"/> render the
/// <c>AND</c>/<c>OR</c>/<c>NOT</c>/<c>XOR</c>/<c>EQUIVALENT</c>/<c>IMPLIES</c>/<c>NAND</c>/<c>NOR</c>/<c>COALESCE</c> operator labels of a rendered tree.
/// <see cref="Evaluation.RuleDescription"/> nodes for <c>ExactlyOne</c>, <c>BETWEEN</c>, <c>If</c>, <c>Project</c>, the <c>Collapse</c> boundary, the inspections (<c>IsTrue</c>, <c>IsFalse</c>, <c>IsUnknown</c>, <c>IsKnown</c>) and the threshold family (<c>AtLeast</c>,
/// <c>AtMost</c>, <c>GreaterThan</c>, <c>LessThan</c>, <c>Exactly</c>) always keep their
/// word/function-call form (e.g. <c>AtLeast(3)</c>), in every style — they have no symbolic or
/// C-style spelling to switch to.
/// </summary>
public enum OperatorStyle
{
    /// <summary>The default: <c>AND</c>, <c>OR</c>, <c>NOT</c>, <c>XOR</c>, <c>EQUIVALENT</c>, <c>IMPLIES</c>, <c>NAND</c>, <c>NOR</c>, <c>COALESCE</c>.</summary>
    Word,

    /// <summary>Mathematical logic notation: <c>∧</c>, <c>∨</c>, <c>¬</c>, <c>⊕</c>, <c>↔</c>, <c>→</c>, <c>↑</c>, <c>↓</c>, <c>??</c>.</summary>
    Symbolic,

    /// <summary>C-family operator notation: <c>&amp;&amp;</c>, <c>||</c>, <c>!</c>, <c>^</c>, <c>==</c>; <c>??</c> for <c>COALESCE</c>; <c>IMPLIES</c>, <c>NAND</c> and <c>NOR</c> have no C-family spelling and keep their word forms. Caveat: <c>==</c> for <c>EQUIVALENT</c> follows C only for known values; in C# <c>null == null</c> is <see langword="true"/>, but <c>Unknown EQUIVALENT Unknown</c> is <c>Unknown</c>, so treat this style as a rendering aid, not as C# semantics.</summary>
    CStyle,
}
