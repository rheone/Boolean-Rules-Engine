namespace TruthWeaver.Tests;

using System.Text;
using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <c>CompiledRule.Canonicalize</c> (ticket 26, ADR-0005 decision 10) gives rules that are equivalent under a fixed set of
/// Strong Kleene-sound rewrites one deterministic representation, without changing what any of them evaluates to.
/// </summary>
public sealed class CanonicalizeTests
{
    /// <summary>
    /// For many generated rules and every {True, False, Unknown} assignment, the canonical rule evaluates like the rule and
    /// the oracle, canonicalising twice changes nothing, and the result is the same on every call.
    /// </summary>
    [Fact]
    public async Task Canonicalize_GeneratedRules_EvaluateEqualToOriginalAndAreIdempotentAndDeterministic_Test()
    {
        Random random = new(20261201);
        List<string> failures = [];
        int checkedRules = 0;

        for (int i = 0; i < 600; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 3);
            K3Rule? original = K3Rule.TryCreate(generated.Text, 3);
            if (original is null)
            {
                continue;
            }

            K3Rule canonical = original.Rewrite(rule => rule.Canonicalize());
            checkedRules++;
            string once = canonical.Compiled.CanonicalText;
            if (once != canonical.Compiled.Canonicalize().CanonicalText)
            {
                failures.Add($"{generated.Text}: not idempotent ({once})");
            }

            if (once != original.Compiled.Canonicalize().CanonicalText)
            {
                failures.Add($"{generated.Text}: not deterministic ({once})");
            }

            if (RuleMetrics.NodeCount(canonical.Compiled) > RuleMetrics.NodeCount(original.Compiled))
            {
                failures.Add($"{generated.Text}: canonical form is larger ({once})");
            }

            foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
            {
                Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                Decision after = await canonical.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                TruthValue oracle = generated.Eval(assignment);
                if (before.Result != after.Result || after.Result != oracle)
                {
                    failures.Add(
                        $"{generated.Text} @ [{string.Join(",", assignment)}]: original={before.Result} canonical={after.Result} oracle={oracle}"
                    );
                }
            }
        }

        Assert.Empty(failures);
        Assert.True(checkedRules > 400, $"only {checkedRules} rules were checked");
    }

    /// <summary>
    /// Two randomly permuted, regrouped, duplicated, aliased (<c>ANY</c>/<c>ALL</c>) and double-negated spellings of the same
    /// AND/OR/NOT rule canonicalise to the same text, and every spelling still evaluates like the oracle.
    /// </summary>
    [Fact]
    public async Task Canonicalize_EquivalentSpellingsOfARule_ProduceTheSameText_Test()
    {
        Random random = new(20261202);
        List<string> failures = [];
        int checkedRules = 0;

        for (int i = 0; i < 500; i++)
        {
            Shape shape = Shape.Generate(random, depth: 3);
            string first = shape.Spell(random);
            string second = shape.Spell(random);
            K3Rule? firstRule = K3Rule.TryCreate(first, 3);
            K3Rule? secondRule = K3Rule.TryCreate(second, 3);
            if (firstRule is null || secondRule is null)
            {
                failures.Add($"did not compile: {first} / {second}");
                continue;
            }

            checkedRules++;
            string firstCanonical = firstRule.Compiled.Canonicalize().CanonicalText;
            string secondCanonical = secondRule.Compiled.Canonicalize().CanonicalText;
            if (firstCanonical != secondCanonical)
            {
                failures.Add($"{first}  vs  {second}: {firstCanonical}  !=  {secondCanonical}");
            }

            foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
            {
                TruthValue expected = shape.Eval(assignment);
                Decision a = await firstRule
                    .Rewrite(rule => rule.Canonicalize())
                    .EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                Decision b = await secondRule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                if (a.Result != expected || b.Result != expected)
                {
                    failures.Add(
                        $"{first} vs {second} @ [{string.Join(",", assignment)}]: {a.Result}/{b.Result} expected {expected}"
                    );
                }
            }
        }

        Assert.Empty(failures);
        Assert.True(checkedRules > 400, $"only {checkedRules} rules were checked");
    }

    /// <summary>Each K3-sound normalisation, applied to a small rule, gives the stated canonical text and the same value.</summary>
    [Theory]
    [InlineData("b AND a", 2, "a AND b")]
    [InlineData("c OR a OR b", 3, "a OR b OR c")]
    [InlineData("(a AND b) AND c", 3, "a AND b AND c")]
    [InlineData("a AND (b AND c)", 3, "a AND b AND c")]
    [InlineData("(c OR b) OR a", 3, "a OR b OR c")]
    [InlineData("a AND a", 1, "a")]
    [InlineData("a OR b OR a", 2, "a OR b")]
    [InlineData("NOT NOT a", 1, "a")]
    [InlineData("NOT NOT NOT a", 1, "NOT a")]
    [InlineData("ANY(b, a)", 2, "a OR b")]
    [InlineData("ALL(b, a)", 2, "a AND b")]
    [InlineData("GreaterThan(1, c, b, a)", 3, "AtLeast(2, a, b, c)")]
    [InlineData("LessThan(2, c, b, a)", 3, "AtMost(1, a, b, c)")]
    [InlineData("ExactlyOne(c, a)", 3, "Exactly(1, a, c)")]
    [InlineData("b XOR a", 2, "(a XOR b)")]
    [InlineData("b EQUIVALENT a", 2, "(a EQUIVALENT b)")]
    [InlineData("NXOR(c, a, b)", 3, "NXOR(a, b, c)")]
    [InlineData("BETWEEN(1, 2, c, a, b)", 3, "BETWEEN(1, 2, a, b, c)")]
    [InlineData("AtLeast(2, c, a, b)", 3, "AtLeast(2, a, b, c)")]
    public async Task Canonicalize_SmallRule_GivesTheStatedCanonicalText_Test(string ruleText, int arity, string expected)
    {
        K3Rule original = K3Rule.TryCreate(ruleText, arity)!;
        K3Rule canonical = original.Rewrite(rule => rule.Canonicalize());

        Assert.Equal(expected, canonical.Compiled.CanonicalText);
        foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
        {
            Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision after = await canonical.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(before.Result, after.Result);
        }
    }

    /// <summary>
    /// Operators whose operand order carries meaning are never reordered, and classical-only laws are never applied:
    /// <c>a OR NOT a</c> keeps both operands (it is <c>Unknown</c>, not <c>True</c>, when <c>a</c> is).
    /// </summary>
    [Theory]
    [InlineData("COALESCE(b, a)", 2)]
    [InlineData("b IMPLIES a", 2)]
    [InlineData("If(c, b, a)", 3)]
    [InlineData("Project(b, True)", 2)]
    [InlineData("a OR NOT a", 1)]
    [InlineData("a AND NOT a", 1)]
    [InlineData("a IMPLIES a", 1)]
    [InlineData("a XOR a", 1)]
    [InlineData("a AND True", 1)]
    public async Task Canonicalize_OrderSensitiveOrClassicalOnlyShape_KeepsItsMeaning_Test(string ruleText, int arity)
    {
        K3Rule original = K3Rule.TryCreate(ruleText, arity)!;
        K3Rule canonical = original.Rewrite(rule => rule.Canonicalize());

        foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
        {
            Decision before = await original.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision after = await canonical.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(before.Result, after.Result);
        }

        // Nothing was dropped or folded: the node count is unchanged for these shapes.
        Assert.Equal(RuleMetrics.NodeCount(original.Compiled), RuleMetrics.NodeCount(canonical.Compiled));
    }

    /// <summary>The canonical rule is a new rule: the original keeps its text and the declared Collapse policy survives.</summary>
    [Fact]
    public void Canonicalize_Rule_LeavesTheOriginalUntouchedAndKeepsTheCollapsePolicy_Test()
    {
        K3Rule original = K3Rule.TryCreate("Collapse(b AND a, UnknownAsFalse)", 2)!;
        string textBefore = original.Compiled.CanonicalText;

        CompiledRule<RuleTestContext> canonical = original.Compiled.Canonicalize();

        Assert.NotSame(original.Compiled, canonical);
        Assert.Equal(textBefore, original.Compiled.CanonicalText);
        Assert.Equal(CollapsePolicy.UnknownAsFalse, canonical.CollapsePolicy);
        Assert.Equal("Collapse(a AND b, UnknownAsFalse)", canonical.CanonicalText);
    }

    /// <summary>A small AND/OR/NOT tree over terms a, b, c that can spell itself many equivalent ways.</summary>
    private abstract record Shape
    {
        /// <summary>Generates a random shape.</summary>
        public static Shape Generate(Random random, int depth)
        {
            if (depth == 0 || random.Next(4) == 0)
            {
                return new Leaf(random.Next(3));
            }

            Shape[] Operands()
            {
                return [.. Enumerable.Range(0, random.Next(2, 4)).Select(_ => Generate(random, depth - 1))];
            }

            return random.Next(3) switch
            {
                0 => new Negation(Generate(random, depth - 1)),
                1 => new Junction(true, Operands()),
                _ => new Junction(false, Operands()),
            };
        }

        /// <summary>Evaluates the shape from the oracle.</summary>
        public abstract TruthValue Eval(IReadOnlyList<TruthValue> values);

        /// <summary>Spells the shape as rule text, randomly permuting, regrouping, duplicating, aliasing and double-negating.</summary>
        public abstract string Spell(Random random);
    }

    private sealed record Leaf(int Index) : Shape
    {
        public override TruthValue Eval(IReadOnlyList<TruthValue> values)
        {
            return values[this.Index];
        }

        public override string Spell(Random random)
        {
            return ((char)('a' + this.Index)).ToString();
        }
    }

    private sealed record Negation(Shape Operand) : Shape
    {
        public override TruthValue Eval(IReadOnlyList<TruthValue> values)
        {
            return K3Oracle.Not(this.Operand.Eval(values));
        }

        public override string Spell(Random random)
        {
            string inner = $"NOT ({this.Operand.Spell(random)})";
            return random.Next(3) == 0 ? $"NOT (NOT ({inner}))" : inner;
        }
    }

    private sealed record Junction(bool IsAnd, Shape[] Operands) : Shape
    {
        public override TruthValue Eval(IReadOnlyList<TruthValue> values)
        {
            IEnumerable<TruthValue> evaluated = this.Operands.Select(o => o.Eval(values));
            return this.IsAnd ? K3Oracle.And(evaluated) : K3Oracle.Or(evaluated);
        }

        public override string Spell(Random random)
        {
            List<string> spelled = [.. this.Operands.Select(o => o.Spell(random))];

            // A repeated operand is idempotent under AND/OR.
            if (random.Next(3) == 0)
            {
                int duplicate = random.Next(this.Operands.Length);
                spelled.Add(this.Operands[duplicate].Spell(random));
            }

            spelled = [.. spelled.OrderBy(_ => random.Next())];

            // Regroup: wrap a random adjacent pair in a nested group of the same operator (associativity).
            string word = this.IsAnd ? "AND" : "OR";
            if (spelled.Count >= 3 && random.Next(2) == 0)
            {
                int at = random.Next(spelled.Count - 1);
                spelled[at] = $"({spelled[at]} {word} {spelled[at + 1]})";
                spelled.RemoveAt(at + 1);
            }

            // ANY/ALL are exact aliases of OR/AND in Strong Kleene logic.
            if (random.Next(3) == 0)
            {
                return $"{(this.IsAnd ? "ALL" : "ANY")}({string.Join(", ", spelled)})";
            }

            StringBuilder text = new("(");
            text.AppendJoin($" {word} ", spelled).Append(')');
            return text.ToString();
        }
    }
}
