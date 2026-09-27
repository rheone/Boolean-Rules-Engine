namespace BooleanRulesEngine.Tests.TestSupport;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Registry;

/// <summary>
/// Small hand-written test predicates (constant-true, constant-false, throwing, delayed/cancellable)
/// per the spec's testing decisions — behavioral test doubles, not mocks, driven through
/// <c>RuleCompiler.Compile</c> and <c>CompiledRule.EvaluateAsync</c>.
/// </summary>
public static class TestPredicates
{
    /// <summary>Registers a zero-argument predicate that always returns a fixed value.</summary>
    public static PredicateRegistryBuilder<RuleTestContext> AddConstant(
        this PredicateRegistryBuilder<RuleTestContext> builder,
        string name,
        bool value
    )
    {
        return builder.Add(
            PredicateSchema.NoArguments(name, name, $"Test predicate '{name}', always {value}."),
            (_, _, _) => ValueTask.FromResult(value)
        );
    }

    /// <summary>Registers a zero-argument predicate that records each invocation (for memoization/short-circuit tests) and returns a fixed value.</summary>
    public static PredicateRegistryBuilder<RuleTestContext> AddCountingConstant(
        this PredicateRegistryBuilder<RuleTestContext> builder,
        string name,
        bool value,
        List<string> invocationLog
    )
    {
        return builder.Add(
            PredicateSchema.NoArguments(name, name, $"Test predicate '{name}', always {value}, logs invocations."),
            (_, _, _) =>
            {
                invocationLog.Add(name);
                return ValueTask.FromResult(value);
            }
        );
    }

    /// <summary>Registers a zero-argument predicate that always throws (surfaces as a <see cref="Fault"/>, i.e. <see cref="TruthValue.Unknown"/>).</summary>
    public static PredicateRegistryBuilder<RuleTestContext> AddThrowing(
        this PredicateRegistryBuilder<RuleTestContext> builder,
        string name
    )
    {
        return builder.Add(
            PredicateSchema.NoArguments(name, name, $"Test predicate '{name}', always faults."),
            (_, _, _) => throw new InvalidOperationException($"'{name}' faulted.")
        );
    }

    /// <summary>Registers a zero-argument predicate that delays for a fixed duration (honoring cancellation) before returning a fixed value.</summary>
    public static PredicateRegistryBuilder<RuleTestContext> AddDelayed(
        this PredicateRegistryBuilder<RuleTestContext> builder,
        string name,
        TimeSpan delay,
        bool value
    )
    {
        return builder.Add(
            PredicateSchema.NoArguments(name, name, $"Test predicate '{name}', delays then returns {value}."),
            async (_, _, ct) =>
            {
                await Task.Delay(delay, ct).ConfigureAwait(false);
                return value;
            }
        );
    }

    /// <summary>Registers a single-string-argument predicate whose truth is "does the argument equal <paramref name="matchValue"/>?" (case-sensitive).</summary>
    public static PredicateRegistryBuilder<RuleTestContext> AddStringArgPredicate(
        this PredicateRegistryBuilder<RuleTestContext> builder,
        string name,
        string argumentName,
        string matchValue
    )
    {
        return builder.Add(
            new PredicateSchema(
                name,
                name,
                $"Test predicate '{name}', true iff '{argumentName}' equals '{matchValue}'.",
                [new PredicateArgumentSchema(argumentName, $"The value to compare against '{matchValue}'.", LiteralKind.String)]
            ),
            (_, args, _) =>
                ValueTask.FromResult(string.Equals(args.GetString(argumentName), matchValue, StringComparison.Ordinal))
        );
    }

    /// <summary>Registers a single-GUID-argument predicate whose truth is "does the argument equal <paramref name="matchValue"/>?".</summary>
    public static PredicateRegistryBuilder<RuleTestContext> AddGuidArgPredicate(
        this PredicateRegistryBuilder<RuleTestContext> builder,
        string name,
        string argumentName,
        Guid matchValue
    )
    {
        return builder.Add(
            new PredicateSchema(
                name,
                name,
                $"Test predicate '{name}', true iff '{argumentName}' equals '{matchValue}'.",
                [new PredicateArgumentSchema(argumentName, $"The GUID to compare against '{matchValue}'.", LiteralKind.Guid)]
            ),
            (_, args, _) => ValueTask.FromResult(args.GetGuid(argumentName) == matchValue)
        );
    }
}
