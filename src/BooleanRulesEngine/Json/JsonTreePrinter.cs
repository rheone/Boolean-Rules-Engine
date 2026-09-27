namespace BooleanRulesEngine.Json;

using System.Text.Json.Nodes;
using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Ast;

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
        return node switch
        {
            ConstantExpression c => new JsonObject { ["const"] = c.Value },
            TermExpression t => TermToNode(t),
            NotExpression n => new JsonObject { ["op"] = "not", ["operands"] = new JsonArray(ToNode(n.Operand)) },
            AndExpression a => new JsonObject { ["op"] = "and", ["operands"] = OperandsArray(a.Operands) },
            OrExpression o => new JsonObject { ["op"] = "or", ["operands"] = OperandsArray(o.Operands) },
            XorExpression x => new JsonObject { ["op"] = "xor", ["operands"] = new JsonArray(ToNode(x.Left), ToNode(x.Right)) },
            XnorExpression xn => new JsonObject
            {
                ["op"] = "xnor",
                ["operands"] = new JsonArray(ToNode(xn.Left), ToNode(xn.Right)),
            },
            ExactlyOneExpression e => new JsonObject { ["op"] = "exactlyOne", ["operands"] = OperandsArray(e.Operands) },
            ThresholdExpression th => new JsonObject
            {
                ["op"] = ThresholdOpName(th.Comparison),
                ["k"] = th.K,
                ["operands"] = OperandsArray(th.Operands),
            },
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };
    }

    private static string ThresholdOpName(ThresholdComparison comparison)
    {
        return comparison switch
        {
            ThresholdComparison.AtLeast => "atLeast",
            ThresholdComparison.AtMost => "atMost",
            ThresholdComparison.GreaterThan => "greaterThan",
            ThresholdComparison.LessThan => "lessThan",
            ThresholdComparison.Exactly => "exactly",
            _ => throw new InvalidOperationException($"Unhandled threshold comparison '{comparison}'."),
        };
    }

    private static JsonArray OperandsArray(EquatableArray<Expression> operands)
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
