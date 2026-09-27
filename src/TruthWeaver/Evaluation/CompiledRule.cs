namespace TruthWeaver.Evaluation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Json;
using TruthWeaver.Printing;
using TruthWeaver.Registry;

/// <summary>
/// The immutable, thread-safe result of compiling a rule's text (CONTEXT.md). Safe to cache and
/// share; compile once, evaluate many times. Runtime rule updates are a compile-and-swap of the
/// reference holding the active instance — no lock is needed (ADR-0002).
/// </summary>
/// <typeparam name="TContext">The application context type this rule evaluates against.</typeparam>
public sealed class CompiledRule<TContext>
{
    private readonly PredicateRegistry<TContext> registry;
    private readonly ILogger logger;
    private readonly Lazy<string> canonicalText;

    internal CompiledRule(Expression root, PredicateRegistry<TContext> registry, ILogger? logger = null)
    {
        this.Root = root;
        this.registry = registry;
        this.logger = logger ?? NullLogger.Instance;
        this.canonicalText = new Lazy<string>(() => CanonicalPrinter.Print(this.Root));
    }

    /// <summary>Gets this rule's canonical printed DSL text — the form <c>RuleCompiler.Compile</c> reproduces a structurally equal tree from.</summary>
    public string CanonicalText => this.canonicalText.Value;

    /// <summary>
    /// Gets the underlying expression tree. Internal — visible to <c>BooleanRulesEngine.Yaml</c> via
    /// <c>InternalsVisibleTo</c>, so its YAML printer can render the same tree <see cref="CanonicalText"/>
    /// and <see cref="PrintJson"/> render, without this package needing to know YAML exists.
    /// </summary>
    internal Expression Root { get; }

    /// <summary>Prints this rule to the flat, key-discriminated JSON tree shape (ADR-0003).</summary>
    /// <returns>The JSON text.</returns>
    public string PrintJson()
    {
        return JsonTreePrinter.Print(this.Root);
    }

    /// <summary>
    /// Describes this rule's expression tree recursively — every operator's label/description (from
    /// <see cref="OperatorInfo"/>) and every term's label/description (from its predicate's registered
    /// <see cref="PredicateSchema"/>), without exposing the underlying closed-set AST types
    /// themselves. Useful for a rule-authoring UI or a generated "what does this rule mean" report.
    /// </summary>
    /// <returns>The root node's description, with every operand described the same way.</returns>
    public RuleDescription Describe()
    {
        return DescribeNode(this.Root, this.registry);
    }

    /// <summary>Renders this rule's structure as Mermaid <c>flowchart</c> text, for a diagram UI.</summary>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public string PrintMermaid()
    {
        return MermaidTreePrinter.Print(this.Describe());
    }

    /// <summary>
    /// Renders this rule's structure as Mermaid <c>flowchart</c> text, colored by one evaluation's
    /// result and short-circuit path.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.EvaluatedTree"/>.</exception>
    public string PrintMermaid(Decision decision)
    {
        return MermaidTreePrinter.Print(this.Describe(), RequireEvaluatedTree(decision));
    }

    /// <summary>Renders this rule's structure as an indented plain-text tree.</summary>
    /// <returns>The indented tree text.</returns>
    public string PrintPlainText()
    {
        return PlainTextTreePrinter.Print(this.Describe());
    }

    /// <summary>
    /// Renders this rule's structure as an indented plain-text tree, annotated by one evaluation's
    /// result and short-circuit path.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <returns>The indented tree text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.EvaluatedTree"/>.</exception>
    public string PrintPlainText(Decision decision)
    {
        return PlainTextTreePrinter.Print(this.Describe(), RequireEvaluatedTree(decision));
    }

    /// <summary>Evaluates this rule against a context.</summary>
    /// <param name="context">The application-supplied evaluation context.</param>
    /// <param name="services">
    /// The service provider to resolve class-based predicates from, fresh for this call — never
    /// captured once at registration, so scoped dependencies (a <c>DbContext</c>, a scoped
    /// <c>HttpClient</c>) resolve correctly even though this <see cref="CompiledRule{TContext}"/>
    /// outlives any one scope (ADR-0002).
    /// </param>
    /// <param name="options">Per-call evaluation options, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns>The evaluation's <see cref="Decision"/>.</returns>
    public async Task<Decision> EvaluateAsync(
        TContext context,
        IServiceProvider services,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        EvaluationOptions effectiveOptions = options ?? EvaluationOptions.Default;
        if (effectiveOptions.Timeout is { } timeout)
        {
            using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            Evaluator<TContext> timedEvaluator = new(
                context,
                services,
                this.registry,
                effectiveOptions,
                timeoutSource.Token,
                this.logger
            );
            return await timedEvaluator.EvaluateAsync(this.Root).ConfigureAwait(false);
        }

        Evaluator<TContext> evaluator = new(context, services, this.registry, effectiveOptions, cancellationToken, this.logger);
        return await evaluator.EvaluateAsync(this.Root).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.CanonicalText;
    }

    private static RuleDescription DescribeNode(Expression node, PredicateRegistry<TContext> registry)
    {
        if (node is TermExpression term)
        {
            (string label, string description) = registry.TryGetSchema(term.Identity.PredicateName, out PredicateSchema? schema)
                ? (schema!.Label, schema.Description)
                : (term.Identity.PredicateName, "An unregistered predicate (CompilationMode.Lenient).");
            return new RuleDescription(label, description, []);
        }

        OperatorDescriptor descriptor = OperatorInfo.Describe(node);
        IReadOnlyList<Expression> operands =
            node is ConstantExpression ? Array.Empty<Expression>() : ExpressionShape.Of(node).Operands;

        return new RuleDescription(
            descriptor.Label,
            descriptor.Description,
            [.. operands.Select(operand => DescribeNode(operand, registry))]
        );
    }

    private static EvaluatedNode RequireEvaluatedTree(Decision decision)
    {
        return decision.EvaluatedTree
            ?? throw new ArgumentException(
                "This decision has no EvaluatedTree to render — it must come from EvaluateAsync on this same rule.",
                nameof(decision)
            );
    }
}
