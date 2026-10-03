namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 04: named arguments, term identity, and per-evaluation memoization.</summary>
public sealed class ArgumentsAndMemoizationTests
{
    [Fact]
    public async Task Term_with_named_argument_evaluates_correctly()
    {
        RuleCompiler<RuleTestContext> compiler = CompilerWithHasRole();

        Decision decision = await compiler
            .Compile("hasRole(role: \"Y\")")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public void Missing_required_argument_is_a_compile_error()
    {
        RuleCompiler<RuleTestContext> compiler = CompilerWithHasRole();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole");

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Diagnostics,
            d => d.Code == DiagnosticCodes.MissingArgument && d.Severity == DiagnosticSeverity.Error
        );
    }

    [Fact]
    public void Type_mismatched_argument_is_a_compile_error()
    {
        RuleCompiler<RuleTestContext> compiler = CompilerWithHasRole();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: 5)");

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Diagnostics,
            d => d.Code == DiagnosticCodes.ArgumentTypeMismatch && d.Severity == DiagnosticSeverity.Error
        );
    }

    [Fact]
    public void Argument_order_in_source_text_does_not_affect_identity()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "hasBoth",
                    "hasBoth",
                    "Test predicate, always true.",
                    [
                        new PredicateArgumentSchema("a", "First argument.", LiteralKind.String),
                        new PredicateArgumentSchema("b", "Second argument.", LiteralKind.String),
                    ]
                ),
                (_, _, _) => ValueTask.FromResult(true ? TruthValue.True : TruthValue.False)
            );
        RuleCompiler<RuleTestContext> compiler = new(builder.Build());

        CompiledRule<RuleTestContext> rule1 = compiler.Compile("hasBoth(a: \"1\", b: \"2\")").CompiledRule!;
        CompiledRule<RuleTestContext> rule2 = compiler.Compile("hasBoth(b: \"2\", a: \"1\")").CompiledRule!;

        Assert.Equal(rule1.CanonicalText, rule2.CanonicalText);
    }

    [Fact]
    public void Role_y_and_role_lowercase_y_are_distinct_terms()
    {
        RuleCompiler<RuleTestContext> compiler = CompilerWithHasRole();

        CompiledRule<RuleTestContext> upper = compiler.Compile("hasRole(role: \"Y\")").CompiledRule!;
        CompiledRule<RuleTestContext> lower = compiler.Compile("hasRole(role: \"y\")").CompiledRule!;

        Assert.NotEqual(upper.CanonicalText, lower.CanonicalText);
    }

    [Fact]
    public async Task Identical_term_referenced_twice_invokes_predicate_exactly_once_per_evaluation()
    {
        List<string> invocationLog = [];
        RuleCompiler<RuleTestContext> compiler = CompilerWithHasRole(invocationLog);

        Decision decision = await compiler
            .Compile("hasRole(role: \"Y\") OR hasRole(role: \"Y\")")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Single(invocationLog);
    }

    [Fact]
    public async Task Memoization_does_not_persist_across_separate_evaluations()
    {
        List<string> invocationLog = [];
        RuleCompiler<RuleTestContext> compiler = CompilerWithHasRole(invocationLog);
        CompiledRule<RuleTestContext> rule = compiler.Compile("hasRole(role: \"Y\")").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(2, invocationLog.Count);
    }

    [Fact]
    public void Array_valued_arguments_with_different_element_order_are_distinct_terms()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "hasAnyRole",
                    "hasAnyRole",
                    "Test predicate, always true.",
                    [new PredicateArgumentSchema("roles", "The roles to check.", LiteralKind.StringArray)]
                ),
                (_, _, _) => ValueTask.FromResult(true ? TruthValue.True : TruthValue.False)
            );
        RuleCompiler<RuleTestContext> compiler = new(builder.Build());

        CompiledRule<RuleTestContext> rule1 = compiler.Compile("hasAnyRole(roles: [\"A\", \"B\"])").CompiledRule!;
        CompiledRule<RuleTestContext> rule2 = compiler.Compile("hasAnyRole(roles: [\"B\", \"A\"])").CompiledRule!;

        Assert.NotEqual(rule1.CanonicalText, rule2.CanonicalText);
    }

    private static RuleCompiler<RuleTestContext> CompilerWithHasRole(List<string>? invocationLog = null)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        builder = invocationLog is null
            ? builder.AddStringArgPredicate("hasRole", "role", "Y")
            : builder.Add(
                new PredicateSchema(
                    "hasRole",
                    "hasRole",
                    "Test predicate, true iff the role argument equals \"Y\".",
                    [new PredicateArgumentSchema("role", "The role code to check for.", LiteralKind.String)]
                ),
                (_, args, _) =>
                {
                    invocationLog.Add($"hasRole(role: \"{args.GetString("role")}\")");
                    return ValueTask.FromResult(
                        string.Equals(args.GetString("role"), "Y", StringComparison.Ordinal)
                            ? TruthValue.True
                            : TruthValue.False
                    );
                }
            );

        return new RuleCompiler<RuleTestContext>(builder.Build());
    }
}
