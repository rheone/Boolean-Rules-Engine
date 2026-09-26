namespace BooleanRulesEngine.Compilation;

using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Evaluation;

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
}
