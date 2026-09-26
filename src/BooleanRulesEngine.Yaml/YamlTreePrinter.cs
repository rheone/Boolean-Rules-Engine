namespace BooleanRulesEngine.Yaml;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Ast;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Prints a compiled expression tree to the identical flat, key-discriminated tree shape JSON uses
/// (ADR-0003), expressed in YAML (ticket 08) — the YAML-direction half of the round-trip guarantee.
/// String-kind literals are always emitted double-quoted so a value that happens to read as
/// <c>true</c>/<c>false</c>/a number (e.g. a role code of <c>"1"</c>) never gets reparsed as a
/// different literal kind.
/// </summary>
internal static class YamlTreePrinter
{
    /// <summary>Prints an expression tree to YAML tree text.</summary>
    /// <param name="root">The tree to print.</param>
    /// <returns>The YAML text.</returns>
    public static string Print(Expression root)
    {
        YamlDocument document = new(ToNode(root));
        YamlStream stream = new(document);
        using StringWriter writer = new();
        stream.Save(writer, assignAnchors: false);
        return writer.ToString();
    }

    private static YamlNode ToNode(Expression node)
    {
        return node switch
        {
            ConstantExpression c => Mapping(("const", Scalar(c.Value ? "true" : "false", ScalarStyle.Plain))),
            TermExpression t => TermToNode(t),
            NotExpression n => OperatorNode("not", [ToNode(n.Operand)]),
            AndExpression a => OperatorNode("and", a.Operands.Select(ToNode)),
            OrExpression o => OperatorNode("or", o.Operands.Select(ToNode)),
            XorExpression x => OperatorNode("xor", [ToNode(x.Left), ToNode(x.Right)]),
            ExactlyOneExpression e => OperatorNode("exactlyOne", e.Operands.Select(ToNode)),
            AtLeastExpression al => OperatorNodeWithThreshold("atLeast", al.K, al.Operands.Select(ToNode)),
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };
    }

    private static YamlScalarNode Scalar(string text, ScalarStyle style)
    {
        return new(text) { Style = style };
    }

    private static YamlMappingNode Mapping(params (string Key, YamlNode Value)[] entries)
    {
        YamlMappingNode mapping = [];
        foreach ((string key, YamlNode value) in entries)
        {
            mapping.Add(new YamlScalarNode(key), value);
        }

        return mapping;
    }

    private static YamlMappingNode OperatorNode(string op, IEnumerable<YamlNode> operands)
    {
        YamlSequenceNode sequence = [.. operands];

        return Mapping(("op", Scalar(op, ScalarStyle.Plain)), ("operands", sequence));
    }

    private static YamlMappingNode OperatorNodeWithThreshold(string op, int k, IEnumerable<YamlNode> operands)
    {
        YamlMappingNode node = OperatorNode(op, operands);
        node.Add(new YamlScalarNode("k"), Scalar(k.ToString(CultureInfo.InvariantCulture), ScalarStyle.Plain));
        return node;
    }

    private static YamlNode TermToNode(TermExpression term)
    {
        YamlMappingNode mapping = new()
        {
            { new YamlScalarNode("predicate"), new YamlScalarNode(term.Identity.PredicateName) },
        };

        if (term.Identity.Arguments.Count > 0)
        {
            YamlMappingNode args = [];
            foreach ((string name, LiteralValue value) in term.Identity.Arguments)
            {
                args.Add(new YamlScalarNode(name), LiteralToNode(value));
            }

            mapping.Add(new YamlScalarNode("args"), args);
        }

        return mapping;
    }

    private static YamlNode LiteralToNode(LiteralValue value)
    {
        return value.Kind switch
        {
            LiteralKind.String => Scalar(value.AsString(), ScalarStyle.DoubleQuoted),
            LiteralKind.Int64 => Scalar(value.AsInt64().ToString(CultureInfo.InvariantCulture), ScalarStyle.Plain),
            LiteralKind.Decimal => Scalar(value.AsDecimal().ToString(CultureInfo.InvariantCulture), ScalarStyle.Plain),
            LiteralKind.Boolean => Scalar(value.AsBoolean() ? "true" : "false", ScalarStyle.Plain),
            LiteralKind.DateTimeOffset => Scalar(
                value.AsDateTimeOffset().ToString("O", CultureInfo.InvariantCulture),
                ScalarStyle.DoubleQuoted
            ),
            _ => ArrayLiteralToNode(value),
        };
    }

    private static YamlNode ArrayLiteralToNode(LiteralValue value)
    {
        YamlSequenceNode sequence = [];
        foreach (LiteralValue item in value.AsArray())
        {
            sequence.Add(LiteralToNode(item));
        }

        return sequence;
    }
}
