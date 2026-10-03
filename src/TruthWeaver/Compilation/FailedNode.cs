namespace TruthWeaver.Compilation;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// The stand-in <see cref="Expression"/> built for a node that failed validation so that compilation can
/// keep reporting diagnostics for the rest of the tree. It lives outside the generic
/// <c>RuleNodeCompiler</c> because it does not depend on the context type.
/// </summary>
internal static class FailedNode
{
    /// <summary>
    /// Gets the substitute for a failed node: <see cref="TruthValue.Unknown"/>, never
    /// <see cref="TruthValue.False"/>, so a failure cannot read as a negative answer (ADR-0005
    /// decision 16). The tree containing it is always discarded because an error diagnostic exists.
    /// </summary>
    public static Expression Placeholder { get; } = new ConstantExpression(TruthValue.Unknown);
}
