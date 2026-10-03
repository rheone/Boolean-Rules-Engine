namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Abstractions;

/// <summary>
/// Generates random rules over terms <c>a</c>, <c>b</c>, <c>c</c> and the three constants together with an
/// expected evaluation built only from <see cref="K3Oracle"/>. Shared by the analyzer-versus-oracle and the
/// rewrite (expansion) property tests so every operator is exercised by one generator.
/// </summary>
public static class K3RuleGenerator
{
    private static readonly (string Name, Func<int, int, bool> Satisfies)[] Thresholds =
    [
        ("AtLeast", (count, k) => count >= k),
        ("AtMost", (count, k) => count <= k),
        ("GreaterThan", (count, k) => count > k),
        ("LessThan", (count, k) => count < k),
        ("Exactly", (count, k) => count == k),
    ];

    private static readonly (string Name, Func<TruthValue, TruthValue> Inspect)[] Inspections =
    [
        ("IsTrue", K3Oracle.IsTrue),
        ("IsFalse", K3Oracle.IsFalse),
        ("IsUnknown", K3Oracle.IsUnknown),
        ("IsKnown", K3Oracle.IsKnown),
    ];

    /// <summary>Enumerates <paramref name="rule"/> and every descendant, parents before children.</summary>
    /// <param name="rule">The root rule.</param>
    /// <returns>All sub-rules.</returns>
    public static IEnumerable<GeneratedRule> Subtrees(GeneratedRule rule)
    {
        yield return rule;
        foreach (GeneratedRule child in rule.Children)
        {
            foreach (GeneratedRule descendant in Subtrees(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Generates a random rule over terms <c>a</c>, <c>b</c>, <c>c</c> and the three constants, together with an
    /// evaluation built only from <see cref="K3Oracle"/>. Compound nodes are always parenthesised so the
    /// no-implicit-mixing rule cannot reject them.
    /// </summary>
    /// <param name="random">The (seeded) random source.</param>
    /// <param name="depth">The maximum nesting depth.</param>
    /// <returns>The generated rule.</returns>
    public static GeneratedRule GenerateRule(Random random, int depth)
    {
        if (depth == 0 || random.Next(5) == 0)
        {
            return GenerateLeaf(random);
        }

        GeneratedRule Child()
        {
            return GenerateRule(random, depth - 1);
        }

        switch (random.Next(20))
        {
            case 0:
                GeneratedRule operand = Child();
                return new GeneratedRule($"NOT ({operand.Text})", v => K3Oracle.Not(operand.Eval(v)), [operand]);
            case 1:
                GeneratedRule[] ands = [.. Enumerable.Range(0, random.Next(2, 4)).Select(_ => Child())];
                return new GeneratedRule(
                    $"({string.Join(" AND ", ands.Select(o => o.Text))})",
                    v => K3Oracle.And(ands.Select(o => o.Eval(v))),
                    ands
                );
            case 2:
                GeneratedRule[] ors = [.. Enumerable.Range(0, random.Next(2, 4)).Select(_ => Child())];
                return new GeneratedRule(
                    $"({string.Join(" OR ", ors.Select(o => o.Text))})",
                    v => K3Oracle.Or(ors.Select(o => o.Eval(v))),
                    ors
                );
            case 3:
                GeneratedRule xl = Child();
                GeneratedRule xr = Child();
                return new GeneratedRule($"({xl.Text} XOR {xr.Text})", v => K3Oracle.Xor(xl.Eval(v), xr.Eval(v)), [xl, xr]);
            case 4:
                GeneratedRule el = Child();
                GeneratedRule er = Child();
                return new GeneratedRule(
                    $"({el.Text} EQUIVALENT {er.Text})",
                    v => K3Oracle.Equivalent(el.Eval(v), er.Eval(v)),
                    [el, er]
                );
            case 5:
                GeneratedRule il = Child();
                GeneratedRule ir = Child();
                return new GeneratedRule(
                    $"({il.Text} IMPLIES {ir.Text})",
                    v => K3Oracle.Implies(il.Eval(v), ir.Eval(v)),
                    [il, ir]
                );
            case 9:
                GeneratedRule nl = Child();
                GeneratedRule nr = Child();
                return new GeneratedRule($"({nl.Text} NAND {nr.Text})", v => K3Oracle.Nand(nl.Eval(v), nr.Eval(v)), [nl, nr]);
            case 10:
                GeneratedRule rl = Child();
                GeneratedRule rr = Child();
                return new GeneratedRule($"({rl.Text} NOR {rr.Text})", v => K3Oracle.Nor(rl.Eval(v), rr.Eval(v)), [rl, rr]);
            case 11:
                GeneratedRule[] nxor = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"NXOR({string.Join(", ", nxor.Select(o => o.Text))})",
                    v => K3Oracle.Nxor([.. nxor.Select(o => o.Eval(v))]),
                    nxor
                );
            case 12:
                GeneratedRule[] anys = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"ANY({string.Join(", ", anys.Select(o => o.Text))})",
                    v => K3Oracle.Any([.. anys.Select(o => o.Eval(v))]),
                    anys
                );
            case 13:
                GeneratedRule[] alls = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"ALL({string.Join(", ", alls.Select(o => o.Text))})",
                    v => K3Oracle.All([.. alls.Select(o => o.Eval(v))]),
                    alls
                );
            case 14:
                GeneratedRule[] nones = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"NONE({string.Join(", ", nones.Select(o => o.Text))})",
                    v => K3Oracle.None([.. nones.Select(o => o.Eval(v))]),
                    nones
                );
            case 15:
                GeneratedRule[] betweenOperands = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                int min = random.Next(0, betweenOperands.Length + 1);
                int max = random.Next(min, betweenOperands.Length + 1);
                if (min == 0 && max == betweenOperands.Length)
                {
                    min = 1; // The full range is rejected as an always-true structural constant.
                }

                return new GeneratedRule(
                    $"BETWEEN({min}, {max}, {string.Join(", ", betweenOperands.Select(o => o.Text))})",
                    v => K3Oracle.Between(min, max, [.. betweenOperands.Select(o => o.Eval(v))]),
                    betweenOperands
                );
            case 16:
                GeneratedRule[] coalesced = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                return new GeneratedRule(
                    $"COALESCE({string.Join(", ", coalesced.Select(o => o.Text))})",
                    v => K3Oracle.Coalesce([.. coalesced.Select(o => o.Eval(v))]),
                    coalesced
                );
            case 19:
                GeneratedRule projected = Child();
                bool unknownAs = random.Next(2) == 0;
                TruthValue replacement = unknownAs ? TruthValue.True : TruthValue.False;
                return new GeneratedRule(
                    $"Project({projected.Text}, {replacement})",
                    v => K3Oracle.Project(projected.Eval(v), replacement),
                    [projected]
                );
            case 18:
                GeneratedRule inspected = Child();
                (string inspection, Func<TruthValue, TruthValue> inspect) = Inspections[random.Next(Inspections.Length)];
                return new GeneratedRule($"{inspection}({inspected.Text})", v => inspect(inspected.Eval(v)), [inspected]);
            case 17:
                GeneratedRule condition = Child();
                GeneratedRule whenTrue = Child();
                GeneratedRule whenFalse = Child();
                return new GeneratedRule(
                    $"If({condition.Text}, {whenTrue.Text}, {whenFalse.Text})",
                    v => K3Oracle.If(condition.Eval(v), whenTrue.Eval(v), whenFalse.Eval(v)),
                    [condition, whenTrue, whenFalse]
                );
            case 6:
                GeneratedRule[] exactlyOne = [.. Enumerable.Range(0, random.Next(2, 4)).Select(_ => Child())];
                return new GeneratedRule(
                    $"ExactlyOne({string.Join(", ", exactlyOne.Select(o => o.Text))})",
                    v => K3Oracle.ExactlyOne([.. exactlyOne.Select(o => o.Eval(v))]),
                    exactlyOne
                );
            default:
                (string name, Func<int, int, bool> satisfies) = Thresholds[random.Next(Thresholds.Length)];
                GeneratedRule[] operands = [.. Enumerable.Range(0, random.Next(2, 5)).Select(_ => Child())];
                int k = random.Next(0, operands.Length + 1);
                return new GeneratedRule(
                    $"{name}({k}, {string.Join(", ", operands.Select(o => o.Text))})",
                    v => K3Oracle.Cardinality(count => satisfies(count, k), [.. operands.Select(o => o.Eval(v))]),
                    operands
                );
        }
    }

    private static GeneratedRule GenerateLeaf(Random random)
    {
        switch (random.Next(6))
        {
            case 0:
                return new GeneratedRule("TRUE", _ => TruthValue.True, []);
            case 1:
                return new GeneratedRule("FALSE", _ => TruthValue.False, []);
            case 2:
                return new GeneratedRule("UNKNOWN", _ => TruthValue.Unknown, []);
            default:
                int index = random.Next(3);
                return new GeneratedRule(((char)('a' + index)).ToString(), v => v[index], []);
        }
    }
}
