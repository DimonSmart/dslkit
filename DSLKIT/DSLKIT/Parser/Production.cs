using System.Collections.Generic;
using System.Text;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;

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

        public static string ProductionToString(Production production, int dotPosition = -1)
        {
            const string dot = " ● ";
            var sb = new StringBuilder();
            sb.Append(production.LeftNonTerminal.Name);
            sb.Append("\t→\t");
            for (var i = 0; i < production.ProductionDefinition.Count; i++)
            {
                if (i == dotPosition)
                {
                    sb.Append(dot);
                }

                sb.Append(production.ProductionDefinition[i].Name);
                sb.Append(" ");
            }

            if (dotPosition == production.ProductionDefinition.Count)
            {
                sb.Append(dot);
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return ProductionToString(this);
        }
    }
}