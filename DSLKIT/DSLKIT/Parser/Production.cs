using DSLKIT.Base;
using DSLKIT.NonTerminals;
using System.Collections.Generic;

namespace DSLKIT.Parser
{

    public class Production : ProductionBase<ITerm>
    {
        public Production(INonTerminal leftNonTerminal, IList<ITerm> productionDefinition) : base(leftNonTerminal, productionDefinition)
        {
        }
    }
}