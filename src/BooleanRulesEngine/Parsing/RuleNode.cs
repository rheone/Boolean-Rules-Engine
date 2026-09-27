namespace BooleanRulesEngine.Parsing;

using BooleanRulesEngine.Ast;
using BooleanRulesEngine.Diagnostics;

// SA1402 relaxed: a single closed-set discriminated union describing raw (pre-validation) parse
// output, shared identically by the DSL, JSON, and YAML front ends (tickets 06/07/08) so that
// "parse(print(x)) is structurally equal to x" and "JSON/YAML parse to the same AST as DSL" are true
// by construction rather than by parallel maintenance of three grammars.
#pragma warning disable SA1402

/// <summary>
/// A node in the raw parse tree produced by any of the three rule-text front ends, before predicate
/// names are resolved and argument literals are validated against a schema. <c>RuleNodeCompiler</c>
/// (in the <c>Compilation</c> namespace) is the single place that turns a <see cref="RuleNode"/> tree
/// into a validated <c>Ast.Expression</c> tree — every front end funnels through it, which is what
/// makes DSL/JSON/YAML round-tripping and cross-format equality a property of the design rather than
/// something tested for and hoped to remain true.
/// </summary>
/// <param name="Span">This node's location in the source it was parsed from.</param>
internal abstract record RuleNode(SourceSpan Span);

/// <summary>The literal <see langword="true"/>/<see langword="false"/> constant.</summary>
internal sealed record ConstantNode(bool Value, SourceSpan Span) : RuleNode(Span);

/// <summary>A raw predicate reference with its (not yet validated) named arguments.</summary>
internal sealed record TermNode(string PredicateName, IReadOnlyList<ArgumentNode> Arguments, SourceSpan Span) : RuleNode(Span);

/// <summary>One raw named argument of a <see cref="TermNode"/>.</summary>
internal sealed record ArgumentNode(string Name, RawLiteral Value, SourceSpan Span);

/// <summary>Logical negation.</summary>
internal sealed record NotNode(RuleNode Operand, SourceSpan Span) : RuleNode(Span);

/// <summary>Logical conjunction (raw operand count may be any size &gt;= 1; the compiler enforces &gt;= 2 was written).</summary>
internal sealed record AndNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>Logical disjunction.</summary>
internal sealed record OrNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// A raw <c>XOR</c> reference. Operand count is not yet validated here — a DSL infix chain
/// (<c>a XOR b XOR c</c>) or an explicit JSON/YAML <c>op: xor</c> node with the wrong operand count
/// both flow through as an <see cref="XorNode"/>, and <c>RuleNodeCompiler</c> uniformly rejects
/// anything other than exactly two operands (ADR-0003).
/// </summary>
internal sealed record XorNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// A raw <c>XNOR</c> reference (logical biconditional / <c>IFF</c>). Same operand-count validation
/// story as <see cref="XorNode"/>: <c>RuleNodeCompiler</c> uniformly rejects anything other than
/// exactly two operands.
/// </summary>
internal sealed record XnorNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>N-ary "exactly one of these is true".</summary>
internal sealed record ExactlyOneNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// N-ary count-threshold reference (<c>AtLeast</c>/<c>AtMost</c>/<c>GreaterThan</c>/<c>LessThan</c>/
/// <c>Exactly</c>); <paramref name="K"/>'s valid range is validated by the compiler, not the parser.
/// </summary>
internal sealed record ThresholdNode(ThresholdComparison Comparison, int K, IReadOnlyList<RuleNode> Operands, SourceSpan Span)
    : RuleNode(Span);

/// <summary>
/// A placeholder produced only after a syntax/structure error has already been reported, so parsing
/// can continue (and thus report further diagnostics in the same pass) without ever throwing.
/// </summary>
internal sealed record ErrorNode(SourceSpan Span) : RuleNode(Span);
