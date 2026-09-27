# Evaluation and compile-time benchmarks

**Status:** ready-for-agent

## Problem Statement

The library's core value proposition — "compile once, evaluate many times"
with per-evaluation memoization and a BDD-based compile-time analyzer — has
no regression protection for its actual performance characteristics. A
future change (e.g. the in-flight `Predicates` package, or the deferred
concurrent-operand-evaluation feature) could silently regress either the
compile-time BDD analysis or the eval-time memoized lookup with nothing
catching it.

## Solution

A BenchmarkDotNet project measuring (a) compile-time cost, including BDD
constant/contradiction analysis, for representative rule shapes and sizes,
and (b) eval-time cost of memoized term lookup under repeated/shared-term
trees, with a committed baseline for comparison.

## User Stories

1. As a maintainer, I want a benchmark for compile-time BDD analysis across
   representative rule sizes, so that a change to the analyzer's algorithm
   has a measurable before/after.
2. As a maintainer, I want a benchmark for eval-time memoized term lookup, so
   that a change to the evaluator or memoization strategy has the same
   protection.
3. As a maintainer, I want a committed baseline result, so that a PR
   reviewer can compare a new benchmark run against a known-good number
   without re-deriving it from scratch.
