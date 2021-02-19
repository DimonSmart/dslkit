using DSLKIT.Base;
using DSLKIT.NonTerminals;
using System.Collections.Generic;
using System.Text;

namespace DSLKIT.Parser
{
    public class Production
    {
        public readonly INonTerminal LeftNonTerminal;
        public readonly IList<ITerm> ProductionDefinition;

        public Production(INonTerminal leftNonTerminal, IList<ITerm> productionDefinition)
        {
            LeftNonTerminal = leftNonTerminal;
            ProductionDefinition = productionDefinition;
        }

        public string ProductionToString(int dotPosition = -1)
        {
            const string dot = "● ";
            var sb = new StringBuilder();
            sb.Append(LeftNonTerminal.Name);
            sb.Append(" → ");
            for (var i = 0; i < ProductionDefinition.Count; i++)
            {
                if (i == dotPosition)
                {
                    sb.Append(dot);
                }

                sb.Append(ProductionDefinition[i].Name);
                sb.Append(" ");
            }

            if (dotPosition == ProductionDefinition.Count)
            {
                sb.Append(dot);
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return ProductionToString();
        }
    }
}