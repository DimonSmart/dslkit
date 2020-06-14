using System.Collections.Generic;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public interface IGrammar
    {
        string Name { get; }
        IEnumerable<ITerminal> Terminals { get; }
        NonTerminal Root { get; }
        ITerminal Eof { get; }
    }
}