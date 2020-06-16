using System.Collections.Generic;
using System.Linq;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class Grammar : IGrammar
    {
        public Grammar(string name, IEnumerable<ITerminal> terminals, IEnumerable<INonTerminal> nonTerminals,
            IEnumerable<Production> productions)
        {
            Name = name;
            Terminals = terminals.ToList();
            NonTerminals = nonTerminals.ToList();
            Productions = productions.ToList();
        }

        public IReadOnlyCollection<Production> Productions { get; }
        public IReadOnlyCollection<ITerminal> Terminals { get; }
        public IReadOnlyCollection<INonTerminal> NonTerminals { get; }

        public ITerminal Eof { get; } = new EofTerminal();
        public string Name { get; }

        public NonTerminal Root { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}, Terminals:{Terminals.Count()}, Eof:{Eof.Name}";
        }
    }
}