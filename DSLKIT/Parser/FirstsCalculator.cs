using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.Parser.ExtendedGrammar;
using DSLKIT.SpecialTerms;

namespace DSLKIT.Parser
{
    public class FirstsCalculator
    {
        private readonly IEnumerable<ExProduction> _exProductions;
        private readonly Dictionary<IExNonTerminal, HashSet<ITerm>> _firsts;
        private readonly HashSet<ExProduction> _searchStack;

        public FirstsCalculator(IEnumerable<ExProduction> exProductions)
        {
            _exProductions = exProductions;
            _firsts = [];
            _searchStack = [];
        }

        public IReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>> Calculate()
        {
            AddFirstSets();
            return new ReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>>(
                _firsts.ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyCollection<ITerm>)pair.Value.ToList()));
        }

        private void AddFirstSets(IExNonTerminal? startExNonTerminal = null)
        {
            foreach (var exProduction in _exProductions
                .Where(p => (startExNonTerminal == null || p.ExLeftNonTerminal.Equals(startExNonTerminal)) &&
                            !_searchStack.Contains(p)))
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
                        AddFirstSets(exNonTerminal);
                        _searchStack.Remove(exProduction);

                        if (_firsts.TryGetValue(exNonTerminal, out var exNonTerminalFirsts))
                        {
                            AddFirsts(exProduction.ExLeftNonTerminal, exNonTerminalFirsts);

                            // If it doesn't contain the empty terminal, then stop
                            if (!_firsts[exNonTerminal].Contains(EmptyTerm.Empty))
                            {
                                allRulesContainsEpsilon = false;
                                break;
                            }
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
                return firsts.Add(term);
            }

            _firsts[nonTerminal] = new HashSet<ITerm> { term };
            return true;
        }
    }
}
