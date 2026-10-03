# k3-followups: research and audit follow-ups

**Status:** ready-for-agent

Follow-up work after the k3-conformance effort (31 tickets, done). Sources: [research findings](../k3-conformance/research-findings.md) and [spec audit](../k3-conformance/spec-audit.md), plus the owner's decisions of 2026-10-03:

- Collapse and Project are methods on the result (`Decision`), not rule-language features; `Decision.Result` is always the raw three-valued value.
- `NXOR` is renamed `PARITY` and the `NXOR` spelling is removed.
- Wrong passages in the `.tmp` reference documents are edited in place with a dated note.
- The two optional ergonomics tickets are included.

The existing issues-log is [../k3-conformance/issues-log.md](../k3-conformance/issues-log.md). Tickets are in `issues/`, numbered in dependency order.
