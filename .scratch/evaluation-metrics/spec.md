# Lightweight evaluation metrics

**Status:** ready-for-agent

## Problem Statement

CONTEXT.md defers full OpenTelemetry-shaped observability (activity per rule,
event per term, fault attributes) as a larger, later feature. In the
meantime, a host running `CompiledRule.EvaluateAsync` has no lightweight,
standard way to observe evaluation volume, fault rate, or compile diagnostic
counts without instrumenting call sites itself.

## Solution

Add a small set of `System.Diagnostics.Metrics` instruments — a `Meter` with
`Counter`s for evaluations performed, faults recorded, and compile
diagnostics raised (tagged by severity) — observable via any
`MeterListener`/OTel exporter a host already has wired up, without taking on
the larger OTel Activity/tracing scope CONTEXT.md defers.

## User Stories

1. As a host operator, I want a counter of evaluations performed and faults
   recorded, so that I can graph evaluation volume and fault rate without
   waiting for the larger OTel feature.
2. As a host operator, I want a counter of compile diagnostics raised by
   severity, so that I can alert on a spike in rejected rule edits.
3. As a maintainer, I want these instruments to add no new third-party
   dependency (`System.Diagnostics.Metrics` is part of the BCL), so that this
   stays a lightweight addition rather than a step toward the deferred OTel
   item.
