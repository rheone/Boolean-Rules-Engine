namespace TruthWeaver.Compilation;

/// <summary>
/// Compile-time resource bounds and mode (ADR-0002/ticket 09), so a rule authored somewhere not
/// code-reviewed (e.g. an admin UI backed by a database) cannot pathologically hang a request thread.
/// </summary>
/// <param name="MaxDepth">The maximum expression tree depth. Exceeding it is a compile <c>Error</c>.</param>
/// <param name="MaxNodeCount">The maximum total node count. Exceeding it is a compile <c>Error</c>.</param>
/// <param name="MaxAnalysisTerms">
/// The maximum number of distinct term identities the BDD-based analyzer will consider; beyond this,
/// analysis is skipped and reported as an <c>Info</c> diagnostic, never silently treated as
/// "not constant".
/// </param>
/// <param name="Mode">How an unregistered predicate name is treated.</param>
public sealed record CompilerOptions(
    int MaxDepth = 32,
    int MaxNodeCount = 512,
    int MaxAnalysisTerms = 20,
    CompilationMode Mode = CompilationMode.Strict
)
{
    /// <summary>Gets the default options: 32 / 512 / 20 / <see cref="CompilationMode.Strict"/>.</summary>
    public static CompilerOptions Default { get; } = new();
}
