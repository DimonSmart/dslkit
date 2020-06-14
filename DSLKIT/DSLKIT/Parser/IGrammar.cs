using System.Collections.Generic;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public interface IGrammar
    {
        string Name { get; }
        IReadOnlyCollection<ITerminal> Terminals { get; }
        IReadOnlyCollection<INonTerminal> NonTerminals { get; }
        NonTerminal Root { get; }
        ITerminal Eof { get; }
    }
}