namespace TruthWeaver.Json;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

// IDISP004 false-positives on `foreach (var x in jsonElement.EnumerateObject()/.EnumerateArray())`:
// JsonElement's enumerators are disposable structs, but a `foreach` loop already compiles to a
// `using`-equivalent dispose in its generated finally block: there is no undisposed value here.
#pragma warning disable IDISP004

/// <summary>
/// Parses the flat, key-discriminated JSON tree shape (ADR-0003) into the same raw
/// <see cref="RuleNode"/> tree the DSL parser produces, so both front ends funnel through identical
/// validation (ticket 07). A node is discriminated by which key is present: <c>const</c>,
/// <c>predicate</c>, or <c>op</c>. Never throws for malformed input — it reports a
/// <see cref="DiagnosticCodes.MalformedTree"/> diagnostic instead. Every diagnostic is located by its path from the
/// document root (<c>$.operands[1].op</c>), because a <see cref="JsonElement"/> does not keep source positions; only
/// invalid JSON syntax also carries the parser's position.
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
            List<Diagnostic> diagnostics = [SyntaxError(json, ex)];
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
        RuleNode? root = ParseNode(element, TreePath.Root, diagnostics);
        return (root, diagnostics);
    }

    /// <summary>Names a JSON value's type for a diagnostic's <c>Found</c>: <c>a string</c>, <c>a number</c>, <c>an array</c>.</summary>
    private static string Describe(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "a string",
            JsonValueKind.Number => "a number",
            JsonValueKind.True or JsonValueKind.False => "a boolean",
            JsonValueKind.Array => "an array",
            JsonValueKind.Object => "an object",
            _ => "null",
        };
    }

    /// <summary>Like <see cref="Describe"/>, but quotes a string's value, for fields where the value is what is wrong.</summary>
    private static string DescribeValue(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String ? $"\"{element.GetString()}\"" : Describe(element);
    }

    private static string FoundOperands(int count)
    {
        return count == 1 ? "1 operand" : $"{count} operands";
    }

    /// <summary>Builds the diagnostic for text that is not JSON at all: the reader's message, its position, and the nearest valid ancestor.</summary>
    private static Diagnostic SyntaxError(string json, JsonException ex)
    {
        string reason = ex.Message;
        int detail = reason.IndexOf(" Path:", StringComparison.Ordinal);
        if (detail >= 0)
        {
            reason = reason[..detail];
        }

        SourceSpan span = ex is { LineNumber: { } line, BytePositionInLine: { } column }
            ? new SourceSpan(OffsetOf(json, line, column), 1)
            : SourceSpan.None;
        return Diagnostic.Error(
            DiagnosticCodes.MalformedTree,
            $"Malformed JSON: {ex.Message}",
            span,
            expected: "well-formed JSON",
            found: reason,
            path: ContainerPathAt(json)
        );
    }

    // JsonException reports a 0-based line and a byte (not character) offset within it; convert to a character offset.
    private static int OffsetOf(string json, long line, long bytePosition)
    {
        int lineStart = 0;
        for (long current = 0; current < line; current++)
        {
            int next = json.IndexOf('\n', lineStart);
            if (next < 0)
            {
                return json.Length;
            }

            lineStart = next + 1;
        }

        int offset = lineStart;
        long bytes = 0;
        while (offset < json.Length && bytes < bytePosition && json[offset] != '\n')
        {
            bytes += Encoding.UTF8.GetByteCount(json.AsSpan(offset, char.IsHighSurrogate(json[offset]) ? 2 : 1));
            offset += char.IsHighSurrogate(json[offset]) ? 2 : 1;
        }

        return Math.Min(offset, json.Length);
    }

    /// <summary>Reads the text token by token until the reader gives up, and returns the path of the innermost container still open.</summary>
    private static string ContainerPathAt(string json)
    {
        TreePathTracker tracker = new();
        Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(json));
        try
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        tracker.Enter(isArray: false);
                        break;
                    case JsonTokenType.StartArray:
                        tracker.Enter(isArray: true);
                        break;
                    case JsonTokenType.EndObject or JsonTokenType.EndArray:
                        tracker.Exit();
                        break;
                    case JsonTokenType.PropertyName:
                        tracker.Key(reader.GetString() ?? string.Empty);
                        break;
                    case JsonTokenType.Comment or JsonTokenType.None:
                        break;
                    default:
                        tracker.Scalar();
                        break;
                }
            }
        }
        catch (JsonException)
        {
            // The reader stopping is the point: the tracker now holds the last good position.
        }

        return tracker.ContainerPath;
    }

    private static RuleNode? ParseNode(JsonElement element, string path, List<Diagnostic> diagnostics)
    {
        RuleNode? node = ParseNodeCore(element, path, diagnostics);
        return node is null ? null : node with { Path = path };
    }

    private static RuleNode? ParseNodeCore(JsonElement element, string path, List<Diagnostic> diagnostics)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Expected a JSON object node but found {element.ValueKind}.",
                    SourceSpan.None,
                    expected: "a JSON object",
                    found: Describe(element),
                    path: path
                )
            );
            return null;
        }

        if (element.TryGetProperty("const", out JsonElement constElement))
        {
            // A constant is a JSON boolean (the original True/False form) or a string naming a K3 value, so
            // Unknown (which JSON has no literal for) can be written as "unknown" in any letter case.
            if (constElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return new ConstantNode(constElement.GetBoolean() ? TruthValue.True : TruthValue.False, SourceSpan.None);
            }

            if (
                constElement.ValueKind == JsonValueKind.String
                && TruthValueText.TryParse(constElement.GetString(), out TruthValue constValue)
            )
            {
                return new ConstantNode(constValue, SourceSpan.None);
            }

            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    "'const' must be a JSON boolean or one of \"true\", \"false\", \"unknown\".",
                    SourceSpan.None,
                    expected: "a JSON boolean or one of \"true\", \"false\", \"unknown\"",
                    found: Describe(constElement),
                    path: TreePath.Property(path, "const")
                )
            );
            return null;
        }

        if (element.TryGetProperty("predicate", out JsonElement predicateElement))
        {
            return ParseTerm(element, predicateElement, path, diagnostics);
        }

        if (element.TryGetProperty("op", out JsonElement opElement))
        {
            if (opElement.ValueKind == JsonValueKind.String)
            {
                return ParseOperator(element, opElement.GetString()!, path, diagnostics);
            }

            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    "'op' must be a JSON string naming an operator.",
                    SourceSpan.None,
                    expected: "a JSON string",
                    found: Describe(opElement),
                    path: TreePath.Property(path, "op")
                )
            );
            return null;
        }

        diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.MalformedTree,
                "A tree node must have a 'const', 'predicate', or 'op' key.",
                SourceSpan.None,
                expected: "a 'const', 'predicate' or 'op' key",
                found: "no such key",
                path: path
            )
        );
        return null;
    }

    private static RuleNode? ParseTerm(
        JsonElement element,
        JsonElement predicateElement,
        string path,
        List<Diagnostic> diagnostics
    )
    {
        if (predicateElement.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    "'predicate' must be a JSON string.",
                    SourceSpan.None,
                    expected: "a JSON string",
                    found: Describe(predicateElement),
                    path: TreePath.Property(path, "predicate")
                )
            );
            return null;
        }

        List<ArgumentNode> arguments = [];
        if (element.TryGetProperty("args", out JsonElement argsElement))
        {
            string argsPath = TreePath.Property(path, "args");
            if (argsElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MalformedTree,
                        "'args' must be a JSON object.",
                        SourceSpan.None,
                        expected: "a JSON object",
                        found: Describe(argsElement),
                        path: argsPath
                    )
                );
                return null;
            }

            foreach (JsonProperty property in argsElement.EnumerateObject())
            {
                string argumentPath = TreePath.Property(argsPath, property.Name);
                RawLiteral? literal = ParseLiteral(property.Value, argumentPath, diagnostics);
                if (literal is null)
                {
                    return null;
                }

                arguments.Add(new ArgumentNode(property.Name, literal, SourceSpan.None) { Path = argumentPath });
            }
        }

        return new TermNode(predicateElement.GetString()!, arguments, SourceSpan.None);
    }

    private static RuleNode? ParseOperator(JsonElement element, string op, string path, List<Diagnostic> diagnostics)
    {
        // The operator name is checked before its operands so a typo is reported on its own, with its suggestion.
        if (!TreeFormatOpNames.TryFromTreeFormat(op, out string? canonicalOpName))
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Unknown operator '{op}'.",
                    SourceSpan.None,
                    expected: "a known operator",
                    found: $"'{op}'",
                    suggestion: NameSuggester.Suggest(op, TreeFormatOpNames.ReadableNames),
                    path: TreePath.Property(path, "op")
                )
            );
            return null;
        }

        string operandsPath = TreePath.Property(path, "operands");
        if (!element.TryGetProperty("operands", out JsonElement operandsElement))
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Operator node '{op}' requires an 'operands' array.",
                    SourceSpan.None,
                    expected: "an 'operands' array",
                    found: "no 'operands' property",
                    path: path
                )
            );
            return null;
        }

        if (operandsElement.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Operator node '{op}' requires an 'operands' array.",
                    SourceSpan.None,
                    expected: "an 'operands' array",
                    found: Describe(operandsElement),
                    path: operandsPath
                )
            );
            return null;
        }

        List<RuleNode> operands = [];
        int index = 0;
        foreach (JsonElement operandElement in operandsElement.EnumerateArray())
        {
            RuleNode? operand = ParseNode(operandElement, TreePath.Index(operandsPath, index++), diagnostics);
            if (operand is null)
            {
                return null;
            }

            operands.Add(operand);
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
                        Diagnostic.Error(
                            DiagnosticCodes.MalformedTree,
                            "'not' requires exactly one operand.",
                            SourceSpan.None,
                            expected: "1 operand",
                            found: FoundOperands(operands.Count),
                            path: operandsPath
                        )
                    );
                    return null;
                }

                return new NotNode(operands[0], SourceSpan.None);
            case "Xor":
                return new XorNode(operands, SourceSpan.None);
            case "Equivalent":
                return new EquivalentNode(operands, SourceSpan.None);
            case "Implies":
                return new ImpliesNode(operands, SourceSpan.None);
            case "Nand":
                return new NandNode(operands, SourceSpan.None);
            case "Nor":
                return new NorNode(operands, SourceSpan.None);
            case "Nxor":
                return new NxorNode(operands, SourceSpan.None);
            case "Any":
                return new AnyNode(operands, SourceSpan.None);
            case "All":
                return new AllNode(operands, SourceSpan.None);
            case "None":
                return new NoneNode(operands, SourceSpan.None);
            case "Coalesce":
                return new CoalesceNode(operands, SourceSpan.None);
            case "If":
                return new IfNode(operands, SourceSpan.None);
            case "IsTrue":
                return new InspectionNode(InspectionKind.IsTrue, operands, SourceSpan.None);
            case "IsFalse":
                return new InspectionNode(InspectionKind.IsFalse, operands, SourceSpan.None);
            case "IsUnknown":
                return new InspectionNode(InspectionKind.IsUnknown, operands, SourceSpan.None);
            case "IsKnown":
                return new InspectionNode(InspectionKind.IsKnown, operands, SourceSpan.None);
            case "Project":
                return ParseProject(element, op, path, operands, diagnostics);
            case "Collapse":
                return ParseCollapse(element, op, path, operands, diagnostics);
            case "ExactlyOne":
                return new ExactlyOneNode(operands, SourceSpan.None);
            case "AtLeast":
                return ParseThreshold(element, op, path, ThresholdComparison.AtLeast, operands, diagnostics);
            case "AtMost":
                return ParseThreshold(element, op, path, ThresholdComparison.AtMost, operands, diagnostics);
            case "GreaterThan":
                return ParseThreshold(element, op, path, ThresholdComparison.GreaterThan, operands, diagnostics);
            case "LessThan":
                return ParseThreshold(element, op, path, ThresholdComparison.LessThan, operands, diagnostics);
            case "Exactly":
                return ParseThreshold(element, op, path, ThresholdComparison.Exactly, operands, diagnostics);
            case "Between":
                return ParseBetween(element, op, path, operands, diagnostics);
            default:
                throw new InvalidOperationException($"Unhandled canonical op-name '{canonicalOpName}'.");
        }
    }

    private static RuleNode? ParseThreshold(
        JsonElement element,
        string op,
        string path,
        ThresholdComparison comparison,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        // TryGetInt32 (not GetInt32) so a fractional or oversized k is a diagnostic instead of an exception.
        bool present = element.TryGetProperty("k", out JsonElement kElement);
        if (!present || kElement.ValueKind != JsonValueKind.Number || !kElement.TryGetInt32(out int k))
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"'{op}' requires a numeric 'k'.",
                    SourceSpan.None,
                    expected: "an integer",
                    found: present ? Describe(kElement) : "no 'k' property",
                    path: present ? TreePath.Property(path, "k") : path
                )
            );
            return null;
        }

        return new ThresholdNode(comparison, k, operands, SourceSpan.None);
    }

    /// <summary>
    /// Reads <c>Collapse</c>'s <c>policy</c>: one of the three policy names (case-insensitive) as a JSON string. Whether the
    /// node is the outermost one is the compiler's rule, not the parser's, so a nested node parses and is rejected there.
    /// </summary>
    private static RuleNode? ParseCollapse(
        JsonElement element,
        string op,
        string path,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        if (
            !element.TryGetProperty("policy", out JsonElement policyElement)
            || policyElement.ValueKind != JsonValueKind.String
            || !CollapsePolicyText.TryParse(policyElement.GetString(), out CollapsePolicy policy)
        )
        {
            bool present = element.TryGetProperty("policy", out policyElement);
            DiagnosticSuggestion? suggestion =
                present && policyElement.ValueKind == JsonValueKind.String
                    ? NameSuggester.Suggest(policyElement.GetString()!, CollapsePolicyText.TreeFormatNames)
                    : null;
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"'{op}' requires 'policy' to be one of {string.Join(", ", CollapsePolicyText.Names)}.",
                    SourceSpan.None,
                    expected: $"one of {string.Join(", ", CollapsePolicyText.TreeFormatNames)}",
                    found: present ? DescribeValue(policyElement) : "no 'policy' property",
                    suggestion: suggestion,
                    path: present ? TreePath.Property(path, "policy") : path
                )
            );
            return null;
        }

        return new CollapseNode(operands, policy, SourceSpan.None);
    }

    /// <summary>
    /// Reads <c>Project</c>'s <c>unknownAs</c>: a JSON boolean or the string <c>"true"</c>/<c>"false"</c> in any letter
    /// case (like <c>const</c>). <c>Unknown</c> is rejected because projecting <c>Unknown</c> to itself is no projection.
    /// </summary>
    private static RuleNode? ParseProject(
        JsonElement element,
        string op,
        string path,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        bool present = element.TryGetProperty("unknownAs", out JsonElement valueElement);
        bool? unknownAs = null;
        if (present)
        {
            if (valueElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                unknownAs = valueElement.GetBoolean();
            }
            else if (
                valueElement.ValueKind == JsonValueKind.String
                && TruthValueText.TryParse(valueElement.GetString(), out TruthValue parsed)
                && parsed != TruthValue.Unknown
            )
            {
                unknownAs = parsed == TruthValue.True;
            }
        }

        if (unknownAs is not { } value)
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"'{op}' requires 'unknownAs' to be true or false (a JSON boolean or the string \"true\"/\"false\").",
                    SourceSpan.None,
                    expected: "true or false",
                    found: present ? DescribeValue(valueElement) : "no 'unknownAs' property",
                    path: present ? TreePath.Property(path, "unknownAs") : path
                )
            );
            return null;
        }

        return new ProjectNode(operands, value, SourceSpan.None);
    }

    private static RuleNode? ParseBetween(
        JsonElement element,
        string op,
        string path,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        if (
            !TryGetBound(element, op, "min", path, diagnostics, out int min)
            || !TryGetBound(element, op, "max", path, diagnostics, out int max)
        )
        {
            return null;
        }

        return new BetweenNode(min, max, operands, SourceSpan.None);
    }

    // Reports the first bound that is missing or not an integer, at that bound's own path.
    private static bool TryGetBound(
        JsonElement element,
        string op,
        string property,
        string path,
        List<Diagnostic> diagnostics,
        out int value
    )
    {
        value = 0;
        bool present = element.TryGetProperty(property, out JsonElement child);
        if (present && child.ValueKind == JsonValueKind.Number && child.TryGetInt32(out value))
        {
            return true;
        }

        diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.MalformedTree,
                $"'{op}' requires integer 'min' and 'max'.",
                SourceSpan.None,
                expected: "an integer",
                found: present ? Describe(child) : $"no '{property}' property",
                path: present ? TreePath.Property(path, property) : path
            )
        );
        return false;
    }

    private static RawLiteral? ParseLiteral(JsonElement element, string path, List<Diagnostic> diagnostics)
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
                int index = 0;
                foreach (JsonElement item in element.EnumerateArray())
                {
                    RawLiteral? converted = ParseLiteral(item, TreePath.Index(path, index++), diagnostics);
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
                        SourceSpan.None,
                        expected: "a string, number, boolean or array",
                        found: Describe(element),
                        path: path
                    )
                );
                return null;
        }
    }
}
