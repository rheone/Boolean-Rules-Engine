namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Constants are <see cref="TruthValue"/> end to end (ADR-0005 decision 11): <c>Unknown</c> is a literal in the DSL,
/// JSON, YAML and the builder; <c>True</c>/<c>False</c>/<c>Unknown</c> and operator names are case-insensitive; the
/// canonical printer is upper camel / upper case.
/// </summary>
public sealed class TruthValueConstantsTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>.CreateBuilder().Build()
    );

    /// <summary>An Unknown literal evaluates to Unknown, in any letter case, and is not a fault.</summary>
    [Theory]
    [InlineData("Unknown")]
    [InlineData("UNKNOWN")]
    [InlineData("unknown")]
    [InlineData("uNkNoWn")]
    public async Task Evaluate_UnknownLiteralInAnyCase_YieldsUnknownWithoutFault_Test(string text)
    {
        CompiledRule<RuleTestContext> rule = Compiler.Compile(text).CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>An Unknown literal combined with a predicate matches the K3 oracle over every input.</summary>
    [Fact]
    public async Task Evaluate_UnknownLiteralAndPredicate_MatchesOracleOverAllInputs_Test()
    {
        K3Rule rule = K3Rule.TryCreate("Unknown AND a", 1)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(1))
        {
            Decision decision = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.And([TruthValue.Unknown, assignment[0]]), decision.Result);
        }
    }

    /// <summary>An Unknown literal or-ed with a predicate matches the K3 oracle over every input.</summary>
    [Fact]
    public async Task Evaluate_UnknownLiteralOrPredicate_MatchesOracleOverAllInputs_Test()
    {
        K3Rule rule = K3Rule.TryCreate("a OR UNKNOWN", 1)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(1))
        {
            Decision decision = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Or([assignment[0], TruthValue.Unknown]), decision.Result);
        }
    }

    /// <summary>Mixed-case constants and operator names compile to the same tree as the canonical spelling.</summary>
    [Theory]
    [InlineData("tRuE aNd nOt FaLsE oR uNkNoWn", "True AND NOT False OR Unknown")]
    [InlineData("exactlyone(true, unknown, false)", "ExactlyOne(True, Unknown, False)")]
    [InlineData("ATLEAST(1, true, unknown)", "AtLeast(1, True, Unknown)")]
    [InlineData("(true xor unknown)", "(True XOR Unknown)")]
    [InlineData("(true Xnor unknown)", "(True XNOR Unknown)")]
    public void Compile_MixedCaseConstantsAndOperators_ProducesSameCanonicalTextAsCanonicalSpelling_Test(
        string mixed,
        string canonical
    )
    {
        string fromMixed = Compiler.Compile(mixed).CompiledRule!.CanonicalText;
        string fromCanonical = Compiler.Compile(canonical).CompiledRule!.CanonicalText;

        Assert.Equal(fromCanonical, fromMixed);
    }

    /// <summary>The canonical printer prints constants in upper camel case and operators in upper case.</summary>
    [Theory]
    [InlineData("true", "True")]
    [InlineData("false", "False")]
    [InlineData("unknown", "Unknown")]
    [InlineData("not unknown", "NOT Unknown")]
    [InlineData("true and unknown", "True AND Unknown")]
    [InlineData("exactlyone(true, unknown, false)", "ExactlyOne(True, Unknown, False)")]
    public void CanonicalText_Constants_AreUpperCamelAndOperatorsUpperCase_Test(string source, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compiler.Compile(source).CompiledRule!;

        Assert.Equal(expected, rule.CanonicalText);
    }

    /// <summary>An Unknown literal survives DSL print and re-parse.</summary>
    [Fact]
    public void CanonicalText_UnknownLiteral_RoundTripsThroughDsl_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("NOT (Unknown OR False)").CompiledRule!;

        CompiledRule<RuleTestContext> reparsed = Compiler.Compile(original.CanonicalText).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>An Unknown literal prints to JSON and re-parses to the same rule.</summary>
    [Fact]
    public void PrintJson_UnknownLiteral_RoundTripsThroughJson_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("Unknown AND True").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileJson(json).CompiledRule!;

        Assert.Contains("\"const\":\"unknown\"", json, StringComparison.Ordinal);
        Assert.Contains("\"const\":true", json, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>JSON accepts a string constant in any letter case, alongside the original boolean form.</summary>
    [Theory]
    [InlineData("""{"const": "unknown"}""", "Unknown")]
    [InlineData("""{"const": "UNKNOWN"}""", "Unknown")]
    [InlineData("""{"const": "True"}""", "True")]
    [InlineData("""{"const": "FALSE"}""", "False")]
    [InlineData("""{"const": true}""", "True")]
    [InlineData("""{"const": false}""", "False")]
    public void CompileJson_ConstantSpellings_CompileToTheSameValue_Test(string json, string expectedCanonical)
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.Equal(expectedCanonical, result.CompiledRule!.CanonicalText);
    }

    /// <summary>JSON operator names are case-insensitive.</summary>
    [Fact]
    public void CompileJson_MixedCaseOperatorNames_CompileToTheSameTreeAsLowerCamel_Test()
    {
        string mixed =
            """{"op": "AND", "operands": [{"const": "Unknown"}, {"op": "ExactlyOne", "operands": [{"const": true}, {"const": false}]}]}""";
        string lower =
            """{"op": "and", "operands": [{"const": "unknown"}, {"op": "exactlyOne", "operands": [{"const": true}, {"const": false}]}]}""";

        Assert.Equal(
            Compiler.CompileJson(lower).CompiledRule!.CanonicalText,
            Compiler.CompileJson(mixed).CompiledRule!.CanonicalText
        );
    }

    /// <summary>A JSON constant that is neither a boolean nor a K3 word is rejected with a malformed-tree diagnostic.</summary>
    [Fact]
    public void CompileJson_ConstantOfUnrecognisedWord_ReportsMalformedTree_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson("""{"const": "maybe"}""");

        Assert.Null(result.CompiledRule);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>An Unknown literal prints to YAML and re-parses to the same rule.</summary>
    [Fact]
    public void PrintYaml_UnknownLiteral_RoundTripsThroughYaml_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("Unknown OR False").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains("const: unknown", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>YAML accepts constants and operator names in any letter case.</summary>
    [Theory]
    [InlineData("const: UNKNOWN", "Unknown")]
    [InlineData("const: Unknown", "Unknown")]
    [InlineData("const: TRUE", "True")]
    [InlineData("const: False", "False")]
    public void CompileYaml_ConstantInAnyCase_CompilesToTheSameValue_Test(string yaml, string expectedCanonical)
    {
        Assert.Equal(expectedCanonical, Compiler.CompileYaml(yaml).CompiledRule!.CanonicalText);
    }

    /// <summary>The builder can create an Unknown constant, which prints and evaluates as Unknown.</summary>
    [Fact]
    public async Task Constant_UnknownBuilder_CompilesPrintsAndEvaluatesAsUnknown_Test()
    {
        CompiledRule<RuleTestContext> rule = RuleBuilder.Constant(TruthValue.Unknown).Compile(Compiler).CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal("Unknown", rule.CanonicalText);
        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>The boolean builder overload still produces the matching K3 constant.</summary>
    [Theory]
    [InlineData(true, "True")]
    [InlineData(false, "False")]
    public void Constant_BooleanBuilder_StillProducesTheMatchingConstant_Test(bool value, string expected)
    {
        CompiledRule<RuleTestContext> rule = RuleBuilder.Constant(value).Compile(Compiler).CompiledRule!;

        Assert.Equal(expected, rule.CanonicalText);
    }

    /// <summary>The rule description labels an Unknown literal as "Unknown".</summary>
    [Fact]
    public void Describe_UnknownLiteral_HasUnknownLabelAndNoOperands_Test()
    {
        RuleDescription description = Compiler.Compile("Unknown").CompiledRule!.Describe();

        Assert.Equal("Unknown", description.Label);
        Assert.False(string.IsNullOrWhiteSpace(description.Description));
        Assert.Empty(description.Operands);
    }

    /// <summary>The evaluated tree labels constants in the canonical spelling and records the Unknown value.</summary>
    [Fact]
    public async Task EvaluateAsync_UnknownLiteral_RecordsCanonicalConstantInEvaluatedTree_Test()
    {
        CompiledRule<RuleTestContext> rule = Compiler.Compile("Unknown").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal("Unknown", decision.EvaluatedTree!.NodeDescription);
        Assert.Equal(TruthValue.Unknown, decision.EvaluatedTree.Result);
    }

    /// <summary>The analyzer does not claim a contradiction for <c>Unknown AND NOT Unknown</c>, which is Unknown in K3.</summary>
    [Fact]
    public void Compile_UnknownAndNotUnknown_RaisesNoStructuralContradiction_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("Unknown AND NOT Unknown");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralContradiction);
    }

    /// <summary>Unknown is a reserved word in any letter case, so it cannot be used as a predicate name.</summary>
    [Theory]
    [InlineData("Unknown")]
    [InlineData("UNKNOWN")]
    [InlineData("unknown")]
    public void IsReservedWord_UnknownInAnyCase_ReturnsTrue_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }
}
