namespace TruthWeaver.Evaluation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Logging;
using TruthWeaver.Metrics;
using TruthWeaver.Registry;

/// <summary>
/// Walks a compiled <see cref="Expression"/> tree once, implementing the three-valued Kleene truth
/// tables (ADR-0001), left-to-right short-circuiting, per-evaluation term memoization, and fault
/// absorption (ADR-0002). A new instance is created for every call to
/// <c>CompiledRule.EvaluateAsync</c> — memoization and the fault list are scoped to exactly one
/// evaluation, never shared across calls.
/// </summary>
/// <typeparam name="TContext">The application context type.</typeparam>
internal sealed class Evaluator<TContext>(
    TContext context,
    IServiceProvider services,
    PredicateRegistry<TContext> registry,
    EvaluationOptions options,
    CancellationToken cancellationToken,
    ILogger? logger = null
)
{
    private readonly TContext context = context;
    private readonly IServiceProvider services = services;
    private readonly PredicateRegistry<TContext> registry = registry;
    private readonly EvaluationOptions options = options;
    private readonly CancellationToken cancellationToken = cancellationToken;
    private readonly ILogger logger = logger ?? NullLogger.Instance;
    private readonly Dictionary<TermIdentity, TruthValue> memo = [];
    private readonly List<Fault> faults = [];
    private readonly List<TraceEntry> trace = [];
    private bool aborted;

    public async Task<Decision> EvaluateAsync(Expression root)
    {
        EvalResult result = await this.EvalAsync(root).ConfigureAwait(false);
        TruthWeaverMetrics.EvaluationPerformed();
        return new Decision(result.Value, this.faults, new Trace(this.trace), result.Node);
    }

    private static string Describe(Expression node)
    {
        if (node is ConstantExpression c)
        {
            return c.Value ? "true" : "false";
        }

        if (node is TermExpression t)
        {
            return t.Identity.ToString();
        }

        NodeShape shape = ExpressionShape.Of(node);
        return shape.OpName switch
        {
            "Not" => "NOT",
            "And" => "AND",
            "Or" => "OR",
            "Xor" => "XOR",
            "Xnor" => "XNOR",
            "ExactlyOne" => "ExactlyOne",
            _ => $"{shape.OpName}({shape.K})",
        };
    }

    private static TruthValue KleeneAnd(TruthValue a, TruthValue b)
    {
        return (a, b) switch
        {
            (TruthValue.False, _) => TruthValue.False,
            (_, TruthValue.False) => TruthValue.False,
            (TruthValue.True, TruthValue.True) => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }

    private static TruthValue KleeneOr(TruthValue a, TruthValue b)
    {
        return (a, b) switch
        {
            (TruthValue.True, _) => TruthValue.True,
            (_, TruthValue.True) => TruthValue.True,
            (TruthValue.False, TruthValue.False) => TruthValue.False,
            _ => TruthValue.Unknown,
        };
    }

    private static TruthValue KleeneNot(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => TruthValue.False,
            TruthValue.False => TruthValue.True,
            _ => TruthValue.Unknown,
        };
    }

    private static TruthValue KleeneXor(TruthValue left, TruthValue right)
    {
        if (left == TruthValue.Unknown || right == TruthValue.Unknown)
        {
            return TruthValue.Unknown;
        }

        return (left == TruthValue.True) ^ (right == TruthValue.True) ? TruthValue.True : TruthValue.False;
    }

    private static TruthValue KleeneXnor(TruthValue left, TruthValue right)
    {
        return KleeneNot(KleeneXor(left, right));
    }

    private static TruthValue EvaluateExactlyOne(IReadOnlyList<TruthValue> operandValues)
    {
        int trueCount = operandValues.Count(v => v == TruthValue.True);
        int unknownCount = operandValues.Count(v => v == TruthValue.Unknown);

        if (trueCount >= 2)
        {
            return TruthValue.False;
        }

        if (unknownCount > 0)
        {
            return TruthValue.Unknown;
        }

        return trueCount == 1 ? TruthValue.True : TruthValue.False;
    }

    /// <summary>
    /// Evaluates any count-threshold comparison against the range of true-operand counts still
    /// reachable given how many operands remain <see cref="TruthValue.Unknown"/> — determinate only
    /// when the comparison agrees at both the lowest and highest possible count (monotonic
    /// comparisons) or when there is no remaining ambiguity at all (<see cref="ThresholdComparison.Exactly"/>,
    /// which is not monotonic in the count).
    /// </summary>
    private static TruthValue EvaluateThreshold(ThresholdComparison comparison, int k, IReadOnlyList<TruthValue> operandValues)
    {
        int trueCount = operandValues.Count(v => v == TruthValue.True);
        int unknownCount = operandValues.Count(v => v == TruthValue.Unknown);
        int minCount = trueCount;
        int maxCount = trueCount + unknownCount;

        if (comparison == ThresholdComparison.Exactly)
        {
            if (k < minCount || k > maxCount)
            {
                return TruthValue.False;
            }

            return unknownCount == 0 ? TruthValue.True : TruthValue.Unknown;
        }

        bool SatisfiesAt(int count) =>
            comparison switch
            {
                ThresholdComparison.AtLeast => count >= k,
                ThresholdComparison.AtMost => count <= k,
                ThresholdComparison.GreaterThan => count > k,
                ThresholdComparison.LessThan => count < k,
                _ => throw new InvalidOperationException($"Unhandled threshold comparison '{comparison}'."),
            };

        bool satisfiesMin = SatisfiesAt(minCount);
        bool satisfiesMax = SatisfiesAt(maxCount);
        if (satisfiesMin && satisfiesMax)
        {
            return TruthValue.True;
        }

        return !satisfiesMin && !satisfiesMax ? TruthValue.False : TruthValue.Unknown;
    }

    private async ValueTask<EvalResult> EvalAsync(Expression node)
    {
        if (this.aborted)
        {
            string skippedDescription = Describe(node);
            this.trace.Add(new TraceEntry(skippedDescription, null, true));
            return new EvalResult(TruthValue.Unknown, new EvaluatedNode(skippedDescription, null, true, []));
        }

        this.cancellationToken.ThrowIfCancellationRequested();

        switch (node)
        {
            case ConstantExpression c:
                TruthValue constantValue = c.Value ? TruthValue.True : TruthValue.False;
                string constantDescription = Describe(node);
                this.trace.Add(new TraceEntry(constantDescription, constantValue, false));
                return new EvalResult(constantValue, new EvaluatedNode(constantDescription, constantValue, false, []));
            case TermExpression t:
                return await this.EvalTermAsync(t).ConfigureAwait(false);
            case NotExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                EvalResult operand = await this.EvalAsync(shape.Operands[0]).ConfigureAwait(false);
                TruthValue value = KleeneNot(operand.Value);
                return new EvalResult(value, new EvaluatedNode("NOT", value, false, [operand.Node]));
            }

            case AndExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                return await this.EvalChainAsync("AND", shape.Operands, TruthValue.True, KleeneAnd, stopValue: TruthValue.False)
                    .ConfigureAwait(false);
            }

            case OrExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                return await this.EvalChainAsync("OR", shape.Operands, TruthValue.False, KleeneOr, stopValue: TruthValue.True)
                    .ConfigureAwait(false);
            }

            case XorExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = KleeneXor(results[0].Value, results[1].Value);
                return new EvalResult(value, new EvaluatedNode("XOR", value, false, [.. results.Select(r => r.Node)]));
            }

            case XnorExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = KleeneXnor(results[0].Value, results[1].Value);
                return new EvalResult(value, new EvaluatedNode("XNOR", value, false, [.. results.Select(r => r.Node)]));
            }

            case ExactlyOneExpression:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = EvaluateExactlyOne([.. results.Select(r => r.Value)]);
                return new EvalResult(value, new EvaluatedNode("ExactlyOne", value, false, [.. results.Select(r => r.Node)]));
            }

            case ThresholdExpression th:
            {
                NodeShape shape = ExpressionShape.Of(node);
                IReadOnlyList<EvalResult> results = await this.EvalAllAsync(shape.Operands).ConfigureAwait(false);
                TruthValue value = EvaluateThreshold(th.Comparison, th.K, [.. results.Select(r => r.Value)]);
                string description = $"{shape.OpName}({shape.K})";
                return new EvalResult(value, new EvaluatedNode(description, value, false, [.. results.Select(r => r.Node)]));
            }

            default:
                throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'.");
        }
    }

    private async ValueTask<EvalResult> EvalChainAsync(
        string description,
        IReadOnlyList<Expression> operands,
        TruthValue identity,
        Func<TruthValue, TruthValue, TruthValue> combine,
        TruthValue stopValue
    )
    {
        TruthValue accumulator = identity;
        List<EvaluatedNode> children = new(operands.Count);
        bool exhaustive = this.options.Mode == EvaluationMode.Exhaustive;
        bool stop = false;
        foreach (Expression operand in operands)
        {
            if (stop || this.aborted)
            {
                string skippedDescription = Describe(operand);
                this.trace.Add(new TraceEntry(skippedDescription, null, true));
                children.Add(new EvaluatedNode(skippedDescription, null, true, []));
                continue;
            }

            EvalResult result = await this.EvalAsync(operand).ConfigureAwait(false);
            children.Add(result.Node);
            accumulator = combine(accumulator, result.Value);
            if (!exhaustive && result.Value == stopValue)
            {
                stop = true;
            }
        }

        return new EvalResult(accumulator, new EvaluatedNode(description, accumulator, false, children));
    }

    private async ValueTask<IReadOnlyList<EvalResult>> EvalAllAsync(IReadOnlyList<Expression> operands)
    {
        List<EvalResult> values = new(operands.Count);
        foreach (Expression operand in operands)
        {
            values.Add(await this.EvalAsync(operand).ConfigureAwait(false));
        }

        return values;
    }

    private async ValueTask<EvalResult> EvalTermAsync(TermExpression term)
    {
        string description = term.Identity.ToString();
        if (term.IsUnknownPredicate)
        {
            this.trace.Add(new TraceEntry(description, TruthValue.Unknown, false));
            return new EvalResult(TruthValue.Unknown, new EvaluatedNode(description, TruthValue.Unknown, false, []));
        }

        if (this.memo.TryGetValue(term.Identity, out TruthValue cached))
        {
            this.trace.Add(new TraceEntry(description, cached, false));
            return new EvalResult(cached, new EvaluatedNode(description, cached, false, []));
        }

        TruthValue result = await this.InvokeAsync(term.Identity).ConfigureAwait(false);
        this.memo[term.Identity] = result;
        this.trace.Add(new TraceEntry(description, result, false));
        return new EvalResult(result, new EvaluatedNode(description, result, false, []));
    }

    private async ValueTask<TruthValue> InvokeAsync(TermIdentity identity)
    {
        if (!this.registry.TryGet(identity.PredicateName, out PredicateDescriptor<TContext>? descriptor) || descriptor is null)
        {
            // Defensive only: a successfully Strict-mode-compiled rule cannot reference an
            // unregistered predicate, so this path is unreachable in practice.
            return TruthValue.Unknown;
        }

        try
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            PredicateArguments args = new(identity.Arguments.ToDictionary(kv => kv.Key, kv => kv.Value));
            bool value = descriptor.Evaluate is { } lambda
                ? await lambda(this.context, args, this.cancellationToken).ConfigureAwait(false)
                : await this.InvokeClassBasedAsync(descriptor, args).ConfigureAwait(false);
            return value ? TruthValue.True : TruthValue.False;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !this.cancellationToken.IsCancellationRequested)
        {
            this.faults.Add(new Fault(identity, ex));
            EvaluationLog.PredicateFaulted(this.logger, identity.ToString(), ex.Message, ex);
            TruthWeaverMetrics.FaultRecorded();

            // Ticket 11's acceptance criteria (FaultBudget = 1 tolerates the first fault and aborts
            // on the second) takes precedence over ADR-0002's own prose example (which reads as
            // "budget = 1 aborts on the first fault") — the ticket is the more operationally precise
            // of the two, so a fault count strictly greater than the budget is what triggers an abort.
            if (this.options.FaultBudget is { } budget && this.faults.Count > budget)
            {
                this.aborted = true;
            }

            return TruthValue.Unknown;
        }
    }

    private ValueTask<bool> InvokeClassBasedAsync(PredicateDescriptor<TContext> descriptor, PredicateArguments args)
    {
        object? instance =
            this.services.GetService(descriptor.ImplementationType!)
            ?? throw new InvalidOperationException(
                $"No service is registered for predicate implementation type '{descriptor.ImplementationType}'. Register it with the application's IServiceProvider."
            );
        IPredicate<TContext> predicate = (IPredicate<TContext>)instance;
        return predicate.EvaluateAsync(this.context, args, this.cancellationToken);
    }

    /// <summary>
    /// One node's outcome, paired with a structural <see cref="EvaluatedNode"/> mirroring the shape
    /// <see cref="Expression"/> is recursed over — every recursive evaluation step returns one of these
    /// instead of a bare <see cref="TruthValue"/>, so the per-node annotations needed for
    /// <see cref="Decision.EvaluatedTree"/> fall out of the existing recursion for free, with no
    /// separate replay pass.
    /// </summary>
    private readonly record struct EvalResult(TruthValue Value, EvaluatedNode Node);
}
