using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public enum ParserConflictKind
    {
        ShiftReduce,
        ReduceReduce
    }

    public sealed record ParserConflict(
        ParserConflictKind Kind,
        int StateNumber,
        string TerminalName,
        string ExistingAction,
        string IncomingAction,
        Resolve? Resolution = null);
}
