namespace TruthWeaver.Compilation;

using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;

/// <summary>
/// The result of <c>RuleCompiler.Compile</c> — never thrown, always returned (ADR-0002/ADR-0003).
/// <see cref="CompiledRule"/> is populated only when <see cref="Diagnostics"/> contains no
/// <see cref="DiagnosticSeverity.Error"/>-severity entries.
/// </summary>
/// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
/// <param name="CompiledRule">The compiled rule, or <see langword="null"/> if compilation failed.</param>
/// <param name="Diagnostics">Every diagnostic raised while parsing, validating, and analyzing.</param>
public sealed record CompilationResult<TContext>(CompiledRule<TContext>? CompiledRule, IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>Gets a value indicating whether compilation succeeded (no <see cref="DiagnosticSeverity.Error"/> diagnostics).</summary>
    public bool Succeeded => this.CompiledRule is not null;

    /// <summary>
    /// Renders <see cref="Diagnostics"/> as plain text for a log or an editor panel (see <see cref="DiagnosticFormatter"/>).
    /// The structured members of each <see cref="Diagnostic"/> remain available for callers that lay them out themselves.
    /// </summary>
    /// <param name="source">The rule text that was compiled, so locations show a line and column and the offending line; <see langword="null"/> to show offsets only.</param>
    /// <returns>The rendered diagnostics, one block each; empty when there are none.</returns>
    public string FormatDiagnostics(string? source = null)
    {
        return DiagnosticFormatter.Format(this.Diagnostics, source);
    }
}
