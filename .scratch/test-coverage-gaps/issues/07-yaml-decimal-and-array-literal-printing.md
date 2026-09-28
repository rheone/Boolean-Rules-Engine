# 07: Cover Yaml Decimal and array literal printing

**What to build:** Extend YAML literal round-trip coverage to include the `Decimal` literal kind and non-string array literal kinds (e.g. `Int64Array`, `BooleanArray`), which `YamlTreePrinter` supports but which aren't currently exercised.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] A rule with a `Decimal` literal argument round-trips through YAML printing/parsing correctly.
- [ ] A rule with an `Int64Array` literal argument round-trips correctly.
- [ ] A rule with a `BooleanArray` literal argument round-trips correctly.
- [ ] Existing `YamlLiteralRoundTripTests` (Guid, DateTimeOffset, StringArray) continue to pass unchanged.
