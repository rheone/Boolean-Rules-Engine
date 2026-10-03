namespace TruthWeaver.Diagnostics;

/// <summary>
/// The "did you mean" engine shared by the DSL, JSON and YAML front ends and the compiler. It is deliberately small and
/// deterministic: a case-insensitive optimal-string-alignment edit distance (insertions, deletions, substitutions and
/// adjacent transpositions), a length-scaled cut-off so short names are not matched to unrelated words, and an ordinal
/// tie-break so the answer never depends on registration or enumeration order.
/// </summary>
internal static class NameSuggester
{
    /// <summary>Finds the known name closest to <paramref name="name"/>, or <see langword="null"/> when none is close enough.</summary>
    /// <param name="name">The unrecognised name as written.</param>
    /// <param name="candidates">The known names, in the spelling to suggest. Duplicates are harmless.</param>
    /// <returns>The nearest candidate, ties going to the ordinally first, or <see langword="null"/>.</returns>
    public static string? Nearest(string name, IEnumerable<string> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        int limit = MaxDistance(name.Length);
        string? best = null;
        int bestDistance = int.MaxValue;
        foreach (string candidate in candidates)
        {
            int distance = Distance(name, candidate, limit);
            if (distance > limit)
            {
                continue;
            }

            if (distance < bestDistance || (distance == bestDistance && string.CompareOrdinal(candidate, best) < 0))
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary>
    /// Wraps <see cref="Nearest"/> as a "did you mean" suggestion.
    /// </summary>
    /// <param name="name">The unrecognised name as written.</param>
    /// <param name="candidates">The known names.</param>
    /// <returns>A replacement suggestion, or <see langword="null"/> when nothing is close enough.</returns>
    public static DiagnosticSuggestion? Suggest(string name, IEnumerable<string> candidates)
    {
        return Nearest(name, candidates) is { } nearest
            ? new DiagnosticSuggestion(DiagnosticSuggestionKind.Replacement, nearest)
            : null;
    }

    // One edit is tolerated in short words, two in medium ones and three in long ones: enough for a typo, too little
    // to turn `a` into `OR` or one unrelated identifier into another.
    private static int MaxDistance(int length)
    {
        return length switch
        {
            <= 4 => 1,
            <= 8 => 2,
            _ => 3,
        };
    }

    // Optimal string alignment distance. Returns limit + 1 as soon as the result cannot be within limit.
    private static int Distance(string source, string target, int limit)
    {
        if (Math.Abs(source.Length - target.Length) > limit)
        {
            return limit + 1;
        }

        int[,] d = new int[source.Length + 1, target.Length + 1];
        for (int i = 0; i <= source.Length; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= target.Length; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                char a = char.ToUpperInvariant(source[i - 1]);
                char b = char.ToUpperInvariant(target[j - 1]);
                int cost = a == b ? 0 : 1;
                int value = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a == char.ToUpperInvariant(target[j - 2]) && b == char.ToUpperInvariant(source[i - 2]))
                {
                    value = Math.Min(value, d[i - 2, j - 2] + 1);
                }

                d[i, j] = value;
            }
        }

        return d[source.Length, target.Length];
    }
}
