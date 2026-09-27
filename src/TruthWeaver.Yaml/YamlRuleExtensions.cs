namespace TruthWeaver.Yaml;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Adds YAML tree support to <see cref="RuleCompiler{TContext}"/> and <see cref="CompiledRule{TContext}"/>,
/// isolated in this package so a consumer with no interest in YAML never acquires the YamlDotNet
/// dependency transitively (ADR-0004).
/// </summary>
public static class YamlRuleExtensions
{
    /// <summary>Compiles the identical flat, key-discriminated tree shape JSON uses (ADR-0003), expressed in YAML.</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate the parsed tree against.</param>
    /// <param name="yaml">The YAML tree text.</param>
    /// <returns>The compilation result.</returns>
    public static CompilationResult<TContext> CompileYaml<TContext>(this RuleCompiler<TContext> compiler, string yaml)
    {
        (RuleNode? root, IReadOnlyList<Diagnostic> diagnostics) = YamlTreeParser.Parse(yaml);
        return CompileFromParsedYaml(compiler, root, diagnostics);
    }

    /// <summary>
    /// Compiles the identical flat, key-discriminated tree shape JSON uses (ADR-0003), expressed in
    /// YAML, from a <see cref="YamlNode"/> already extracted from a larger document — e.g. one mapping
    /// node of a multi-rule document parsed with YamlDotNet — rather than requiring the caller to
    /// re-serialize it to standalone YAML text first. Extracting the subtree is the caller's own
    /// navigation via YamlDotNet's node APIs; the engine adds no path/pointer syntax of its own
    /// (ADR-0003).
    /// </summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate the parsed tree against.</param>
    /// <param name="node">The YAML tree node.</param>
    /// <returns>The compilation result.</returns>
    public static CompilationResult<TContext> CompileYaml<TContext>(this RuleCompiler<TContext> compiler, YamlNode node)
    {
        (RuleNode? root, IReadOnlyList<Diagnostic> diagnostics) = YamlTreeParser.Parse(node);
        return CompileFromParsedYaml(compiler, root, diagnostics);
    }

    /// <summary>Prints a compiled rule to the flat, key-discriminated YAML tree shape (ADR-0003).</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="rule">The compiled rule to print.</param>
    /// <returns>The YAML text.</returns>
    public static string PrintYaml<TContext>(this CompiledRule<TContext> rule)
    {
        return YamlTreePrinter.Print(rule.Root);
    }

    private static CompilationResult<TContext> CompileFromParsedYaml<TContext>(
        RuleCompiler<TContext> compiler,
        RuleNode? root,
        IReadOnlyList<Diagnostic> diagnostics
    )
    {
        return root is null ? new CompilationResult<TContext>(null, diagnostics) : compiler.CompileFromNode(root, diagnostics);
    }
}
