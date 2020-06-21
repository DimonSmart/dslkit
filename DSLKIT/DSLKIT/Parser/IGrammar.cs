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
        IReadOnlyCollection<Production> Productions { get; }
        IReadOnlyDictionary<INonTerminal, IList<ITerminal>> Firsts { get; }
        INonTerminal Root { get; }
        ITerminal Eof { get; }
    }
}