namespace TruthWeaver.Compilation;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;

/// <summary>
/// Turns a raw <see cref="RuleNode"/> tree (produced identically by the DSL, JSON, and YAML front
/// ends) into a validated, immutable <see cref="Expression"/> tree plus diagnostics — the
/// Validate + Build stages of the compilation pipeline (ADR-0003). This is the single place that
/// resolves predicate names against a <see cref="PredicateRegistry{TContext}"/>, validates argument
/// schemas, and enforces the resource limits from <see cref="CompilerOptions"/>, so every front end
/// gets identical validation behavior for free.
/// </summary>
/// <typeparam name="TContext">The application context type predicates in <see cref="PredicateRegistry{TContext}"/> read from.</typeparam>
internal sealed class RuleNodeCompiler<TContext>
{
    private readonly PredicateRegistry<TContext> registry;
    private readonly CompilerOptions options;
    private readonly List<Diagnostic> diagnostics = [];
    private int nodeCount;
    private bool nodeLimitReported;

    private RuleNodeCompiler(PredicateRegistry<TContext> registry, CompilerOptions options)
    {
        this.registry = registry;
        this.options = options;
    }

    /// <summary>Validates and builds an expression tree from a raw parse tree.</summary>
    /// <param name="root">The raw parse tree root.</param>
    /// <param name="registry">The predicate registry to resolve term names against.</param>
    /// <param name="options">The compiler's resource limits and mode.</param>
    /// <returns>
    /// The built tree (or <see langword="null"/> if any <see cref="DiagnosticSeverity.Error"/>
    /// diagnostic was produced), the outermost <c>Collapse</c> policy the rule declared (or <see langword="null"/>), and
    /// every diagnostic raised while validating. A root <see cref="CollapseNode"/> is not part of the tree: it is peeled
    /// into the policy here, which is what makes every other <see cref="CollapseNode"/> a nested, rejected one.
    /// </returns>
    public static (Expression? Tree, CollapsePolicy? Collapse, IReadOnlyList<Diagnostic> Diagnostics) Compile(
        RuleNode root,
        PredicateRegistry<TContext> registry,
        CompilerOptions options
    )
    {
        RuleNodeCompiler<TContext> compiler = new(registry, options);
        CollapsePolicy? collapse = null;
        RuleNode body = root;
        if (root is CollapseNode outermost)
        {
            if (outermost.Operands.Count != 1)
            {
                compiler.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MalformedTree,
                        $"Collapse requires exactly 1 operand but found {outermost.Operands.Count}.",
                        outermost.Span,
                        expected: "1 operand",
                        found: CountText(outermost.Operands.Count)
                    )
                );
                return (null, null, compiler.diagnostics);
            }

            collapse = outermost.Policy;
            body = outermost.Operands[0];
        }

        Expression tree = compiler.Build(body, depth: 1);
        bool hasErrors = compiler.diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        return (hasErrors ? null : tree, collapse, compiler.diagnostics);
    }

    /// <summary>Phrases an operand count for a diagnostic's expected/found pair: <c>1 operand</c>, <c>3 operands</c>.</summary>
    private static string CountText(int count)
    {
        return count == 1 ? "1 operand" : $"{count} operands";
    }

    private static DiagnosticSuggestion NestingHint(string name)
    {
        return new DiagnosticSuggestion(
            DiagnosticSuggestionKind.Hint,
            $"Add parentheses (or nest {name} nodes) to say how the chained operands group."
        );
    }

    /// <summary>Names the form a raw literal was written in, for an argument-type mismatch's <c>Found</c>.</summary>
    private static string DescribeLiteral(RawLiteral literal)
    {
        return literal.Form switch
        {
            RawLiteralForm.QuotedString => $"the string \"{literal.Text}\"",
            RawLiteralForm.Number => $"the number {literal.Text}",
            RawLiteralForm.Boolean => $"the boolean {(literal.BooleanValue ? "true" : "false")}",
            _ => "a list",
        };
    }

    private static TermIdentity BuildUnknownIdentity(TermNode node)
    {
        List<KeyValuePair<string, LiteralValue>> args =
        [
            .. node.Arguments.Select(a => new KeyValuePair<string, LiteralValue>(a.Name, LiteralConversion.Guess(a.Value))),
        ];
        return new TermIdentity(node.PredicateName, args);
    }

    /// <summary>
    /// The threshold values that would make a comparison structurally trivial (always true or always
    /// false regardless of what the operands evaluate to) for a given operand count — e.g.
    /// <c>AtLeast(0, ...)</c> is always true, <c>AtLeast(n + 1, ...)</c> is always false. Rejecting
    /// these catches an authoring mistake at compile time rather than silently accepting a constant
    /// rule, the same rationale ticket 09 applied to the original <c>AtLeast</c> operator.
    /// </summary>
    private static (int MinK, int MaxK) ValidThresholdRange(ThresholdComparison comparison, int operandCount)
    {
        return comparison switch
        {
            ThresholdComparison.AtLeast => (1, operandCount),
            ThresholdComparison.AtMost => (0, operandCount - 1),
            ThresholdComparison.GreaterThan => (0, operandCount - 1),
            ThresholdComparison.LessThan => (1, operandCount),
            ThresholdComparison.Exactly => (0, operandCount),
            _ => throw new InvalidOperationException($"Unhandled threshold comparison '{comparison}'."),
        };
    }

    private Expression Build(RuleNode node, int depth)
    {
        this.nodeCount++;
        if (this.nodeCount > this.options.MaxNodeCount)
        {
            if (!this.nodeLimitReported)
            {
                this.nodeLimitReported = true;
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MaxNodeCountExceeded,
                        $"Rule exceeds the maximum node count of {this.options.MaxNodeCount}.",
                        node.Span,
                        expected: $"at most {this.options.MaxNodeCount} nodes",
                        found: "more nodes than that"
                    )
                );
            }

            return FailedNode.Placeholder;
        }

        if (depth > this.options.MaxDepth)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MaxDepthExceeded,
                    $"Rule exceeds the maximum tree depth of {this.options.MaxDepth}.",
                    node.Span,
                    expected: $"nesting at most {this.options.MaxDepth} deep",
                    found: "deeper nesting"
                )
            );
            return FailedNode.Placeholder;
        }

        return node switch
        {
            ConstantNode c => new ConstantExpression(c.Value),
            ErrorNode => FailedNode.Placeholder,
            TermNode t => this.BuildTerm(t),
            NotNode n => new NotExpression(this.Build(n.Operand, depth + 1)),
            AndNode a => this.BuildVariadic(
                a.Operands,
                depth,
                a.Span,
                2,
                operands => new AndExpression(new EquatableArray<Expression>(operands))
            ),
            OrNode o => this.BuildVariadic(
                o.Operands,
                depth,
                o.Span,
                2,
                operands => new OrExpression(new EquatableArray<Expression>(operands))
            ),
            XorNode x => this.BuildXor(x, depth),
            EquivalentNode eq => this.BuildEquivalent(eq, depth),
            ImpliesNode i => this.BuildImplies(i, depth),
            NandNode nd => this.BuildNegatedBinary(nd.Operands, nd.Span, "NAND", depth, (l, r) => new NandExpression(l, r)),
            NorNode nr => this.BuildNegatedBinary(nr.Operands, nr.Span, "NOR", depth, (l, r) => new NorExpression(l, r)),
            NxorNode nx => this.BuildVariadic(
                nx.Operands,
                depth,
                nx.Span,
                2,
                operands => new NxorExpression(new EquatableArray<Expression>(operands))
            ),
            AnyNode an => this.BuildVariadic(
                an.Operands,
                depth,
                an.Span,
                2,
                operands => new AnyExpression(new EquatableArray<Expression>(operands))
            ),
            AllNode al => this.BuildVariadic(
                al.Operands,
                depth,
                al.Span,
                2,
                operands => new AllExpression(new EquatableArray<Expression>(operands))
            ),
            NoneNode no => this.BuildVariadic(
                no.Operands,
                depth,
                no.Span,
                2,
                operands => new NoneExpression(new EquatableArray<Expression>(operands))
            ),
            ExactlyOneNode e => this.BuildVariadic(
                e.Operands,
                depth,
                e.Span,
                2,
                operands => new ExactlyOneExpression(new EquatableArray<Expression>(operands))
            ),
            ThresholdNode th => this.BuildThreshold(th, depth),
            BetweenNode bt => this.BuildBetween(bt, depth),
            CoalesceNode co => this.BuildVariadic(
                co.Operands,
                depth,
                co.Span,
                2,
                operands => new CoalesceExpression(new EquatableArray<Expression>(operands))
            ),
            IfNode ifNode => this.BuildIf(ifNode, depth),
            InspectionNode ins => this.BuildInspection(ins, depth),
            ProjectNode pr => this.BuildProject(pr, depth),
            CollapseNode collapse => this.RejectNestedCollapse(collapse, depth),
            _ => throw new InvalidOperationException($"Unhandled rule node type '{node.GetType()}'."),
        };
    }

    /// <summary>Builds <c>If(condition, whenTrue, whenFalse)</c>; anything but exactly three operands is a <see cref="DiagnosticCodes.MalformedTree"/>.</summary>
    private Expression BuildIf(IfNode node, int depth)
    {
        if (node.Operands.Count != 3)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"If requires exactly 3 operands (condition, whenTrue, whenFalse) but found {node.Operands.Count}.",
                    node.Span,
                    expected: "3 operands",
                    found: CountText(node.Operands.Count)
                )
            );
            return FailedNode.Placeholder;
        }

        return new IfExpression(
            this.Build(node.Operands[0], depth + 1),
            this.Build(node.Operands[1], depth + 1),
            this.Build(node.Operands[2], depth + 1)
        );
    }

    /// <summary>Builds an inspection (<c>IsTrue</c>/<c>IsFalse</c>/<c>IsUnknown</c>/<c>IsKnown</c>); anything but one operand is a <see cref="DiagnosticCodes.MalformedTree"/>.</summary>
    private Expression BuildInspection(InspectionNode node, int depth)
    {
        if (node.Operands.Count != 1)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"{node.Kind} requires exactly 1 operand but found {node.Operands.Count}.",
                    node.Span,
                    expected: "1 operand",
                    found: CountText(node.Operands.Count)
                )
            );
            return FailedNode.Placeholder;
        }

        return new InspectionExpression(node.Kind, this.Build(node.Operands[0], depth + 1));
    }

    /// <summary>
    /// Reports a <c>Collapse</c> that is not the rule's outermost expression (ADR-0005 decision 14). Its operand is still
    /// built so problems inside it surface in the same pass instead of only after the author fixes the nesting.
    /// </summary>
    private Expression RejectNestedCollapse(CollapseNode node, int depth)
    {
        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.NestedCollapse,
                "Collapse can only be the outermost expression of a rule, because it is the final step that turns the result into a two-valued answer. Move it to the outside of the whole rule, or use Project(expr, True) / Project(expr, False) to resolve Unknown inside the rule.",
                node.Span,
                expected: "Collapse as the outermost expression",
                found: "Collapse nested inside another expression",
                suggestion: new DiagnosticSuggestion(
                    DiagnosticSuggestionKind.Hint,
                    "Move Collapse to the outside of the whole rule, or use Project(expr, True) or Project(expr, False) to resolve Unknown inside the rule."
                )
            )
        );
        return node.Operands.Count == 1 ? this.Build(node.Operands[0], depth + 1) : FailedNode.Placeholder;
    }

    /// <summary>Builds <c>Project(x, True|False)</c>; anything but one operand is a <see cref="DiagnosticCodes.MalformedTree"/>.</summary>
    private Expression BuildProject(ProjectNode node, int depth)
    {
        if (node.Operands.Count != 1)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"Project requires exactly 1 operand but found {node.Operands.Count}.",
                    node.Span,
                    expected: "1 operand",
                    found: CountText(node.Operands.Count)
                )
            );
            return FailedNode.Placeholder;
        }

        return new ProjectExpression(this.Build(node.Operands[0], depth + 1), node.UnknownAs);
    }

    private Expression BuildVariadic(
        IReadOnlyList<RuleNode> operands,
        int depth,
        SourceSpan span,
        int minOperands,
        Func<IReadOnlyList<Expression>, Expression> construct
    )
    {
        if (operands.Count < minOperands)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"This operator requires at least {minOperands} operands but found {operands.Count}.",
                    span,
                    expected: $"at least {minOperands} operands",
                    found: CountText(operands.Count)
                )
            );
            return FailedNode.Placeholder;
        }

        List<Expression> built = new(operands.Count);
        foreach (RuleNode operand in operands)
        {
            built.Add(this.Build(operand, depth + 1));
        }

        return construct(built);
    }

    private Expression BuildXor(XorNode node, int depth)
    {
        if (node.Operands.Count != 2)
        {
            string message =
                $"XOR is binary only; found {node.Operands.Count} operands. "
                + "Use NXOR(...) for n-ary parity (an odd number of True operands) or ExactlyOne(...) for n-ary 'exactly one'.";
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.XorArityViolation,
                    message,
                    node.Span,
                    expected: "2 operands",
                    found: CountText(node.Operands.Count),
                    suggestion: new DiagnosticSuggestion(
                        DiagnosticSuggestionKind.Hint,
                        "Use NXOR(...) for n-ary parity (an odd number of True operands) or ExactlyOne(...) for n-ary 'exactly one'."
                    )
                )
            );
            return FailedNode.Placeholder;
        }

        Expression left = this.Build(node.Operands[0], depth + 1);
        Expression right = this.Build(node.Operands[1], depth + 1);
        return new XorExpression(left, right);
    }

    private Expression BuildEquivalent(EquivalentNode node, int depth)
    {
        if (node.Operands.Count != 2)
        {
            string message =
                $"EQUIVALENT is binary only; found {node.Operands.Count} operands. "
                + "Add parentheses (or nest EQUIVALENT nodes) to say how chained equivalences group.";
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.XorArityViolation,
                    message,
                    node.Span,
                    expected: "2 operands",
                    found: CountText(node.Operands.Count),
                    suggestion: NestingHint("EQUIVALENT")
                )
            );
            return FailedNode.Placeholder;
        }

        Expression left = this.Build(node.Operands[0], depth + 1);
        Expression right = this.Build(node.Operands[1], depth + 1);
        return new EquivalentExpression(left, right);
    }

    /// <summary>
    /// Builds a strictly binary infix operator (<c>NAND</c>/<c>NOR</c>). Anything but two operands (a DSL chain or a
    /// malformed JSON/YAML node) is an <see cref="DiagnosticCodes.XorArityViolation"/> with a parentheses hint.
    /// </summary>
    private Expression BuildNegatedBinary(
        IReadOnlyList<RuleNode> operands,
        SourceSpan span,
        string name,
        int depth,
        Func<Expression, Expression, Expression> construct
    )
    {
        if (operands.Count != 2)
        {
            string message =
                $"{name} is binary only; found {operands.Count} operands. "
                + $"Add parentheses (or nest {name} nodes) to say how chained operations group.";
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.XorArityViolation,
                    message,
                    span,
                    expected: "2 operands",
                    found: CountText(operands.Count),
                    suggestion: NestingHint(name)
                )
            );
            return FailedNode.Placeholder;
        }

        Expression left = this.Build(operands[0], depth + 1);
        Expression right = this.Build(operands[1], depth + 1);
        return construct(left, right);
    }

    private Expression BuildImplies(ImpliesNode node, int depth)
    {
        if (node.Operands.Count != 2)
        {
            string message =
                $"IMPLIES is binary only; found {node.Operands.Count} operands. "
                + "Add parentheses (or nest IMPLIES nodes) to say how chained implications group.";
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.XorArityViolation,
                    message,
                    node.Span,
                    expected: "2 operands",
                    found: CountText(node.Operands.Count),
                    suggestion: NestingHint("IMPLIES")
                )
            );
            return FailedNode.Placeholder;
        }

        Expression antecedent = this.Build(node.Operands[0], depth + 1);
        Expression consequent = this.Build(node.Operands[1], depth + 1);
        return new ImpliesExpression(antecedent, consequent);
    }

    private Expression BuildThreshold(ThresholdNode node, int depth)
    {
        if (node.Operands.Count < 1)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"{node.Comparison} requires at least one operand.",
                    node.Span,
                    expected: "at least 1 operand",
                    found: CountText(0)
                )
            );
            return FailedNode.Placeholder;
        }

        (int minK, int maxK) = ValidThresholdRange(node.Comparison, node.Operands.Count);
        if (node.K < minK || node.K > maxK)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InvalidThresholdValue,
                    $"{node.Comparison}'s threshold k={node.K} must satisfy {minK} <= k <= {maxK} for {node.Operands.Count} operand(s) (any value outside that range makes the result a structural constant).",
                    node.Span,
                    expected: $"{minK} <= k <= {maxK}",
                    found: $"k={node.K}"
                )
            );
            return FailedNode.Placeholder;
        }

        List<Expression> built = new(node.Operands.Count);
        foreach (RuleNode operand in node.Operands)
        {
            built.Add(this.Build(operand, depth + 1));
        }

        return new ThresholdExpression(node.Comparison, node.K, new EquatableArray<Expression>(built));
    }

    /// <summary>
    /// Builds <c>BETWEEN(min, max, ...)</c>. Like the threshold family it rejects bounds that would make the result
    /// a structural constant (<c>0 &lt;= min &lt;= max &lt;= n</c>; the full range <c>0..n</c> is always True), and
    /// like <c>ANY</c>/<c>ALL</c>/<c>ExactlyOne</c> it needs at least two operands (ADR-0005 decision 13).
    /// </summary>
    private Expression BuildBetween(BetweenNode node, int depth)
    {
        int operandCount = node.Operands.Count;
        if (operandCount < 2)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedTree,
                    $"BETWEEN requires at least 2 operands but found {operandCount}.",
                    node.Span,
                    expected: "at least 2 operands",
                    found: CountText(operandCount)
                )
            );
            return FailedNode.Placeholder;
        }

        bool inRange = node.Min >= 0 && node.Min <= node.Max && node.Max <= operandCount;
        if (!inRange || (node.Min == 0 && node.Max == operandCount))
        {
            string reason = inRange
                ? $"the full range 0..{operandCount} is always True (a structural constant)"
                : $"it must satisfy 0 <= min <= max <= {operandCount} for {operandCount} operand(s)";
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InvalidThresholdValue,
                    $"BETWEEN's bounds min={node.Min}, max={node.Max} are invalid: {reason}.",
                    node.Span,
                    expected: $"0 <= min <= max <= {operandCount}, excluding the full range",
                    found: $"min={node.Min}, max={node.Max}"
                )
            );
            return FailedNode.Placeholder;
        }

        List<Expression> built = new(operandCount);
        foreach (RuleNode operand in node.Operands)
        {
            built.Add(this.Build(operand, depth + 1));
        }

        return new BetweenExpression(node.Min, node.Max, new EquatableArray<Expression>(built));
    }

    private Expression BuildTerm(TermNode node)
    {
        if (!this.registry.TryGet(node.PredicateName, out PredicateDescriptor<TContext>? descriptor) || descriptor is null)
        {
            if (this.options.Mode == CompilationMode.Lenient)
            {
                return new TermExpression(BuildUnknownIdentity(node), IsUnknownPredicate: true);
            }

            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.UnknownPredicate,
                    $"No predicate named '{node.PredicateName}' is registered.",
                    node.Span,
                    expected: "a registered predicate name or an operator",
                    found: $"'{node.PredicateName}'",
                    suggestion: NameSuggester.Suggest(node.PredicateName, this.registry.Names.Concat(DslVocabulary.Keywords))
                )
            );
            return FailedNode.Placeholder;
        }

        PredicateSchema schema = descriptor.Schema;
        Dictionary<string, LiteralValue> resolvedArgs = [];
        HashSet<string> suppliedNames = new(StringComparer.Ordinal);
        foreach (ArgumentNode arg in node.Arguments)
        {
            suppliedNames.Add(arg.Name);
            PredicateArgumentSchema? argSchema = schema.Arguments.FirstOrDefault(a =>
                string.Equals(a.Name, arg.Name, StringComparison.Ordinal)
            );
            if (argSchema is null)
            {
                string[] declared = [.. schema.Arguments.Select(a => a.Name)];
                string expectedArguments = declared.Length == 0 ? "no arguments" : $"one of {string.Join(", ", declared)}";
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.UnknownArgument,
                        $"Predicate '{schema.Name}' does not declare an argument named '{arg.Name}'.",
                        arg.Span,
                        expected: expectedArguments,
                        found: $"'{arg.Name}'",
                        suggestion: NameSuggester.Suggest(arg.Name, declared)
                    )
                );
                continue;
            }

            if (!LiteralConversion.TryConvert(arg.Value, argSchema.Type, out LiteralValue value))
            {
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.ArgumentTypeMismatch,
                        $"Argument '{arg.Name}' of predicate '{schema.Name}' must be of kind '{argSchema.Type}'.",
                        arg.Value.Span,
                        expected: $"a value of kind '{argSchema.Type}'",
                        found: DescribeLiteral(arg.Value)
                    )
                );
                continue;
            }

            resolvedArgs[arg.Name] = value;
        }

        foreach (PredicateArgumentSchema argSchema in schema.Arguments)
        {
            if (resolvedArgs.ContainsKey(argSchema.Name) || suppliedNames.Contains(argSchema.Name))
            {
                continue;
            }

            if (argSchema.Required)
            {
                string supplied =
                    suppliedNames.Count == 0
                        ? "no arguments"
                        : $"only {string.Join(", ", suppliedNames.Order(StringComparer.Ordinal).Select(n => $"'{n}'"))}";
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MissingArgument,
                        $"Predicate '{schema.Name}' requires argument '{argSchema.Name}'.",
                        node.Span,
                        expected: $"argument '{argSchema.Name}'",
                        found: supplied
                    )
                );
            }
            else if (argSchema.Default is { } defaultValue)
            {
                resolvedArgs[argSchema.Name] = defaultValue;
            }
        }

        TermIdentity identity = new(
            schema.Name,
            [.. resolvedArgs.Select(kv => new KeyValuePair<string, LiteralValue>(kv.Key, kv.Value))]
        );
        return new TermExpression(identity);
    }
}
