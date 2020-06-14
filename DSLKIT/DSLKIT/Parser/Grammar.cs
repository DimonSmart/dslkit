using System.Collections.Generic;
using System.Linq;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class Grammar : IGrammar
    {
        private readonly ITerminal Empty = new EmptyTerminal();

        public Grammar(string name, IEnumerable<ITerminal> terminals)
        {
            Name = name;
            Terminals = terminals;
        }

        public ITerminal Eof { get; } = new EofTerminal();
        public string Name { get; }
        public IEnumerable<ITerminal> Terminals { get; }
        public NonTerminal Root { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}, Terminals:{Terminals.Count()}, Eof:{Eof.Name}";
        }
    }
}