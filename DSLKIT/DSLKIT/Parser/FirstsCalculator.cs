using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DSLKIT.Parser
{
    public class FirstsCalculatorEx
    {
        private readonly IEnumerable<ExtendedGrammarProduction> _extendedGrammarProductions;
        private readonly Dictionary<INonTerminal, IList<ITerm>> _firsts;
        private readonly HashSet<ExtendedGrammarProduction> _searchStack;

        public FirstsCalculatorEx(IEnumerable<ExtendedGrammarProduction> productions)
        {
            _extendedGrammarProductions = productions;
            _firsts = new Dictionary<INonTerminal, IList<ITerm>>();
            _searchStack = new HashSet<ExtendedGrammarProduction>();
        }

        public IReadOnlyDictionary<INonTerminal, IList<ITerm>> Calculate()
        {
            AddFirstSets();
            return new ReadOnlyDictionary<INonTerminal, IList<ITerm>>(_firsts);
        }

        private void AddFirstSets(INonTerminal nonTerminal = null)
        {
            foreach (var extendedGrammarProduction in _extendedGrammarProductions
                .Where(p => (p.Production.LeftNonTerminal == nonTerminal || nonTerminal == null) && !_searchStack.Contains(p)))
            {
                var allRulesContainsEpsilon = true;
                foreach (var term in extendedGrammarProduction.Production.ProductionDefinition)
                {
                    if (term is ITerminal terminal)
                    {
                        AddFirst(extendedGrammarProduction.Production.LeftNonTerminal, terminal);
                        allRulesContainsEpsilon = false;
                        break;
                    }

                    if (term is INonTerminal rNonTerminal)
                    {
                        _searchStack.Add(extendedGrammarProduction);
                        AddFirstSets(rNonTerminal);
                        _searchStack.Remove(extendedGrammarProduction);

                        AddFirsts(extendedGrammarProduction.Production.LeftNonTerminal, _firsts[rNonTerminal]);

                        // If it doesn't contain the empty terminal, then stop
                        if (!_firsts[rNonTerminal].Contains(EmptyTerm.Empty))
                        {
                            allRulesContainsEpsilon = false;
                            break;
                        }
                    }
                }

                if (allRulesContainsEpsilon)
                {
                    AddFirst(extendedGrammarProduction.Production.LeftNonTerminal, EmptyTerm.Empty);
                }
            }
        }

        private bool AddFirsts(INonTerminal nonTerminal, IEnumerable<ITerm> terms)
        {
            var added = false;
            foreach (var terminal in terms)
            {
                added |= AddFirst(nonTerminal, terminal);
            }

            return added;
        }

        private bool AddFirst(INonTerminal nonTerminal, ITerm term)
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