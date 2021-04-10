using DSLKIT.NonTerminals;
using DSLKIT.Terminals;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DSLKIT.Parser
{
    public class FirstsCalculator
    {
        private readonly IEnumerable<Production> _productions;
        private readonly Dictionary<INonTerminal, IList<ITerminal>> _firsts;
        private readonly HashSet<Production> _searchStack;

        public FirstsCalculator(IEnumerable<Production> productions)
        {
            _productions = productions;
            _firsts = new Dictionary<INonTerminal, IList<ITerminal>>();
            _searchStack = new HashSet<Production>();
        }

        public IReadOnlyDictionary<INonTerminal, IList<ITerminal>> Calculate()
        {
            AddFirstSets();
            return new ReadOnlyDictionary<INonTerminal, IList<ITerminal>>(_firsts);
        }

        private void AddFirstSets(INonTerminal nonTerminal = null)
        {
            foreach (var production in _productions
                .Where(p => (p.LeftNonTerminal == nonTerminal || nonTerminal == null) && !_searchStack.Contains(p)))
            {
                foreach (var term in production.ProductionDefinition)
                {
                    if (term is ITerminal terminal)
                    {
                        AddFirst(production.LeftNonTerminal, terminal);
                        break;
                    }

                    var rNonTerminal = term as INonTerminal;
                    _searchStack.Add(production);
                    AddFirstSets(rNonTerminal);
                    _searchStack.Remove(production);

                    AddFirsts(production.LeftNonTerminal, _firsts[rNonTerminal]);

                    // If it doesn't contain the empty terminal, then stop
                    //if (!_firsts[rNonTerminal].Contains(EmptyTerm.Empty))
                    //{
                    //    break;
                    //}
                }
            }
        }

        private bool AddFirsts(INonTerminal nonTerminal, IEnumerable<ITerminal> terminals)
        {
            var added = false;
            foreach (var terminal in terminals)
            {
                added |= AddFirst(nonTerminal, terminal);
            }

            return added;
        }

        private bool AddFirst(INonTerminal nonTerminal, ITerminal terminal)
        {
            if (_firsts.TryGetValue(nonTerminal, out var firsts))
            {
                if (firsts.Contains(terminal))
                {
                    return false;
                }

                firsts.Add(terminal);
                return true;
            }

            _firsts[nonTerminal] = new List<ITerminal> { terminal };
            return true;
        }
    }
}