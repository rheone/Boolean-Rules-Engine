namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// The shared internal seam (<see cref="ExpressionShape.Of"/>) that every consumer walking an
/// <see cref="Expression"/> tree's operator nodes now uses instead of re-deriving op-name/K/operands
/// with its own independent switch.
/// </summary>
public sealed class ExpressionShapeTests
{
    private static readonly TermExpression TermA = new(new TermIdentity("a", []));
    private static readonly TermExpression TermB = new(new TermIdentity("b", []));

    [Fact]
    public void Not_shape_has_a_single_operand_and_no_k()
    {
        NotExpression node = new(TermA);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Not", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA], shape.Operands);
    }

    [Fact]
    public void And_shape_carries_its_operands_in_order()
    {
        AndExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("And", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Or_shape_carries_its_operands_in_order()
    {
        OrExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Or", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Xor_shape_exposes_left_then_right_as_operands()
    {
        XorExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Xor", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Equivalent_shape_exposes_left_then_right_as_operands()
    {
        EquivalentExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Equivalent", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Nand_shape_exposes_left_then_right_as_operands()
    {
        NandExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Nand", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Nor_shape_exposes_left_then_right_as_operands()
    {
        NorExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Nor", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Implies_shape_exposes_antecedent_then_consequent_as_operands()
    {
        ImpliesExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Implies", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void ExactlyOne_shape_carries_its_operands_in_order()
    {
        ExactlyOneExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("ExactlyOne", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Theory]
    [InlineData(ThresholdComparison.AtLeast)]
    [InlineData(ThresholdComparison.AtMost)]
    [InlineData(ThresholdComparison.GreaterThan)]
    [InlineData(ThresholdComparison.LessThan)]
    [InlineData(ThresholdComparison.Exactly)]
    public void Threshold_shape_op_name_matches_its_comparison_and_carries_k(ThresholdComparison comparison)
    {
        ThresholdExpression node = new(comparison, 2, new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal(comparison.ToString(), shape.OpName);
        Assert.Equal(2, shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Constant_nodes_have_no_operand_shape()
    {
        ConstantExpression node = new(TruthValue.True);

        Assert.Throws<ArgumentException>(() => ExpressionShape.Of(node));
    }

    [Fact]
    public void Term_nodes_have_no_operand_shape()
    {
        Assert.Throws<ArgumentException>(() => ExpressionShape.Of(TermA));
    }
}
