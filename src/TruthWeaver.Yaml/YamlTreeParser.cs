namespace TruthWeaver.Yaml;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Parses the identical flat, key-discriminated tree shape JSON uses (ADR-0003), expressed in YAML,
/// into the same raw <see cref="RuleNode"/> tree the DSL and JSON front ends produce (ticket 08).
/// Never throws for malformed YAML — it reports a <see cref="DiagnosticCodes.MalformedTree"/>
/// diagnostic instead. Each diagnostic is located by its path from the document root (<c>$.operands[1].op</c>, the same
/// syntax JSON uses) and, because YamlDotNet keeps node positions, by the span of the offending node as well.
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
            List<Diagnostic> diagnostics = [SyntaxError(yaml, ex)];
            return (null, diagnostics);
        }

        if (stream.Documents.Count == 0)
        {
            List<Diagnostic> diagnostics =
            [
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    "The YAML document is empty.",
                    SourceSpan.None,
                    expected: "a YAML mapping",
                    found: "an empty document",
                    path: TreePath.Root
                ),
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
        RuleNode? root = ParseNode(node, TreePath.Root, diagnostics);
        return (root, diagnostics);
    }

    /// <summary>Gets the source range YamlDotNet recorded for a node, as a span an editor can underline.</summary>
    private static SourceSpan SpanOf(YamlNode node)
    {
        int start = (int)node.Start.Index;
        return new SourceSpan(start, Math.Max(0, (int)node.End.Index - start));
    }

    /// <summary>Names a node's shape for a diagnostic's <c>Found</c>.</summary>
    private static string Describe(YamlNode node)
    {
        return node switch
        {
            YamlScalarNode => "a scalar",
            YamlSequenceNode => "a sequence",
            YamlMappingNode => "a mapping",
            _ => "an alias",
        };
    }

    /// <summary>Quotes a scalar's text, or names the shape of anything else: for fields where the value is what is wrong.</summary>
    private static string DescribeValue(YamlNode node)
    {
        return node is YamlScalarNode { Value: { } text } ? $"'{text}'" : Describe(node);
    }

    private static string FoundOperands(int count)
    {
        return count == 1 ? "1 operand" : $"{count} operands";
    }

    /// <summary>Builds the diagnostic for text that is not YAML: the parser's message, its position, and the nearest valid ancestor.</summary>
    private static Diagnostic SyntaxError(string yaml, YamlException ex)
    {
        // YamlDotNet prefixes its messages with "(Line: .., Col: .., Idx: ..) - (..): "; the position is carried by the span.
        string reason = ex.Message;
        int detail = reason.IndexOf("): ", StringComparison.Ordinal);
        if (detail >= 0)
        {
            reason = reason[(detail + 3)..];
        }

        int start = Math.Clamp((int)ex.Start.Index, 0, yaml.Length);
        int length = Math.Max(1, (int)ex.End.Index - (int)ex.Start.Index);
        return Diagnostic.Error(
            DiagnosticCodes.MalformedTree,
            $"Malformed YAML: {ex.Message}",
            new SourceSpan(start, length),
            expected: "well-formed YAML",
            found: reason,
            path: ContainerPathAt(yaml)
        );
    }

    /// <summary>Reads the text event by event until the parser gives up, and returns the path of the innermost container still open.</summary>
    private static string ContainerPathAt(string yaml)
    {
        TreePathTracker tracker = new();
        try
        {
            using StringReader reader = new(yaml);
            Parser parser = new(reader);
            while (parser.MoveNext())
            {
                switch (parser.Current)
                {
                    case MappingStart:
                        tracker.Enter(isArray: false);
                        break;
                    case SequenceStart:
                        tracker.Enter(isArray: true);
                        break;
                    case MappingEnd or SequenceEnd:
                        tracker.YamlExit();
                        break;
                    case YamlDotNet.Core.Events.Scalar scalar:
                        tracker.YamlScalar(scalar.Value);
                        break;
                    case AnchorAlias:
                        tracker.YamlScalar(string.Empty);
                        break;
                }
            }
        }
        catch (YamlException)
        {
            // The parser stopping is the point: the tracker now holds the last good position.
        }

        return tracker.ContainerPath;
    }

    private static RuleNode? ParseNode(YamlNode node, string path, List<Diagnostic> diagnostics)
    {
        RuleNode? parsed = ParseNodeCore(node, path, diagnostics);
        return parsed is null ? null : parsed with { Path = path, Span = SpanOf(node) };
    }

    private static RuleNode? ParseNodeCore(YamlNode node, string path, List<Diagnostic> diagnostics)
    {
        if (node is not YamlMappingNode mapping)
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Expected a YAML mapping node but found {node.NodeType}.",
                    SpanOf(node),
                    expected: "a YAML mapping",
                    found: Describe(node),
                    path: path
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
                        SpanOf(constNode),
                        expected: "a YAML boolean or one of true, false, unknown",
                        found: DescribeValue(constNode),
                        path: TreePath.Property(path, "const")
                    )
                );
                return null;
            }

            return new ConstantNode(constValue, SourceSpan.None);
        }

        if (TryGetChild(mapping, "predicate", out YamlNode? predicateNode))
        {
            return ParseTerm(mapping, predicateNode, path, diagnostics);
        }

        if (TryGetChild(mapping, "op", out YamlNode? opNode))
        {
            if (opNode is YamlScalarNode { Value: { } opText })
            {
                return ParseOperator(mapping, opNode, opText, path, diagnostics);
            }

            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    "'op' must be a YAML string naming an operator.",
                    SpanOf(opNode),
                    expected: "a YAML string",
                    found: Describe(opNode),
                    path: TreePath.Property(path, "op")
                )
            );
            return null;
        }

        diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.MalformedTree,
                "A tree node must have a 'const', 'predicate', or 'op' key.",
                SpanOf(mapping),
                expected: "a 'const', 'predicate' or 'op' key",
                found: "no such key",
                path: path
            )
        );
        return null;
    }

    private static RuleNode? ParseTerm(
        YamlMappingNode mapping,
        YamlNode predicateNode,
        string path,
        List<Diagnostic> diagnostics
    )
    {
        if (predicateNode is not YamlScalarNode { Value: { } predicateName })
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    "'predicate' must be a YAML string.",
                    SpanOf(predicateNode),
                    expected: "a YAML string",
                    found: Describe(predicateNode),
                    path: TreePath.Property(path, "predicate")
                )
            );
            return null;
        }

        List<ArgumentNode> arguments = [];
        if (TryGetChild(mapping, "args", out YamlNode? argsNode))
        {
            string argsPath = TreePath.Property(path, "args");
            if (argsNode is not YamlMappingNode argsMapping)
            {
                diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MalformedTree,
                        "'args' must be a YAML mapping.",
                        SpanOf(argsNode),
                        expected: "a YAML mapping",
                        found: Describe(argsNode),
                        path: argsPath
                    )
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
                            SpanOf(entry.Key),
                            expected: "a YAML string",
                            found: Describe(entry.Key),
                            path: argsPath
                        )
                    );
                    return null;
                }

                string argumentPath = TreePath.Property(argsPath, argName);
                RawLiteral? literal = ParseLiteral(entry.Value, argumentPath, diagnostics);
                if (literal is null)
                {
                    return null;
                }

                arguments.Add(new ArgumentNode(argName, literal, SpanOf(entry.Value)) { Path = argumentPath });
            }
        }

        return new TermNode(predicateName, arguments, SourceSpan.None);
    }

    private static RuleNode? ParseOperator(
        YamlMappingNode mapping,
        YamlNode opNode,
        string op,
        string path,
        List<Diagnostic> diagnostics
    )
    {
        // The operator name is checked before its operands so a typo is reported on its own, with its suggestion.
        if (!TreeFormatOpNames.TryFromTreeFormat(op, out string? canonicalOpName))
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Unknown operator '{op}'.",
                    SpanOf(opNode),
                    expected: "a known operator",
                    found: $"'{op}'",
                    suggestion: NameSuggester.Suggest(op, TreeFormatOpNames.ReadableNames),
                    path: TreePath.Property(path, "op")
                )
            );
            return null;
        }

        string operandsPath = TreePath.Property(path, "operands");
        if (!TryGetChild(mapping, "operands", out YamlNode? operandsNode))
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Operator node '{op}' requires an 'operands' sequence.",
                    SpanOf(mapping),
                    expected: "an 'operands' sequence",
                    found: "no 'operands' key",
                    path: path
                )
            );
            return null;
        }

        if (operandsNode is not YamlSequenceNode operandsSequence)
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Operator node '{op}' requires an 'operands' sequence.",
                    SpanOf(operandsNode),
                    expected: "an 'operands' sequence",
                    found: Describe(operandsNode),
                    path: operandsPath
                )
            );
            return null;
        }

        List<RuleNode> operands = [];
        int index = 0;
        foreach (YamlNode operandNode in operandsSequence.Children)
        {
            RuleNode? operand = ParseNode(operandNode, TreePath.Index(operandsPath, index++), diagnostics);
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
                            SpanOf(operandsNode),
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
                return ParseProject(mapping, op, path, operands, diagnostics);
            case "Collapse":
                return ParseCollapse(mapping, op, path, operands, diagnostics);
            case "ExactlyOne":
                return new ExactlyOneNode(operands, SourceSpan.None);
            case "AtLeast":
                return ParseThreshold(mapping, op, path, ThresholdComparison.AtLeast, operands, diagnostics);
            case "AtMost":
                return ParseThreshold(mapping, op, path, ThresholdComparison.AtMost, operands, diagnostics);
            case "GreaterThan":
                return ParseThreshold(mapping, op, path, ThresholdComparison.GreaterThan, operands, diagnostics);
            case "LessThan":
                return ParseThreshold(mapping, op, path, ThresholdComparison.LessThan, operands, diagnostics);
            case "Exactly":
                return ParseThreshold(mapping, op, path, ThresholdComparison.Exactly, operands, diagnostics);
            case "Between":
                return ParseBetween(mapping, op, path, operands, diagnostics);
            default:
                throw new InvalidOperationException($"Unhandled canonical op-name '{canonicalOpName}'.");
        }
    }

    private static RuleNode? ParseThreshold(
        YamlMappingNode mapping,
        string op,
        string path,
        ThresholdComparison comparison,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        bool present = TryGetChild(mapping, "k", out YamlNode? kNode);
        if (!present || kNode is not YamlScalarNode { Value: { } kText } || !int.TryParse(kText, out int k))
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"'{op}' requires a numeric 'k'.",
                    present ? SpanOf(kNode!) : SpanOf(mapping),
                    expected: "an integer",
                    found: present ? DescribeValue(kNode!) : "no 'k' key",
                    path: present ? TreePath.Property(path, "k") : path
                )
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
        string path,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        bool present = TryGetChild(mapping, "policy", out YamlNode? policyNode);
        if (
            !present
            || policyNode is not YamlScalarNode { Value: { } policyText }
            || !CollapsePolicyText.TryParse(policyText, out CollapsePolicy policy)
        )
        {
            DiagnosticSuggestion? suggestion = policyNode is YamlScalarNode { Value: { } written }
                ? NameSuggester.Suggest(written, CollapsePolicyText.TreeFormatNames)
                : null;
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"'{op}' requires 'policy' to be one of {string.Join(", ", CollapsePolicyText.Names)}.",
                    present ? SpanOf(policyNode!) : SpanOf(mapping),
                    expected: $"one of {string.Join(", ", CollapsePolicyText.TreeFormatNames)}",
                    found: present ? DescribeValue(policyNode!) : "no 'policy' key",
                    suggestion: suggestion,
                    path: present ? TreePath.Property(path, "policy") : path
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
        string path,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        bool present = TryGetChild(mapping, "unknownAs", out YamlNode? valueNode);
        if (
            !present
            || valueNode is not YamlScalarNode { Value: { } valueText }
            || !TruthValueText.TryParse(valueText, out TruthValue parsed)
            || parsed == TruthValue.Unknown
        )
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"'{op}' requires 'unknownAs' to be true or false.",
                    present ? SpanOf(valueNode!) : SpanOf(mapping),
                    expected: "true or false",
                    found: present ? DescribeValue(valueNode!) : "no 'unknownAs' key",
                    path: present ? TreePath.Property(path, "unknownAs") : path
                )
            );
            return null;
        }

        return new ProjectNode(operands, parsed == TruthValue.True, SourceSpan.None);
    }

    private static RuleNode? ParseBetween(
        YamlMappingNode mapping,
        string op,
        string path,
        List<RuleNode> operands,
        List<Diagnostic> diagnostics
    )
    {
        if (
            !TryGetBound(mapping, op, "min", path, diagnostics, out int min)
            || !TryGetBound(mapping, op, "max", path, diagnostics, out int max)
        )
        {
            return null;
        }

        return new BetweenNode(min, max, operands, SourceSpan.None);
    }

    // Reports the first bound that is missing or not an integer, at that bound's own path.
    private static bool TryGetBound(
        YamlMappingNode mapping,
        string op,
        string key,
        string path,
        List<Diagnostic> diagnostics,
        out int value
    )
    {
        value = 0;
        bool present = TryGetChild(mapping, key, out YamlNode? node);
        if (
            present
            && node is YamlScalarNode { Value: { } text }
            && int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value)
        )
        {
            return true;
        }

        diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.MalformedTree,
                $"'{op}' requires integer 'min' and 'max'.",
                present ? SpanOf(node!) : SpanOf(mapping),
                expected: "an integer",
                found: present ? DescribeValue(node!) : $"no '{key}' key",
                path: present ? TreePath.Property(path, key) : path
            )
        );
        return false;
    }

    private static RawLiteral? ParseLiteral(YamlNode node, string path, List<Diagnostic> diagnostics)
    {
        switch (node)
        {
            case YamlScalarNode scalar:
                return ClassifyScalar(scalar);
            case YamlSequenceNode sequence:
                List<RawLiteral> items = [];
                int index = 0;
                foreach (YamlNode child in sequence.Children)
                {
                    RawLiteral? converted = ParseLiteral(child, TreePath.Index(path, index++), diagnostics);
                    if (converted is null)
                    {
                        return null;
                    }

                    items.Add(converted);
                }

                return RawLiteral.OfArray(items, SpanOf(sequence));
            default:
                diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MalformedTree,
                        $"Unsupported YAML node type '{node.NodeType}' for a literal value.",
                        SpanOf(node),
                        expected: "a scalar or a sequence",
                        found: Describe(node),
                        path: path
                    )
                );
                return null;
        }
    }

    private static RawLiteral ClassifyScalar(YamlScalarNode scalar)
    {
        string text = scalar.Value ?? string.Empty;
        SourceSpan span = SpanOf(scalar);

        // A quoted scalar is always the author's explicit string, regardless of its content
        // (e.g. role: "true" must stay the string "true", not become a boolean).
        if (scalar.Style is ScalarStyle.SingleQuoted or ScalarStyle.DoubleQuoted or ScalarStyle.Literal or ScalarStyle.Folded)
        {
            return RawLiteral.OfString(text, span);
        }

        if (TryParseBoolean(text, out bool boolValue))
        {
            return RawLiteral.OfBoolean(boolValue, span);
        }

        return IsNumber(text) ? RawLiteral.OfNumber(text, span) : RawLiteral.OfString(text, span);
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
