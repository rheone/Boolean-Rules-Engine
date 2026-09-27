# BooleanRulesEngine.Testing

**Status:** ready-for-agent

## Problem Statement

Consumers writing tests against compiled rules currently hand-roll `Decision`
assertions, fake `IPredicate<TContext>` implementations, and `TContext`
instances for every test project. This boilerplate is repeated across
projects with no shared, supported home — the same gap the
`predicates-package` feature closes for common predicate implementations,
just on the testing side instead.

## Solution

A new package, `BooleanRulesEngine.Testing`, depending only on
`BooleanRulesEngine.Abstractions` (ADR-0004 package boundaries — it must not
pull in the parser, compiler, analyzer, or YamlDotNet), providing fluent
`Decision` assertions and predicate/context test doubles for consumers
writing tests against compiled rules.

## User Stories

1. As a consumer writing tests against a compiled rule, I want fluent
   assertions on a `Decision` (e.g. `decision.Should().BeSatisfied()`,
   `.HaveFault(...)`), so that my test intent reads clearly instead of
   unpacking `IsSatisfied`/`Faults` by hand.
2. As a consumer, I want a lightweight fake predicate I can register with a
   fixed answer, so that I can test rule combination logic without writing a
   bespoke `IPredicate<TContext>` class per test.
3. As a consumer, I want a fake predicate that can simulate a fault, so that
   I can test `Unknown`/fault-absorption behavior without throwing from a
   real predicate implementation.
