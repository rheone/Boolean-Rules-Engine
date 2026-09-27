namespace TruthWeaver.Tests;

using CsCheck;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket: property-based round-trip testing for the DSL printer/parser. Generalizes
/// <see cref="CanonicalPrinterTests"/>'s hand-picked examples across the whole
/// <see cref="Expression"/> shape space: for any randomly generated valid tree,
/// <c>parse(print(tree))</c> must be structurally equal to <c>tree</c>. Uses CsCheck for generation
/// and shrinking (see <c>Directory.Packages.props</c> for why CsCheck was chosen).
/// </summary>
public sealed class DslRoundTripPropertyTests
{
    /// <summary>
    /// The generator's recursion cutoff. Bounded well under <see cref="CompilerOptions"/>'s default
    /// 32/512 depth/node limits so every generated tree is one the default compiler accepts outright
    /// — this suite is about round-tripping, not about exercising the resource-limit diagnostics.
    /// </summary>
    private const int MaxTreeDepth = 4;

    /// <summary>The bounded sample size — enough to exercise every shape below, with no unbounded/flaky runtime.</summary>
    private const int SampleIterations = 300;

    private static readonly IReadOnlyList<TermSpec> TermSpecs =
    [
        new TermSpec("flag", null, null),
        new TermSpec("termString", "value", LiteralKind.String),
        new TermSpec("termInt64", "value", LiteralKind.Int64),
        new TermSpec("termDecimal", "value", LiteralKind.Decimal),
        new TermSpec("termBoolean", "value", LiteralKind.Boolean),
        new TermSpec("termDateTimeOffset", "value", LiteralKind.DateTimeOffset),
        new TermSpec("termGuid", "value", LiteralKind.Guid),
        new TermSpec("termStringArray", "value", LiteralKind.StringArray),
        new TermSpec("termInt64Array", "value", LiteralKind.Int64Array),
        new TermSpec("termDecimalArray", "value", LiteralKind.DecimalArray),
        new TermSpec("termBooleanArray", "value", LiteralKind.BooleanArray),
        new TermSpec("termDateTimeOffsetArray", "value", LiteralKind.DateTimeOffsetArray),
        new TermSpec("termGuidArray", "value", LiteralKind.GuidArray),
    ];

    /// <summary>
    /// A "safe-ish" DSL string character set that still exercises the printer/lexer's quote and
    /// backslash escaping (<see cref="LiteralValue"/>'s <c>EscapeForDsl</c> and <c>Lexer.ReadString</c>),
    /// plus a raw newline/tab (valid, unescaped, inside a DSL quoted string).
    /// </summary>
    private static readonly Gen<char> GenStringChar = Gen.Frequency(
        (85, Gen.Char.AlphaNumeric),
        (3, Gen.Const(' ')),
        (3, Gen.Const('"')),
        (3, Gen.Const('\\')),
        (3, Gen.Const('\n')),
        (3, Gen.Const('\t'))
    );

    private static readonly Gen<string> GenString = Gen.String[GenStringChar, 0, 10];

    private static readonly Gen<Expression> GenConstant = Gen.Bool.Select(value => (Expression)new ConstantExpression(value));

    private static readonly Gen<Expression> GenTerm = Gen.OneOf([.. TermSpecs.Select(BuildTermGen)]);

    private static readonly Gen<Expression> GenLeaf = Gen.OneOf(GenConstant, GenTerm);

    /// <summary>
    /// The full recursive expression generator — every operator (<c>AND</c>/<c>OR</c>/<c>NOT</c>/
    /// <c>XOR</c>/<c>XNOR</c>/<c>ExactlyOne</c>/the threshold family) plus leaves, arity/threshold-range
    /// constrained to mirror <c>RuleNodeCompiler</c>'s own validation exactly, so no generated tree is
    /// ever rejected for a reason unrelated to round-tripping.
    /// </summary>
    private static readonly Gen<Expression> GenExpressionTree = Gen.Recursive<Expression>(
        (depth, self) =>
        {
            if (depth >= MaxTreeDepth)
            {
                return GenLeaf;
            }

            Gen<Expression> genAnd = self.Array[2, 4]
                .Select(operands => (Expression)new AndExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genOr = self.Array[2, 4]
                .Select(operands => (Expression)new OrExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genNot = self.Select(operand => (Expression)new NotExpression(operand));
            Gen<Expression> genXor = self.Select(self, (left, right) => (Expression)new XorExpression(left, right));
            Gen<Expression> genXnor = self.Select(self, (left, right) => (Expression)new XnorExpression(left, right));
            Gen<Expression> genExactlyOne = self.Array[2, 4]
                .Select(operands => (Expression)new ExactlyOneExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genThreshold = BuildThresholdGen(self);

            return Gen.Frequency(
                (3, GenLeaf),
                (2, genAnd),
                (2, genOr),
                (2, genNot),
                (1, genXor),
                (1, genXnor),
                (1, genExactlyOne),
                (1, genThreshold)
            );
        }
    );

    [Fact]
    public void Parsing_the_printed_form_of_a_generated_tree_reproduces_a_structurally_equal_tree()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());

        GenExpressionTree.Sample(
            tree =>
            {
                string printed = CanonicalPrinter.Print(tree);
                CompilationResult<RuleTestContext> result = compiler.Compile(printed);
                string diagnosticMessages = string.Join("; ", result.Diagnostics.Select(d => d.Message));
                string failureMessage = $"Expected '{printed}' to compile cleanly but got: {diagnosticMessages}";

                Assert.True(result.Succeeded, failureMessage);
                Assert.Equal(tree, result.CompiledRule!.Root);
            },
            iter: SampleIterations
        );
    }

    private static Gen<LiteralValue> GenLiteralValue(LiteralKind kind)
    {
        return kind switch
        {
            LiteralKind.String => GenString.Select(LiteralValue.OfString),
            LiteralKind.Int64 => Gen.Long[-1_000_000, 1_000_000].Select(LiteralValue.OfInt64),
            LiteralKind.Decimal => Gen.Decimal[-100_000m, 100_000m].Select(LiteralValue.OfDecimal),
            LiteralKind.Boolean => Gen.Bool.Select(LiteralValue.OfBoolean),
            LiteralKind.DateTimeOffset => Gen.DateTimeOffset.Select(LiteralValue.OfDateTimeOffset),
            LiteralKind.Guid => Gen.Guid.Select(LiteralValue.OfGuid),
            LiteralKind.StringArray => GenLiteralArray(LiteralKind.String),
            LiteralKind.Int64Array => GenLiteralArray(LiteralKind.Int64),
            LiteralKind.DecimalArray => GenLiteralArray(LiteralKind.Decimal),
            LiteralKind.BooleanArray => GenLiteralArray(LiteralKind.Boolean),
            LiteralKind.DateTimeOffsetArray => GenLiteralArray(LiteralKind.DateTimeOffset),
            LiteralKind.GuidArray => GenLiteralArray(LiteralKind.Guid),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled literal kind."),
        };
    }

    private static Gen<LiteralValue> GenLiteralArray(LiteralKind elementKind)
    {
        return GenLiteralValue(elementKind).Array[0, 4].Select(items => LiteralValue.OfArray(elementKind, items));
    }

    private static Gen<Expression> BuildTermGen(TermSpec spec)
    {
        if (spec.ArgumentKind is not { } kind)
        {
            return Gen.Const((Expression)new TermExpression(new TermIdentity(spec.PredicateName, [])));
        }

        return GenLiteralValue(kind)
            .Select(value =>
            {
                TermIdentity identity = new(
                    spec.PredicateName,
                    [new KeyValuePair<string, LiteralValue>(spec.ArgumentName!, value)]
                );
                return (Expression)new TermExpression(identity);
            });
    }

    /// <summary>
    /// The threshold value range that keeps <c>k</c> compile-valid for a given comparison and operand
    /// count — deliberately mirrors <c>RuleNodeCompiler.ValidThresholdRange</c> (private there), kept
    /// in sync intentionally rather than shared, since this is generator-side test scaffolding rather
    /// than production logic.
    /// </summary>
    private static (int MinK, int MaxK) ValidThresholdRange(ThresholdComparison comparison, int operandCount)
    {
        return comparison switch
        {
            ThresholdComparison.AtLeast => (1, operandCount),
            ThresholdComparison.AtMost => (0, operandCount - 1),
            ThresholdComparison.GreaterThan => (0, operandCount - 1),
            ThresholdComparison.LessThan => (1, operandCount),
            ThresholdComparison.Exactly => (0, operandCount),
            _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unhandled threshold comparison."),
        };
    }

    private static Gen<Expression> BuildThresholdGen(Gen<Expression> operandGen)
    {
        return Gen.Enum<ThresholdComparison>()
            .SelectMany(comparison =>
                Gen.Int[1, 4]
                    .SelectMany(operandCount =>
                    {
                        (int minK, int maxK) = ValidThresholdRange(comparison, operandCount);
                        return Gen.Int[minK, maxK]
                            .Select(
                                operandGen.Array[operandCount],
                                (k, operands) =>
                                    (Expression)new ThresholdExpression(comparison, k, new EquatableArray<Expression>(operands))
                            );
                    })
            );
    }

    private static PredicateRegistry<RuleTestContext> BuildRegistry()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        foreach (TermSpec spec in TermSpecs)
        {
            builder.Add(
                spec.ArgumentKind is { } kind
                    ? new PredicateSchema(
                        spec.PredicateName,
                        spec.PredicateName,
                        $"Property-test predicate '{spec.PredicateName}', argument of kind '{kind}'.",
                        [new PredicateArgumentSchema(spec.ArgumentName!, "Generated argument.", kind)]
                    )
                    : PredicateSchema.NoArguments(
                        spec.PredicateName,
                        spec.PredicateName,
                        $"Property-test predicate '{spec.PredicateName}'."
                    ),
                (_, _, _) => ValueTask.FromResult(true)
            );
        }

        return builder.Build();
    }

    private sealed record TermSpec(string PredicateName, string? ArgumentName, LiteralKind? ArgumentKind);
}
