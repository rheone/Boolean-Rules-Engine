namespace TruthWeaver.Tests;

using System.Text.Json.Nodes;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <c>CompiledRule.ExpandToNand</c> and <c>ExpandToNor</c> (ticket 24, ADR-0005 decision 10) rewrite a rule so its only
/// logical operator is the chosen universal gate. <c>COALESCE</c> is the one documented semantic boundary: it is not a
/// Kleene-monotone function, so no combination of <c>NAND</c>/<c>NOR</c> can express it, and it is left in place with its
/// operands rewritten.
/// </summary>
public sealed class ExpandToUniversalGateTests
{
    /// <summary>
    /// Generated rules longer than this are skipped: thresholds become a disjunction over operand subsets, so a wide
    /// threshold over deeply nested operands would make the gate-only tree enormous and the test needlessly slow.
    /// </summary>
    private const int MaxGeneratedTextLength = 30;

    /// <summary>Spellings that expand to <c>COALESCE</c> (the boundary): the operator itself, <c>Project</c> and the inspections.</summary>
    private static readonly string[] BoundaryNames = ["COALESCE", "Project", "IsTrue", "IsFalse", "IsUnknown", "IsKnown"];

    /// <summary>
    /// For many generated rules (every operator, nested) and every {True, False, Unknown} assignment of the terms, the
    /// gate-only rule evaluates to the same value as both the original rule and the independent oracle.
    /// </summary>
    [Theory]
    [InlineData("nand")]
    [InlineData("nor")]
    public async Task ExpandToNandOrNor_GeneratedRules_EvaluateEqualToOriginalForEveryAssignment_Test(string gate)
    {
        Random random = new(gate == "nand" ? 20261005 : 20261006);
        List<string> disagreements = [];
        int checkedRules = 0;

        for (int i = 0; i < 2500; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 2);
            K3Rule? original = generated.Text.Length > MaxGeneratedTextLength ? null : K3Rule.TryCreate(generated.Text, 3);
            if (original is null)
            {
                continue;
            }

            K3Rule rewritten = original.Rewrite(rule => Expand(rule, gate));
            checkedRules++;
            foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
            {
                Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                Decision after = await rewritten.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                TruthValue oracle = generated.Eval(assignment);
                if (before.Result != after.Result || after.Result != oracle)
                {
                    disagreements.Add(
                        $"{generated.Text} @ [{string.Join(",", assignment)}]: original={before.Result} {gate}={after.Result} oracle={oracle}"
                    );
                }
            }
        }

        Assert.Empty(disagreements);
        Assert.True(checkedRules > 100, $"only {checkedRules} rules were checked");
    }

    /// <summary>
    /// Whatever a generated rule contains, the output uses only the target gate; the single exception is
    /// <c>COALESCE</c>, and only when the rule used <c>COALESCE</c>, <c>Project</c> or an inspection.
    /// </summary>
    [Theory]
    [InlineData("nand")]
    [InlineData("nor")]
    public void ExpandToNandOrNor_GeneratedRules_LeaveOnlyTheTargetGateAndTheCoalesceBoundary_Test(string gate)
    {
        Random random = new(gate == "nand" ? 20261007 : 20261008);
        int checkedRules = 0;
        int withBoundary = 0;

        for (int i = 0; i < 2500; i++)
        {
            string text = K3RuleGenerator.GenerateRule(random, depth: 2).Text;
            K3Rule? original = text.Length > MaxGeneratedTextLength ? null : K3Rule.TryCreate(text, 3);
            if (original is null)
            {
                continue;
            }

            HashSet<string> operators = OperatorsOf(Expand(original.Compiled, gate));
            bool usesBoundary = BoundaryNames.Any(name => text.Contains(name, StringComparison.Ordinal));
            HashSet<string> allowed = usesBoundary ? [gate, "coalesce"] : [gate];
            checkedRules++;
            withBoundary += usesBoundary ? 1 : 0;

            Assert.True(operators.IsSubsetOf(allowed), $"{text} produced {string.Join(",", operators)}");
        }

        Assert.True(checkedRules > 100, $"only {checkedRules} rules were checked");
        Assert.True(withBoundary > 5, $"only {withBoundary} rules exercised the boundary");
    }

    /// <summary>Each operator that has a pure-gate form, written alone, becomes gate-only and evaluates like the original.</summary>
    [Theory]
    [InlineData("nand", "NOT a", 1)]
    [InlineData("nand", "a AND b AND c", 3)]
    [InlineData("nand", "a OR b OR c", 3)]
    [InlineData("nand", "a IMPLIES b", 2)]
    [InlineData("nand", "a XOR b", 2)]
    [InlineData("nand", "a EQUIVALENT b", 2)]
    [InlineData("nand", "a NOR b", 2)]
    [InlineData("nand", "NXOR(a, b, c)", 3)]
    [InlineData("nand", "ExactlyOne(a, b, c)", 3)]
    [InlineData("nand", "AtLeast(2, a, b, c)", 3)]
    [InlineData("nand", "AtMost(1, a, b, c)", 3)]
    [InlineData("nand", "Exactly(0, a, b, c)", 3)]
    [InlineData("nand", "Exactly(3, a, b, c)", 3)]
    [InlineData("nand", "BETWEEN(1, 2, a, b, c)", 3)]
    [InlineData("nand", "If(a, b, c)", 3)]
    [InlineData("nor", "NOT a", 1)]
    [InlineData("nor", "a AND b AND c", 3)]
    [InlineData("nor", "a OR b OR c", 3)]
    [InlineData("nor", "a IMPLIES b", 2)]
    [InlineData("nor", "a XOR b", 2)]
    [InlineData("nor", "a EQUIVALENT b", 2)]
    [InlineData("nor", "a NAND b", 2)]
    [InlineData("nor", "NXOR(a, b, c)", 3)]
    [InlineData("nor", "ExactlyOne(a, b, c)", 3)]
    [InlineData("nor", "AtLeast(2, a, b, c)", 3)]
    [InlineData("nor", "AtMost(1, a, b, c)", 3)]
    [InlineData("nor", "Exactly(0, a, b, c)", 3)]
    [InlineData("nor", "Exactly(3, a, b, c)", 3)]
    [InlineData("nor", "BETWEEN(1, 2, a, b, c)", 3)]
    [InlineData("nor", "If(a, b, c)", 3)]
    public async Task ExpandToNandOrNor_Operator_BecomesGateOnlyAndEquivalent_Test(string gate, string ruleText, int arity)
    {
        K3Rule original = K3Rule.TryCreate(ruleText, arity)!;
        CompiledRule<RuleTestContext> gateOnly = Expand(original.Compiled, gate);
        K3Rule rewritten = original.Rewrite(rule => Expand(rule, gate));

        Assert.True(OperatorsOf(gateOnly).IsSubsetOf([gate]), gateOnly.CanonicalText);
        foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
        {
            Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision after = await rewritten.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(before.Result, after.Result);
        }
    }

    /// <summary>The textbook NAND identities: <c>NOT A = A NAND A</c>, <c>A AND B = (A NAND B) NAND (A NAND B)</c>, <c>A OR B = (A NAND A) NAND (B NAND B)</c>.</summary>
    [Theory]
    [InlineData("NOT a", "(a NAND a)")]
    [InlineData("a AND b", "((a NAND b) NAND (a NAND b))")]
    [InlineData("a OR b", "((a NAND a) NAND (b NAND b))")]
    public void ExpandToNand_BasicGates_UseTheTextbookIdentities_Test(string ruleText, string expected)
    {
        CompiledRule<RuleTestContext> rule = K3Rule.TryCreate(ruleText, 2)!.Compiled;

        Assert.Equal(expected, rule.ExpandToNand().CanonicalText);
    }

    /// <summary>The NOR duals: <c>NOT A = A NOR A</c>, <c>A OR B = (A NOR B) NOR (A NOR B)</c>, <c>A AND B = (A NOR A) NOR (B NOR B)</c>.</summary>
    [Theory]
    [InlineData("NOT a", "(a NOR a)")]
    [InlineData("a OR b", "((a NOR b) NOR (a NOR b))")]
    [InlineData("a AND b", "((a NOR a) NOR (b NOR b))")]
    public void ExpandToNor_BasicGates_UseTheTextbookIdentities_Test(string ruleText, string expected)
    {
        CompiledRule<RuleTestContext> rule = K3Rule.TryCreate(ruleText, 2)!.Compiled;

        Assert.Equal(expected, rule.ExpandToNor().CanonicalText);
    }

    /// <summary>
    /// <c>COALESCE</c> cannot be built from <c>NAND</c>/<c>NOR</c> (every such circuit is monotone in the information
    /// order, <c>COALESCE</c> is not), so it stays as the boundary while everything around and inside it is rewritten.
    /// </summary>
    [Theory]
    [InlineData("nand")]
    [InlineData("nor")]
    public async Task ExpandToNandOrNor_Coalesce_StaysAsTheBoundaryWithItsOperandsRewritten_Test(string gate)
    {
        K3Rule original = K3Rule.TryCreate("COALESCE(a AND b, NOT c)", 3)!;
        CompiledRule<RuleTestContext> gateRule = Expand(original.Compiled, gate);
        K3Rule rewritten = original.Rewrite(rule => Expand(rule, gate));

        Assert.Equal(new HashSet<string> { gate, "coalesce" }, OperatorsOf(gateRule));
        foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
        {
            Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision after = await rewritten.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(before.Result, after.Result);
        }
    }

    /// <summary>The original rule is immutable and keeps its own operators.</summary>
    [Theory]
    [InlineData("nand")]
    [InlineData("nor")]
    public void ExpandToNandOrNor_Rule_LeavesTheOriginalUntouched_Test(string gate)
    {
        K3Rule original = K3Rule.TryCreate("(a IMPLIES b) XOR ANY(a, b, c)", 3)!;
        string textBefore = original.Compiled.CanonicalText;

        CompiledRule<RuleTestContext> rewritten = Expand(original.Compiled, gate);

        Assert.NotSame(original.Compiled, rewritten);
        Assert.NotEqual(textBefore, rewritten.CanonicalText);
        Assert.Equal(textBefore, original.Compiled.CanonicalText);
    }

    /// <summary>The gate-only rule prints canonical text that recompiles to the identical rule.</summary>
    [Theory]
    [InlineData("nand", "a XOR b")]
    [InlineData("nand", "AtLeast(2, a, b, c)")]
    [InlineData("nor", "a IMPLIES b")]
    [InlineData("nor", "Exactly(1, a, b, c)")]
    public void ExpandToNandOrNor_Rule_PrintsCanonicalTextThatRecompilesToTheSameRule_Test(string gate, string ruleText)
    {
        CompiledRule<RuleTestContext> rewritten = Expand(K3Rule.TryCreate(ruleText, 3)!.Compiled, gate);
        K3Rule? recompiled = K3Rule.TryCreate(rewritten.CanonicalText, 3);

        Assert.NotNull(recompiled);
        Assert.Equal(rewritten.CanonicalText, recompiled.Compiled.CanonicalText);
    }

    /// <summary>A declared outermost <c>Collapse</c> policy survives the rewrite and still produces the same outcome.</summary>
    [Theory]
    [InlineData("nand")]
    [InlineData("nor")]
    public async Task ExpandToNandOrNor_RuleWithCollapse_PreservesThePolicyAndOutcome_Test(string gate)
    {
        K3Rule original = K3Rule.TryCreate("Collapse(a IMPLIES b, UnknownIsError)", 2)!;
        K3Rule rewritten = original.Rewrite(rule => Expand(rule, gate));
        TruthValue[] assignment = [TruthValue.True, TruthValue.Unknown];

        Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
        Decision after = await rewritten.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

        Assert.Equal(CollapsePolicy.UnknownIsError, rewritten.Compiled.CollapsePolicy);
        Assert.Equal(CollapseOutcome.RejectedUnresolved, after.Outcome);
        Assert.Equal(before.Result, after.Result);
    }

    /// <summary>A predicate that throws is <c>Unknown</c> plus a fault in the gate-only rule, as in the original.</summary>
    [Theory]
    [InlineData("nand")]
    [InlineData("nor")]
    public async Task ExpandToNandOrNor_FaultingPredicate_ResultsInTheSameUnknownAndAFault_Test(string gate)
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
            .Compile("boom OR ok")
            .CompiledRule!;
        CompiledRule<RuleTestContext> rewritten = Expand(original, gate);

        Decision before = await original.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );
        Decision after = await rewritten.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(before.Result, after.Result);
        Assert.NotEmpty(after.Faults);
    }

    private static CompiledRule<RuleTestContext> Expand(CompiledRule<RuleTestContext> rule, string gate)
    {
        return gate == "nand" ? rule.ExpandToNand() : rule.ExpandToNor();
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
