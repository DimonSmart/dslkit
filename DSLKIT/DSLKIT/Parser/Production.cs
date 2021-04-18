using DSLKIT.Base;
using DSLKIT.NonTerminals;
using System;
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

        public string ProductionToString(int dotPosition = -1, Func<int, string, string> formatter = null)
        {
            const string dot = "● ";
            var sb = new StringBuilder();
            sb.Append(formatter == null ? LeftNonTerminal.Name : formatter(-1, LeftNonTerminal.Name));
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
                        var label = ProductionDefinition[i].Name;
                        label = formatter == null ? label : formatter(i, label);
                        sb.Append(label);
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