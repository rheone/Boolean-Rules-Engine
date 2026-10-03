namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Exhaustive conformance of the engine's current operators against <see cref="K3Oracle"/>: every
/// {True, False, Unknown} assignment, operand counts up to 4, through the public compile/evaluate pipeline.
/// </summary>
public sealed class K3ConformanceTests
{
    private const int MaxArity = 4;

    /// <summary>NOT over every input matches the oracle.</summary>
    [Fact]
    public async Task Evaluate_NotOverAllAssignments_MatchesOracle_Test()
    {
        List<string> mismatches = await MismatchesAsync("NOT a", 1, v => K3Oracle.Not(v[0]));

        Assert.Empty(mismatches);
    }

    /// <summary>AND over 2..4 operands matches the oracle for every assignment.</summary>
    [Fact]
    public async Task Evaluate_AndOverAllAssignments_MatchesOracle_Test()
    {
        List<string> mismatches = [];
        for (int arity = 2; arity <= MaxArity; arity++)
        {
            string text = string.Join(" AND ", Names(arity).Split(", "));
            mismatches.AddRange(await MismatchesAsync(text, arity, K3Oracle.And));
        }

        Assert.Empty(mismatches);
    }

    /// <summary>OR over 2..4 operands matches the oracle for every assignment.</summary>
    [Fact]
    public async Task Evaluate_OrOverAllAssignments_MatchesOracle_Test()
    {
        List<string> mismatches = [];
        for (int arity = 2; arity <= MaxArity; arity++)
        {
            string text = string.Join(" OR ", Names(arity).Split(", "));
            mismatches.AddRange(await MismatchesAsync(text, arity, K3Oracle.Or));
        }

        Assert.Empty(mismatches);
    }

    /// <summary>Binary XOR matches the oracle for every assignment.</summary>
    [Fact]
    public async Task Evaluate_XorOverAllAssignments_MatchesOracle_Test()
    {
        List<string> mismatches = await MismatchesAsync("a XOR b", 2, v => K3Oracle.Xor(v[0], v[1]));

        Assert.Empty(mismatches);
    }

    /// <summary>Binary EQUIVALENT (legacy XNOR, symbol and IFF spellings) matches the oracle for every assignment.</summary>
    [Fact]
    public async Task Evaluate_EquivalentOverAllAssignments_MatchesOracle_Test()
    {
        List<string> mismatches = await MismatchesAsync("a XNOR b", 2, v => K3Oracle.Equivalent(v[0], v[1]));

        Assert.Empty(mismatches);
    }

    /// <summary>Binary IMPLIES, in word, symbol and lower-case spellings, matches the oracle for every assignment.</summary>
    [Theory]
    [InlineData("a IMPLIES b")]
    [InlineData("a implies b")]
    [InlineData("a → b")]
    public async Task Evaluate_ImpliesOverAllAssignments_MatchesOracle_Test(string rule)
    {
        List<string> mismatches = await MismatchesAsync(rule, 2, v => K3Oracle.Implies(v[0], v[1]));

        Assert.Empty(mismatches);
    }

    /// <summary>Binary NAND, in word, symbol and lower-case spellings, matches the oracle for every assignment.</summary>
    [Theory]
    [InlineData("a NAND b")]
    [InlineData("a nand b")]
    [InlineData("a ↑ b")]
    public async Task Evaluate_NandOverAllAssignments_MatchesOracle_Test(string rule)
    {
        List<string> mismatches = await MismatchesAsync(rule, 2, v => K3Oracle.Nand(v[0], v[1]));

        Assert.Empty(mismatches);
    }

    /// <summary>Binary NOR, in word, symbol and lower-case spellings, matches the oracle for every assignment.</summary>
    [Theory]
    [InlineData("a NOR b")]
    [InlineData("a nor b")]
    [InlineData("a ↓ b")]
    public async Task Evaluate_NorOverAllAssignments_MatchesOracle_Test(string rule)
    {
        List<string> mismatches = await MismatchesAsync(rule, 2, v => K3Oracle.Nor(v[0], v[1]));

        Assert.Empty(mismatches);
    }

    /// <summary>ExactlyOne over 2..4 operands matches the oracle's interval cardinality for every assignment.</summary>
    [Fact]
    public async Task Evaluate_ExactlyOneOverAllAssignments_MatchesOracle_Test()
    {
        List<string> mismatches = [];
        for (int arity = 2; arity <= MaxArity; arity++)
        {
            mismatches.AddRange(await MismatchesAsync($"ExactlyOne({Names(arity)})", arity, K3Oracle.ExactlyOne));
        }

        Assert.Empty(mismatches);
    }

    /// <summary>
    /// Every threshold operator, every operand count 2..4 and every legal <c>k</c> matches the oracle's
    /// interval cardinality. Out-of-range <c>k</c> is a compile error and is skipped here (covered by
    /// <c>XorExactlyOneThresholdTests</c>).
    /// </summary>
    [Fact]
    public async Task Evaluate_ThresholdFamilyOverAllAssignments_MatchesOracle_Test()
    {
        (string Name, Func<int, int, bool> Satisfies)[] family =
        [
            ("AtLeast", (count, k) => count >= k),
            ("AtMost", (count, k) => count <= k),
            ("GreaterThan", (count, k) => count > k),
            ("LessThan", (count, k) => count < k),
            ("Exactly", (count, k) => count == k),
        ];

        List<string> mismatches = [];
        foreach ((string name, Func<int, int, bool> satisfies) in family)
        {
            for (int arity = 2; arity <= MaxArity; arity++)
            {
                for (int k = 0; k <= arity; k++)
                {
                    string text = $"{name}({k}, {Names(arity)})";
                    if (K3Rule.TryCreate(text, arity) is null)
                    {
                        continue;
                    }

                    int threshold = k;
                    mismatches.AddRange(
                        await MismatchesAsync(text, arity, v => K3Oracle.Cardinality(c => satisfies(c, threshold), v))
                    );
                }
            }
        }

        Assert.Empty(mismatches);
    }

    private static string Names(int arity)
    {
        return string.Join(", ", Enumerable.Range(0, arity).Select(i => (char)('a' + i)));
    }

    /// <summary>
    /// Evaluates <paramref name="ruleText"/> under every assignment of <paramref name="arity"/> inputs and
    /// returns a human-readable line for each row where the engine disagrees with <paramref name="expected"/>.
    /// </summary>
    private static async Task<List<string>> MismatchesAsync(
        string ruleText,
        int arity,
        Func<IReadOnlyList<TruthValue>, TruthValue> expected
    )
    {
        K3Rule rule =
            K3Rule.TryCreate(ruleText, arity) ?? throw new InvalidOperationException($"'{ruleText}' did not compile.");
        List<string> mismatches = [];
        foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
        {
            Decision decision = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            TruthValue want = expected(assignment);
            if (decision.Result != want)
            {
                mismatches.Add($"{ruleText} with [{string.Join(", ", assignment)}]: engine={decision.Result}, oracle={want}");
            }
        }

        return mismatches;
    }
}
