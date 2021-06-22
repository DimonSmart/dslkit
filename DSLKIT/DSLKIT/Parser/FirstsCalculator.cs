using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.SpecialTerms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DSLKIT.Parser
{
    public class FirstsCalculator
    {
        private readonly IEnumerable<ExProduction> _exProductions;
        private readonly Dictionary<IExNonTerminal, IList<ITerm>> _firsts;
        private readonly HashSet<ExProduction> _searchStack;

        public FirstsCalculator(IEnumerable<ExProduction> exProductions)
        {
            _exProductions = exProductions;
            _firsts = new Dictionary<IExNonTerminal, IList<ITerm>>();
            _searchStack = new HashSet<ExProduction>();
        }

        public IReadOnlyDictionary<IExNonTerminal, IList<ITerm>> Calculate()
        {
            AddFirstSets();
            return new ReadOnlyDictionary<IExNonTerminal, IList<ITerm>>(_firsts);
        }

        private void AddFirstSets(INonTerminal nonTerminal = null)
        {
            foreach (var exProduction in _exProductions
                .Where(p => (p.ExLeftNonTerminal == nonTerminal || nonTerminal == null) && !_searchStack.Contains(p)))
            {
                var allRulesContainsEpsilon = true;
                foreach (var exTerm in exProduction.ExProductionDefinition)
                {
                    if (exTerm is IExTerminal exTerminal)
                    {
                        AddFirst(exProduction.ExLeftNonTerminal, exTerminal.Terminal);
                        allRulesContainsEpsilon = false;
                        break;
                    }

                    if (exTerm is IExNonTerminal exNonTerminal)
                    {
                        _searchStack.Add(exProduction);
                        AddFirstSets(exNonTerminal.NonTerminal);
                        _searchStack.Remove(exProduction);

                        AddFirsts(exProduction.ExLeftNonTerminal, _firsts[exNonTerminal]);

                        // If it doesn't contain the empty terminal, then stop
                        if (!_firsts[exNonTerminal].Contains(EmptyTerm.Empty))
                        {
                            allRulesContainsEpsilon = false;
                            break;
                        }
                    }
                }

                if (allRulesContainsEpsilon)
                {
                    AddFirst(exProduction.ExLeftNonTerminal, EmptyTerm.Empty);
                }
            }
        }

        private bool AddFirsts(IExNonTerminal nonTerminal, IEnumerable<ITerm> terms)
        {
            var added = false;
            foreach (var terminal in terms)
            {
                added |= AddFirst(nonTerminal, terminal);
            }

            return added;
        }

        private bool AddFirst(IExNonTerminal nonTerminal, ITerm term)
        {
            if (_firsts.TryGetValue(nonTerminal, out var firsts))
            {
                if (firsts.Contains(term))
                {
                    return false;
                }

                firsts.Add(term);
                return true;
            }

            _firsts[nonTerminal] = new List<ITerm> { term };
            return true;
        }
    }
}