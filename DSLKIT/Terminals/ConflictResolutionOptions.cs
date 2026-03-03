namespace DSLKIT.Terminals
{
    /// <summary>
    /// Associativity used by precedence declarations.
    /// </summary>
    public enum Assoc
    {
        Left,
        Right,
        None
    }

    /// <summary>
    /// Resolution for shift/reduce conflicts.
    /// </summary>
    public enum Resolve
    {
        Shift,
        Reduce
    }

    internal readonly record struct PrecedenceRule(int Level, Assoc Associativity);
}
