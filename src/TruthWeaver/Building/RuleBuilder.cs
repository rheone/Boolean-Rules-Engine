namespace TruthWeaver.Building;

using System.Text.Json.Nodes;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;

/// <summary>
/// A fluent, programmatic way to assemble a rule without hand-writing DSL/JSON/YAML text —
/// useful when a rule's shape comes from application logic (e.g. a dynamically built list of
/// conditions) rather than from an author typing it directly. A builder tree renders to the exact
/// flat, key-discriminated JSON tree shape ADR-0003 defines and compiles through
/// <see cref="RuleCompiler{TContext}.CompileJson(string)"/>, so a builder-assembled rule receives
/// every diagnostic a hand-written one would (unknown predicate, bad argument, invalid threshold,
/// XOR/EQUIVALENT arity, resource limits, structural tautology/contradiction) — nothing here bypasses the
/// Validate/Analyze stages of the compilation pipeline.
/// </summary>
public abstract class RuleBuilder
{
    private RuleBuilder() { }

    /// <summary>Creates a builder for the literal <c>True</c>/<c>False</c> constant.</summary>
    /// <param name="value">The constant's value.</param>
    /// <returns>A builder for the constant.</returns>
    public static RuleBuilder Constant(bool value)
    {
        return new ConstantBuilder(value ? TruthValue.True : TruthValue.False);
    }

    /// <summary>Creates a builder for a literal K3 constant, including <see cref="TruthValue.Unknown"/>.</summary>
    /// <param name="value">The constant's value.</param>
    /// <returns>A builder for the constant.</returns>
    public static RuleBuilder Constant(TruthValue value)
    {
        return new ConstantBuilder(value);
    }

    /// <summary>Creates a builder for a zero-argument predicate reference.</summary>
    /// <param name="name">The predicate's registered name.</param>
    /// <returns>A builder for the term.</returns>
    public static RuleBuilder Predicate(string name)
    {
        return new PredicateBuilder(name, []);
    }

    /// <summary>Creates a builder for a predicate reference with named arguments.</summary>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="arguments">
    /// The named argument values. Supported value types are <see cref="string"/>, <see cref="bool"/>,
    /// <see cref="int"/>, <see cref="long"/>, <see cref="double"/>, <see cref="decimal"/>,
    /// <see cref="DateTimeOffset"/>, and <see cref="IEnumerable{T}"/> of any of those (for an
    /// array-valued argument).
    /// </param>
    /// <returns>A builder for the term.</returns>
    public static RuleBuilder Predicate(string name, params (string Name, object Value)[] arguments)
    {
        return new PredicateBuilder(name, arguments);
    }

    /// <summary>Creates a builder for logical conjunction.</summary>
    /// <param name="operands">The conjuncts (at least two).</param>
    /// <returns>A builder for the <c>AND</c> expression.</returns>
    public static RuleBuilder And(params RuleBuilder[] operands)
    {
        return new OperatorBuilder("and", operands);
    }

    /// <summary>Creates a builder for logical disjunction.</summary>
    /// <param name="operands">The disjuncts (at least two).</param>
    /// <returns>A builder for the <c>OR</c> expression.</returns>
    public static RuleBuilder Or(params RuleBuilder[] operands)
    {
        return new OperatorBuilder("or", operands);
    }

    /// <summary>Creates a builder for logical negation.</summary>
    /// <param name="operand">The negated sub-expression.</param>
    /// <returns>A builder for the <c>NOT</c> expression.</returns>
    public static RuleBuilder Not(RuleBuilder operand)
    {
        return new OperatorBuilder("not", [operand]);
    }

    /// <summary>Creates a builder for binary exclusive-or.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>A builder for the <c>XOR</c> expression.</returns>
    public static RuleBuilder Xor(RuleBuilder left, RuleBuilder right)
    {
        return new OperatorBuilder("xor", [left, right]);
    }

    /// <summary>Creates a builder for the binary logical biconditional (<c>EQUIVALENT</c>, also written <c>IFF</c> or <c>↔</c>).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>A builder for the <c>EQUIVALENT</c> expression.</returns>
    public static RuleBuilder Equivalent(RuleBuilder left, RuleBuilder right)
    {
        return new OperatorBuilder("equivalent", [left, right]);
    }

    /// <summary>
    /// Creates a builder for the biconditional under its pre-ADR-0005 name. Forwards to <see cref="Equivalent"/>
    /// (same rule, same canonical text); kept so existing callers keep compiling.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>A builder for the <c>EQUIVALENT</c> expression.</returns>
    public static RuleBuilder Xnor(RuleBuilder left, RuleBuilder right)
    {
        return Equivalent(left, right);
    }

    /// <summary>Creates a builder for material implication (<c>NOT antecedent OR consequent</c>).</summary>
    /// <param name="antecedent">The "if" operand.</param>
    /// <param name="consequent">The "then" operand.</param>
    /// <returns>A builder for the <c>IMPLIES</c> expression.</returns>
    public static RuleBuilder Implies(RuleBuilder antecedent, RuleBuilder consequent)
    {
        return new OperatorBuilder("implies", [antecedent, consequent]);
    }

    /// <summary>Creates a builder for negated conjunction (<c>NOT (left AND right)</c>, also written <c>NAND</c> or <c>↑</c>).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>A builder for the <c>NAND</c> expression.</returns>
    public static RuleBuilder Nand(RuleBuilder left, RuleBuilder right)
    {
        return new OperatorBuilder("nand", [left, right]);
    }

    /// <summary>Creates a builder for negated disjunction (<c>NOT (left OR right)</c>, also written <c>NOR</c> or <c>↓</c>).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>A builder for the <c>NOR</c> expression.</returns>
    public static RuleBuilder Nor(RuleBuilder left, RuleBuilder right)
    {
        return new OperatorBuilder("nor", [left, right]);
    }

    /// <summary>Creates a builder for n-ary parity (<c>NXOR(a, b, ...)</c>): <c>Unknown</c> if any operand is <c>Unknown</c>, otherwise <c>True</c> for an odd number of <c>True</c> operands.</summary>
    /// <param name="operands">The operands (at least two).</param>
    /// <returns>A builder for the <c>NXOR</c> expression.</returns>
    public static RuleBuilder Nxor(params RuleBuilder[] operands)
    {
        return new OperatorBuilder("nxor", operands);
    }

    /// <summary>Creates a builder for <c>ANY(...)</c>: at least one operand is true (<c>AtLeast(1, ...)</c>).</summary>
    /// <param name="operands">The operands (at least two).</param>
    /// <returns>A builder for the <c>ANY</c> expression.</returns>
    public static RuleBuilder Any(params RuleBuilder[] operands)
    {
        return new OperatorBuilder("any", operands);
    }

    /// <summary>Creates a builder for <c>ALL(...)</c>: every operand is true (<c>AtLeast(n, ...)</c>).</summary>
    /// <param name="operands">The operands (at least two).</param>
    /// <returns>A builder for the <c>ALL</c> expression.</returns>
    public static RuleBuilder All(params RuleBuilder[] operands)
    {
        return new OperatorBuilder("all", operands);
    }

    /// <summary>Creates a builder for <c>NONE(...)</c>: no operand is true (<c>AtMost(0, ...)</c>).</summary>
    /// <param name="operands">The operands (at least two).</param>
    /// <returns>A builder for the <c>NONE</c> expression.</returns>
    public static RuleBuilder None(params RuleBuilder[] operands)
    {
        return new OperatorBuilder("none", operands);
    }

    /// <summary>Creates a builder for the n-ary "exactly one of these is true" operator.</summary>
    /// <param name="operands">The operands (at least two).</param>
    /// <returns>A builder for the <c>ExactlyOne</c> expression.</returns>
    public static RuleBuilder ExactlyOne(params RuleBuilder[] operands)
    {
        return new OperatorBuilder("exactlyOne", operands);
    }

    /// <summary>
    /// Creates a builder for <c>COALESCE(...)</c>: the first operand that is not <c>Unknown</c> (<c>True</c> and
    /// <c>False</c> pass through).
    /// </summary>
    /// <param name="operands">The operands in priority order (at least two).</param>
    /// <returns>A builder for the <c>COALESCE</c> expression.</returns>
    public static RuleBuilder Coalesce(params RuleBuilder[] operands)
    {
        return new OperatorBuilder("coalesce", operands);
    }

    /// <summary>
    /// Creates a builder for <c>IsTrue(operand)</c>: <c>True</c> iff the operand is <c>True</c>, otherwise <c>False</c>
    /// (never <c>Unknown</c>).
    /// </summary>
    /// <param name="operand">The expression to inspect.</param>
    /// <returns>A builder for the <c>IsTrue</c> expression.</returns>
    public static RuleBuilder IsTrue(RuleBuilder operand)
    {
        return new OperatorBuilder("isTrue", [operand]);
    }

    /// <summary>
    /// Creates a builder for <c>IsFalse(operand)</c>: <c>True</c> iff the operand is <c>False</c>, otherwise <c>False</c>
    /// (never <c>Unknown</c>).
    /// </summary>
    /// <param name="operand">The expression to inspect.</param>
    /// <returns>A builder for the <c>IsFalse</c> expression.</returns>
    public static RuleBuilder IsFalse(RuleBuilder operand)
    {
        return new OperatorBuilder("isFalse", [operand]);
    }

    /// <summary>
    /// Creates a builder for <c>IsUnknown(operand)</c>: <c>True</c> iff the operand is <c>Unknown</c>, otherwise <c>False</c>
    /// (never <c>Unknown</c>).
    /// </summary>
    /// <param name="operand">The expression to inspect.</param>
    /// <returns>A builder for the <c>IsUnknown</c> expression.</returns>
    public static RuleBuilder IsUnknown(RuleBuilder operand)
    {
        return new OperatorBuilder("isUnknown", [operand]);
    }

    /// <summary>
    /// Creates a builder for <c>IsKnown(operand)</c>: <c>True</c> iff the operand is <c>True</c> or <c>False</c>, otherwise
    /// <c>False</c> (never <c>Unknown</c>).
    /// </summary>
    /// <param name="operand">The expression to inspect.</param>
    /// <returns>A builder for the <c>IsKnown</c> expression.</returns>
    public static RuleBuilder IsKnown(RuleBuilder operand)
    {
        return new OperatorBuilder("isKnown", [operand]);
    }

    /// <summary>
    /// Creates a builder for <c>If(condition, whenTrue, whenFalse)</c>: <paramref name="whenTrue"/> when the condition is
    /// <c>True</c>, <paramref name="whenFalse"/> when it is <c>False</c>, and for an <c>Unknown</c> condition the branch value
    /// only if both branches are the same definite value, otherwise <c>Unknown</c>.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <param name="whenTrue">The result when the condition is <c>True</c>.</param>
    /// <param name="whenFalse">The result when the condition is <c>False</c>.</param>
    /// <returns>A builder for the <c>If</c> expression.</returns>
    public static RuleBuilder If(RuleBuilder condition, RuleBuilder whenTrue, RuleBuilder whenFalse)
    {
        return new OperatorBuilder("if", [condition, whenTrue, whenFalse]);
    }

    /// <summary>
    /// Creates a builder for <c>BETWEEN(min, max, ...)</c>: the number of true operands lies in the inclusive range
    /// <c>[min, max]</c> (<c>AtLeast(min, ...) AND AtMost(max, ...)</c>).
    /// </summary>
    /// <param name="min">The inclusive lower bound (at least 0).</param>
    /// <param name="max">The inclusive upper bound (at least <paramref name="min"/>, at most the operand count).</param>
    /// <param name="operands">The operands (at least two).</param>
    /// <returns>A builder for the <c>BETWEEN</c> expression.</returns>
    public static RuleBuilder Between(int min, int max, params RuleBuilder[] operands)
    {
        return new BetweenBuilder(min, max, operands);
    }

    /// <summary>Creates a builder for "at least <paramref name="k"/> of these operands are true".</summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>AtLeast</c> expression.</returns>
    public static RuleBuilder AtLeast(int k, params RuleBuilder[] operands)
    {
        return new ThresholdBuilder("atLeast", k, operands);
    }

    /// <summary>Creates a builder for "at most <paramref name="k"/> of these operands are true".</summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>AtMost</c> expression.</returns>
    public static RuleBuilder AtMost(int k, params RuleBuilder[] operands)
    {
        return new ThresholdBuilder("atMost", k, operands);
    }

    /// <summary>Creates a builder for "more than <paramref name="k"/> of these operands are true".</summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>GreaterThan</c> expression.</returns>
    public static RuleBuilder GreaterThan(int k, params RuleBuilder[] operands)
    {
        return new ThresholdBuilder("greaterThan", k, operands);
    }

    /// <summary>Creates a builder for "fewer than <paramref name="k"/> of these operands are true".</summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>LessThan</c> expression.</returns>
    public static RuleBuilder LessThan(int k, params RuleBuilder[] operands)
    {
        return new ThresholdBuilder("lessThan", k, operands);
    }

    /// <summary>Creates a builder for "exactly <paramref name="k"/> of these operands are true".</summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>Exactly</c> expression.</returns>
    public static RuleBuilder Exactly(int k, params RuleBuilder[] operands)
    {
        return new ThresholdBuilder("exactly", k, operands);
    }

    /// <summary>Renders this builder's tree to the flat JSON tree shape text (ADR-0003).</summary>
    /// <returns>The JSON text.</returns>
    public string ToJson()
    {
        return this.ToNode().ToJsonString();
    }

    /// <summary>
    /// Compiles this builder's tree through the exact same Validate → Analyze → Build pipeline any
    /// other rule source (DSL/JSON/YAML text) uses.
    /// </summary>
    /// <typeparam name="TContext">The application context type the compiler's registry evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate and build this tree with.</param>
    /// <returns>The compilation result.</returns>
    public CompilationResult<TContext> Compile<TContext>(RuleCompiler<TContext> compiler)
    {
        return compiler.CompileJson(this.ToJson());
    }

    private protected abstract JsonNode ToNode();

    private static JsonNode ValueToNode(object value)
    {
        return value switch
        {
            string s => JsonValue.Create(s),
            bool b => JsonValue.Create(b),
            int i => JsonValue.Create(i),
            long l => JsonValue.Create(l),
            double d => JsonValue.Create(d),
            decimal m => JsonValue.Create(m),
            DateTimeOffset dto => JsonValue.Create(dto.ToString("O", CultureInfo.InvariantCulture)),
            IEnumerable<object> items => ArrayToNode(items),
            _ => throw new ArgumentException($"Unsupported argument value type '{value.GetType()}'.", nameof(value)),
        };
    }

    private static JsonArray ArrayToNode(IEnumerable<object> items)
    {
        JsonArray array = [];
        foreach (object item in items)
        {
            array.Add(ValueToNode(item));
        }

        return array;
    }

    private static JsonArray OperandsNode(IReadOnlyList<RuleBuilder> operands)
    {
        JsonArray array = [];
        foreach (RuleBuilder operand in operands)
        {
            array.Add(operand.ToNode());
        }

        return array;
    }

    private sealed class ConstantBuilder(TruthValue value) : RuleBuilder
    {
        private readonly TruthValue value = value;

        private protected override JsonNode ToNode()
        {
            // Same shape JsonTreePrinter writes: booleans for True/False, the string "unknown" otherwise.
            return this.value switch
            {
                TruthValue.True => new JsonObject { ["const"] = true },
                TruthValue.False => new JsonObject { ["const"] = false },
                _ => new JsonObject { ["const"] = TruthValueText.TreeFormat(this.value) },
            };
        }
    }

    private sealed class PredicateBuilder(string name, (string Name, object Value)[] arguments) : RuleBuilder
    {
        private readonly string name = name;
        private readonly (string Name, object Value)[] arguments = arguments;

        private protected override JsonNode ToNode()
        {
            JsonObject node = new() { ["predicate"] = this.name };
            if (this.arguments.Length > 0)
            {
                JsonObject args = [];
                foreach ((string argName, object argValue) in this.arguments)
                {
                    args[argName] = ValueToNode(argValue);
                }

                node["args"] = args;
            }

            return node;
        }
    }

    private sealed class OperatorBuilder(string op, IReadOnlyList<RuleBuilder> operands) : RuleBuilder
    {
        private readonly string op = op;
        private readonly IReadOnlyList<RuleBuilder> operands = operands;

        private protected override JsonNode ToNode()
        {
            return new JsonObject { ["op"] = this.op, ["operands"] = OperandsNode(this.operands) };
        }
    }

    private sealed class BetweenBuilder(int min, int max, IReadOnlyList<RuleBuilder> operands) : RuleBuilder
    {
        private readonly int min = min;
        private readonly int max = max;
        private readonly IReadOnlyList<RuleBuilder> operands = operands;

        private protected override JsonNode ToNode()
        {
            return new JsonObject
            {
                ["op"] = "between",
                ["min"] = this.min,
                ["max"] = this.max,
                ["operands"] = OperandsNode(this.operands),
            };
        }
    }

    private sealed class ThresholdBuilder(string op, int k, IReadOnlyList<RuleBuilder> operands) : RuleBuilder
    {
        private readonly string op = op;
        private readonly int k = k;
        private readonly IReadOnlyList<RuleBuilder> operands = operands;

        private protected override JsonNode ToNode()
        {
            return new JsonObject
            {
                ["op"] = this.op,
                ["k"] = this.k,
                ["operands"] = OperandsNode(this.operands),
            };
        }
    }
}
