namespace TruthWeaver.Yaml.Tests.TestSupport;

using TruthWeaver.Abstractions;
using TruthWeaver.Registry;

/// <summary>Small hand-written test predicates used to exercise <see cref="YamlRuleExtensions"/>.</summary>
public static class TestPredicates
{
    /// <summary>Registers a zero-argument predicate that always returns a fixed value.</summary>
    public static PredicateRegistryBuilder<YamlTestContext> AddConstant(
        this PredicateRegistryBuilder<YamlTestContext> builder,
        string name,
        bool value
    )
    {
        return builder.Add(
            PredicateSchema.NoArguments(name, name, $"Test predicate '{name}', always {value}."),
            (_, _, _) => ValueTask.FromResult(value ? TruthValue.True : TruthValue.False)
        );
    }

    /// <summary>Registers a single-string-argument predicate whose truth is "does the argument equal <paramref name="matchValue"/>?" (case-sensitive).</summary>
    public static PredicateRegistryBuilder<YamlTestContext> AddStringArgPredicate(
        this PredicateRegistryBuilder<YamlTestContext> builder,
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
                ValueTask.FromResult(
                    string.Equals(args.GetString(argumentName), matchValue, StringComparison.Ordinal)
                        ? TruthValue.True
                        : TruthValue.False
                )
        );
    }
}
