namespace BooleanRulesEngine.Yaml;

using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Evaluation;
using BooleanRulesEngine.Parsing;

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
        return root is null ? new CompilationResult<TContext>(null, diagnostics) : compiler.CompileFromNode(root, diagnostics);
    }

    /// <summary>Prints a compiled rule to the flat, key-discriminated YAML tree shape (ADR-0003).</summary>
    /// <typeparam name="TContext">The application context type the compiled rule evaluates against.</typeparam>
    /// <param name="rule">The compiled rule to print.</param>
    /// <returns>The YAML text.</returns>
    public static string PrintYaml<TContext>(this CompiledRule<TContext> rule)
    {
        return YamlTreePrinter.Print(rule.Root);
    }
}
