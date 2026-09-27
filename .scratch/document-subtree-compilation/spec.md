# Compiling a rule from a sub-tree of a larger document

**Status:** ready-for-agent

## Problem Statement

`RuleCompiler.CompileJson(string)` and `CompileYaml(string)` require their entire input to be the rule tree (ADR-0003's flat, key-discriminated shape). In practice a rule expression is often just one field of a larger, application-owned JSON or YAML document with a known schema (e.g. one property of a config object, one item in an array of named rule slots) — not the whole document. Today the only way to compile that sub-tree is to extract it and re-serialize it back to a standalone JSON/YAML string first, which is wasteful and awkward when the caller already has a parsed document in hand.

## Solution

Add overloads that accept an already-parsed node — `System.Text.Json.JsonElement` for `CompileJson`, a YamlDotNet `YamlNode` for `CompileYaml` — so a caller navigates to the sub-tree using the JSON/YAML library's own APIs (indexers, `EnumerateArray()`, etc.) and hands that node directly to the compiler. This is deliberately **not** a path or pointer language added to the engine: it mirrors ADR-0003's existing "arguments are literals only, no context-path syntax" stance, and keeps the caller in full control of navigation against a schema only it knows. A document with multiple embedded rule expressions is handled by calling the single-node overload once per node the caller has already located — no batch API is needed.

## User Stories

1. As a host application storing a rule as one field of a larger JSON document with a known schema, I want to compile a `JsonElement` I've already navigated to, so that I don't have to re-serialize a sub-tree back to a string first.
2. As a host application storing a rule as one field of a larger YAML document, I want the identical capability against a `YamlNode`.
3. As a host application with multiple named rule slots in one document, I want to compile each by calling the single-node overload once per slot I navigate to myself, so that I don't need a batch API or a path/pointer language added to the engine.
