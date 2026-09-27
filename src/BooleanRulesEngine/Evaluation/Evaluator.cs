namespace BooleanRulesEngine.Evaluation;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Ast;
using BooleanRulesEngine.Logging;
using BooleanRulesEngine.Registry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        TruthValue result = await this.EvalAsync(root).ConfigureAwait(false);
        return new Decision(result, this.faults, new Trace(this.trace));
    }

    private static string Describe(Expression node)
    {
        return node switch
        {
            ConstantExpression c => c.Value ? "true" : "false",
            TermExpression t => t.Identity.ToString(),
            NotExpression => "NOT",
            AndExpression => "AND",
            OrExpression => "OR",
            XorExpression => "XOR",
            XnorExpression => "XNOR",
            ExactlyOneExpression => "ExactlyOne",
            ThresholdExpression th => $"{th.Comparison}({th.K})",
            _ => node.GetType().Name,
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

    private async ValueTask<TruthValue> EvalAsync(Expression node)
    {
        if (this.aborted)
        {
            this.trace.Add(new TraceEntry(Describe(node), null, true));
            return TruthValue.Unknown;
        }

        this.cancellationToken.ThrowIfCancellationRequested();

        switch (node)
        {
            case ConstantExpression c:
                TruthValue constantValue = c.Value ? TruthValue.True : TruthValue.False;
                this.trace.Add(new TraceEntry(Describe(node), constantValue, false));
                return constantValue;
            case TermExpression t:
                return await this.EvalTermAsync(t).ConfigureAwait(false);
            case NotExpression n:
                return KleeneNot(await this.EvalAsync(n.Operand).ConfigureAwait(false));
            case AndExpression a:
                return await this.EvalChainAsync(a.Operands, TruthValue.True, KleeneAnd, stopValue: TruthValue.False)
                    .ConfigureAwait(false);
            case OrExpression o:
                return await this.EvalChainAsync(o.Operands, TruthValue.False, KleeneOr, stopValue: TruthValue.True)
                    .ConfigureAwait(false);
            case XorExpression x:
                return KleeneXor(
                    await this.EvalAsync(x.Left).ConfigureAwait(false),
                    await this.EvalAsync(x.Right).ConfigureAwait(false)
                );
            case XnorExpression xn:
                return KleeneXnor(
                    await this.EvalAsync(xn.Left).ConfigureAwait(false),
                    await this.EvalAsync(xn.Right).ConfigureAwait(false)
                );
            case ExactlyOneExpression e:
                return EvaluateExactlyOne(await this.EvalAllAsync(e.Operands).ConfigureAwait(false));
            case ThresholdExpression th:
                return EvaluateThreshold(th.Comparison, th.K, await this.EvalAllAsync(th.Operands).ConfigureAwait(false));
            default:
                throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'.");
        }
    }

    private async ValueTask<TruthValue> EvalChainAsync(
        EquatableArray<Expression> operands,
        TruthValue identity,
        Func<TruthValue, TruthValue, TruthValue> combine,
        TruthValue stopValue
    )
    {
        TruthValue accumulator = identity;
        bool exhaustive = this.options.Mode == EvaluationMode.Exhaustive;
        bool stop = false;
        foreach (Expression operand in operands)
        {
            if (stop || this.aborted)
            {
                this.trace.Add(new TraceEntry(Describe(operand), null, true));
                continue;
            }

            TruthValue value = await this.EvalAsync(operand).ConfigureAwait(false);
            accumulator = combine(accumulator, value);
            if (!exhaustive && value == stopValue)
            {
                stop = true;
            }
        }

        return accumulator;
    }

    private async ValueTask<IReadOnlyList<TruthValue>> EvalAllAsync(EquatableArray<Expression> operands)
    {
        List<TruthValue> values = new(operands.Count);
        foreach (Expression operand in operands)
        {
            values.Add(await this.EvalAsync(operand).ConfigureAwait(false));
        }

        return values;
    }

    private async ValueTask<TruthValue> EvalTermAsync(TermExpression term)
    {
        if (term.IsUnknownPredicate)
        {
            this.trace.Add(new TraceEntry(term.Identity.ToString(), TruthValue.Unknown, false));
            return TruthValue.Unknown;
        }

        if (this.memo.TryGetValue(term.Identity, out TruthValue cached))
        {
            this.trace.Add(new TraceEntry(term.Identity.ToString(), cached, false));
            return cached;
        }

        TruthValue result = await this.InvokeAsync(term.Identity).ConfigureAwait(false);
        this.memo[term.Identity] = result;
        this.trace.Add(new TraceEntry(term.Identity.ToString(), result, false));
        return result;
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
}
