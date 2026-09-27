# Rule tree JSON Schema

**Status:** ready-for-agent

## Problem Statement

ADR-0003 defines the JSON/YAML rule tree shape in prose only. A rule-authoring
UI or non-.NET tool that wants to validate a rule tree before sending it to
`CompileJson`/`CompileYaml` has nothing machine-readable to validate against,
and has to reverse-engineer the shape from the ADR or from `CompileJson`'s
own error messages.

## Solution

Publish a JSON Schema document describing the rule tree shape (`op`/
`operands`, `predicate`/`args`, the threshold family's `k` parameter, and
every `LiteralKind`), validated in CI against the same fixture rules the
compiler's Validate stage exercises, and ship it as a content asset in the
`BooleanRulesEngine` package.

## User Stories

1. As a rule-authoring UI builder, I want a JSON Schema for the rule tree, so
   that I can validate a tree client-side before sending it to `CompileJson`,
   without embedding the engine itself.
2. As a maintainer, I want the schema checked against the same fixtures the
   compiler validates, so that the schema and the compiler's actual
   acceptance criteria never drift apart silently.
