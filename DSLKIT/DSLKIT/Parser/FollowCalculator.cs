using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser.ExtendedGrammar;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    /// <summary>
    /// Conventions: a, b, and c represent a terminal or non-terminal.
    /// a* represents zero or more terminals or non-terminals (possibly both).
    /// a+ represents one or more...
    /// D is a non-terminal.
    /// 1. Place an End of Input token($) into the Root rule's follow set.
    /// 2. Suppose we have a rule R → a* Db.
    /// Everything in First(b)(except for ε) is added to Follow(D).
    /// If First(b) contains ε then everything in Follow(R) is put in Follow(D).
    /// 3. Finally, if we have a rule R → a* D, then everything in Follow(R) is placed in Follow(D).
    /// 4. The Follow set of a terminal is an empty set.
    /// </summary>
    public class FollowCalculator
    {
        private readonly INonTerminal _root;
        private readonly IEofTerminal _eof;
        private readonly IEnumerable<ExProduction> _exProductions;
        private readonly IDictionary<IExNonTerminal, IList<ITerm>> _firsts;

        private readonly Dictionary<IExNonTerminal, IList<ITerm>> _follow =
            new Dictionary<IExNonTerminal, IList<ITerm>>();

        private readonly IEnumerable<IExNonTerminal> _exNonTerminals;

        public FollowCalculator(INonTerminal root, IEofTerminal eof,
            IEnumerable<ExProduction> exProductions,
            IDictionary<IExNonTerminal, IList<ITerm>> firsts)
        {
            _root = root;
            _eof = eof;
            _exProductions = exProductions;
            _firsts = firsts;
            _exNonTerminals = GetExNonTerminals().ToList();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public IDictionary<IExNonTerminal, IList<ITerm>> Calculate()
        {
            // TODO: Add sets information fot the start rule
            _follow.Add(_root.ToExNonTerminal(null, null), new List<ITerm> { _eof });
            foreach (var nonterminal in _exNonTerminals)
            {
                var visited = new HashSet<IExNonTerminal>();
                RecursiveFollow(nonterminal, nonterminal, visited);
            }

            return _follow;

            //bool updated;
            //do
            //{
            //    updated = false;
            //    foreach (var exProduction in _exProductions)
            //    {
            //        var rule = exProduction.ExProductionDefinition;
            //        var r = exProduction.ExLeftNonTerminal;
            //        var count = rule.Count;

            //        if (count == 3)
            //        {
            //            if (rule[count - 2] is IExNonTerminal B)
            //            {
            //                // If r -> pBq is a production, where p, B and q are any grammar symbols,
            //                // then everything in FIRST(q)  except Є is in FOLLOW(B).
            //                var firstsOfq = GetFirsts(rule[count - 1]);
            //                var firstsOfqExceptEpsilon = firstsOfq.Where(i => !(i is EmptyTerm));
            //                var firstsOfqContainEpsilon = firstsOfq.Any(i => i is EmptyTerm);
            //                if (!firstsOfqContainEpsilon)
            //                {
            //                    updated |= AddFollow(B, firstsOfqExceptEpsilon);
            //                }
            //                // If r->pBq is a production and FIRST(q) contains Є, 
            //                // then FOLLOW(B) contains { FIRST(q) – Є } U FOLLOW(r)
            //                else
            //                {
            //                    updated |= AddFollow(B, firstsOfqExceptEpsilon);
            //                    updated |= AddFollow(B, GetFollow(r));
            //                }
            //                continue;
            //            }
            //        }
            //        if (count == 2)
            //        {
            //            // If r->pB is a production, then everything in FOLLOW(r) is in FOLLOW(B).
            //            if (rule[count - 1] is IExNonTerminal B)
            //            {
            //                updated |= AddFollow(B, GetFollow(r));
            //            }
            //        }

            //        //if (rule.Count >= 2)
            //        //{
            //        //    var b = rule[count - 1];
            //        //    if (rule[count - 2] is IExNonTerminal d)
            //        //    {
            //        //        updated |= AddFollow(d, GetFirsts(b).Where(i => !(i is EmptyTerm)).ToList());
            //        //        if (GetFirsts(b).Contains(EmptyTerm.Empty))
            //        //        {
            //        //            updated |= AddFollow(d, GetFollow(r));
            //        //        }
            //        //    }
            //        //}

            //        //if (rule[count - 1] is IExNonTerminal d1)
            //        //{
            //        //    updated |= AddFollow(d1, GetFollow(r));
            //        //}
            //    }
            //} while (updated);

            //return _follow;
        }

        private IEnumerable<IExNonTerminal> GetExNonTerminals()
        {
            var result = new HashSet<IExNonTerminal>();

            foreach (var exProduction in _exProductions)
            {
                result.Add(exProduction.ExLeftNonTerminal);
                foreach (var exNonTerminal in exProduction.ExProductionDefinition.OfType<IExNonTerminal>())
                {
                    result.Add(exNonTerminal);
                }
            }

            return result;
        }


        public IDictionary<IExNonTerminal, IList<ITerm>> Calc()
        {
            foreach (var nonterminal in _exNonTerminals)
            {
                var visited = new HashSet<IExNonTerminal>();
                RecursiveFollow(nonterminal, nonterminal, visited);
            }



            return _follow;
        }

        public void RecursiveFollow(
            IExNonTerminal startExNonTerminal,
            IExNonTerminal currentNonTerminal,
            HashSet<IExNonTerminal> visited)
        {
            if (visited.Contains(currentNonTerminal))
            {
                return;
            }

            visited.Add(currentNonTerminal);
            foreach (var nonterminal  in _exNonTerminals)
            {
                foreach (var exProduction in _exProductions.Where(i => i.ExLeftNonTerminal.Equals(nonterminal)))
                {
                    var currentProductionLength = exProduction.ExProductionDefinition.Count;
                    for (var index = 0; index < currentProductionLength; index++)
                    {
                        var exTerm = exProduction.ExProductionDefinition[index];
                        if (exTerm != currentNonTerminal)
                        {
                            continue;
                        }
                        // nextTerm
                        var k = index + 1;

                        while (k < currentProductionLength)
                        {
                            var exNonTerminal = exProduction.ExProductionDefinition[k] as IExNonTerminal;
                            if (exNonTerminal == null)
                            {
                                break;
                            }

                            AddFollow(startExNonTerminal, GetFirsts(exNonTerminal));
                            if (!HasEpsilon(exNonTerminal))
                            {
                                break;

                            }
                            k++;
                        }

                        if (k < currentProductionLength)
                        {
                            var exTerminal = exProduction.ExProductionDefinition[k] as IExTerminal;
                            if (exTerminal != null)
                            {
                                AddFollow(startExNonTerminal, new ITerm[] { exTerminal.Terminal });
                            }
                        }

                        if (k == currentProductionLength)
                        {
                            if (nonterminal == startExNonTerminal)
                            {
                                AddFollow(startExNonTerminal, _eof);
                            }
                            RecursiveFollow(startExNonTerminal, nonterminal, visited);
                        }
                    }
                }
            }
        }


        private bool HasEpsilon(IExTerm exTerm)
        {
            if (exTerm.Term == EmptyTerm.Empty)
            {
                return true;
            }

            if (exTerm is IExTerminal)
            {
                return false;
            }

            if (!(exTerm is IExNonTerminal exNonTerminal))
            {
                throw new InvalidOperationException($"{nameof(exTerm)} should be IExNonTerminal or IExTerminal");
            }

            if (!_firsts.TryGetValue(exNonTerminal, out var firsts))
            {
                throw new InvalidOperationException($"{nameof(exTerm)} should be presented in Firsts collection");
            }

            return firsts.Any(i => i == EmptyTerm.Empty);
        }


        private bool AddFollow(IExNonTerminal exNonTerminal, ITerm term)
        {
            if (_follow.TryGetValue(exNonTerminal, out var follow))
            {
                if (follow.Contains(term))
                {
                    return false;
                }

                follow.Add(term);
                return true;
            }

            _follow[exNonTerminal] = new List<ITerm> { term };
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

        private IList<ITerm> GetFollow(IExNonTerminal exNonTerminal)
        {
            return !_follow.TryGetValue(exNonTerminal, out var follow) ? new List<ITerm>() : follow;
        }

        private IList<ITerm> GetFirsts(IExTerm term)
        {
            switch (term)
            {
                case IExTerminal exTerminal:
                    return new List<ITerm> { exTerminal.Terminal };
                case IExNonTerminal exNonTerminal:
                    return _firsts[exNonTerminal];
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}