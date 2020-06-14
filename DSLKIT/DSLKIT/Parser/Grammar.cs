using System.Collections.Generic;
using System.Linq;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class Grammar : IGrammar
    {
        private readonly ITerminal Empty = new EmptyTerminal();

        public Grammar(string name, IEnumerable<ITerminal> terminals, IEnumerable<NonTerminal> nonTerminals)
        {
            Name = name;
            Terminals = terminals.ToList();
            NonTerminals = nonTerminals.ToList();
        }

        public ITerminal Eof { get; } = new EofTerminal();
        public string Name { get; }
        public IReadOnlyCollection<ITerminal> Terminals { get; }
        public IReadOnlyCollection<INonTerminal> NonTerminals { get; }
        public NonTerminal Root { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}, Terminals:{Terminals.Count()}, Eof:{Eof.Name}";
        }
    }
}