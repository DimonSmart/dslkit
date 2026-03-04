using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser.ExtendedGrammar;
using DSLKIT.SpecialTerms;

namespace DSLKIT.Parser
{
    /// <summary>
    ///     Conventions: a, b, and c represent a terminal or non-terminal.
    ///     a* represents zero or more terminals or non-terminals (possibly both).
    ///     a+ represents one or more...
    ///     D is a non-terminal.
    ///     1. Place an End of Input token($) into the Root rule's follow set.
    ///     2. Suppose we have a rule R → a* Db. Everything in First(b)(except ε) is added to Follow(D).
    ///     2.1 If First(b) contains ε then everything in Follow(R) is put in Follow(D).
    ///     3. Finally, if we have a rule R → a* D, then everything in Follow(R) is placed in Follow(D).
    ///     4. The Follow set of a terminal is an empty set.
    /// </summary>
    public class FollowCalculator
    {
        private readonly IEofTerminal _eof;
        private readonly IEnumerable<ExProduction> _exProductions;
        private readonly IReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>> _firsts;
        private readonly Dictionary<IExTerm, FirstInfo> _firstInfoByTerm = [];

        private readonly Dictionary<IExNonTerminal, HashSet<ITerm>> _follow =
            [];

        private readonly INonTerminal _root;


        public FollowCalculator(INonTerminal root, IEofTerminal eof,
            IEnumerable<ExProduction> exProductions,
            IReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>> firsts)
        {
            _root = root;
            _eof = eof;
            _exProductions = exProductions;
            _firsts = firsts;
        }

        public IReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>> Calculate()
        {
            _follow.Add(_exProductions.Select(p => p.ExLeftNonTerminal)
                .Single(p => p.NonTerminal == _root && p.To == null), new HashSet<ITerm> { _eof });

            bool updated;
            do
            {
                updated = false;
                foreach (var exProduction in _exProductions)
                {
                    var rule = exProduction.ExProductionDefinition;
                    var r = exProduction.ExLeftNonTerminal;
                    var count = rule.Count;
                    if (count >= 2)
                    {
                        // 2.Suppose we have a rule R → a* DB.
                        // Everything in First(B)(except for ε) is added to Follow(D).
                        for (var i = 0; i < rule.Count - 1; i++)
                        {
                            var termD = rule[i];
                            var termB = rule[i + 1];
                            if (termD is not IExNonTerminal exNonTerminalD)
                            {
                                continue;
                            }

                            var firstInfo = GetFirstInfo(termB);
                            updated |= AddFollow(exNonTerminalD, firstInfo.NonEmptyTerms);
                        }

                        // Add back cycle with epsilon checking
                        // 2.Suppose we have a rule R → a* DB.
                        // 2.1 If First(B) contains ε then everything in Follow(R) is put in Follow(D)
                        for (var i = rule.Count - 2; i >= 0; i--)
                        {
                            var termD = rule[i];
                            var termB = rule[i + 1];

                            var firstInfo = GetFirstInfo(termB);
                            if (firstInfo.HasEpsilon && termD is IExNonTerminal exNonTerminalD)
                            {
                                updated |= AddFollow(exNonTerminalD, GetFollow(r));
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    // if we have a rule R → a* D, then everything in Follow(R) is placed in Follow(D).
                    if (rule[count - 1] is IExNonTerminal lastNonTerminal)
                    {
                        updated |= AddFollow(lastNonTerminal, GetFollow(r));
                    }
                }
            } while (updated);

            return new ReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>>(
                _follow.ToDictionary(
                    pair => pair.Key,
                    pair => (IReadOnlyCollection<ITerm>)pair.Value.ToList()));
        }

        private bool AddFollow(IExNonTerminal exNonTerminal, ITerm term)
        {
            if (_follow.TryGetValue(exNonTerminal, out var follow))
            {
                return follow.Add(term);
            }

            _follow[exNonTerminal] = new HashSet<ITerm> { term };
            return true;
        }

        private bool AddFollow(IExNonTerminal d, IEnumerable<ITerm> follows)
        {
            var added = false;
            foreach (var follow in follows)
            {
                added |= AddFollow(d, follow);
            }

            return added;
        }

        private IReadOnlyCollection<ITerm> GetFollow(IExNonTerminal exNonTerminal)
        {
            return _follow.TryGetValue(exNonTerminal, out var follow) ? follow : [];
        }

        private IReadOnlyCollection<ITerm> GetFirsts(IExTerm term)
        {
            switch (term)
            {
                case IExTerminal exTerminal:
                    return [exTerminal.Terminal];
                case IExNonTerminal exNonTerminal:
                    return _firsts[exNonTerminal];
                default:
                    throw new InvalidOperationException();
            }
        }

        private FirstInfo GetFirstInfo(IExTerm term)
        {
            if (_firstInfoByTerm.TryGetValue(term, out var firstInfo))
            {
                return firstInfo;
            }

            var firsts = GetFirsts(term);
            var nonEmptyTerms = new List<ITerm>(firsts.Count);
            var hasEpsilon = false;
            foreach (var first in firsts)
            {
                if (first is EmptyTerm)
                {
                    hasEpsilon = true;
                    continue;
                }

                nonEmptyTerms.Add(first);
            }

            firstInfo = new FirstInfo(nonEmptyTerms, hasEpsilon);
            _firstInfoByTerm[term] = firstInfo;
            return firstInfo;
        }

        private readonly struct FirstInfo
        {
            public FirstInfo(IReadOnlyCollection<ITerm> nonEmptyTerms, bool hasEpsilon)
            {
                NonEmptyTerms = nonEmptyTerms;
                HasEpsilon = hasEpsilon;
            }

            public IReadOnlyCollection<ITerm> NonEmptyTerms { get; }
            public bool HasEpsilon { get; }
        }
    }
}
