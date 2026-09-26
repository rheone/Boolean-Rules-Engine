namespace BooleanRulesEngine.Compilation;

using BooleanRulesEngine.Analysis;
using BooleanRulesEngine.Ast;
using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Evaluation;
using BooleanRulesEngine.Json;
using BooleanRulesEngine.Logging;
using BooleanRulesEngine.Parsing;
using BooleanRulesEngine.Registry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Compiles rule text (DSL, JSON, or — via <c>BooleanRulesEngine.Yaml</c> — YAML) into an immutable
/// <see cref="CompiledRule{TContext}"/>, following the Parse → Validate → Analyze → Build pipeline
/// (ADR-0003). Never throws for an authoring error: every problem, from a syntax error to a
/// structural tautology, becomes a <see cref="Diagnostic"/> in the returned
/// <see cref="CompilationResult{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">The application context type compiled rules evaluate against.</typeparam>
/// <remarks>Initializes a new instance of the <see cref="RuleCompiler{TContext}"/> class.</remarks>
/// <param name="registry">The predicate registry rule text is validated against.</param>
/// <param name="options">Compiler resource limits and mode, or <see langword="null"/> for the defaults.</param>
/// <param name="logger">A logger for compile diagnostics and rule-swap notifications, or <see langword="null"/> to log nowhere.</param>
public sealed class RuleCompiler<TContext>(
    PredicateRegistry<TContext> registry,
    CompilerOptions? options = null,
    ILogger<RuleCompiler<TContext>>? logger = null
)
{
    private readonly PredicateRegistry<TContext> registry = registry;
    private readonly CompilerOptions options = options ?? CompilerOptions.Default;
    private readonly ILogger logger = (ILogger?)logger ?? NullLogger.Instance;

    /// <summary>Compiles canonical DSL rule text.</summary>
    /// <param name="dslText">The rule text.</param>
    /// <returns>The compilation result.</returns>
    public CompilationResult<TContext> Compile(string dslText)
    {
        (RuleNode root, IReadOnlyList<Diagnostic> parseDiagnostics) = DslParser.Parse(dslText);
        return this.CompileNode(root, parseDiagnostics);
    }

    /// <summary>Compiles the flat, key-discriminated JSON tree shape (ADR-0003).</summary>
    /// <param name="json">The JSON tree text.</param>
    /// <returns>The compilation result.</returns>
    public CompilationResult<TContext> CompileJson(string json)
    {
        (RuleNode? root, IReadOnlyList<Diagnostic> parseDiagnostics) = JsonTreeParser.Parse(json);
        if (root is null)
        {
            this.LogDiagnostics(parseDiagnostics);
            return new CompilationResult<TContext>(null, parseDiagnostics);
        }

        return this.CompileNode(root, parseDiagnostics);
    }

    /// <summary>
    /// Notifies this compiler's logger that the host application swapped its active
    /// <see cref="CompiledRule{TContext}"/> reference — the library owns compilation and evaluation
    /// only, not rule storage or swap scheduling (ADR-0002), so it logs only what it is told.
    /// </summary>
    /// <param name="ruleIdentifier">A host-supplied identifier for the swapped rule (e.g. its name or storage key).</param>
    public void NotifyRuleSwapped(string? ruleIdentifier = null)
    {
        RuleSwapLog.RuleSwapped(this.logger, ruleIdentifier ?? "(unnamed)");
    }

    /// <summary>
    /// Compiles an already-parsed raw tree. Internal, and visible to <c>BooleanRulesEngine.Yaml</c>
    /// via <c>InternalsVisibleTo</c>, so the YAML front end reuses this exact validation/analysis
    /// pipeline rather than re-implementing it.
    /// </summary>
    /// <param name="root">The raw parse tree root.</param>
    /// <param name="frontEndDiagnostics">Diagnostics already raised by the front end that produced <paramref name="root"/>.</param>
    /// <returns>The compilation result.</returns>
    internal CompilationResult<TContext> CompileFromNode(RuleNode root, IReadOnlyList<Diagnostic> frontEndDiagnostics)
    {
        return this.CompileNode(root, frontEndDiagnostics);
    }

    private CompilationResult<TContext> CompileNode(RuleNode root, IReadOnlyList<Diagnostic> frontEndDiagnostics)
    {
        List<Diagnostic> diagnostics = [.. frontEndDiagnostics];
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            this.LogDiagnostics(diagnostics);
            return new CompilationResult<TContext>(null, diagnostics);
        }

        (Expression? tree, IReadOnlyList<Diagnostic> validationDiagnostics) = RuleNodeCompiler<TContext>.Compile(
            root,
            this.registry,
            this.options
        );
        diagnostics.AddRange(validationDiagnostics);

        if (tree is not null)
        {
            diagnostics.AddRange(Analyzer.Analyze(tree, this.options));
        }

        this.LogDiagnostics(diagnostics);

        bool hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        CompiledRule<TContext>? compiled =
            !hasErrors && tree is not null ? new CompiledRule<TContext>(tree, this.registry, this.logger) : null;
        return new CompilationResult<TContext>(compiled, diagnostics);
    }

    private void LogDiagnostics(IReadOnlyList<Diagnostic> diagnostics)
    {
        foreach (Diagnostic diagnostic in diagnostics)
        {
            RuleCompilerLog.DiagnosticProduced(
                this.logger,
                RuleCompilerLog.ToLogLevel(diagnostic.Severity),
                diagnostic.Code,
                diagnostic.Severity,
                diagnostic.Message,
                diagnostic.Span.Start,
                diagnostic.Span.Length
            );
        }
    }
}
