# DSL string literal escaping

**Status:** ready-for-agent

## Problem Statement

ADR-0003 guarantees `parse(print(x))` is structurally equal to `x` for every supported rule shape. That guarantee is currently false: `LiteralValue.ToString()` (used by `TermIdentity.ToString()`, which the canonical printer and the evaluator's trace/logging both call) renders a `LiteralKind.String` value as `$"\"{stringValue}\""` with no escaping. A term argument containing `"` or `\` (e.g. `hasRole(role: "V\"IP")`) compiles successfully but prints as syntactically broken DSL that will not reparse.

Separately, the lexer's string-literal reader (`Lexer.ReadString`) recognizes `\"`, `\\`, `\n`, `\t` and silently drops the backslash for any other escape (`_ => next`), so a typo like `\p` silently changes the literal's value instead of failing to compile.

JSON and YAML printing are unaffected — both already delegate to `JsonValue.Create`/YamlDotNet's own scalar emitters, which handle escaping correctly.

## Solution

Fix the DSL layer's string literal handling symmetrically: escape on print, validate on read.

## User Stories

1. As a rule author round-tripping a compiled rule through the canonical printer, I want string literal arguments containing `"` or `\` to print with correct escaping, so that `parse(print(x))` stays structurally equal to `x` per ADR-0003.
2. As a rule author, I want an unrecognized escape sequence in a DSL string literal to be a compile-time diagnostic, so that a typo doesn't silently change the literal's value with no warning.
