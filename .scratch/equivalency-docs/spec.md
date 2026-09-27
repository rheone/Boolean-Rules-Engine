# Operator equivalency rules documentation

**Status:** ready-for-agent

## Problem Statement

The operator set (`AND`/`OR`/`NOT`/`XOR`/`XNOR`/`ExactlyOne`/the threshold family/constants) has several non-obvious identities between its members — e.g. `Exactly(n, ...)` with `n` equal to the operand count means the same thing as `AND(...)`. Nothing currently documents these, so a rule author or reviewer has to work them out from first principles, and the question "why isn't there an `All`/`None` operator" (raised in conversation, not yet recorded anywhere) has no written answer.

## Solution

A new "Equivalency rules" section in `CONTEXT.md`, cross-linked from ADR-0003's threshold-family amendment, listing the identities and explicitly recording why `All`/`None` were considered and not added.

## User Stories

1. As a rule author or reviewer, I want a documented list of operator identities (e.g. `AtMost(0, ...) ≡ NOT(OR(...))`), so that I don't have to re-derive them from the truth tables every time.
2. As a future contributor considering adding `All`/`None` operators, I want the reasoning against them (they duplicate `AND`/`NOT(OR(...))`, and ADR-0003 already rejects operator synonyms) recorded where the rest of the operator-set decisions live, so the question isn't rediscovered from scratch.
