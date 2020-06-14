using System.Collections.Generic;
using System.Text;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class Production
    {
        public readonly NonTerminal LeftNonTerminal;
        public readonly IList<ITerm> RightValues = new List<ITerm>();

        public Production(NonTerminal leftNonTerminal)
        {
            LeftNonTerminal = leftNonTerminal;
        }

        public static string ProductionToString(Production production, int dotPosition = -1)
        {
            const string dot = " ● ";
            var sb = new StringBuilder();
            sb.Append(production.LeftNonTerminal.Name);
            sb.Append(" → ");
            for (var i = 0; i < production.RightValues.Count; i++)
            {
                if (i == dotPosition)
                {
                    sb.Append(dot);
                }

                sb.Append(production.RightValues[i].Name);
                sb.Append(" ");
            }

            if (dotPosition == production.RightValues.Count)
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