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

    private static readonly Gen<Expression> GenConstant = Gen.Enum<TruthValue>()
        .Select(value => (Expression)new ConstantExpression(value));

    private static readonly Gen<Expression> GenTerm = Gen.OneOf([.. TermSpecs.Select(BuildTermGen)]);

    private static readonly Gen<Expression> GenLeaf = Gen.OneOf(GenConstant, GenTerm);

    /// <summary>
    /// The full recursive expression generator — every operator (<c>AND</c>/<c>OR</c>/<c>NOT</c>/
    /// <c>XOR</c>/<c>EQUIVALENT</c>/<c>ExactlyOne</c>/the threshold family) plus leaves, arity/threshold-range
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
            Gen<Expression> genXnor = self.Select(self, (left, right) => (Expression)new EquivalentExpression(left, right));
            Gen<Expression> genImplies = self.Select(self, (left, right) => (Expression)new ImpliesExpression(left, right));
            Gen<Expression> genNand = self.Select(self, (left, right) => (Expression)new NandExpression(left, right));
            Gen<Expression> genNor = self.Select(self, (left, right) => (Expression)new NorExpression(left, right));
            Gen<Expression> genNxor = self.Array[2, 4]
                .Select(operands => (Expression)new NxorExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genAny = self.Array[2, 4]
                .Select(operands => (Expression)new AnyExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genAll = self.Array[2, 4]
                .Select(operands => (Expression)new AllExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genNone = self.Array[2, 4]
                .Select(operands => (Expression)new NoneExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genExactlyOne = self.Array[2, 4]
                .Select(operands => (Expression)new ExactlyOneExpression(new EquatableArray<Expression>(operands)));
            Gen<Expression> genThreshold = BuildThresholdGen(self);
            Gen<Expression> genBetween = BuildBetweenGen(self);
            Gen<Expression> genCoalesce = self.Array[2, 4]
                .Select(operands => (Expression)new CoalesceExpression(new EquatableArray<Expression>(operands)));

            Gen<Expression> genInspection = Gen.Enum<InspectionKind>()
                .Select(self, (kind, operand) => (Expression)new InspectionExpression(kind, operand));
            Gen<Expression> genProject = Gen.Bool.Select(
                self,
                (unknownAs, operand) => (Expression)new ProjectExpression(operand, unknownAs)
            );
            Gen<Expression> genIf = self.Array[3]
                .Select(operands => (Expression)new IfExpression(operands[0], operands[1], operands[2]));

            return Gen.Frequency(
                (3, GenLeaf),
                (2, genAnd),
                (2, genOr),
                (2, genNot),
                (1, genXor),
                (1, genXnor),
                (1, genImplies),
                (1, genNand),
                (1, genNor),
                (1, genNxor),
                (1, genAny),
                (1, genAll),
                (1, genNone),
                (1, genExactlyOne),
                (1, genThreshold),
                (1, genBetween),
                (1, genCoalesce),
                (1, genIf),
                (1, genInspection),
                (1, genProject)
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

    /// <summary>
    /// Ticket 21: the depth-cycling rendering, which mixes <c>()</c>, <c>[]</c> and <c>{}</c>, parses back to the same
    /// tree as the parentheses-only form for any generated tree (delimiters never change the compiled tree).
    /// </summary>
    [Fact]
    public void Parsing_the_depth_cycled_form_of_a_generated_tree_reproduces_a_structurally_equal_tree()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());

        GenExpressionTree.Sample(
            tree =>
            {
                string printed = CanonicalPrinter.Print(tree, null, GroupingStyle.DepthCycling);
                CompilationResult<RuleTestContext> result = compiler.Compile(printed);
                string diagnosticMessages = string.Join("; ", result.Diagnostics.Select(d => d.Message));

                Assert.True(result.Succeeded, $"Expected '{printed}' to compile cleanly but got: {diagnosticMessages}");
                Assert.Equal(tree, result.CompiledRule!.Root);
            },
            iter: SampleIterations
        );
    }

    /// <summary>
    /// Ticket 22: padding the printed text of a generated tree with random whitespace (including tabs and newlines) around
    /// its punctuation never changes the compiled tree, and normalising the padded text gives the same text as normalising
    /// the tidy print.
    /// </summary>
    [Fact]
    public void Normalising_a_whitespace_padded_print_of_a_generated_tree_matches_the_tidy_print_and_reparses_equal()
    {
        RuleCompiler<RuleTestContext> compiler = new(BuildRegistry());

        GenExpressionTree
            .Select(Gen.Int, (tree, seed) => (Tree: tree, Seed: seed))
            .Sample(
                sample =>
                {
                    string tidy = CanonicalPrinter.Print(sample.Tree);
                    string padded = PadWhitespace(tidy, new Random(sample.Seed));
                    string normalized = RuleText.NormalizeWhitespace(padded);

                    Assert.Equal(RuleText.NormalizeWhitespace(tidy), normalized);
                    CompilationResult<RuleTestContext> result = compiler.Compile(padded);
                    Assert.True(result.Succeeded, $"Expected padded '{padded}' to compile cleanly.");
                    Assert.Equal(sample.Tree, result.CompiledRule!.Root);
                    Assert.Equal(sample.Tree, compiler.Compile(normalized).CompiledRule!.Root);
                },
                iter: SampleIterations
            );
    }

    /// <summary>
    /// Surrounds every punctuation character outside a string literal, and replaces every space outside one, with a random
    /// run of whitespace. String literal contents are copied untouched because whitespace inside them is data.
    /// </summary>
    private static string PadWhitespace(string text, Random random)
    {
        const string Whitespace = " \t\r\n";
        string RandomRun()
        {
            return new string(
                Enumerable.Range(0, random.Next(0, 4)).Select(_ => Whitespace[random.Next(Whitespace.Length)]).ToArray()
            );
        }

        System.Text.StringBuilder builder = new();
        bool inString = false;
        bool escaped = false;
        foreach (char c in text)
        {
            if (inString)
            {
                builder.Append(c);

                // A backslash escapes the next character, so an escaped quote does not end the literal.
                if (escaped)
                {
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (c == ' ')
            {
                builder.Append(' ').Append(RandomRun());
            }
            else if ("(),:[]".Contains(c))
            {
                builder.Append(RandomRun()).Append(c).Append(RandomRun());
            }
            else
            {
                builder.Append(c);
                inString = c == '"';
            }
        }

        return builder.ToString();
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

    /// <summary>Generates BETWEEN nodes whose bounds satisfy <c>0 &lt;= min &lt;= max &lt;= n</c>, excluding the rejected full range.</summary>
    private static Gen<Expression> BuildBetweenGen(Gen<Expression> operandGen)
    {
        return Gen.Int[2, 4]
            .SelectMany(operandCount =>
                Gen.Int[0, operandCount]
                    .SelectMany(min =>
                        Gen.Int[min, operandCount]
                            .Where(max => !(min == 0 && max == operandCount))
                            .Select(
                                operandGen.Array[operandCount],
                                (max, operands) =>
                                    (Expression)new BetweenExpression(min, max, new EquatableArray<Expression>(operands))
                            )
                    )
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
                (_, _, _) => ValueTask.FromResult(true ? TruthValue.True : TruthValue.False)
            );
        }

        return builder.Build();
    }

    private sealed record TermSpec(string PredicateName, string? ArgumentName, LiteralKind? ArgumentKind);
}
