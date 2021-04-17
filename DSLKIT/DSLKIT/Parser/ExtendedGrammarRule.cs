using DSLKIT.NonTerminals;
using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public class ExtendedGrammarRule : ProductionBase<ExtendedGrammarTerm>
    {
        public ExtendedGrammarRule(INonTerminal leftNonTerminal, IList<ExtendedGrammarTerm> productionDefinition) : base(leftNonTerminal, productionDefinition)
        {
        }
    }
}