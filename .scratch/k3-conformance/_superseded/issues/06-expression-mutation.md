# 06: Expression mutation

**Status:** ready-for-agent after 02-05 (all transforms in scope, ADR-0005 #10)
**Blocked by:** 02, 03, 04, 05

**What to build:** Rewrite transforms over the immutable tree, each opt-in, each preserving K3 semantics over the oracle.

- [ ] Expand derived operators to primitives
- [ ] Expand to NAND-only and NOR-only (`NOT A = A NAND A`, etc.)
- [ ] Compress back to non-primitive/derived forms where possible
- [ ] Simplification (cheaper equivalent) per `.tmp/Strong Kleene K3 Expression Simplification…` and rule catalog; only K3-sound rules (verify each: e.g. `A OR NOT A` is not `True` in K3)
- [ ] Canonicalization (deterministic representative of equivalent expressions), distinct from simplification
- [ ] Text normalization: collapse whitespace to single spaces, trim, space around operators
- [ ] Property test: transform(expr) equals expr on every `{T,F,U}` assignment
