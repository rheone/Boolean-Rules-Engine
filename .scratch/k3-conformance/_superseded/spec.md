# Strong K3 conformance and language surface

**Status:** ready-for-agent (design tree fully grilled; ADR-0005 has no open decisions)

Source: `.scratch/2026-10-02-TODO.md`. Reference only (verify before trusting): `.tmp/`. Decisions: [ADR-0005](../../docs/adr/0005-strong-k3-language-surface.md). All work is TDD (red/green/refactor), tests named `{Member}_{Scenario}_{Expectation}_Test`, validation per CLAUDE.md.

## Problem

The engine is K3 internally but its authoring surface is a subset of the K3 language the reference specs define, and several explicitly requested capabilities (aliases, derived operators, `COALESCE`, `If`, inspection, boundaries, delimiters, expression rewriting, readable validation) do not exist.

## Categories and tickets

| # | Ticket | Blocked by |
| - | ------ | ---------- |
| 01 | [K3 verification and conformance](issues/01-k3-conformance-verification.md) | — |
| 02 | [Operator set, aliases, notation](issues/02-operator-set-and-notation.md) | 01 |
| 03 | [Cardinality aliases and derived-operator docs](issues/03-cardinality-aliases.md) | 02 |
| 04 | [K3 value operations and boundaries](issues/04-value-operations-and-boundaries.md) | 02 |
| 05 | [Grouping delimiters](issues/05-grouping-delimiters.md) | 02 |
| 06 | [Expression mutation (expand, compress, simplify, canonicalize, whitespace)](issues/06-expression-mutation.md) | 02, 03, 04, 05 |
| 07 | [User-readable validation messages](issues/07-validation-messages.md) | 02, 05 |
| 08 | [Predicate catalog hand-off (gap list only)](issues/08-predicate-catalog-handoff.md) | 01 |

ADR work is already done for the settled decisions (ADR-0005); each ticket updates ADR-0005 / CONTEXT.md / README for its own area.

## Out of scope

Implementing the ~50-predicate catalog (existing `predicate-catalog` track), context-bound arguments, new serialization formats.
