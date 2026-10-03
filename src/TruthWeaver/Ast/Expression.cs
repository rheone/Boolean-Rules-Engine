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

/// <summary>Which K3 state an <see cref="InspectionExpression"/> tests its operand for.</summary>
public enum InspectionKind
{
    /// <summary><c>IsTrue(x)</c>: <c>True</c> iff the operand is <c>True</c>.</summary>
    IsTrue,

    /// <summary><c>IsFalse(x)</c>: <c>True</c> iff the operand is <c>False</c>.</summary>
    IsFalse,

    /// <summary><c>IsUnknown(x)</c>: <c>True</c> iff the operand is <c>Unknown</c>.</summary>
    IsUnknown,

    /// <summary><c>IsKnown(x)</c>: <c>True</c> iff the operand is <c>True</c> or <c>False</c> (not <c>Unknown</c>).</summary>
    IsKnown,
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

/// <summary>
/// A literal K3 constant: <see cref="TruthValue.True"/>, <see cref="TruthValue.False"/> or
/// <see cref="TruthValue.Unknown"/> (ADR-0005). Written <c>True</c>/<c>False</c>/<c>Unknown</c> in any letter case.
/// </summary>
/// <param name="Value">The constant's value.</param>
public sealed record ConstantExpression(TruthValue Value) : Expression;

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

/// <summary>Binary exclusive-or. Three or more operands are a compile error; <see cref="NxorExpression"/> is the n-ary parity operator (ADR-0005 decisions 4 and 7).</summary>
/// <param name="Left">The left operand.</param>
/// <param name="Right">The right operand.</param>
public sealed record XorExpression(Expression Left, Expression Right) : Expression;

/// <summary>
/// The Strong Kleene biconditional (<c>EQUIVALENT</c>, written <c>IFF</c> or <c>↔</c> as well; the legacy name is
/// <c>XNOR</c>) — the negation of <see cref="XorExpression"/>, so it is <c>Unknown</c> whenever either operand is.
/// Deliberately binary: it is not generalized to n-ary parity for the same reason <c>XOR</c> isn't (ADR-0003).
/// Formerly <c>XnorExpression</c> (renamed by ADR-0005 decision 5).
/// </summary>
/// <param name="Left">The left operand.</param>
/// <param name="Right">The right operand.</param>
public sealed record EquivalentExpression(Expression Left, Expression Right) : Expression;

/// <summary>
/// Strong Kleene material implication, <c>NOT antecedent OR consequent</c> (ADR-0005). It is a first-class node
/// rather than sugar, so it round-trips as written; its primitive definition drives the analyzer and the
/// conformance oracle. A <c>False</c> antecedent or a <c>True</c> consequent yields <c>True</c>; otherwise an
/// <c>Unknown</c> operand makes the result <c>Unknown</c>.
/// </summary>
/// <param name="Antecedent">The "if" operand, evaluated first.</param>
/// <param name="Consequent">The "then" operand.</param>
public sealed record ImpliesExpression(Expression Antecedent, Expression Consequent) : Expression;

/// <summary>
/// Strong Kleene negated conjunction, <c>NOT (left AND right)</c> (ADR-0005). A first-class binary node so it
/// round-trips as written; <c>False</c> if both operands are <c>True</c>, <c>True</c> if either is <c>False</c>,
/// otherwise <c>Unknown</c>. Both operands are always evaluated (no short-circuit).
/// </summary>
/// <param name="Left">The left operand.</param>
/// <param name="Right">The right operand.</param>
public sealed record NandExpression(Expression Left, Expression Right) : Expression;

/// <summary>
/// Strong Kleene negated disjunction, <c>NOT (left OR right)</c> (ADR-0005). A first-class binary node so it
/// round-trips as written; <c>True</c> if both operands are <c>False</c>, <c>False</c> if either is <c>True</c>,
/// otherwise <c>Unknown</c>. Both operands are always evaluated (no short-circuit).
/// </summary>
/// <param name="Left">The left operand.</param>
/// <param name="Right">The right operand.</param>
public sealed record NorExpression(Expression Left, Expression Right) : Expression;

/// <summary>
/// N-ary Strong Kleene parity (<c>NXOR(a, b, ...)</c>, ADR-0005 decision 4): <c>True</c> when an odd number of
/// operands are <c>True</c> and none is <c>Unknown</c>, <c>False</c> when an even number are <c>True</c> and none is
/// <c>Unknown</c>, and <c>Unknown</c> whenever any operand is <c>Unknown</c>. Not the same as <c>ExactlyOne</c>
/// (which differs from parity for three or more operands). All operands are always evaluated.
/// </summary>
/// <param name="Operands">The operands (at least two).</param>
public sealed record NxorExpression(EquatableArray<Expression> Operands) : Expression;

/// <summary>
/// <c>ANY(...)</c>: at least one operand is <c>True</c>, defined as <c>AtLeast(1, ...)</c> over the
/// definitely-true / possibly-true interval (ADR-0005 decision 6) but kept as its own node so it round-trips as written.
/// </summary>
/// <param name="Operands">The operands (at least two).</param>
public sealed record AnyExpression(EquatableArray<Expression> Operands) : Expression;

/// <summary>
/// <c>ALL(...)</c>: every operand is <c>True</c>, defined as <c>AtLeast(n, ...)</c> for <c>n</c> operands
/// (ADR-0005 decision 6) but kept as its own node so it round-trips as written.
/// </summary>
/// <param name="Operands">The operands (at least two).</param>
public sealed record AllExpression(EquatableArray<Expression> Operands) : Expression;

/// <summary>
/// <c>NONE(...)</c>: no operand is <c>True</c>, defined as <c>AtMost(0, ...)</c> (ADR-0005 decision 6) but kept as
/// its own node so it round-trips as written.
/// </summary>
/// <param name="Operands">The operands (at least two).</param>
public sealed record NoneExpression(EquatableArray<Expression> Operands) : Expression;

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

/// <summary>
/// <c>BETWEEN(min, max, ...)</c>: the number of <c>True</c> operands lies in the inclusive range
/// <c>[min, max]</c>. Defined as <c>AND(AtLeast(min, ...), AtMost(max, ...))</c> over the definitely-true /
/// possibly-true interval (ADR-0005 decision 6) but kept as its own node so it round-trips as written.
/// </summary>
/// <param name="Min">The inclusive lower bound on the true-operand count (at least 0).</param>
/// <param name="Max">The inclusive upper bound on the true-operand count (at least <paramref name="Min"/>, at most the operand count); <c>Min = 0</c> with <c>Max = n</c> is rejected as an always-true range.</param>
/// <param name="Operands">The operands (at least two).</param>
public sealed record BetweenExpression(int Min, int Max, EquatableArray<Expression> Operands) : Expression;

/// <summary>
/// <c>IsTrue(x)</c>, <c>IsFalse(x)</c>, <c>IsUnknown(x)</c> or <c>IsKnown(x)</c>: tests the K3 state of the operand
/// instead of combining values. The result is always a definite <c>True</c> or <c>False</c> (never <c>Unknown</c>), so an
/// inspection can sit anywhere in a rule without collapsing or poisoning the rest of it. One node with a
/// <see cref="Kind"/> (like <see cref="ThresholdExpression"/>) because the four differ only in the state they test.
/// </summary>
/// <param name="Kind">Which state is tested.</param>
/// <param name="Operand">The expression whose result is inspected.</param>
public sealed record InspectionExpression(InspectionKind Kind, Expression Operand) : Expression;

/// <summary>
/// <c>If(condition, whenTrue, whenFalse)</c> (ternary <c>condition ? whenTrue : whenFalse</c>): a K3-aware conditional.
/// A <c>True</c> condition yields <paramref name="WhenTrue"/>, a <c>False</c> one yields <paramref name="WhenFalse"/>, and an
/// <c>Unknown</c> condition does not guess a branch: the result is the branch value when both branches are the same
/// definite value, otherwise <c>Unknown</c>. Its primitive definition is
/// <c>(c AND t) OR (NOT c AND f) OR (t AND f)</c>.
/// </summary>
/// <param name="Condition">The condition, evaluated first.</param>
/// <param name="WhenTrue">The branch taken when the condition is <c>True</c>.</param>
/// <param name="WhenFalse">The branch taken when the condition is <c>False</c>.</param>
public sealed record IfExpression(Expression Condition, Expression WhenTrue, Expression WhenFalse) : Expression;

/// <summary>
/// <c>COALESCE(a, b, ...)</c> (infix <c>a ?? b</c>): the first operand that is not <c>Unknown</c>; <c>True</c> and
/// <c>False</c> pass through unchanged and the result is <c>Unknown</c> only when every operand is. Operands are
/// evaluated left to right and the rest are skipped once a known value is found.
/// </summary>
/// <param name="Operands">The operands in priority order (at least two).</param>
public sealed record CoalesceExpression(EquatableArray<Expression> Operands) : Expression;
