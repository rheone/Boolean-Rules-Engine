# BooleanRulesEngine.Predicates

**Status:** ready-for-agent

## Problem Statement

The engine ships zero built-in predicates by design — `CONTEXT.md` frames predicates as opaque, application-owned code, and no `Predicates` directory exists anywhere in `src/`. In practice, though, nearly every consumer re-implements the same small set of general-purpose comparisons (case-insensitive string equality, starts-with/contains, regex match, null-or-empty, order-insensitive set equality) as boilerplate `IPredicate<TContext>` classes before it gets to anything domain-specific.

## Solution

A new package, `BooleanRulesEngine.Predicates`, depending only on `BooleanRulesEngine.Abstractions` (ADR-0004 package boundaries — it must not pull in the parser, compiler, analyzer, or YamlDotNet). It provides a minimal, generic, opt-in set of predicates. Each predicate is parameterized by a `Func<TContext, ...>` value selector rather than assuming any particular shape of `TContext`, so registering one is "point it at the right property," not "implement a new class per context type."

## User Stories

1. As a host application, I want to depend on `BooleanRulesEngine.Predicates` only if I want these general-purpose predicates, so that a host with no interest in them never acquires anything beyond `Abstractions`.
2. As a rule-registry owner, I want case-sensitive and case-insensitive string equality, `StartsWith`, `EndsWith`, and `Contains` predicates available out of the box, parameterized by a selector into my `TContext`, so that I stop hand-writing the same string-comparison predicate class per project.
3. As a rule-registry owner, I want an `IsNullOrEmpty` predicate and an order-insensitive set-equality predicate, so that "does this collection match this set" and "is this value absent" don't need bespoke predicates either.
4. As a rule-registry owner, I want a regex-match predicate with its pattern compiled once (not per evaluation), so that I get this common capability without a performance trap.
5. As a predicate consumer, I want every predicate in this package to follow the same `PredicateSchema`/`Label`/`Description` contract as any hand-written predicate (CONTEXT.md), so that a rule-authoring UI treats them identically to domain predicates.
