# 13: Structured logging via ILogger<T>

**What to build:** Logging through `ILogger<T>` (`Microsoft.Extensions.Logging.Abstractions`) only — never a concrete provider — for three event categories: faults absorbed during evaluation, compile diagnostics produced during `Compile`, and rule-swap events (logged when the library is told a `CompiledRule` reference has been swapped, since the library itself does not own rule storage or swap scheduling per ADR-0002). Each is a structured log event (named fields, not just an interpolated message), so a host application's chosen provider (Serilog or otherwise) can query on them.

**Blocked by:** 09

**Status:** done (verified against existing codebase — already implemented prior to this pass)

- [ ] Evaluating a rule with a faulting predicate logs a structured event (via an injected `ILogger<T>`) including the term identity and exception, at an appropriate level (e.g. `Warning`)
- [ ] Compiling a rule that produces any diagnostic logs a structured event per diagnostic, including its code, severity, and source span
- [ ] A rule-swap event, when the host signals one occurred, logs a structured event distinguishing it from fault/diagnostic events
- [ ] No package in this solution references a concrete logging provider (Serilog, NLog, etc.) — only `Microsoft.Extensions.Logging.Abstractions`
- [ ] Tests substitute `ILogger<T>` (NSubstitute) and assert on the structured log call rather than parsing rendered message text
