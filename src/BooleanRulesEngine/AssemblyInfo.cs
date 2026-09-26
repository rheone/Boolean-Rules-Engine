using System.Runtime.CompilerServices;

// BooleanRulesEngine.Yaml (ticket 08) reuses this package's raw parse-tree types (RuleNode and its
// variants, RawLiteral) and RuleCompiler.CompileFromNode so the YAML front end shares the exact same
// validate/analyze/build pipeline as the DSL and JSON front ends, rather than re-implementing it.
[assembly: InternalsVisibleTo("BooleanRulesEngine.Yaml")]
