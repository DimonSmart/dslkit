using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DSLKIT.Base;
using DSLKIT.Parser.ExtendedGrammar;
using DSLKIT.SpecialTerms;

namespace DSLKIT.Parser
{
    public class FirstsCalculator
    {
        private static readonly ITerm Empty = EmptyTerm.Empty;
        private readonly IReadOnlyList<ExProduction> _exProductions;
        private readonly Dictionary<IExNonTerminal, HashSet<ITerm>> _firsts;

        public FirstsCalculator(IEnumerable<ExProduction> exProductions)
        {
            _exProductions = exProductions as IReadOnlyList<ExProduction> ?? [.. exProductions];
            _firsts = [];
        }

        public IReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>> Calculate()
        {
            InitializeFirstSets();

            bool updated;
            do
            {
                updated = false;
                foreach (var exProduction in _exProductions)
                {
                    var leftFirsts = _firsts[exProduction.ExLeftNonTerminal];
                    var allTermsCanBeEmpty = true;

                    foreach (var exTerm in exProduction.ExProductionDefinition)
                    {
                        if (exTerm is IExTerminal exTerminal)
                        {
                            updated |= leftFirsts.Add(exTerminal.Terminal);
                            allTermsCanBeEmpty = false;
                            break;
                        }

                        if (exTerm is IExNonTerminal exNonTerminal)
                        {
                            var rightFirsts = _firsts[exNonTerminal];
                            var containsEmpty = false;

                            foreach (var rightFirst in rightFirsts)
                            {
                                if (ReferenceEquals(rightFirst, Empty))
                                {
                                    containsEmpty = true;
                                }

                                updated |= leftFirsts.Add(rightFirst);
                            }

                            if (!containsEmpty)
                            {
                                allTermsCanBeEmpty = false;
                                break;
                            }

                            continue;
                        }

                        if (exTerm is IExEmptyTerm)
                        {
                            continue;
                        }

                        allTermsCanBeEmpty = false;
                        break;
                    }

                    if (allTermsCanBeEmpty)
                    {
                        updated |= leftFirsts.Add(Empty);
                    }
                }
            } while (updated);

            var snapshot = new Dictionary<IExNonTerminal, IReadOnlyCollection<ITerm>>(_firsts.Count);
            foreach (var pair in _firsts)
            {
                snapshot[pair.Key] = [.. pair.Value];
            }

            return new ReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>>(snapshot);
        }

        private void InitializeFirstSets()
        {
            foreach (var exProduction in _exProductions)
            {
                if (_firsts.ContainsKey(exProduction.ExLeftNonTerminal))
                {
                    continue;
                }

                _firsts[exProduction.ExLeftNonTerminal] = [];
            }
        }
    }
}
