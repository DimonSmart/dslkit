using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public interface IGrammar
    {
        NonTerminal Root { get; }
        ITerminal Eof { get; }
    }
}