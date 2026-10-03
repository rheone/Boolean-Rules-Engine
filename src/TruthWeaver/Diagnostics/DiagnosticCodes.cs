namespace TruthWeaver.Diagnostics;

/// <summary>Well-known <see cref="Diagnostic.Code"/> values produced by the compiler pipeline.</summary>
public static class DiagnosticCodes
{
    /// <summary>A syntax error while parsing rule text.</summary>
    public const string SyntaxError = "BRE0001";

    /// <summary>A term references a predicate name with no matching registration.</summary>
    public const string UnknownPredicate = "BRE0002";

    /// <summary>A term omits a required argument declared by the predicate's schema.</summary>
    public const string MissingArgument = "BRE0003";

    /// <summary>A term supplies an argument whose value's kind does not match the predicate's schema.</summary>
    public const string ArgumentTypeMismatch = "BRE0004";

    /// <summary>A term supplies an argument name the predicate's schema does not declare.</summary>
    public const string UnknownArgument = "BRE0005";

    /// <summary><c>XOR</c>, <c>EQUIVALENT</c> (<c>XNOR</c>), <c>IMPLIES</c>, <c>NAND</c> or <c>NOR</c> was given other than exactly two operands.</summary>
    public const string InfixArityViolation = "BRE0006";

    /// <summary>
    /// An infix operator other than <c>NOT</c>/<c>AND</c>/<c>OR</c> (<c>XOR</c>, <c>XNOR</c>, ...) was combined
    /// with <c>AND</c>/<c>OR</c> or with a different such operator at the same syntactic level without
    /// parentheses (ADR-0005 decision 8). The span is the offending operator, or the bare infix expression.
    /// </summary>
    public const string AmbiguousOperatorMixing = "BRE0007";

    /// <summary>
    /// A count-threshold operator (<c>AtLeast</c>/<c>AtMost</c>/<c>GreaterThan</c>/<c>LessThan</c>/
    /// <c>Exactly</c>) was given a <c>k</c> that makes it a structural constant for its operand count.
    /// </summary>
    public const string InvalidThresholdValue = "BRE0008";

    /// <summary>The expression tree exceeds <c>CompilerOptions.MaxDepth</c>.</summary>
    public const string MaxDepthExceeded = "BRE0009";

    /// <summary>The expression tree exceeds <c>CompilerOptions.MaxNodeCount</c>.</summary>
    public const string MaxNodeCountExceeded = "BRE0010";

    /// <summary>Constant/contradiction analysis was skipped because the term count exceeds <c>CompilerOptions.MaxAnalysisTerms</c>.</summary>
    public const string AnalysisSkippedTooManyTerms = "BRE0011";

    /// <summary>
    /// The Strong K3 analyzer found a sub-expression that is <c>True</c> for every <c>{True, False, Unknown}</c>
    /// assignment of its terms (a K3 tautology). <c>A OR NOT A</c> is not one: it is <c>Unknown</c> when
    /// <c>A</c> is. The name is historical ("structural" as opposed to evaluated) and kept so the code and
    /// constant are stable.
    /// </summary>
    public const string StructuralTautology = "BRE0012";

    /// <summary>
    /// The Strong K3 analyzer found a sub-expression that is <c>False</c> for every <c>{True, False, Unknown}</c>
    /// assignment of its terms (a K3 contradiction). <c>A AND NOT A</c> is not one: it is <c>Unknown</c> when
    /// <c>A</c> is. The name is historical ("structural" as opposed to evaluated) and kept so the code and
    /// constant are stable.
    /// </summary>
    public const string StructuralContradiction = "BRE0013";

    /// <summary>The tree/JSON/YAML source is malformed independently of DSL syntax (e.g. unknown <c>op</c>, missing discriminator key).</summary>
    public const string MalformedTree = "BRE0014";

    /// <summary>A string literal in DSL rule text contains a <c>\</c> not followed by one of the supported escapes (<c>\"</c>, <c>\\</c>, <c>\n</c>, <c>\t</c>).</summary>
    public const string InvalidEscapeSequence = "BRE0015";

    /// <summary>
    /// A <c>Collapse(expr, policy)</c> appears anywhere other than as the outermost expression of a rule (ADR-0005 decision
    /// 14). Collapse is the final evaluation boundary, so it cannot sit inside another operator, a <c>Project</c>, a branch
    /// or another <c>Collapse</c>. The span is the nested collapse expression.
    /// </summary>
    public const string NestedCollapse = "BRE0016";
}
