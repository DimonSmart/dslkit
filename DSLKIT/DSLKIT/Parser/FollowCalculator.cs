using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    /// <summary>
    ///   Conventions: a, b, and c represent a terminal or non-terminal.
    ///    a* represents zero or more terminals or non-terminals (possibly both).
    ///    a+ represents one or more...
    ///    D is a non-terminal.
    /// 1. Place an End of Input token($) into the Root rule's follow set.
    /// 2. Suppose we have a rule R → a* Db. 
    ///    Everything in First(b)(except for ε) is added to Follow(D).
    ///    If First(b) contains ε then everything in Follow(R) is put in Follow(D).
    /// 3. Finally, if we have a rule R → a* D, then everything in Follow(R) is placed in Follow(D).
    /// 4. The Follow set of a terminal is an empty set.
    /// </summary>
    public class FollowCalculator
    {
        private readonly Dictionary<INonTerminal, IList<ITerm>> _follow = new Dictionary<INonTerminal, IList<ITerm>>();
        private readonly IGrammar _grammar;

        public FollowCalculator(IGrammar grammar)
        {
            _grammar = grammar;
        }

        public IReadOnlyDictionary<INonTerminal, IList<ITerm>> Calculate()
        {
            _follow.Add(_grammar.Root, new List<ITerm> {_grammar.Eof});

            bool updated;
            do
            {
                updated = false;
                foreach (var production in _grammar.Productions)
                {
                    var rule = production.ProductionDefinition;
                    var r = production.LeftNonTerminal;
                    var count = rule.Count;
                    if (rule.Count >= 2)
                    {
                        var b = rule[count - 1];
                        if (rule[count - 2] is INonTerminal d)
                        {
                            updated |= AddFollow(d, GetFirsts(b).Where(i => !(i is EmptyTerm)).ToList());
                            if (GetFirsts(b).Contains(EmptyTerm.Empty))
                            {
                                updated |= AddFollow(d, GetFollow(r));
                            }
                        }
                    }

                    if (rule[count - 1] is INonTerminal d1)
                    {
                        updated |= AddFollow(d1, GetFollow(r));
                    }
                }
            } while (updated);

            return new ReadOnlyDictionary<INonTerminal, IList<ITerm>>(_follow);
        }

        private bool AddFollow(INonTerminal nonTerminal, ITerm term)
        {
            if (_follow.TryGetValue(nonTerminal, out var follow))
            {
                if (follow.Contains(term))
                {
                    return false;
                }

                follow.Add(term);
                return true;
            }

            _follow[nonTerminal] = new List<ITerm> {term};
            return true;
        }

        private bool AddFollow(INonTerminal d, IList<ITerm> follows)
        {
            var added = false;
            foreach (var follow in follows)
            {
                added |= AddFollow(d, follow);
            }

            return added;
        }

        private IList<ITerm> GetFollow(INonTerminal nonTerminal)
        {
            return !_follow.TryGetValue(nonTerminal, out var follow) ? new List<ITerm>() : follow;
        }

        private IList<ITerm> GetFirsts(ITerm term)
        {
            switch (term)
            {
                case ITerminal terminal:
                    return new List<ITerm> {terminal};
                case INonTerminal nonTerminal:
                    return _grammar.Firsts[nonTerminal];
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}