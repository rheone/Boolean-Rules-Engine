namespace BooleanRulesEngine.Analysis;

/// <summary>
/// A minimal reduced-ordered binary decision diagram (ROBDD) manager: variables are ordered by
/// increasing integer index (the analyzer assigns indices by each term's first occurrence in the
/// tree — a simple, explicit variable-ordering choice, not tuned for any particular rule shape),
/// nodes are uniquified so structurally identical sub-BDDs are physically the same node, and
/// <c>Ite</c> (if-then-else) is the single primitive every boolean operator is built from. This
/// gives every compiled expression node an O(1) tautology/contradiction check (compare its BDD node
/// id against the <see cref="True"/>/<see cref="False"/> terminal) without repeated brute-force
/// truth-table enumeration per sub-expression (ticket 10).
/// </summary>
internal sealed class BddManager
{
    /// <summary>The terminal representing structural <see langword="false"/>.</summary>
    public const int False = 0;

    /// <summary>The terminal representing structural <see langword="true"/>.</summary>
    public const int True = 1;

    private readonly List<(int Var, int Low, int High)> nodes;
    private readonly Dictionary<(int Var, int Low, int High), int> uniqueTable = [];
    private readonly Dictionary<(int I, int T, int E), int> iteCache = [];

    public BddManager()
    {
        this.nodes = [(int.MinValue, -1, -1), (int.MinValue, -1, -1)];
    }

    /// <summary>Creates (or returns the existing) BDD node for a boolean variable.</summary>
    /// <param name="index">The variable's position in the analyzer's chosen ordering.</param>
    /// <returns>The node id.</returns>
    public int Variable(int index)
    {
        return this.MakeNode(index, False, True);
    }

    /// <summary>Computes the BDD for logical negation.</summary>
    /// <param name="a">The operand's node id.</param>
    /// <returns>The result's node id.</returns>
    public int Not(int a)
    {
        return this.Ite(a, False, True);
    }

    /// <summary>Computes the BDD for logical conjunction.</summary>
    /// <param name="a">The left operand's node id.</param>
    /// <param name="b">The right operand's node id.</param>
    /// <returns>The result's node id.</returns>
    public int And(int a, int b)
    {
        return this.Ite(a, b, False);
    }

    /// <summary>Computes the BDD for logical disjunction.</summary>
    /// <param name="a">The left operand's node id.</param>
    /// <param name="b">The right operand's node id.</param>
    /// <returns>The result's node id.</returns>
    public int Or(int a, int b)
    {
        return this.Ite(a, True, b);
    }

    /// <summary>Computes the BDD for exclusive-or.</summary>
    /// <param name="a">The left operand's node id.</param>
    /// <param name="b">The right operand's node id.</param>
    /// <returns>The result's node id.</returns>
    public int Xor(int a, int b)
    {
        return this.Ite(a, this.Not(b), b);
    }

    /// <summary>Computes the BDD for if-then-else: <c>i ? t : e</c>, over structural (not Kleene) boolean values.</summary>
    /// <param name="i">The condition's node id.</param>
    /// <param name="t">The then-branch's node id.</param>
    /// <param name="e">The else-branch's node id.</param>
    /// <returns>The result's node id.</returns>
    public int Ite(int i, int t, int e)
    {
        if (i == True)
        {
            return t;
        }

        if (i == False)
        {
            return e;
        }

        if (t == True && e == False)
        {
            return i;
        }

        if (t == e)
        {
            return t;
        }

        (int I, int T, int E) key = (i, t, e);
        if (this.iteCache.TryGetValue(key, out int cached))
        {
            return cached;
        }

        int topVar = this.TopVariable(i, t, e);
        int iLow = this.Restrict(i, topVar, false);
        int iHigh = this.Restrict(i, topVar, true);
        int tLow = this.Restrict(t, topVar, false);
        int tHigh = this.Restrict(t, topVar, true);
        int eLow = this.Restrict(e, topVar, false);
        int eHigh = this.Restrict(e, topVar, true);

        int low = this.Ite(iLow, tLow, eLow);
        int high = this.Ite(iHigh, tHigh, eHigh);
        int result = this.MakeNode(topVar, low, high);
        this.iteCache[key] = result;
        return result;
    }

    private int TopVariable(int i, int t, int e)
    {
        int min = int.MaxValue;
        if (i > True)
        {
            min = Math.Min(min, this.nodes[i].Var);
        }

        if (t > True)
        {
            min = Math.Min(min, this.nodes[t].Var);
        }

        if (e > True)
        {
            min = Math.Min(min, this.nodes[e].Var);
        }

        return min;
    }

    private int Restrict(int node, int var, bool value)
    {
        if (node <= True)
        {
            return node;
        }

        (int nodeVar, int low, int high) = this.nodes[node];
        if (nodeVar != var)
        {
            return node;
        }

        return value ? high : low;
    }

    private int MakeNode(int var, int low, int high)
    {
        if (low == high)
        {
            return low;
        }

        (int, int, int) key = (var, low, high);
        if (this.uniqueTable.TryGetValue(key, out int existing))
        {
            return existing;
        }

        int id = this.nodes.Count;
        this.nodes.Add(key);
        this.uniqueTable[key] = id;
        return id;
    }
}
