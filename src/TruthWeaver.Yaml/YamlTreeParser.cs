namespace TruthWeaver.Yaml;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Parses the identical flat, key-discriminated tree shape JSON uses (ADR-0003), expressed in YAML,
/// into the same raw <see cref="RuleNode"/> tree the DSL and JSON front ends produce (ticket 08).
/// Never throws for malformed YAML — it reports a <see cref="DiagnosticCodes.MalformedTree"/>
/// diagnostic instead.
/// </summary>
internal static class YamlTreeParser
{
    /// <summary>Parses YAML tree text into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="yaml">The YAML tree text.</param>
    /// <returns>
    /// The parsed root node (or <see langword="null"/> if the YAML itself was malformed) and the
    /// diagnostics raised while parsing.
    /// </returns>
    public static (RuleNode? Root, IReadOnlyList<Diagnostic> Diagnostics) Parse(string yaml)
    {
        YamlStream stream = [];
        try
        {
            using StringReader reader = new(yaml);
            stream.Load(reader);
        }
        catch (YamlException ex)
        {
            List<Diagnostic> diagnostics =
            [
                Diagnostic.Error(DiagnosticCodes.MalformedTree, $"Malformed YAML: {ex.Message}", SourceSpan.None),
            ];
            return (null, diagnostics);
        }

        if (stream.Documents.Count == 0)
        {
            List<Diagnostic> diagnostics =
            [
                Diagnostic.Error(DiagnosticCodes.MalformedTree, "The YAML document is empty.", SourceSpan.None),
            ];
            return (null, diagnostics);
        }

        return Parse(stream.Documents[0].RootNode);
    }

    /// <summary>Parses an already-materialized YAML tree node (e.g. a subtree of a larger document) into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="node">The YAML tree node.</param>
    /// <returns>
    /// The parsed root node (or <see langword="null"/> if the node was malformed) and the
    /// diagnostics raised while parsing.
    /// </returns>
    public static (RuleNode? Root, IReadOnlyList<Diagnostic> Diagnostics) Parse(YamlNode node)
    {
        List<Diagnostic> diagnostics = [];
        RuleNode? root = ParseNode(node, diagnostics);
        return (root, diagnostics);
    }

    private static RuleNode? ParseNode(YamlNode node, List<Diagnostic> diagnostics)
    {
        if (node is not YamlMappingNode mapping)
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Expected a YAML mapping node but found {node.NodeType}.",
                    SourceSpan.None
                )
            );
            return null;
        }

        if (TryGetChild(mapping, "const", out YamlNode? constNode))
        {
            if (
                constNode is not YamlScalarNode { Value: { } constText }
                || !TruthValueText.TryParse(constText, out TruthValue constValue)
            )
            {
                diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MalformedTree,
                        "'const' must be a YAML boolean or one of true, false, unknown.",
                        SourceSpan.None
                    )
                );
                return null;
            }

            return new ConstantNode(constValue, SourceSpan.None);
        }

        if (TryGetChild(mapping, "predicate", out YamlNode? predicateNode))
        {
            return ParseTerm(mapping, predicateNode, diagnostics);
        }

        if (TryGetChild(mapping, "op", out YamlNode? opNode) && opNode is YamlScalarNode { Value: { } opText })
        {
            return ParseOperator(mapping, opText, diagnostics);
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

    private static RuleNode? ParseTerm(YamlMappingNode mapping, YamlNode predicateNode, List<Diagnostic> diagnostics)
    {
        if (predicateNode is not YamlScalarNode { Value: { } predicateName })
        {
            diagnostics.Add(
                Diagnostic.Error(DiagnosticCodes.MalformedTree, "'predicate' must be a YAML string.", SourceSpan.None)
            );
            return null;
        }

        List<ArgumentNode> arguments = [];
        if (TryGetChild(mapping, "args", out YamlNode? argsNode))
        {
            if (argsNode is not YamlMappingNode argsMapping)
            {
                diagnostics.Add(
                    Diagnostic.Error(DiagnosticCodes.MalformedTree, "'args' must be a YAML mapping.", SourceSpan.None)
                );
                return null;
            }

            foreach (KeyValuePair<YamlNode, YamlNode> entry in argsMapping.Children)
            {
                if (entry.Key is not YamlScalarNode { Value: { } argName })
                {
                    diagnostics.Add(
                        Diagnostic.Error(
                            DiagnosticCodes.MalformedTree,
                            "An argument name must be a YAML string.",
                            SourceSpan.None
                        )
                    );
                    return null;
                }

                RawLiteral? literal = ParseLiteral(entry.Value, diagnostics);
                if (literal is null)
                {
                    return null;
                }

                arguments.Add(new ArgumentNode(argName, literal, SourceSpan.None));
            }
        }

        return new TermNode(predicateName, arguments, SourceSpan.None);
    }

    private static RuleNode? ParseOperator(YamlMappingNode mapping, string op, List<Diagnostic> diagnostics)
    {
        if (
            !TryGetChild(mapping, "operands", out YamlNode? operandsNode)
            || operandsNode is not YamlSequenceNode operandsSequence
        )
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Operator node '{op}' requires an 'operands' sequence.",
                    SourceSpan.None
                )
            );
            return null;
        }

        List<RuleNode> operands = [];
        foreach (YamlNode operandNode in operandsSequence.Children)
        {
            RuleNode? operand = ParseNode(operandNode, diagnostics);
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
                return ParseProject(mapping, op, operands, diagnostics);
            case "Collapse":
                return ParseCollapse(mapping, op, operands, diagnostics);
            case "ExactlyOne":
                return new ExactlyOneNode(operands, SourceSpan.None);
            case "AtLeast":
                return ParseThreshold(mapping, op, ThresholdComparison.AtLeast, operands, diagnostics);
            case "AtMost":
                return ParseThreshold(mapping, op, ThresholdComparison.AtMost, operands, diagnostics);
            case "GreaterThan":
                return ParseThreshold(mapping, op, ThresholdComparison.GreaterThan, operands, diagnostics);
            case "LessThan":
                return ParseThreshold(mapping, op, ThresholdComparison.LessThan, operands, diagnostics);
            case "Exactly":
                return ParseThreshold(mapping, op, ThresholdComparison.Exactly, operands, diagnostics);
            case "Between":
                return ParseBetween(mapping, op, operands, diagnostics);
            default:
                throw new InvalidOperationException($"Unhandled canonical op-name '{canonicalOpName}'.");
        }
    }

    private static RuleNode? ParseThreshold(
        YamlMappingNode mapping,
        string op,
        ThresholdComparison comparison,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        if (
            !TryGetChild(mapping, "k", out YamlNode? kNode)
            || kNode is not YamlScalarNode { Value: { } kText }
            || !int.TryParse(kText, out int k)
        )
        {
            diagnostics.Add(
                Diagnostic.Error(DiagnosticCodes.MalformedTree, $"'{op}' requires a numeric 'k'.", SourceSpan.None)
            );
            return null;
        }

        return new ThresholdNode(comparison, k, operands, SourceSpan.None);
    }

    /// <summary>
    /// Reads <c>Collapse</c>'s <c>policy</c>: one of the three policy names (case-insensitive). Whether the node is the
    /// outermost one is the compiler's rule, not the parser's, so a nested node parses and is rejected there.
    /// </summary>
    private static RuleNode? ParseCollapse(
        YamlMappingNode mapping,
        string op,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        if (
            !TryGetChild(mapping, "policy", out YamlNode? policyNode)
            || policyNode is not YamlScalarNode { Value: { } policyText }
            || !CollapsePolicyText.TryParse(policyText, out CollapsePolicy policy)
        )
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"'{op}' requires 'policy' to be one of {string.Join(", ", CollapsePolicyText.Names)}.",
                    SourceSpan.None
                )
            );
            return null;
        }

        return new CollapseNode(operands, policy, SourceSpan.None);
    }

    /// <summary>
    /// Reads <c>Project</c>'s <c>unknownAs</c>: <c>true</c> or <c>false</c> in any letter case. <c>Unknown</c> is rejected
    /// because projecting <c>Unknown</c> to itself is no projection.
    /// </summary>
    private static RuleNode? ParseProject(
        YamlMappingNode mapping,
        string op,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        if (
            !TryGetChild(mapping, "unknownAs", out YamlNode? valueNode)
            || valueNode is not YamlScalarNode { Value: { } valueText }
            || !TruthValueText.TryParse(valueText, out TruthValue parsed)
            || parsed == TruthValue.Unknown
        )
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"'{op}' requires 'unknownAs' to be true or false.",
                    SourceSpan.None
                )
            );
            return null;
        }

        return new ProjectNode(operands, parsed == TruthValue.True, SourceSpan.None);
    }

    private static RuleNode? ParseBetween(
        YamlMappingNode mapping,
        string op,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        if (!TryGetInteger(mapping, "min", out int min) || !TryGetInteger(mapping, "max", out int max))
        {
            diagnostics.Add(
                Diagnostic.Error(DiagnosticCodes.MalformedTree, $"'{op}' requires integer 'min' and 'max'.", SourceSpan.None)
            );
            return null;
        }

        return new BetweenNode(min, max, operands, SourceSpan.None);
    }

    private static bool TryGetInteger(YamlMappingNode mapping, string key, out int value)
    {
        value = 0;
        return TryGetChild(mapping, key, out YamlNode? node)
            && node is YamlScalarNode { Value: { } text }
            && int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private static RawLiteral? ParseLiteral(YamlNode node, List<Diagnostic> diagnostics)
    {
        switch (node)
        {
            case YamlScalarNode scalar:
                return ClassifyScalar(scalar);
            case YamlSequenceNode sequence:
                List<RawLiteral> items = [];
                foreach (YamlNode child in sequence.Children)
                {
                    RawLiteral? converted = ParseLiteral(child, diagnostics);
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
                        $"Unsupported YAML node type '{node.NodeType}' for a literal value.",
                        SourceSpan.None
                    )
                );
                return null;
        }
    }

    private static RawLiteral ClassifyScalar(YamlScalarNode scalar)
    {
        string text = scalar.Value ?? string.Empty;

        // A quoted scalar is always the author's explicit string, regardless of its content
        // (e.g. role: "true" must stay the string "true", not become a boolean).
        if (scalar.Style is ScalarStyle.SingleQuoted or ScalarStyle.DoubleQuoted or ScalarStyle.Literal or ScalarStyle.Folded)
        {
            return RawLiteral.OfString(text, SourceSpan.None);
        }

        if (TryParseBoolean(text, out bool boolValue))
        {
            return RawLiteral.OfBoolean(boolValue, SourceSpan.None);
        }

        return IsNumber(text) ? RawLiteral.OfNumber(text, SourceSpan.None) : RawLiteral.OfString(text, SourceSpan.None);
    }

    private static bool TryParseBoolean(string text, out bool value)
    {
        if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    private static bool IsNumber(string text)
    {
        return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    private static bool TryGetChild(YamlMappingNode mapping, string key, [NotNullWhen(true)] out YamlNode? value)
    {
        foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
        {
            if (entry.Key is YamlScalarNode { Value: { } keyText } && string.Equals(keyText, key, StringComparison.Ordinal))
            {
                value = entry.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}
