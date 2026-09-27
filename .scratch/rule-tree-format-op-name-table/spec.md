# Shared op-name lookup for JSON/YAML print and parse

**Status:** ready-for-agent

## Problem Statement

`JsonTreePrinter`/`YamlTreePrinter` and `JsonTreeParser`/`YamlTreeParser` each
independently map every operator to/from its tree-format op-name string
(`"and"`, `"or"`, `"atLeast"`, …). Confirmed duplication: `ThresholdOpName` is
copy-pasted verbatim between the two printers, and the full op-name dispatch
switch (all ten operator names, not just the threshold family) is copy-pasted
verbatim between the two parsers. The printers additionally inline the same
op-name strings a second time in their own `ToNode` switches. That's the same
lookup maintained independently in four places, with no shared test forcing
them to agree — ADR-0003's threshold-family and `XNOR` additions each required
updating all four copies by hand.

## Solution

Introduce one shared op-name lookup — operator ⇄ tree-format string, covering
every operator (`AND`/`OR`/`NOT`/`XOR`/`XNOR`/`ExactlyOne`/the threshold
family), not just the threshold family where the byte-identical duplication
was most visible. Both printers and both parsers consume it. JSON and YAML
remain two separate adapters at the ADR-0004 format seam — this only removes
the duplicated string-mapping table each currently carries internally.

Depends on the shared node-shape seam from `expression-node-shape-seam`, since
the printers/parsers being deepened here are the same ones migrated in that
ticket.

## User Stories

1. As a maintainer adding or renaming an operator's tree-format name, I want to
   update one lookup, so that JSON and YAML, print and parse, can't silently
   drift out of agreement.
2. As a maintainer, I want the op-name lookup covered by one shared test, so
   that "does this format still accept name X" cannot pass on one format and
   fail on the other without anyone noticing.
