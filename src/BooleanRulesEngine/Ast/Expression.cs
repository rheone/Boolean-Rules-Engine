namespace BooleanRulesEngine.Ast;

using BooleanRulesEngine.Abstractions;

// SA1402 (one type per file) is intentionally relaxed here: these records are a single closed-set
// discriminated union (the operator set is closed by design, ADR-0004) and are far more readable
// read together than split across eight near-empty files.
#pragma warning disable SA1402

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

/// <summary>N-ary "exactly one of these operands is true".</summary>
/// <param name="Operands">The operands (at least two).</param>
public sealed record ExactlyOneExpression(EquatableArray<Expression> Operands) : Expression;

/// <summary>N-ary threshold: true iff at least <paramref name="K"/> operands are true.</summary>
/// <param name="K">The threshold, with <c>1 &lt;= K &lt;= Operands.Count</c>.</param>
/// <param name="Operands">The operands.</param>
public sealed record AtLeastExpression(int K, EquatableArray<Expression> Operands) : Expression;
