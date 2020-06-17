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
            IEnumerable<Production> productions)
        {
            Name = name;
            Terminals = terminals.ToList();
            NonTerminals = nonTerminals.ToList();
            Productions = productions.ToList();
            Firsts = new ReadOnlyDictionary<INonTerminal, IList<ITerminal>>(CalculateFirsts(Productions));
        }

        public IReadOnlyCollection<Production> Productions { get; }
        public IReadOnlyCollection<ITerminal> Terminals { get; }
        public IReadOnlyCollection<INonTerminal> NonTerminals { get; }
        public IReadOnlyDictionary<INonTerminal, IList<ITerminal>> Firsts { get; }

        public ITerminal Eof { get; } = new EofTerminal();
        public string Name { get; }

        public NonTerminal Root { get; set; }

        public static IDictionary<INonTerminal, IList<ITerminal>> CalculateFirsts(
            IReadOnlyCollection<Production> productions)
        {
            var res = new Dictionary<INonTerminal, IList<ITerminal>>();

            foreach (var production in productions)
            {
                FirstRecursive(res, production, productions);
            }

            return res;
        }


        public static IList<ITerminal> First(List<ITerm> tokens, IReadOnlyCollection<Production> productions)
        {
            if (tokens.FirstOrDefault() is ITerminal terminal)
            {
                return new List<ITerminal> {terminal};
            }

            if (tokens.FirstOrDefault() is NonTerminal nonterminal)
            {
                var firsts = First(new List<ITerm> {nonterminal}, productions);
                return firsts;
            }

            return new List<ITerminal>();
        }


        public static void FirstRecursive(IDictionary<INonTerminal, IList<ITerminal>> firsts, Production production,
            IReadOnlyCollection<Production> productions)
        {
            if (production.ProductionDefinition.First() == Constants.Empty)
            {
                AddFirst(firsts, production.LeftNonTerminal, Constants.Empty);
            }

            if (production.ProductionDefinition.First() is ITerminal terminal)
            {
                AddFirst(firsts, production.LeftNonTerminal, terminal);
            }
        }


        private static void AddFirst(IDictionary<INonTerminal, IList<ITerminal>> allFirsts, INonTerminal nonTerminal,
            ITerminal terminal)
        {
            if (allFirsts.TryGetValue(nonTerminal, out var firsts))
            {
                if (firsts.Contains(terminal))
                {
                    return;
                }

                firsts.Add(terminal);
                return;
            }

            allFirsts[nonTerminal] = new List<ITerminal> {terminal};
        }


        public override string ToString()
        {
            return $"Name: {Name}, Terminals:{Terminals.Count()}, Eof:{Eof.Name}";
        }
    }
}