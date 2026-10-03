# 07: User-readable validation messages

**Status:** ready-for-agent after 02, 05
**Blocked by:** 02, 05

**What to build:** Human-readable explanations for malformed expressions from DSL, JSON, and YAML, built on existing `Diagnostic`/`SourceSpan`.

- [ ] Structured result: code, message, span or JSON/YAML path, optional suggestion, expected/found pair, plus plain-text rendering (ADR-0005 #11)
- [ ] Message catalog keyed by `DiagnosticCodes`
- [ ] DSL: line/column and caret excerpt; JSON/YAML: path to offending node
- [ ] Covers new failures (unbalanced/mismatched delimiters, unknown operator or alias, bad `BETWEEN` range, wrong arity, `NXOR`/`XOR` misuse)
- [ ] Tests assert observable message text for each code
