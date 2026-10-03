namespace TruthWeaver.Tests;

using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// JSON and YAML share one op-name lookup (<see cref="TreeFormatOpNames"/>), so this asserts the two
/// formats accept and produce the identical set of op-name strings for every operator rather than each
/// format drifting on its own copy of the table.
/// </summary>
public sealed class TreeFormatOpNameParityTests
{
    [Theory]
    [InlineData("NOT a", "not")]
    [InlineData("a AND b", "and")]
    [InlineData("a OR b", "or")]
    [InlineData("a XOR b", "xor")]
    [InlineData("a EQUIVALENT b", "equivalent")]
    [InlineData("a IMPLIES b", "implies")]
    [InlineData("a NAND b", "nand")]
    [InlineData("a NOR b", "nor")]
    [InlineData("NXOR(a, b)", "nxor")]
    [InlineData("ANY(a, b)", "any")]
    [InlineData("ALL(a, b)", "all")]
    [InlineData("NONE(a, b)", "none")]
    [InlineData("BETWEEN(0, 1, a, b)", "between")]
    [InlineData("COALESCE(a, b)", "coalesce")]
    [InlineData("a ?? b", "coalesce")]
    [InlineData("If(a, b, a)", "if")]
    [InlineData("a ? b : a", "if")]
    [InlineData("IsTrue(a)", "isTrue")]
    [InlineData("IsFalse(a)", "isFalse")]
    [InlineData("IsUnknown(a)", "isUnknown")]
    [InlineData("IsKnown(a)", "isKnown")]
    [InlineData("ExactlyOne(a, b)", "exactlyOne")]
    [InlineData("AtLeast(1, a, b)", "atLeast")]
    [InlineData("AtMost(1, a, b)", "atMost")]
    [InlineData("GreaterThan(1, a, b)", "greaterThan")]
    [InlineData("LessThan(1, a, b)", "lessThan")]
    [InlineData("Exactly(1, a, b)", "exactly")]
    public void Json_and_yaml_print_the_identical_op_name_string_for_every_operator(string dsl, string expectedOpName)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> rule = compiler.Compile(dsl).CompiledRule!;

        string json = rule.PrintJson();
        string yaml = rule.PrintYaml();

        Assert.Contains(
            $"\"op\":\"{expectedOpName}\"",
            json.Replace(" ", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal
        );
        Assert.Contains($"op: {expectedOpName}", yaml, StringComparison.Ordinal);
        Assert.Equal(expectedOpName, TreeFormatOpNames.ToTreeFormat(ExpressionShape.Of(rule.Root).OpName));
    }

    [Theory]
    [InlineData("not")]
    [InlineData("and")]
    [InlineData("or")]
    [InlineData("xor")]
    [InlineData("equivalent")]
    [InlineData("xnor")]
    [InlineData("iff")]
    [InlineData("implies")]
    [InlineData("nand")]
    [InlineData("nor")]
    [InlineData("nxor")]
    [InlineData("any")]
    [InlineData("all")]
    [InlineData("none")]
    [InlineData("between")]
    [InlineData("coalesce")]
    [InlineData("if")]
    [InlineData("isTrue")]
    [InlineData("isFalse")]
    [InlineData("isUnknown")]
    [InlineData("isKnown")]
    [InlineData("exactlyOne")]
    [InlineData("atLeast")]
    [InlineData("atMost")]
    [InlineData("greaterThan")]
    [InlineData("lessThan")]
    [InlineData("exactly")]
    public void Json_and_yaml_both_accept_the_identical_set_of_op_name_strings(string opName)
    {
        Assert.True(TreeFormatOpNames.TryFromTreeFormat(opName, out _));
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build());
    }
}
