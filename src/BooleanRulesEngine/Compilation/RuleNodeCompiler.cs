namespace BooleanRulesEngine.Compilation;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Ast;
using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Parsing;
using BooleanRulesEngine.Registry;

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
    /// diagnostic was produced) plus every diagnostic raised while validating.
    /// </returns>
    public static (Expression? Tree, IReadOnlyList<Diagnostic> Diagnostics) Compile(
        RuleNode root,
        PredicateRegistry<TContext> registry,
        CompilerOptions options
    )
    {
        RuleNodeCompiler<TContext> compiler = new(registry, options);
        Expression tree = compiler.Build(root, depth: 1);
        bool hasErrors = compiler.diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        return (hasErrors ? null : tree, compiler.diagnostics);
    }

    private static TermIdentity BuildUnknownIdentity(TermNode node)
    {
        List<KeyValuePair<string, LiteralValue>> args =
        [
            .. node.Arguments.Select(a => new KeyValuePair<string, LiteralValue>(a.Name, LiteralConversion.Guess(a.Value))),
        ];
        return new TermIdentity(node.PredicateName, args);
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
                        node.Span
                    )
                );
            }

            return new ConstantExpression(false);
        }

        if (depth > this.options.MaxDepth)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MaxDepthExceeded,
                    $"Rule exceeds the maximum tree depth of {this.options.MaxDepth}.",
                    node.Span
                )
            );
            return new ConstantExpression(false);
        }

        return node switch
        {
            ConstantNode c => new ConstantExpression(c.Value),
            ErrorNode => new ConstantExpression(false),
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
            ExactlyOneNode e => this.BuildVariadic(
                e.Operands,
                depth,
                e.Span,
                2,
                operands => new ExactlyOneExpression(new EquatableArray<Expression>(operands))
            ),
            AtLeastNode al => this.BuildAtLeast(al, depth),
            _ => throw new InvalidOperationException($"Unhandled rule node type '{node.GetType()}'."),
        };
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
                    span
                )
            );
            return new ConstantExpression(false);
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
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.XorArityViolation,
                    $"XOR is binary only; found {node.Operands.Count} operands. Use ExactlyOne(...) for n-ary 'exactly one'.",
                    node.Span
                )
            );
            return new ConstantExpression(false);
        }

        Expression left = this.Build(node.Operands[0], depth + 1);
        Expression right = this.Build(node.Operands[1], depth + 1);
        return new XorExpression(left, right);
    }

    private Expression BuildAtLeast(AtLeastNode node, int depth)
    {
        if (node.Operands.Count < 1)
        {
            this.diagnostics.Add(
                Diagnostic.Error(DiagnosticCodes.MalformedTree, "AtLeast requires at least one operand.", node.Span)
            );
            return new ConstantExpression(false);
        }

        if (node.K < 1 || node.K > node.Operands.Count)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InvalidAtLeastThreshold,
                    $"AtLeast's threshold k={node.K} must satisfy 1 <= k <= {node.Operands.Count} (the operand count).",
                    node.Span
                )
            );
            return new ConstantExpression(false);
        }

        List<Expression> built = new(node.Operands.Count);
        foreach (RuleNode operand in node.Operands)
        {
            built.Add(this.Build(operand, depth + 1));
        }

        return new AtLeastExpression(node.K, new EquatableArray<Expression>(built));
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
                    node.Span
                )
            );
            return new ConstantExpression(false);
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
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.UnknownArgument,
                        $"Predicate '{schema.Name}' does not declare an argument named '{arg.Name}'.",
                        arg.Span
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
                        arg.Value.Span
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
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MissingArgument,
                        $"Predicate '{schema.Name}' requires argument '{argSchema.Name}'.",
                        node.Span
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
