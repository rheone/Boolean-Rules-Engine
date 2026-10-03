# 01: Refresh the library roadmap and deferred features

**What to build:** Re-score .scratch/library-roadmap/spec.md and .scratch/deferred-features/spec.md now that the Strong K3 language surface, rewrites (expand, compress, canonicalise, simplify), structured diagnostics and the dual-rail analyzer exist. Several verdicts are stale: the BDD-based equivalence check and 'simplify my rule' overlap Canonicalize and Simplify; ready-made predicate factories must return TruthValue; the fuzzer can reuse the shared rule generator; De Morgan / negation-normal-form printing overlaps the rewrites. Update each item's impact, complexity and verdict, add new candidates raised by the research and audit (see the k3-hardening tickets), and remove or mark obsolete items.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Every item in both documents has a current verdict and rationale or is marked obsolete with a reason
- [ ] New items from k3-hardening and k3-followups are cross-referenced
- [ ] No code is changed

See also [spec](../spec.md).
