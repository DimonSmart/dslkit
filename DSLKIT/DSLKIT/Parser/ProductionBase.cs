using DSLKIT.Base;
using DSLKIT.NonTerminals;
using System.Collections.Generic;
using System.Text;

namespace DSLKIT.Parser
{
    public class ProductionBase<T> where T : ITerm
    {
        public readonly INonTerminal LeftNonTerminal;
        public readonly IList<T> ProductionDefinition;

        public ProductionBase(INonTerminal leftNonTerminal, IList<T> productionDefinition)
        {
            LeftNonTerminal = leftNonTerminal;
            ProductionDefinition = productionDefinition;
        }

        public string ProductionToString(int dotPosition = -1)
        {
            const string dot = "● ";
            var sb = new StringBuilder();
            sb.Append(LeftNonTerminal.Name);
            sb.Append(" →");

            if (ProductionDefinition.Count == 0)
            {
                sb.Append(" ε");
            }
            else
            {
                for (var i = 0; i <= ProductionDefinition.Count; i++)
                {
                    sb.Append(" ");
                    if (i == dotPosition)
                    {
                        sb.Append(dot);
                    }

                    if (i < ProductionDefinition.Count)
                    {
                        sb.Append(ProductionDefinition[i].Name);
                    }
                }
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return ProductionToString();
        }
    }
}