# 09: Analyzer lint rules for the new operators

**What to build:** Extend the compile-time analysis to flag useless or suspicious constructs the simplifier already understands: IsKnown/IsUnknown/IsTrue/IsFalse over an operand that can never be Unknown (or never be known), a COALESCE whose first operand is definite, an If whose branches are equal or condition is constant, a threshold or BETWEEN made vacuous by constants, duplicate operands, and double negation. Each finding is an informational or warning diagnostic with a code, a structured suggestion and a K3-sound justification, opt-in or configurable so existing rules do not start producing new warnings unexpectedly.

**Blocked by:** 08

**Status:** ready-for-agent

- [ ] Each lint finding has a code, message, span or path and suggestion, and is verified against the oracle (the flagged construct really is redundant)
- [ ] No new diagnostics appear for rules that were clean before unless the option is enabled
- [ ] README lists the lints and how to configure them
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).
