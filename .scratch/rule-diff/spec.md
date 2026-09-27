# Structural rule diff

**Status:** ready-for-agent

## Problem Statement

A host that persists rule versions (per CONTEXT.md's "Rule = a named,
versioned unit of persistence") has no way to show what changed between two
versions of a rule beyond a raw text diff of the DSL/JSON, which doesn't
account for structurally-equivalent reordering and doesn't explain the
change in terms of the rule's meaning.

## Solution

A structural diff API built on `RuleDescription` (the existing `Describe()`
output) that compares two compiled rules and reports added/removed/changed
nodes, plus a human-readable renderer for that diff suitable for an audit
log or a rule-review UI.

## User Stories

1. As a host application maintaining rule version history, I want a
   structural diff between two versions of a compiled rule, so that I can
   show "what changed" independent of superficial text formatting
   differences.
2. As a reviewer approving a rule edit, I want a human-readable rendering of
   that diff, so that I can review the change's meaning without reading two
   full rule trees side by side.
