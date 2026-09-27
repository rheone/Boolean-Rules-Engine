namespace TruthWeaver.Ast;

using TruthWeaver.Abstractions;

// SA1402 (one type per file) is intentionally relaxed here: these records are a single closed-set
// discriminated union (the operator set is closed by design, ADR-0004) and are far more readable
// read together than split across eight near-empty files.
#pragma warning disable SA1402

/// <summary>Which comparison a <see cref="ThresholdExpression"/> applies against its true-operand count.</summary>
public enum ThresholdComparison
{
    /// <summary><c>AtLeast(k, ...)</c>: true iff the true-operand count is <c>&gt;= k</c>.</summary>
    AtLeast,

    /// <summary><c>AtMost(k, ...)</c>: true iff the true-operand count is <c>&lt;= k</c>.</summary>
    AtMost,

    /// <summary><c>GreaterThan(k, ...)</c>: true iff the true-operand count is <c>&gt; k</c>.</summary>
    GreaterThan,

    /// <summary><c>LessThan(k, ...)</c>: true iff the true-operand count is <c>&lt; k</c>.</summary>
    LessThan,

    /// <summary><c>Exactly(k, ...)</c>: true iff the true-operand count is exactly <c>k</c>.</summary>
    Exactly,
}

/// <summary>
/// The base of the immutable expression tree a <c>CompiledRule</c> wraps (CONTEXT.md's conceptual
/// model). Every node type below is a closed set by design (ADR-0004) — adding an operator is a
/// versioned change to this package, not a plugin point.
/// </summary>
public abstract record Expression
{
    private protected Expression() { }
}

/// <summary>The literal <see langword="true"/>/<see langword="false"/> constant.</summary>
/// <param name="Value">The constant's value.</param>
public sealed record ConstantExpression(bool Value) : Expression;

/// <summary>A leaf node: a predicate bound to concrete, validated arguments.</summary>
/// <param name="Identity">This term's identity — the unit of memoization and structural equality.</param>
/// <param name="IsUnknownPredicate">
/// <see langword="true"/> when this term was compiled under <c>CompilationMode.Lenient</c> against an
/// unregistered predicate name; such a term always evaluates to <see cref="TruthValue.Unknown"/> and
/// never invokes anything (ticket 09).
/// </param>
public sealed record TermExpression(TermIdentity Identity, bool IsUnknownPredicate = false) : Expression;

/// <summary>Logical negation. <c>NOT</c> binds tighter than <c>AND</c>, which binds tighter than <c>OR</c>.</summary>
/// <param name="Operand">The negated sub-expression.</param>
public sealed record NotExpression(Expression Operand) : Expression;

/// <summary>Logical conjunction, left-to-right, short-circuiting at the first <see langword="false"/> operand.</summary>
/// <param name="Operands">The conjuncts, in source order (at least two).</param>
public sealed record AndExpression(EquatableArray<Expression> Operands) : Expression;

/// <summary>Logical disjunction, left-to-right, short-circuiting at the first <see langword="true"/> operand.</summary>
/// <param name="Operands">The disjuncts, in source order (at least two).</param>
public sealed record OrExpression(EquatableArray<Expression> Operands) : Expression;

/// <summary>Binary exclusive-or — deliberately not generalized to n-ary parity (ADR-0003).</summary>
/// <param name="Left">The left operand.</param>
/// <param name="Right">The right operand.</param>
public sealed record XorExpression(Expression Left, Expression Right) : Expression;

/// <summary>
/// Binary exclusive-nor (logical biconditional / <c>IFF</c>) — the negation of <see cref="XorExpression"/>,
/// deliberately not generalized to n-ary parity for the same reason <c>XOR</c> isn't (ADR-0003).
/// </summary>
/// <param name="Left">The left operand.</param>
/// <param name="Right">The right operand.</param>
public sealed record XnorExpression(Expression Left, Expression Right) : Expression;

/// <summary>N-ary "exactly one of these operands is true".</summary>
/// <param name="Operands">The operands (at least two).</param>
public sealed record ExactlyOneExpression(EquatableArray<Expression> Operands) : Expression;

/// <summary>
/// N-ary count-threshold operator: <c>AtLeast(k, ...)</c>, <c>AtMost(k, ...)</c>,
/// <c>GreaterThan(k, ...)</c>, <c>LessThan(k, ...)</c>, and <c>Exactly(k, ...)</c> all compile to
/// this one node, parameterized by <see cref="ThresholdComparison"/> — they differ only in which
/// comparison against the count of true operands they apply.
/// </summary>
/// <param name="Comparison">Which comparison against the true-operand count this threshold applies.</param>
/// <param name="K">The threshold value being compared against. Valid range depends on <paramref name="Comparison"/> and the operand count — see <c>RuleNodeCompiler</c>.</param>
/// <param name="Operands">The operands.</param>
public sealed record ThresholdExpression(ThresholdComparison Comparison, int K, EquatableArray<Expression> Operands)
    : Expression;
