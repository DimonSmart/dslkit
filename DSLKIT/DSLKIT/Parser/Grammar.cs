using DSLKIT.NonTerminals;
using DSLKIT.Terminals;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Parser
{
    public class Grammar : IGrammar
    {
        public Grammar(string name,
            IEnumerable<ITerminal> terminals,
            IEnumerable<INonTerminal> nonTerminals,
            IEnumerable<Production> productions,
            INonTerminal root)
        {
            Name = name;
            Terminals = terminals.ToList();
            NonTerminals = nonTerminals.ToList();
            Productions = productions.ToList();
            Root = root;
            Firsts = CalculateFirsts();
        }

        public IReadOnlyCollection<Production> Productions { get; }
        public IReadOnlyCollection<ITerminal> Terminals { get; }
        public IReadOnlyCollection<INonTerminal> NonTerminals { get; }
        public IReadOnlyDictionary<INonTerminal, IList<ITerminal>> Firsts { get; }

        public ITerminal Eof { get; } = new EofTerminal();
        public string Name { get; }

        public INonTerminal Root { get; set; }

        public IReadOnlyDictionary<INonTerminal, IList<ITerminal>> CalculateFirsts()
        {
            return new FirstsCalculator(Productions).Calculate();
        }

        public override string ToString()
        {
            return $"Name: {Name}, Terminals:{Terminals.Count}, Eof:{Eof.Name}";
        }
    }
}