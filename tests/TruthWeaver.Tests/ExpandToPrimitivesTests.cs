namespace TruthWeaver.Tests;

using System.Text.Json.Nodes;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <c>CompiledRule.ExpandToPrimitives</c> (ticket 23, ADR-0005 decision 10) rewrites every derived operator into the
/// primitive kernel (<c>NOT</c>, <c>AND</c>, <c>OR</c>, <c>AtLeast</c>, <c>AtMost</c>, <c>Exactly</c>, <c>COALESCE</c>)
/// without changing what the rule evaluates to.
/// </summary>
public sealed class ExpandToPrimitivesTests
{
    private static readonly HashSet<string> PrimitiveOps = ["not", "and", "or", "atLeast", "atMost", "exactly", "coalesce"];

    /// <summary>
    /// For many generated rules (every operator, nested) and every {True, False, Unknown} assignment of the terms, the
    /// expanded rule evaluates to the same value as both the original rule and the independent oracle.
    /// </summary>
    [Fact]
    public async Task ExpandToPrimitives_GeneratedRules_EvaluateEqualToOriginalForEveryAssignment_Test()
    {
        Random random = new(20261003);
        List<string> disagreements = [];
        int checkedRules = 0;

        for (int i = 0; i < 400; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 3);
            K3Rule? original = K3Rule.TryCreate(generated.Text, 3);
            if (original is null)
            {
                continue;
            }

            K3Rule expanded = original.Rewrite(rule => rule.ExpandToPrimitives());
            checkedRules++;
            foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
            {
                Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                Decision after = await expanded.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                TruthValue oracle = generated.Eval(assignment);
                if (before.Result != after.Result || after.Result != oracle)
                {
                    disagreements.Add(
                        $"{generated.Text} @ [{string.Join(",", assignment)}]: original={before.Result} expanded={after.Result} oracle={oracle}"
                    );
                }
            }
        }

        Assert.Empty(disagreements);
        Assert.True(checkedRules > 300, $"only {checkedRules} rules were checked");
    }

    /// <summary>Whatever a generated rule contains, the expanded rule is built from primitive operators only.</summary>
    [Fact]
    public void ExpandToPrimitives_GeneratedRules_LeaveOnlyPrimitiveOperators_Test()
    {
        Random random = new(20261004);
        int checkedRules = 0;

        for (int i = 0; i < 400; i++)
        {
            string text = K3RuleGenerator.GenerateRule(random, depth: 3).Text;
            K3Rule? original = K3Rule.TryCreate(text, 3);
            if (original is null)
            {
                continue;
            }

            CompiledRule<RuleTestContext> expanded = original.Compiled.ExpandToPrimitives();
            checkedRules++;

            Assert.True(OperatorsOf(expanded).IsSubsetOf(PrimitiveOps), $"{text} expanded to {expanded.CanonicalText}");
        }

        Assert.True(checkedRules > 300, $"only {checkedRules} rules were checked");
    }

    /// <summary>Each derived operator, written alone, expands to primitives that evaluate like the original.</summary>
    [Theory]
    [InlineData("a IMPLIES b", 2)]
    [InlineData("a EQUIVALENT b", 2)]
    [InlineData("a XOR b", 2)]
    [InlineData("a NAND b", 2)]
    [InlineData("a NOR b", 2)]
    [InlineData("NXOR(a, b)", 2)]
    [InlineData("NXOR(a, b, c)", 3)]
    [InlineData("NXOR(a, b, c, d)", 4)]
    [InlineData("ANY(a, b, c)", 3)]
    [InlineData("ALL(a, b, c)", 3)]
    [InlineData("NONE(a, b, c)", 3)]
    [InlineData("BETWEEN(1, 2, a, b, c)", 3)]
    [InlineData("BETWEEN(0, 2, a, b, c)", 3)]
    [InlineData("BETWEEN(2, 3, a, b, c)", 3)]
    [InlineData("ExactlyOne(a, b, c)", 3)]
    [InlineData("GreaterThan(1, a, b, c)", 3)]
    [InlineData("LessThan(2, a, b, c)", 3)]
    [InlineData("If(a, b, c)", 3)]
    [InlineData("IsTrue(a)", 1)]
    [InlineData("IsFalse(a)", 1)]
    [InlineData("IsUnknown(a)", 1)]
    [InlineData("IsKnown(a)", 1)]
    [InlineData("Project(a, True)", 1)]
    [InlineData("Project(a, False)", 1)]
    public async Task ExpandToPrimitives_DerivedOperator_BecomesEquivalentPrimitives_Test(string ruleText, int arity)
    {
        K3Rule original = K3Rule.TryCreate(ruleText, arity)!;
        CompiledRule<RuleTestContext> expandedRule = original.Compiled.ExpandToPrimitives();
        K3Rule expanded = original.Rewrite(rule => rule.ExpandToPrimitives());

        Assert.True(OperatorsOf(expandedRule).IsSubsetOf(PrimitiveOps), expandedRule.CanonicalText);
        foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
        {
            Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision after = await expanded.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(before.Result, after.Result);
        }
    }

    /// <summary>The expansion is a new rule; the original keeps its derived operators and its text.</summary>
    [Fact]
    public void ExpandToPrimitives_Rule_LeavesTheOriginalUntouched_Test()
    {
        K3Rule original = K3Rule.TryCreate("(a IMPLIES b) XOR ANY(a, b, c)", 3)!;
        string textBefore = original.Compiled.CanonicalText;
        string jsonBefore = original.Compiled.PrintJson();

        CompiledRule<RuleTestContext> expanded = original.Compiled.ExpandToPrimitives();

        Assert.NotSame(original.Compiled, expanded);
        Assert.NotEqual(textBefore, expanded.CanonicalText);
        Assert.Equal(textBefore, original.Compiled.CanonicalText);
        Assert.Equal(jsonBefore, original.Compiled.PrintJson());
    }

    /// <summary>Expanding a rule that already uses only primitives changes nothing, and expanding twice equals expanding once.</summary>
    [Fact]
    public void ExpandToPrimitives_AlreadyPrimitiveRule_IsReturnedStructurallyUnchanged_Test()
    {
        K3Rule original = K3Rule.TryCreate("NOT a AND (b OR AtLeast(2, a, b, c)) OR COALESCE(a, Exactly(1, b, c))", 3)!;

        CompiledRule<RuleTestContext> once = original.Compiled.ExpandToPrimitives();
        CompiledRule<RuleTestContext> twice = once.ExpandToPrimitives();

        Assert.Equal(original.Compiled.CanonicalText, once.CanonicalText);
        Assert.Equal(once.CanonicalText, twice.CanonicalText);
    }

    /// <summary>The expanded rule prints canonical text that recompiles to the identical rule.</summary>
    [Theory]
    [InlineData("a IMPLIES b", 2)]
    [InlineData("a EQUIVALENT b", 2)]
    [InlineData("NXOR(a, b, c)", 3)]
    [InlineData("BETWEEN(1, 2, a, b, c)", 3)]
    [InlineData("If(a, b, c)", 3)]
    [InlineData("IsUnknown(a)", 1)]
    [InlineData("GreaterThan(2, a, b, c) OR LessThan(1, a, b)", 3)]
    public void ExpandToPrimitives_Rule_PrintsCanonicalTextThatRecompilesToTheSameRule_Test(string ruleText, int arity)
    {
        CompiledRule<RuleTestContext> expanded = K3Rule.TryCreate(ruleText, arity)!.Compiled.ExpandToPrimitives();
        K3Rule? recompiled = K3Rule.TryCreate(expanded.CanonicalText, arity);

        Assert.NotNull(recompiled);
        Assert.Equal(expanded.CanonicalText, recompiled.Compiled.CanonicalText);
    }

    /// <summary>A declared outermost <c>Collapse</c> policy is an evaluation boundary, not a derived operator, and survives expansion.</summary>
    [Fact]
    public async Task ExpandToPrimitives_RuleWithCollapse_PreservesThePolicyAndOutcome_Test()
    {
        K3Rule original = K3Rule.TryCreate("Collapse(a IMPLIES b, UnknownIsError)", 2)!;
        K3Rule expanded = original.Rewrite(rule => rule.ExpandToPrimitives());
        TruthValue[] assignment = [TruthValue.True, TruthValue.Unknown];

        Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
        Decision after = await expanded.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

        Assert.Equal(CollapsePolicy.UnknownIsError, expanded.Compiled.CollapsePolicy);
        Assert.Equal(CollapseOutcome.RejectedUnresolved, after.Outcome);
        Assert.Equal(before.Outcome, after.Outcome);
        Assert.Equal(before.Result, after.Result);
    }

    /// <summary>A predicate that throws becomes <c>Unknown</c> plus a fault; the expansion reports the same result and a fault.</summary>
    [Fact]
    public async Task ExpandToPrimitives_FaultingPredicate_ResultsInTheSameUnknownAndAFault_Test()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                PredicateSchema.NoArguments("boom", "boom", "Always throws."),
                (_, _, _) => throw new InvalidOperationException("boom")
            )
            .AddConstant("ok", true)
            .Build();
        CompiledRule<RuleTestContext> original = new RuleCompiler<RuleTestContext>(registry)
            .Compile("boom XOR ok")
            .CompiledRule!;
        CompiledRule<RuleTestContext> expanded = original.ExpandToPrimitives();

        Decision before = await original.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Decision after = await expanded.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, before.Result);
        Assert.Equal(before.Result, after.Result);
        Assert.NotEmpty(after.Faults);
        Assert.False(after.IsSatisfied);
    }

    /// <summary>Collects every operator name used in a rule's JSON tree form.</summary>
    private static HashSet<string> OperatorsOf(CompiledRule<RuleTestContext> rule)
    {
        HashSet<string> operators = [];
        Collect(JsonNode.Parse(rule.PrintJson()), operators);
        return operators;
    }

    private static void Collect(JsonNode? node, HashSet<string> operators)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (KeyValuePair<string, JsonNode?> property in obj)
                {
                    if (property.Key == "op" && property.Value?.GetValue<string>() is { } op)
                    {
                        operators.Add(op);
                    }

                    Collect(property.Value, operators);
                }

                break;
            case JsonArray array:
                foreach (JsonNode? item in array)
                {
                    Collect(item, operators);
                }

                break;
            default:
                break;
        }
    }
}
