namespace TruthWeaver.Diffing;

/// <summary>The result of <see cref="RuleDiff.Compare{TContext}"/>: every structural difference found, in tree order.</summary>
/// <param name="Entries">The diff entries, or empty when the two rules are structurally identical.</param>
public sealed record RuleDiffResult(IReadOnlyList<RuleDiffEntry> Entries)
{
    /// <summary>Gets a value indicating whether any difference was found.</summary>
    public bool HasChanges => this.Entries.Count > 0;
}
