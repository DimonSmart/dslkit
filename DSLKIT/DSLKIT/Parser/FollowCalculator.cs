using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DSLKIT.Parser
{
    public class FollowCalculator
    {
        readonly Dictionary<INonTerminal, IList<ITerminal>> _follow = new Dictionary<INonTerminal, IList<ITerminal>>();
        private readonly IGrammar _grammar;

        public FollowCalculator(IGrammar grammar)
        {
            _grammar = grammar;
        }

        public IReadOnlyDictionary<INonTerminal, IList<ITerminal>> Calculate()
        {
            // Conventions: a, b, and c represent a terminal or non-terminal.
            //    a* represents zero or more terminals or non-terminals (possibly both).
            //    a+ represents one or more...
            //    D is a non-terminal.
            // 1. Place an End of Input token($) into the Root rule's follow set.
            // 2. Suppose we have a rule R → a* Db. 
            //    Everything in First(b)(except for ε) is added to Follow(D).
            //    If First(b) contains ε then everything in Follow(R) is put in Follow(D).
            // 3. Finally, if we have a rule R → a* D, then everything in Follow(R) is placed in Follow(D).
            // 4/ The Follow set of a terminal is an empty set.

            // 1. Place an End of Input token($) into the Root rule's follow set.
            _follow.Add(_grammar.Root, new List<ITerminal> { _grammar.Eof });

            bool updated;
            do
            {
                updated = false;
                foreach (var production in _grammar.Productions)
                {
                    var rule = production.ProductionDefinition;
                    var R = production.LeftNonTerminal;
                    var count = rule.Count;
                    // 2
                    if (rule.Count >= 2)
                    {
                        // R → a* Db
                        var b = rule[count - 1];
                        var D = rule[count - 2] as INonTerminal;
                        if (D != null)
                        {
                            // (except for ε)
                            if (b != EmptyTerm.Empty)
                            {
                                updated |= AddFollow(D, GetFirsts(b));
                            }

                            var first_of_b = GetFirsts(b);
                            //if (first_of_b.Contains(EmptyTerm.Empty))
                            //{
                            //    updated |= AddFollow(D, GetFollow(R));
                            //}
                        }
                    }

                    var D1 = rule[count - 1] as INonTerminal;
                    if (D1 != null)
                    {
                        updated |= AddFollow(D1, GetFollow(R));
                    }
                }
            } while (updated);

            return new ReadOnlyDictionary<INonTerminal, IList<ITerminal>>(_follow);
        }

        private bool AddFollow(INonTerminal nonTerminal, ITerminal terminal)
        {
            if (_follow.TryGetValue(nonTerminal, out var follow))
            {
                if (follow.Contains(terminal))
                {
                    return false;
                }

                follow.Add(terminal);
                return true;
            }

            _follow[nonTerminal] = new List<ITerminal> { terminal };
            return true;
        }

        private bool AddFollow(INonTerminal d, IList<ITerminal> follows)
        {
            var added = false;
            foreach (var follow in follows) //.Where(i => i != EmptyTerm.Empty))
            {
                added |= AddFollow(d, follow);
            }
            return added;
        }

        private IList<ITerminal> GetFollow(INonTerminal nonTerminal)
        {
            if (_follow.TryGetValue(nonTerminal, out IList<ITerminal> follow))
            {
                return follow;
            }

            return new List<ITerminal>();
        }

        private IList<ITerminal> GetFirsts(ITerm term)
        {
            switch (term)
            {
                case ITerminal terminal:
                    return new List<ITerminal> { terminal };
                case INonTerminal nontermanal:
                    return _grammar.Firsts[nontermanal];
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}