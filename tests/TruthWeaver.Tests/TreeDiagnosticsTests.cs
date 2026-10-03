namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Readable diagnostics for malformed JSON and YAML rules (ADR-0005 decision 11, k3-conformance 29): each diagnostic is
/// located by a path from the document root (<c>$.operands[1].op</c>) and carries the same expected-versus-found pair
/// and "did you mean" suggestion as the DSL diagnostics.
/// </summary>
public sealed class TreeDiagnosticsTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("isManager", true)
            .AddStringArgPredicate("hasRole", "role", "Y")
            .Build()
    );

    /// <summary>An unknown JSON op is located at the op's own path and answered with the nearest operator.</summary>
    [Fact]
    public void CompileJson_UnknownOp_ReportsPathExpectedFoundAndSuggestion_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"annd","operands":[{"const":true},{"const":false}]}""");

        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal("$.op", diagnostic.Path);
        Assert.Equal("a known operator", diagnostic.Expected);
        Assert.Equal("'annd'", diagnostic.Found);
        Assert.Equal(new DiagnosticSuggestion(DiagnosticSuggestionKind.Replacement, "and"), diagnostic.Suggestion);
    }

    /// <summary>The path of a nested unknown op names the operand index and the key.</summary>
    [Fact]
    public void CompileJson_UnknownOpInSecondOperand_ReportsTheNestedPath_Test()
    {
        Diagnostic diagnostic = SingleJson(
            """{"op":"and","operands":[{"const":true},{"op":"orr","operands":[{"const":true},{"const":false}]}]}"""
        );

        Assert.Equal("$.operands[1].op", diagnostic.Path);
        Assert.Equal("or", diagnostic.Suggestion?.Text);
    }

    /// <summary>A node with no <c>operands</c> property reports that at the node's path.</summary>
    [Fact]
    public void CompileJson_OperatorWithoutOperands_ReportsExpectedOperandsArray_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"and"}""");

        Assert.Equal("$", diagnostic.Path);
        Assert.Equal("an 'operands' array", diagnostic.Expected);
        Assert.Equal("no 'operands' property", diagnostic.Found);
    }

    /// <summary>A non-array <c>operands</c> is located at that property and names the JSON type found.</summary>
    [Fact]
    public void CompileJson_OperandsNotAnArray_ReportsThePropertyAndTheTypeFound_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"and","operands":5}""");

        Assert.Equal("$.operands", diagnostic.Path);
        Assert.Equal("an 'operands' array", diagnostic.Expected);
        Assert.Equal("a number", diagnostic.Found);
    }

    /// <summary>The wrong operand count is located at the node's operands, with the counts expected and found.</summary>
    [Fact]
    public void CompileJson_XorWithThreeOperands_ReportsPathAndCounts_Test()
    {
        Diagnostic diagnostic = SingleJson(
            """{"op":"and","operands":[{"const":true},{"op":"xor","operands":[{"const":true},{"const":false},{"const":true}]}]}"""
        );

        Assert.Equal(DiagnosticCodes.XorArityViolation, diagnostic.Code);
        Assert.Equal("$.operands[1].operands", diagnostic.Path);
        Assert.Equal("2 operands", diagnostic.Expected);
        Assert.Equal("3 operands", diagnostic.Found);
    }

    /// <summary>A <c>not</c> with two operands reports the operand count at its operands.</summary>
    [Fact]
    public void CompileJson_NotWithTwoOperands_ReportsOperandCounts_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"not","operands":[{"const":true},{"const":false}]}""");

        Assert.Equal("$.operands", diagnostic.Path);
        Assert.Equal("1 operand", diagnostic.Expected);
        Assert.Equal("2 operands", diagnostic.Found);
    }

    /// <summary>A <c>const</c> that is not a boolean or a K3 name is located at the <c>const</c> property.</summary>
    [Fact]
    public void CompileJson_BadConst_ReportsTheConstPath_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"not","operands":[{"const":5}]}""");

        Assert.Equal("$.operands[0].const", diagnostic.Path);
        Assert.Equal("a JSON boolean or one of \"true\", \"false\", \"unknown\"", diagnostic.Expected);
        Assert.Equal("a number", diagnostic.Found);
    }

    /// <summary>A node whose JSON type is not an object is located at the node.</summary>
    [Fact]
    public void CompileJson_OperandOfTheWrongType_ReportsTheOperandPath_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"and","operands":[1,2]}""");

        Assert.Equal("$.operands[0]", diagnostic.Path);
        Assert.Equal("a JSON object", diagnostic.Expected);
        Assert.Equal("a number", diagnostic.Found);
    }

    /// <summary>An <c>op</c> that is not a string is located at the <c>op</c> property.</summary>
    [Fact]
    public void CompileJson_OpNotAString_ReportsTheOpPath_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":5,"operands":[]}""");

        Assert.Equal("$.op", diagnostic.Path);
        Assert.Equal("a JSON string", diagnostic.Expected);
        Assert.Equal("a number", diagnostic.Found);
    }

    /// <summary>An object with none of <c>const</c>, <c>predicate</c> or <c>op</c> says so at the node.</summary>
    [Fact]
    public void CompileJson_NodeWithoutADiscriminatorKey_ReportsTheNodePath_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"and","operands":[{"foo":1},{"const":true}]}""");

        Assert.Equal("$.operands[0]", diagnostic.Path);
        Assert.Equal("a 'const', 'predicate' or 'op' key", diagnostic.Expected);
        Assert.Equal("no such key", diagnostic.Found);
    }

    /// <summary>A non-numeric <c>k</c> is located at <c>k</c>.</summary>
    [Fact]
    public void CompileJson_NonNumericK_ReportsTheKPath_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"atLeast","k":"two","operands":[{"const":true},{"const":false}]}""");

        Assert.Equal("$.k", diagnostic.Path);
        Assert.Equal("an integer", diagnostic.Expected);
        Assert.Equal("a string", diagnostic.Found);
    }

    /// <summary>A fractional <c>k</c> is reported as a diagnostic rather than thrown.</summary>
    [Fact]
    public void CompileJson_FractionalK_ReportsADiagnosticInsteadOfThrowing_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"atLeast","k":1.5,"operands":[{"const":true},{"const":false}]}""");

        Assert.Equal("$.k", diagnostic.Path);
        Assert.Equal("a number", diagnostic.Found);
    }

    /// <summary>A threshold whose <c>k</c> is outside its valid range is located at <c>k</c>.</summary>
    [Fact]
    public void CompileJson_ThresholdKOutOfRange_ReportsTheKPath_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"atLeast","k":5,"operands":[{"const":true},{"const":false}]}""");

        Assert.Equal(DiagnosticCodes.InvalidThresholdValue, diagnostic.Code);
        Assert.Equal("$.k", diagnostic.Path);
    }

    /// <summary>A misspelt <c>Collapse</c> policy is located at <c>policy</c> and answered in the tree spelling.</summary>
    [Fact]
    public void CompileJson_MisspeltCollapsePolicy_SuggestsTheTreeSpelling_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"collapse","policy":"unknownAsFals","operands":[{"const":true}]}""");

        Assert.Equal("$.policy", diagnostic.Path);
        Assert.Equal("unknownAsFalse", diagnostic.Suggestion?.Text);
    }

    /// <summary>A <c>Project</c> whose <c>unknownAs</c> is not a boolean is located at that property.</summary>
    [Fact]
    public void CompileJson_BadUnknownAs_ReportsTheUnknownAsPath_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"project","unknownAs":"unknown","operands":[{"const":true}]}""");

        Assert.Equal("$.unknownAs", diagnostic.Path);
        Assert.Equal("true or false", diagnostic.Expected);
    }

    /// <summary>A <c>between</c> with a non-integer <c>min</c> is located at <c>min</c>; a missing <c>max</c> is reported at the node.</summary>
    [Theory]
    [InlineData("""{"op":"between","min":"x","max":1,"operands":[{"const":true},{"const":false}]}""", "$.min")]
    [InlineData("""{"op":"between","min":0,"operands":[{"const":true},{"const":false}]}""", "$")]
    public void CompileJson_BadBetweenBound_ReportsTheBoundPath_Test(string json, string path)
    {
        Diagnostic diagnostic = SingleJson(json);

        Assert.Equal(path, diagnostic.Path);
        Assert.Equal("an integer", diagnostic.Expected);
    }

    /// <summary>An unknown predicate is located at its <c>predicate</c> property with the nearest registered name.</summary>
    [Fact]
    public void CompileJson_UnknownPredicate_ReportsThePathAndSuggestsARegisteredName_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"op":"not","operands":[{"predicate":"isManger"}]}""");

        Assert.Equal(DiagnosticCodes.UnknownPredicate, diagnostic.Code);
        Assert.Equal("$.operands[0].predicate", diagnostic.Path);
        Assert.Equal("isManager", diagnostic.Suggestion?.Text);
    }

    /// <summary>A JSON predicate name is never answered with a DSL operator word.</summary>
    [Fact]
    public void CompileJson_UnknownPredicateNearAnOperatorWord_DoesNotSuggestTheDslOperator_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"predicate":"ANDD"}""");

        Assert.Null(diagnostic.Suggestion);
    }

    /// <summary>A predicate missing a required argument is reported at the term, naming the argument.</summary>
    [Fact]
    public void CompileJson_MissingPredicateArgument_ReportsTheTermPathAndArgument_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"predicate":"hasRole"}""");

        Assert.Equal(DiagnosticCodes.MissingArgument, diagnostic.Code);
        Assert.Equal("$", diagnostic.Path);
        Assert.Equal("argument 'role'", diagnostic.Expected);
    }

    /// <summary>An undeclared argument name is located at the argument and answered with the declared one.</summary>
    [Fact]
    public void CompileJson_UnknownArgumentName_ReportsTheArgumentPathAndSuggestion_Test()
    {
        Diagnostic diagnostic = Assert.Single(
            Compiler.CompileJson("""{"predicate":"hasRole","args":{"rol":"x"}}""").Diagnostics,
            d => d.Code == DiagnosticCodes.UnknownArgument
        );

        Assert.Equal(DiagnosticCodes.UnknownArgument, diagnostic.Code);
        Assert.Equal("$.args.rol", diagnostic.Path);
        Assert.Equal("role", diagnostic.Suggestion?.Text);
    }

    /// <summary>An argument value of the wrong kind is located at the value.</summary>
    [Fact]
    public void CompileJson_ArgumentOfTheWrongKind_ReportsTheArgumentPath_Test()
    {
        Diagnostic diagnostic = SingleJson("""{"predicate":"hasRole","args":{"role":5}}""");

        Assert.Equal(DiagnosticCodes.ArgumentTypeMismatch, diagnostic.Code);
        Assert.Equal("$.args.role", diagnostic.Path);
    }

    /// <summary>Invalid JSON is located at the nearest valid ancestor, with the parser position as a span.</summary>
    [Fact]
    public void CompileJson_TruncatedJson_ReportsTheNearestAncestorPathAndPosition_Test()
    {
        const string json = """{"op":"and","operands":[{"const":true},""";

        Diagnostic diagnostic = SingleJson(json);

        Assert.Equal("$.operands", diagnostic.Path);
        Assert.Equal("well-formed JSON", diagnostic.Expected);
        Assert.NotEqual(SourceSpan.None, diagnostic.Span);
        Assert.Contains("(line 1, column", DiagnosticFormatter.Format(diagnostic, json));
    }

    /// <summary>Invalid JSON at the root is located at <c>$</c>.</summary>
    [Fact]
    public void CompileJson_NotJsonAtAll_ReportsTheRootPath_Test()
    {
        Diagnostic diagnostic = SingleJson("nope");

        Assert.Equal("$", diagnostic.Path);
    }

    /// <summary>The rendering of a path-located diagnostic uses the path where the DSL style uses line and column.</summary>
    [Fact]
    public void FormatDiagnostics_JsonUnknownOp_RendersThePathExpectedFoundAndSuggestion_Test()
    {
        const string json = """{"op":"annd","operands":[{"const":true},{"const":false}]}""";
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        string text = result.FormatDiagnostics(json);

        string expected = string.Join(
            "\n",
            "BRE0014 error at $.op: Unknown operator 'annd'.",
            "  Expected: a known operator",
            "  Found: 'annd'",
            "  Did you mean: and"
        );
        Assert.Equal(expected, text);
    }

    /// <summary>An unknown YAML op is located by the same path syntax and answered with the nearest operator.</summary>
    [Fact]
    public void CompileYaml_UnknownOp_ReportsPathExpectedFoundAndSuggestion_Test()
    {
        Diagnostic diagnostic = SingleYaml("op: and\noperands:\n  - const: true\n  - op: orr\n    operands: []");

        Assert.Equal("$.operands[1].op", diagnostic.Path);
        Assert.Equal("a known operator", diagnostic.Expected);
        Assert.Equal("'orr'", diagnostic.Found);
        Assert.Equal("or", diagnostic.Suggestion?.Text);
    }

    /// <summary>A YAML diagnostic also carries the span of the offending node, so a line and column can be shown.</summary>
    [Fact]
    public void FormatDiagnostics_YamlUnknownOp_RendersPathLineAndColumnAndSourceLine_Test()
    {
        const string yaml = "op: and\noperands:\n  - const: true\n  - op: orr\n    operands: []";
        CompilationResult<RuleTestContext> result = Compiler.CompileYaml(yaml);

        string text = result.FormatDiagnostics(yaml);

        Assert.StartsWith("BRE0014 error at $.operands[1].op (line 4, column 9): Unknown operator 'orr'.", text);
        Assert.Contains("\n    - op: orr\n          ^^^\n", text);
        Assert.Contains("Did you mean: or", text);
    }

    /// <summary>A YAML node with the wrong operand count is located by path.</summary>
    [Fact]
    public void CompileYaml_XorWithThreeOperands_ReportsPathAndCounts_Test()
    {
        Diagnostic diagnostic = SingleYaml("op: xor\noperands:\n  - const: true\n  - const: false\n  - const: true");

        Assert.Equal(DiagnosticCodes.XorArityViolation, diagnostic.Code);
        Assert.Equal("$.operands", diagnostic.Path);
        Assert.Equal("2 operands", diagnostic.Expected);
        Assert.Equal("3 operands", diagnostic.Found);
    }

    /// <summary>Missing operands in YAML are reported at the node, naming the sequence expected.</summary>
    [Fact]
    public void CompileYaml_OperatorWithoutOperands_ReportsExpectedOperandsSequence_Test()
    {
        Diagnostic diagnostic = SingleYaml("op: and");

        Assert.Equal("$", diagnostic.Path);
        Assert.Equal("an 'operands' sequence", diagnostic.Expected);
        Assert.Equal("no 'operands' key", diagnostic.Found);
    }

    /// <summary>A non-sequence <c>operands</c> in YAML is located at that key and names the node type found.</summary>
    [Fact]
    public void CompileYaml_OperandsNotASequence_ReportsThePropertyAndTheTypeFound_Test()
    {
        Diagnostic diagnostic = SingleYaml("op: and\noperands: nope");

        Assert.Equal("$.operands", diagnostic.Path);
        Assert.Equal("a scalar", diagnostic.Found);
    }

    /// <summary>A YAML node that is not a mapping is located at the node.</summary>
    [Fact]
    public void CompileYaml_OperandOfTheWrongType_ReportsTheOperandPath_Test()
    {
        Diagnostic diagnostic = SingleYaml("op: and\noperands:\n  - just\n  - const: true");

        Assert.Equal("$.operands[0]", diagnostic.Path);
        Assert.Equal("a YAML mapping", diagnostic.Expected);
        Assert.Equal("a scalar", diagnostic.Found);
    }

    /// <summary>A bad YAML <c>const</c>, <c>k</c>, <c>min</c>, <c>policy</c> and <c>unknownAs</c> are each located at their key.</summary>
    [Theory]
    [InlineData("const: maybe", "$.const")]
    [InlineData("op: atLeast\nk: two\noperands:\n  - const: true\n  - const: false", "$.k")]
    [InlineData("op: between\nmin: x\nmax: 1\noperands:\n  - const: true\n  - const: false", "$.min")]
    [InlineData("op: between\nmin: 0\noperands:\n  - const: true\n  - const: false", "$")]
    [InlineData("op: collapse\npolicy: nope\noperands:\n  - const: true", "$.policy")]
    [InlineData("op: project\nunknownAs: unknown\noperands:\n  - const: true", "$.unknownAs")]
    public void CompileYaml_BadFieldValue_ReportsTheFieldPath_Test(string yaml, string path)
    {
        Diagnostic diagnostic = SingleYaml(yaml);

        Assert.Equal(path, diagnostic.Path);
        Assert.NotNull(diagnostic.Expected);
        Assert.NotNull(diagnostic.Found);
    }

    /// <summary>A misspelt YAML <c>Collapse</c> policy is answered in the tree spelling.</summary>
    [Fact]
    public void CompileYaml_MisspeltCollapsePolicy_SuggestsTheTreeSpelling_Test()
    {
        Diagnostic diagnostic = SingleYaml("op: collapse\npolicy: unknownAsFals\noperands:\n  - const: true");

        Assert.Equal("unknownAsFalse", diagnostic.Suggestion?.Text);
    }

    /// <summary>An unknown YAML predicate and its arguments are located by path, with suggestions.</summary>
    [Fact]
    public void CompileYaml_UnknownPredicateAndArgument_AreLocatedByPathWithSuggestions_Test()
    {
        Diagnostic predicate = SingleYaml("predicate: isManger");
        Diagnostic argument = Assert.Single(
            Compiler.CompileYaml("predicate: hasRole\nargs:\n  rol: x").Diagnostics,
            d => d.Code == DiagnosticCodes.UnknownArgument
        );

        Assert.Equal("$.predicate", predicate.Path);
        Assert.Equal("isManager", predicate.Suggestion?.Text);
        Assert.Equal("$.args.rol", argument.Path);
        Assert.Equal("role", argument.Suggestion?.Text);
    }

    /// <summary>A YAML predicate with a missing argument is reported at the term.</summary>
    [Fact]
    public void CompileYaml_MissingPredicateArgument_ReportsTheTermPath_Test()
    {
        Diagnostic diagnostic = SingleYaml("predicate: hasRole");

        Assert.Equal("$", diagnostic.Path);
        Assert.Equal("argument 'role'", diagnostic.Expected);
    }

    /// <summary>Invalid YAML syntax is located at the nearest valid ancestor with the parser position.</summary>
    [Fact]
    public void CompileYaml_InvalidSyntax_ReportsTheNearestAncestorPathAndPosition_Test()
    {
        const string yaml = "op: and\noperands: [ {const: true}, {const: ";

        Diagnostic diagnostic = SingleYaml(yaml);

        Assert.Equal("$.operands[1]", diagnostic.Path);
        Assert.Equal("well-formed YAML", diagnostic.Expected);
        Assert.NotEqual(SourceSpan.None, diagnostic.Span);
    }

    /// <summary>An empty YAML document is reported at the root.</summary>
    [Fact]
    public void CompileYaml_EmptyDocument_ReportsTheRootPath_Test()
    {
        Diagnostic diagnostic = SingleYaml(string.Empty);

        Assert.Equal("$", diagnostic.Path);
        Assert.Equal("a YAML mapping", diagnostic.Expected);
    }

    /// <summary>DSL diagnostics have no path.</summary>
    [Fact]
    public void Compile_DslDiagnostic_HasNoPath_Test()
    {
        Diagnostic diagnostic = Assert.Single(Compiler.Compile("a ANDD b").Diagnostics);

        Assert.Null(diagnostic.Path);
    }

    private static Diagnostic SingleJson(string json)
    {
        return Assert.Single(Compiler.CompileJson(json).Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    private static Diagnostic SingleYaml(string yaml)
    {
        return Assert.Single(Compiler.CompileYaml(yaml).Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }
}
