# DSL round-trip property-based testing

**Status:** ready-for-agent

## Problem Statement

ADR-0003 guarantees `parse(print(x))` is structurally equal to `x` for every
DSL-representable rule shape. Today that guarantee is checked only by
hand-picked example-based tests. The `dsl-escaping` feature shows this
guarantee can silently break for an input nobody thought to write by hand
(a string literal containing `"` or `\`) — the failure mode is exactly the
kind a systematic generator would have caught before a user did.

## Solution

Add a property-based test that generates random valid `Expression` trees
spanning the full operator set (`AND`/`OR`/`NOT`/`XOR`/`XNOR`/`ExactlyOne`/
the threshold family/constants/terms with every `LiteralKind`), prints each
through `CanonicalPrinter`, reparses via `RuleCompiler`, and asserts the
reparsed tree is structurally equal to the original. On a failure, the
framework should shrink to a minimal reproducing counterexample rather than
reporting the full random tree.

## User Stories

1. As a maintainer of the DSL printer/parser, I want an automated generator
   that explores tree shapes and literal values I wouldn't think to
   hand-write, so that round-trip regressions are caught before release.
2. As a maintainer, I want a failing case to shrink to a minimal reproducing
   example, so that debugging a round-trip failure doesn't require sifting
   through a large random tree.
