using DSLKIT.NonTerminals;
using DSLKIT.Terminals;
using System;
using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public class FollowCalculator
    {
        private readonly IEnumerable<Production> _productions;
        public FollowCalculator(IEnumerable<Production> productions)
        {
            _productions = productions;
        }

        public IReadOnlyDictionary<INonTerminal, IList<ITerminal>> Calculate()
        {
            throw new NotImplementedException();
        }
    }
}