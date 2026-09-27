namespace BooleanRulesEngine.Benchmarks;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Building;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Testing;

/// <summary>
/// Deterministic rule/registry generators shared by the compile-time and eval-time benchmarks, so
/// every benchmark class builds its fixtures the same way instead of hand-rolling ad hoc trees.
/// </summary>
internal static class RuleFixtures
{
    /// <summary>
    /// Builds a registry of <paramref name="termCount"/> distinct zero-argument predicates named
    /// <c>term0</c>..<c>term{N-1}</c>, each a stateless lambda that always answers <see langword="true"/>.
    /// </summary>
    /// <param name="termCount">How many distinct predicates to register.</param>
    /// <returns>The built registry.</returns>
    public static PredicateRegistry<BenchmarkContext> BuildRegistry(int termCount)
    {
        PredicateRegistryBuilder<BenchmarkContext> builder = PredicateRegistry<BenchmarkContext>.CreateBuilder();
        for (int i = 0; i < termCount; i++)
        {
            (PredicateSchema schema, Func<BenchmarkContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
                FakePredicates.Returning<BenchmarkContext>($"term{i}", true);
            builder.Add(schema, evaluate);
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds a "grouped" rule shape representative of real authoring — an <c>AND</c> of several
    /// <c>OR</c> groups (e.g. "must satisfy every one of these categories, each satisfied by any of
    /// several alternatives"). Each group's members are its own, mostly-disjoint slice of the
    /// <c>term0</c>..<c>term{termCount-1}</c> pool; every fifth group additionally repeats the very
    /// first term (<c>term0</c>), so the tree has a handful of terms genuinely shared across distant
    /// branches (structurally interesting for the BDD analyzer's term-identity deduplication) without
    /// the dense, adversarial variable interleaving that makes BDD construction blow up - a naive
    /// first-appearance variable ordering (as used by <c>Analysis.BddManager</c>) can be exponential in
    /// node count for CNF-shaped trees whose clauses overlap heavily and irregularly, which is exactly
    /// what fully random group membership would produce at this size.
    /// </summary>
    /// <param name="termCount">The size of the term name pool (<c>term0</c>..<c>term{N-1}</c>) group members are drawn from.</param>
    /// <param name="groupSize">How many terms each <c>OR</c> group contains.</param>
    /// <returns>The root <c>AND</c>-of-<c>OR</c>-groups builder.</returns>
    public static RuleBuilder BuildGroupedRule(int termCount, int groupSize)
    {
        const int SharedTermRepeatEveryNGroups = 5;
        int groupCount = Math.Max(1, termCount / groupSize);
        RuleBuilder[] groups = new RuleBuilder[groupCount];
        for (int g = 0; g < groupCount; g++)
        {
            List<RuleBuilder> members = new(groupSize + 1);
            for (int m = 0; m < groupSize; m++)
            {
                int termIndex = (g * groupSize) + m;
                members.Add(RuleBuilder.Predicate($"term{termIndex}"));
            }

            if (g > 0 && g % SharedTermRepeatEveryNGroups == 0)
            {
                members.Add(RuleBuilder.Predicate("term0"));
            }

            groups[g] = RuleBuilder.Or([.. members]);
        }

        return RuleBuilder.And(groups);
    }

    /// <summary>
    /// Builds a rule exercising shared-term fan-out: an <c>OR</c> of <paramref name="branchCount"/>
    /// <c>AND</c> branches, each branch referencing the same <paramref name="sharedTermName"/> term
    /// alongside one branch-unique term. Evaluating this rule (in
    /// <see cref="Evaluation.EvaluationMode.Exhaustive"/> mode, so every branch actually runs) invokes
    /// the shared predicate at most once per evaluation regardless of <paramref name="branchCount"/>
    /// (ADR-0002's per-evaluation term memoization, keyed by term identity) while invoking each
    /// branch-unique predicate exactly once.
    /// </summary>
    /// <param name="sharedTermName">The name of the term referenced from every branch.</param>
    /// <param name="branchCount">How many branches (and branch-unique terms) to generate.</param>
    /// <returns>The root <c>OR</c>-of-<c>AND</c>-branches builder.</returns>
    public static RuleBuilder BuildSharedTermFanOut(string sharedTermName, int branchCount)
    {
        RuleBuilder[] branches = new RuleBuilder[branchCount];
        for (int b = 0; b < branchCount; b++)
        {
            branches[b] = RuleBuilder.And(RuleBuilder.Predicate(sharedTermName), RuleBuilder.Predicate($"branch{b}"));
        }

        return RuleBuilder.Or(branches);
    }

    /// <summary>
    /// Builds the registry a <see cref="BuildSharedTermFanOut"/> rule needs: the shared term plus one
    /// distinct term per branch, all stateless lambdas that always answer <see langword="true"/>.
    /// </summary>
    /// <param name="sharedTermName">The shared term's registered name.</param>
    /// <param name="branchCount">How many branch-unique terms to register.</param>
    /// <returns>The built registry.</returns>
    public static PredicateRegistry<BenchmarkContext> BuildSharedTermRegistry(string sharedTermName, int branchCount)
    {
        PredicateRegistryBuilder<BenchmarkContext> builder = PredicateRegistry<BenchmarkContext>.CreateBuilder();
        (
            PredicateSchema sharedSchema,
            Func<BenchmarkContext, PredicateArguments, CancellationToken, ValueTask<bool>> sharedEvaluate
        ) = FakePredicates.Returning<BenchmarkContext>(sharedTermName, true);
        builder.Add(sharedSchema, sharedEvaluate);
        for (int b = 0; b < branchCount; b++)
        {
            (PredicateSchema schema, Func<BenchmarkContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
                FakePredicates.Returning<BenchmarkContext>($"branch{b}", true);
            builder.Add(schema, evaluate);
        }

        return builder.Build();
    }
}
