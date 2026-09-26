namespace BooleanRulesEngine.Compilation;

/// <summary>How the compiler treats a term referencing an unregistered predicate name (ADR-0002).</summary>
public enum CompilationMode
{
    /// <summary>
    /// An unregistered predicate name is a compile <c>Error</c> diagnostic; <c>CompiledRule</c> is
    /// <see langword="null"/>. The correct mode for a write/persistence path.
    /// </summary>
    Strict,

    /// <summary>
    /// An unregistered predicate name compiles successfully as a term that permanently evaluates to
    /// <c>TruthValue.Unknown</c>. Intended for read paths in a multi-service shared-rule-store
    /// topology, never for the write/persistence path.
    /// </summary>
    Lenient,
}
