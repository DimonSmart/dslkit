using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;

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
            var firsts = new Dictionary<INonTerminal, IList<ITerminal>>();
            AddFirstSets(firsts, null, new HashSet<Production>());
            return new ReadOnlyDictionary<INonTerminal, IList<ITerminal>>(firsts);
        }

        private void AddFirstSets(IDictionary<INonTerminal, IList<ITerminal>> firsts, INonTerminal nonTerminal,
            HashSet<Production> searchStack)
        {
            foreach (var production in Productions
                .Where(p => (p.LeftNonTerminal == nonTerminal || nonTerminal == null) && !searchStack.Contains(p)))
            {
                foreach (var term in production.ProductionDefinition)
                {
                    if (term is ITerminal terminal)
                    {
                        AddFirst(firsts, production.LeftNonTerminal, terminal);
                        break;
                    }

                    var rNonTerminal = term as INonTerminal;
                    searchStack.Add(production);
                    AddFirstSets(firsts, rNonTerminal, searchStack);
                    searchStack.Remove(production);

                    AddFirsts(firsts, production.LeftNonTerminal, firsts[rNonTerminal]);

                    // If it doesn't contain the empty terminal, then stop
                    if (!firsts[rNonTerminal].Contains(Constants.Empty))
                    {
                        break;
                    }
                }
            }
        }

        private static bool AddFirsts(IDictionary<INonTerminal, IList<ITerminal>> allFirsts, INonTerminal nonTerminal,
            IEnumerable<ITerminal> terminals)
        {
            var added = false;
            foreach (var terminal in terminals)
            {
                added |= AddFirst(allFirsts, nonTerminal, terminal);
            }

            return added;
        }

        private static bool AddFirst(
            IDictionary<INonTerminal,
                IList<ITerminal>> allFirsts,
            INonTerminal nonTerminal,
            ITerminal terminal)
        {
            if (allFirsts.TryGetValue(nonTerminal, out var firsts))
            {
                if (firsts.Contains(terminal))
                {
                    return false;
                }

                firsts.Add(terminal);
                return true;
            }

            allFirsts[nonTerminal] = new List<ITerminal> {terminal};
            return true;
        }


        public override string ToString()
        {
            return $"Name: {Name}, Terminals:{Terminals.Count}, Eof:{Eof.Name}";
        }
    }
}