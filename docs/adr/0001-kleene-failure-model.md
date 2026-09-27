# ADR-0001: Kleene three-valued failure model

## Status

Accepted

## Context

A boolean expression is normally two-valued: `true` or `false`. But this
engine's terms (predicates) frequently do I/O — database calls, HTTP calls,
reading configuration — and I/O fails. A predicate can time out, throw, or be
cancelled mid-evaluation.

The naive options are:

1. **Propagate the exception.** Two-valued logic, a predicate failure throws
   out of `Evaluate`, and the caller decides what to do.
2. **Two-valued, fail-closed.** A predicate failure is silently coerced to
   `false`.
3. **Three-valued (Kleene) logic internally**, with `True` / `False` /
   `Unknown`, converted to a two-valued decision only at the API boundary.

Option 1 pushes the entire failure-handling burden onto every caller, at
every call site, for a library whose primary use case is "can the current
user do X right now" — exactly the place a caller is least likely to want to
handle a `TimeoutException` from three call sites down. It also means a
single unresolvable term aborts an evaluation that boolean algebra says
should have been resolvable anyway (`OR(true, <fault>)` is trivially `true`
regardless of the second operand).

Option 2 is actively dangerous specifically because this library's primary
consumer is a permission check. `NOT(<fault>)` silently becomes `NOT(false)`
= `true` under naive fail-closed coercion applied to `NOT`'s operand — a
database outage that should deny access instead grants it, because "unknown"
was folded into "false" before `NOT` saw it. Kleene logic keeps this correct:
`NOT(Unknown)` is `Unknown`, not `True`.

## Decision

Evaluation uses three-valued Kleene logic internally, expressed as a
dedicated type:

```csharp
public enum TruthValue
{
    False,
    True,
    Unknown,
}
```

not `bool?`. `bool?` reads as "optional bool" — a different concept — and
C#'s lifted `&`/`|` operators on `bool?` happen to implement Kleene logic,
but nothing stops a caller from writing `if (result == true)` against a
`bool?` and silently treating `Unknown` as falsy or truthy depending on
context. A dedicated enum keeps `TruthValue` out of accidental boolean
contexts, gives `switch` exhaustiveness, and reads correctly in logs and
traces.

**Kleene truth tables** (`U` = `Unknown`):

| `AND` | `False` | `True` | `U` |
| --- | --- | --- | --- |
| **`False`** | `False` | `False` | `False` |
| **`True`** | `False` | `True` | `U` |
| **`U`** | `False` | `U` | `U` |

| `OR` | `False` | `True` | `U` |
| --- | --- | --- | --- |
| **`False`** | `False` | `True` | `U` |
| **`True`** | `True` | `True` | `True` |
| **`U`** | `U` | `True` | `U` |

| `NOT` | |
| --- | --- |
| `False` | `True` |
| `True` | `False` |
| `U` | `U` |

`XOR`, `XNOR`, `ExactlyOne`, and the threshold family (`AtLeast(k)`/
`AtMost(k)`/`GreaterThan(k)`/`LessThan(k)`/`Exactly(k)`, added after this ADR
was first accepted — see [ADR-0003's Amendments](0003-rule-syntax-and-serialization.md#amendments))
follow the same principle: the result is determinate only when it is
determinate regardless of what an `Unknown` operand would have resolved to;
otherwise it is `Unknown`.

**A predicate signals a fault by throwing.** The evaluator catches the
exception at the term boundary, records a `Fault` (term identity + the
exception), and treats that term as `Unknown` for the remainder of this
evaluation. Predicate authors are not required to write
try/catch-and-wrap-as-Unknown boilerplate — a database timeout is naturally
an exception at the call site, and forcing manual translation is friction
authors will routinely skip. Authors who want to express "I cannot determine
this" without treating it as exceptional may still return an explicit
"unknown" result rather than a `bool`; see [ADR-0002](0002-evaluation-semantics.md)
for the exact predicate contract shape.

**At the API boundary, the engine converts back to two values and fails
closed:**

```csharp
public sealed record Decision(
    TruthValue Result,
    IReadOnlyList<Fault> Faults,
    Trace? Trace = null)
{
    public bool IsSatisfied => Result == TruthValue.True;
}
```

`IsSatisfied` is `true` only for `TruthValue.True`. `Unknown` — whether from
one absorbed fault or a hundred — reads as "not satisfied," which is the
correct default for an authorization consumer, while `Decision.Faults`
preserves *why* so the caller can log, alert, or retry rather than silently
denying with no signal that anything went wrong.

## Consequences

- A single unresolvable term does not necessarily fail an evaluation; only
  the terms whose values are actually load-bearing for the final result do.
  This is a direct efficiency and availability win over option 1 or 2.
- Every operator implementation (`AND`, `OR`, `NOT`, `XOR`, `XNOR`,
  `ExactlyOne`, and the threshold family) must be written against the
  three-valued truth tables, not lifted naively from two-valued C# `&&`/`||`,
  which do not have `Unknown` semantics to begin with.
- Callers who need fail-fast-on-any-fault behavior (e.g. during a known
  outage) use `EvaluationOptions.FaultBudget` (see
  [ADR-0002](0002-evaluation-semantics.md)) rather than the engine changing
  its default semantics for everyone.
- `bool?` must never appear on the public surface of this library; code
  review should reject it on sight in this codebase.

## Related

- [ADR-0002: Evaluation semantics](0002-evaluation-semantics.md) — how faults
  are produced, absorbed, and budgeted during evaluation.
- [CONTEXT.md](../../CONTEXT.md) — vocabulary and the predicate-author
  contract.
