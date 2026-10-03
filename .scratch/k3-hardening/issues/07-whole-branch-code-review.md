# 07: Whole-branch code review before merge

**What to build:** The Strong K3 work was built by many independent iterations, so review it as one change. Run the repository's code-review process (standards and spec axes) over the full branch against the k3-conformance and k3-followups specs, covering correctness against the oracle, public API consistency (names, XML docs, exception-free failures), duplicated helpers across the operator layers, dead code left by the removed Project/Collapse/NXOR spellings, and test quality (tests that never failed at runtime when first written). File the findings as tickets, fixing only trivial ones.

**Blocked by:** k3-followups 01

**Status:** ready-for-agent

- [ ] A review report exists for the whole branch with findings ranked by severity
- [ ] Each non-trivial finding is a ticket; trivial ones are fixed in the same change
- [ ] The report states what was not reviewed

See also [spec](../spec.md).
