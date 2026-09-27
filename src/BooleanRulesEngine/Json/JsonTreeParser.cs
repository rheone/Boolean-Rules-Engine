namespace BooleanRulesEngine.Json;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using BooleanRulesEngine.Ast;
using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Parsing;

// IDISP004 false-positives on `foreach (var x in jsonElement.EnumerateObject()/.EnumerateArray())`:
// JsonElement's enumerators are disposable structs, but a `foreach` loop already compiles to a
// `using`-equivalent dispose in its generated finally block: there is no undisposed value here.
#pragma warning disable IDISP004

/// <summary>
/// Parses the flat, key-discriminated JSON tree shape (ADR-0003) into the same raw
/// <see cref="RuleNode"/> tree the DSL parser produces, so both front ends funnel through identical
/// validation (ticket 07). A node is discriminated by which key is present: <c>const</c>,
/// <c>predicate</c>, or <c>op</c>. Never throws for malformed input — it reports a
/// <see cref="DiagnosticCodes.MalformedTree"/> diagnostic instead.
/// </summary>
internal static class JsonTreeParser
{
    /// <summary>Parses JSON tree text into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="json">The JSON tree text.</param>
    /// <returns>
    /// The parsed root node (or <see langword="null"/> if the JSON itself was malformed) and the
    /// diagnostics raised while parsing.
    /// </returns>
    public static (RuleNode? Root, IReadOnlyList<Diagnostic> Diagnostics) Parse(
        [StringSyntax(StringSyntaxAttribute.Json)] string json
    )
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            List<Diagnostic> diagnostics =
            [
                Diagnostic.Error(DiagnosticCodes.MalformedTree, $"Malformed JSON: {ex.Message}", SourceSpan.None),
            ];
            return (null, diagnostics);
        }

        using (document)
        {
            return Parse(document.RootElement);
        }
    }

    /// <summary>Parses an already-materialized JSON tree node (e.g. a subtree of a larger document) into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="element">The JSON tree node.</param>
    /// <returns>
    /// The parsed root node (or <see langword="null"/> if the element was malformed) and the
    /// diagnostics raised while parsing.
    /// </returns>
    public static (RuleNode? Root, IReadOnlyList<Diagnostic> Diagnostics) Parse(JsonElement element)
    {
        List<Diagnostic> diagnostics = [];
        RuleNode? root = ParseNode(element, diagnostics);
        return (root, diagnostics);
    }

    private static RuleNode? ParseNode(JsonElement element, List<Diagnostic> diagnostics)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Expected a JSON object node but found {element.ValueKind}.",
                    SourceSpan.None
                )
            );
            return null;
        }

        if (element.TryGetProperty("const", out JsonElement constElement))
        {
            if (constElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                diagnostics.Add(
                    Diagnostic.Error(DiagnosticCodes.MalformedTree, "'const' must be a JSON boolean.", SourceSpan.None)
                );
                return null;
            }

            return new ConstantNode(constElement.GetBoolean(), SourceSpan.None);
        }

        if (element.TryGetProperty("predicate", out JsonElement predicateElement))
        {
            return ParseTerm(element, predicateElement, diagnostics);
        }

        if (element.TryGetProperty("op", out JsonElement opElement) && opElement.ValueKind == JsonValueKind.String)
        {
            return ParseOperator(element, opElement.GetString()!, diagnostics);
        }

        diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.MalformedTree,
                "A tree node must have a 'const', 'predicate', or 'op' key.",
                SourceSpan.None
            )
        );
        return null;
    }

    private static RuleNode? ParseTerm(JsonElement element, JsonElement predicateElement, List<Diagnostic> diagnostics)
    {
        if (predicateElement.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add(
                Diagnostic.Error(DiagnosticCodes.MalformedTree, "'predicate' must be a JSON string.", SourceSpan.None)
            );
            return null;
        }

        List<ArgumentNode> arguments = [];
        if (element.TryGetProperty("args", out JsonElement argsElement))
        {
            if (argsElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(
                    Diagnostic.Error(DiagnosticCodes.MalformedTree, "'args' must be a JSON object.", SourceSpan.None)
                );
                return null;
            }

            foreach (JsonProperty property in argsElement.EnumerateObject())
            {
                RawLiteral? literal = ParseLiteral(property.Value, diagnostics);
                if (literal is null)
                {
                    return null;
                }

                arguments.Add(new ArgumentNode(property.Name, literal, SourceSpan.None));
            }
        }

        return new TermNode(predicateElement.GetString()!, arguments, SourceSpan.None);
    }

    private static RuleNode? ParseOperator(JsonElement element, string op, List<Diagnostic> diagnostics)
    {
        if (
            !element.TryGetProperty("operands", out JsonElement operandsElement)
            || operandsElement.ValueKind != JsonValueKind.Array
        )
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Operator node '{op}' requires an 'operands' array.",
                    SourceSpan.None
                )
            );
            return null;
        }

        List<RuleNode> operands = [];
        foreach (JsonElement operandElement in operandsElement.EnumerateArray())
        {
            RuleNode? operand = ParseNode(operandElement, diagnostics);
            if (operand is null)
            {
                return null;
            }

            operands.Add(operand);
        }

        if (!TreeFormatOpNames.TryFromTreeFormat(op, out string? canonicalOpName))
        {
            diagnostics.Add(Diagnostic.Error(DiagnosticCodes.MalformedTree, $"Unknown operator '{op}'.", SourceSpan.None));
            return null;
        }

        switch (canonicalOpName)
        {
            case "And":
                return new AndNode(operands, SourceSpan.None);
            case "Or":
                return new OrNode(operands, SourceSpan.None);
            case "Not":
                if (operands.Count != 1)
                {
                    diagnostics.Add(
                        Diagnostic.Error(DiagnosticCodes.MalformedTree, "'not' requires exactly one operand.", SourceSpan.None)
                    );
                    return null;
                }

                return new NotNode(operands[0], SourceSpan.None);
            case "Xor":
                return new XorNode(operands, SourceSpan.None);
            case "Xnor":
                return new XnorNode(operands, SourceSpan.None);
            case "ExactlyOne":
                return new ExactlyOneNode(operands, SourceSpan.None);
            case "AtLeast":
                return ParseThreshold(element, op, ThresholdComparison.AtLeast, operands, diagnostics);
            case "AtMost":
                return ParseThreshold(element, op, ThresholdComparison.AtMost, operands, diagnostics);
            case "GreaterThan":
                return ParseThreshold(element, op, ThresholdComparison.GreaterThan, operands, diagnostics);
            case "LessThan":
                return ParseThreshold(element, op, ThresholdComparison.LessThan, operands, diagnostics);
            case "Exactly":
                return ParseThreshold(element, op, ThresholdComparison.Exactly, operands, diagnostics);
            default:
                throw new InvalidOperationException($"Unhandled canonical op-name '{canonicalOpName}'.");
        }
    }

    private static RuleNode? ParseThreshold(
        JsonElement element,
        string op,
        ThresholdComparison comparison,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        if (!element.TryGetProperty("k", out JsonElement kElement) || kElement.ValueKind != JsonValueKind.Number)
        {
            diagnostics.Add(
                Diagnostic.Error(DiagnosticCodes.MalformedTree, $"'{op}' requires a numeric 'k'.", SourceSpan.None)
            );
            return null;
        }

        return new ThresholdNode(comparison, kElement.GetInt32(), operands, SourceSpan.None);
    }

    private static RawLiteral? ParseLiteral(JsonElement element, List<Diagnostic> diagnostics)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return RawLiteral.OfString(element.GetString()!, SourceSpan.None);
            case JsonValueKind.Number:
                return RawLiteral.OfNumber(element.GetRawText(), SourceSpan.None);
            case JsonValueKind.True:
                return RawLiteral.OfBoolean(true, SourceSpan.None);
            case JsonValueKind.False:
                return RawLiteral.OfBoolean(false, SourceSpan.None);
            case JsonValueKind.Array:
                List<RawLiteral> items = [];
                foreach (JsonElement item in element.EnumerateArray())
                {
                    RawLiteral? converted = ParseLiteral(item, diagnostics);
                    if (converted is null)
                    {
                        return null;
                    }

                    items.Add(converted);
                }

                return RawLiteral.OfArray(items, SourceSpan.None);
            default:
                diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MalformedTree,
                        $"Unsupported literal JSON value kind '{element.ValueKind}'.",
                        SourceSpan.None
                    )
                );
                return null;
        }
    }
}
