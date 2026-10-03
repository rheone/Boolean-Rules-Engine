namespace TruthWeaver.Evaluation;

using System.Text;
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

    internal CompiledRule(
        Expression root,
        PredicateRegistry<TContext> registry,
        ILogger? logger = null,
        CollapsePolicy? collapsePolicy = null
    )
    {
        this.Root = root;
        this.registry = registry;
        this.logger = logger ?? NullLogger.Instance;
        this.CollapsePolicy = collapsePolicy;
        this.canonicalText = new Lazy<string>(() => CanonicalPrinter.Print(this.Root, this.CollapsePolicy));
    }

    /// <summary>
    /// Gets the policy of the outermost <c>Collapse(expr, policy)</c> this rule was written with, or
    /// <see langword="null"/> if it declared none (ADR-0005 decision 14). When set, <see cref="EvaluateAsync"/> applies it
    /// to produce <see cref="Decision.Outcome"/> (and, for the two lenient policies, a definite <see cref="Decision.Result"/>).
    /// A policy is not part of the expression tree: it is the evaluation boundary around it, so the analyzer and the
    /// operators see only the inner expression, while <see cref="Describe"/> and the evaluated tree show the boundary as
    /// their root.
    /// </summary>
    public CollapsePolicy? CollapsePolicy { get; }

    /// <summary>Gets this rule's canonical printed DSL text — the form <c>RuleCompiler.Compile</c> reproduces a structurally equal tree from.</summary>
    public string CanonicalText => this.canonicalText.Value;

    /// <summary>
    /// Gets the underlying expression tree. Internal — visible to <c>TruthWeaver.Yaml</c> via
    /// <c>InternalsVisibleTo</c>, so its YAML printer can render the same tree <see cref="CanonicalText"/>
    /// and <see cref="PrintJson"/> render, without this package needing to know YAML exists.
    /// </summary>
    internal Expression Root { get; }

    /// <summary>Prints this rule to the flat, key-discriminated JSON tree shape (ADR-0003).</summary>
    /// <returns>The JSON text.</returns>
    public string PrintJson()
    {
        return JsonTreePrinter.Print(this.Root, this.CollapsePolicy);
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
        RuleDescription inner = DescribeNode(this.Root, this.registry);
        if (this.CollapsePolicy is not { } policy)
        {
            return inner;
        }

        // A declared collapse is the root of the described tree, so it mirrors the evaluated tree EvaluateAsync returns.
        OperatorDescriptor boundary = OperatorInfo.DescribeCollapse(policy);
        return new RuleDescription(boundary.Label, boundary.Description, [inner]);
    }

    /// <summary>Renders this rule's structure as Mermaid <c>flowchart</c> text, for a diagram UI.</summary>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public string PrintMermaid(bool showArgumentValues = true)
    {
        return MermaidTreePrinter.Print(this.Describe(), showArgumentValues: showArgumentValues);
    }

    /// <summary>
    /// Renders this rule's structure as Mermaid <c>flowchart</c> text, colored by one evaluation's
    /// result and short-circuit path.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.EvaluatedTree"/>.</exception>
    public string PrintMermaid(Decision decision, bool showArgumentValues = true)
    {
        return MermaidTreePrinter.Print(
            this.Describe(),
            RequireEvaluatedTree(decision),
            showArgumentValues: showArgumentValues
        );
    }

    /// <summary>Renders this rule's structure as an indented plain-text tree.</summary>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <returns>The indented tree text.</returns>
    public string PrintPlainText(bool showArgumentValues = true)
    {
        return PlainTextTreePrinter.Print(this.Describe(), showArgumentValues: showArgumentValues);
    }

    /// <summary>
    /// Renders this rule's structure as an indented plain-text tree, annotated by one evaluation's
    /// result and short-circuit path.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <returns>The indented tree text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.EvaluatedTree"/>.</exception>
    public string PrintPlainText(Decision decision, bool showArgumentValues = true)
    {
        return PlainTextTreePrinter.Print(
            this.Describe(),
            RequireEvaluatedTree(decision),
            showArgumentValues: showArgumentValues
        );
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
            return this.ApplyCollapse(await timedEvaluator.EvaluateAsync(this.Root).ConfigureAwait(false));
        }

        Evaluator<TContext> evaluator = new(context, services, this.registry, effectiveOptions, cancellationToken, this.logger);
        return this.ApplyCollapse(await evaluator.EvaluateAsync(this.Root).ConfigureAwait(false));
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
            return new RuleDescription(label, description, [], ArgumentText(term.Identity));
        }

        OperatorDescriptor descriptor = OperatorInfo.Describe(node);
        IReadOnlyList<Expression> operands = (node is ConstantExpression) ? [] : ExpressionShape.Of(node).Operands;

        return new RuleDescription(
            descriptor.Label,
            descriptor.Description,
            [.. operands.Select(operand => DescribeNode(operand, registry))]
        );
    }

    /// <summary>
    /// Renders a term's rule-text arguments as comma-joined <c>name: value</c> pairs, matching the
    /// per-argument formatting <see cref="TermIdentity.ToString"/> uses for its parenthesized part, but
    /// without repeating the predicate name — that comes from the term's own
    /// <see cref="RuleDescription.Label"/> instead.
    /// </summary>
    /// <param name="identity">The term's identity.</param>
    /// <returns>The joined argument text, or <see langword="null"/> for a zero-argument term.</returns>
    private static string? ArgumentText(TermIdentity identity)
    {
        if (identity.Arguments.Count == 0)
        {
            return null;
        }

        StringBuilder builder = new();
        for (int i = 0; i < identity.Arguments.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            KeyValuePair<string, LiteralValue> argument = identity.Arguments[i];
            builder.Append(argument.Key).Append(": ").Append(argument.Value);
        }

        return builder.ToString();
    }

    private static EvaluatedNode RequireEvaluatedTree(Decision decision)
    {
        return decision.EvaluatedTree
            ?? throw new ArgumentException(
                "This decision has no EvaluatedTree to render — it must come from EvaluateAsync on this same rule.",
                nameof(decision)
            );
    }

    /// <summary>
    /// Applies this rule's declared collapse, if any, to a finished evaluation. The inner result stays reachable as the
    /// single child of the wrapped evaluated tree, faults are never touched (a rejected outcome is not a fault), and
    /// <see cref="CollapseOutcome.RejectedUnresolved"/> leaves <see cref="Decision.Result"/> as the three-valued
    /// <see cref="TruthValue.Unknown"/> so <see cref="Decision.IsSatisfied"/> stays fail-closed.
    /// </summary>
    private Decision ApplyCollapse(Decision decision)
    {
        if (this.CollapsePolicy is not { } policy)
        {
            return decision;
        }

        CollapseOutcome outcome = decision.Collapse(policy);
        TruthValue result = outcome switch
        {
            CollapseOutcome.True => TruthValue.True,
            CollapseOutcome.False => TruthValue.False,
            _ => TruthValue.Unknown,
        };

        EvaluatedNode? tree = decision.EvaluatedTree is { } inner
            ? new EvaluatedNode(OperatorInfo.DescribeCollapse(policy).Label, result, false, [inner])
            : null;
        return decision with { Result = result, EvaluatedTree = tree, Outcome = outcome };
    }
}
