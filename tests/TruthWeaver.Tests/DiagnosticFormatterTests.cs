namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>The plain-text rendering of diagnostics and the line/column convenience (k3-conformance 28).</summary>
public sealed class DiagnosticFormatterTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
    );

    /// <summary>Offsets convert to 1-based line and column numbers across line breaks.</summary>
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(4, 1, 5)]
    [InlineData(7, 2, 1)]
    [InlineData(8, 2, 2)]
    [InlineData(13, 3, 1)]
    public void GetLocation_OffsetInMultiLineSource_ReturnsOneBasedLineAndColumn_Test(int start, int line, int column)
    {
        SourceLocation location = new SourceSpan(start, 1).GetLocation("a AND\r\nb OR\r\nc");

        Assert.Equal(new SourceLocation(line, column), location);
    }

    /// <summary>The rendering has the header, the source line, a caret underline and the structured extras.</summary>
    [Fact]
    public void Format_ErrorWithAllMembers_RendersHeaderSourceLineCaretsExpectedFoundAndSuggestion_Test()
    {
        const string source = "a ANDD b";
        Diagnostic diagnostic = Compiler.Compile(source).Diagnostics.Single();

        string text = DiagnosticFormatter.Format(diagnostic, source);

        string expected = string.Join(
            "\n",
            "BRE0001 error at line 1, column 3: " + diagnostic.Message,
            "  a ANDD b",
            "    ^^^^",
            "  Expected: an operator or the end of the rule",
            "  Found: 'ANDD'",
            "  Did you mean: AND"
        );
        Assert.Equal(expected, text);
    }

    /// <summary>A diagnostic on a later line points at that line, not the first.</summary>
    [Fact]
    public void Format_DiagnosticOnSecondLine_ShowsThatLineAndItsColumn_Test()
    {
        const string source = "a AND\n(b XOR b XOR a)";
        Diagnostic diagnostic = Compiler.Compile(source).Diagnostics.Single(d => d.Code == DiagnosticCodes.XorArityViolation);

        string text = DiagnosticFormatter.Format(diagnostic, source);

        Assert.StartsWith("BRE0006 error at line 2, column 2:", text);
        Assert.Contains("\n  (b XOR b XOR a)\n   ^^^^^^^^^^^^^\n", text);
    }

    /// <summary>A hint is rendered as a hint, not as a replacement.</summary>
    [Fact]
    public void Format_HintSuggestion_RendersHintLine_Test()
    {
        const string source = "a XOR b AND a";
        Diagnostic diagnostic = Compiler.Compile(source).Diagnostics.Single();

        string text = DiagnosticFormatter.Format(diagnostic, source);

        Assert.Contains("\n  Hint: ", text);
        Assert.DoesNotContain("Did you mean", text);
    }

    /// <summary>Without source text the rendering degrades to the offset, with no source line.</summary>
    [Fact]
    public void Format_WithoutSource_RendersOffsetAndNoSourceLine_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("a ANDD b").Diagnostics.Single();

        string text = DiagnosticFormatter.Format(diagnostic);

        Assert.StartsWith("BRE0001 error at offset 2:", text);
        Assert.DoesNotContain("^", text);
    }

    /// <summary>A diagnostic without a location, such as an analyzer warning, omits the location.</summary>
    [Fact]
    public void Format_DiagnosticWithoutLocation_OmitsTheLocationClause_Test()
    {
        Diagnostic diagnostic = Diagnostic.Warning("BRE0012", "always true", SourceSpan.None);

        string text = DiagnosticFormatter.Format(diagnostic, "a OR b");

        Assert.Equal("BRE0012 warning: always true", text);
    }

    /// <summary>Several diagnostics render one after another, separated by a line break.</summary>
    [Fact]
    public void Format_SeveralDiagnostics_JoinsThemOnePerBlock_Test()
    {
        const string source = "a & b)";
        IReadOnlyList<Diagnostic> diagnostics = Compiler.Compile(source).Diagnostics;

        string text = DiagnosticFormatter.Format(diagnostics, source);

        Assert.Equal(string.Join("\n", diagnostics.Select(d => DiagnosticFormatter.Format(d, source))), text);
        Assert.True(diagnostics.Count >= 2);
    }

    /// <summary>The compilation result renders its own diagnostics.</summary>
    [Fact]
    public void FormatDiagnostics_FailedCompilation_MatchesTheFormatter_Test()
    {
        const string source = "a ANDD b";
        CompilationResult<RuleTestContext> result = Compiler.Compile(source);

        string text = result.FormatDiagnostics(source);

        Assert.Equal(DiagnosticFormatter.Format(result.Diagnostics, source), text);
    }
}
