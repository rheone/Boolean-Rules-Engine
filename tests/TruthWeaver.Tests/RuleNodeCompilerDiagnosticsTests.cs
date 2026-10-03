namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 14: <c>RuleNodeCompiler</c>'s malformed-tree diagnostics and optional-argument default
/// substitution. The JSON front end doesn't enforce AND/OR/ExactlyOne/threshold operand-count arity at
/// parse time (only the DSL parser's own grammar makes that structurally impossible), so a
/// hand-written or embedded JSON subtree reaches the compiler's own defensive arity checks — reachable
/// through the public <c>RuleCompiler.CompileJson</c> surface, no internal access needed.
/// </summary>
public sealed class RuleNodeCompilerDiagnosticsTests
{
    [Theory]
    [InlineData("""{"op": "and", "operands": [{"const": true}]}""")]
    [InlineData("""{"op": "or", "operands": []}""")]
    [InlineData("""{"op": "exactlyOne", "operands": [{"const": true}]}""")]
    public void A_variadic_operator_with_fewer_than_two_operands_produces_a_malformed_tree_diagnostic_not_an_exception(
        string json
    )
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> result = compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Assert.Contains(
            result.Diagnostics,
            d =>
                d.Code == DiagnosticCodes.MalformedTree
                && d.Severity == DiagnosticSeverity.Error
                && d.Message.Contains("requires at least", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void A_threshold_operator_with_zero_operands_produces_a_malformed_tree_diagnostic_not_an_exception()
    {
        RuleCompiler<RuleTestContext> compiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> result = compiler.CompileJson("""{"op": "atLeast", "k": 1, "operands": []}""");

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Assert.Contains(
            result.Diagnostics,
            d =>
                d.Code == DiagnosticCodes.MalformedTree
                && d.Severity == DiagnosticSeverity.Error
                && d.Message.Contains("requires at least one operand", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void A_term_argument_name_the_predicate_schema_does_not_declare_produces_an_unknown_argument_diagnostic()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(bogusArg: \"x\")");

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Assert.Contains(
            result.Diagnostics,
            d =>
                d.Code == DiagnosticCodes.UnknownArgument
                && d.Severity == DiagnosticSeverity.Error
                && d.Message.Contains("bogusArg", StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task An_omitted_optional_argument_with_a_declared_default_resolves_to_that_default_when_evaluated()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasRoleOrDefault",
                        "hasRoleOrDefault",
                        "True iff 'role' equals 'GUEST', its declared default when omitted.",
                        [
                            new PredicateArgumentSchema(
                                "role",
                                "The role to check for; defaults to 'GUEST' when omitted.",
                                LiteralKind.String,
                                Required: false,
                                Default: LiteralValue.OfString("GUEST")
                            ),
                        ]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(
                            string.Equals(args.GetString("role"), "GUEST", StringComparison.Ordinal)
                                ? TruthValue.True
                                : TruthValue.False
                        )
                )
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRoleOrDefault");

        Assert.True(result.Succeeded);
        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Assert.Equal(TruthValue.True, decision.Result);
    }
}
