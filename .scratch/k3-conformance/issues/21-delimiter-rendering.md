# 21: Delimiter rendering

**What to build:** The printer normalises every delimiter to parentheses; an optional printer varies delimiters by nesting depth for readability.

**Blocked by:** 20

**Status:** done

- [x] Default output uses parentheses only
- [x] Depth-varying printer is opt-in and re-parses to an equal tree
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Added the public `GroupingStyle` enum (`Parentheses`, `DepthCycling`) in `TruthWeaver.Printing` and `CompiledRule.PrintText(GroupingStyle)`, following the existing `PrintJson`/`PrintMermaid` naming; `CanonicalPrinter.Print` takes the style as an optional third argument. `CanonicalText` is unchanged and verified parentheses-only.

Decisions: the cycle is `(`, `[`, `{` by group depth starting at `(` for the outermost group (matches the reference example); depth counts wrapped groups only, so call argument lists stay `(` and do not deepen it; no separate "convert all to parens" option because the default already is that. A CsCheck property test checks the depth-cycled text of every generated tree compiles to an equal tree.
