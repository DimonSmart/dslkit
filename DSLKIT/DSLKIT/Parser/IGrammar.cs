using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;
using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public interface IGrammar
    {
        string Name { get; }
        IReadOnlyCollection<ITerminal> Terminals { get; }
        IReadOnlyCollection<INonTerminal> NonTerminals { get; }
        IReadOnlyCollection<Production> Productions { get; }
        IReadOnlyDictionary<INonTerminal, IList<ITerm>> Firsts { get; }
        IReadOnlyDictionary<INonTerminal, IList<ITerminal>> Follow { get; }
        INonTerminal Root { get; }
        ITerminal Eof { get; }
    }
}