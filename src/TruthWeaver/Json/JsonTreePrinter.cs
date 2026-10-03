namespace TruthWeaver.Json;

using System.Text.Json.Nodes;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Prints a compiled expression tree to the flat, key-discriminated JSON tree shape (ADR-0003) —
/// the JSON-direction half of ticket 07's round-trip guarantee.
/// </summary>
internal static class JsonTreePrinter
{
    /// <summary>Prints an expression tree to JSON tree text.</summary>
    /// <param name="root">The tree to print.</param>
    /// <returns>The JSON text.</returns>
    public static string Print(Expression root)
    {
        return ToNode(root).ToJsonString();
    }

    private static JsonNode ToNode(Expression node)
    {
        if (node is ConstantExpression c)
        {
            // True/False stay plain JSON booleans (compatible with existing documents); Unknown has no JSON
            // literal, so it is written as the string "unknown".
            return c.Value switch
            {
                TruthValue.True => new JsonObject { ["const"] = true },
                TruthValue.False => new JsonObject { ["const"] = false },
                _ => new JsonObject { ["const"] = TruthValueText.TreeFormat(c.Value) },
            };
        }

        if (node is TermExpression t)
        {
            return TermToNode(t);
        }

        NodeShape shape = ExpressionShape.Of(node);
        JsonObject obj = new()
        {
            ["op"] = TreeFormatOpNames.ToTreeFormat(shape.OpName),
            ["operands"] = OperandsArray(shape.Operands),
        };
        if (shape.UnknownAs is { } unknownAs)
        {
            // Project carries its policy as a plain JSON boolean, like a True/False const.
            obj["unknownAs"] = unknownAs;
        }
        else if (shape.Max is { } max)
        {
            // BETWEEN carries its two bounds as min/max instead of a single threshold k.
            obj["min"] = shape.K;
            obj["max"] = max;
        }
        else if (shape.K is { } k)
        {
            obj["k"] = k;
        }

        return obj;
    }

    private static JsonArray OperandsArray(IReadOnlyList<Expression> operands)
    {
        JsonArray array = [];
        foreach (Expression operand in operands)
        {
            array.Add(ToNode(operand));
        }

        return array;
    }

    private static JsonNode TermToNode(TermExpression term)
    {
        JsonObject obj = new() { ["predicate"] = term.Identity.PredicateName };
        if (term.Identity.Arguments.Count > 0)
        {
            JsonObject args = [];
            foreach ((string name, LiteralValue value) in term.Identity.Arguments)
            {
                args[name] = LiteralToNode(value);
            }

            obj["args"] = args;
        }

        return obj;
    }

    private static JsonNode? LiteralToNode(LiteralValue value)
    {
        return value.Kind switch
        {
            LiteralKind.String => JsonValue.Create(value.AsString()),
            LiteralKind.Int64 => JsonValue.Create(value.AsInt64()),
            LiteralKind.Decimal => JsonValue.Create(value.AsDecimal()),
            LiteralKind.Boolean => JsonValue.Create(value.AsBoolean()),
            LiteralKind.Guid => JsonValue.Create(value.AsGuid().ToString()),
            LiteralKind.DateTimeOffset => JsonValue.Create(
                value.AsDateTimeOffset().ToString("O", CultureInfo.InvariantCulture)
            ),
            _ => ArrayLiteralToNode(value),
        };
    }

    private static JsonNode ArrayLiteralToNode(LiteralValue value)
    {
        JsonArray array = [];
        foreach (LiteralValue item in value.AsArray())
        {
            array.Add(LiteralToNode(item));
        }

        return array;
    }
}
