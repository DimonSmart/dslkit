using System.Collections.Generic;
using System.Text;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public class ExProduction
    {
        public readonly Production Production;
        public readonly IExNonTerminal ExLeftNonTerminal;
        public readonly IReadOnlyList<IExTerm> ExProductionDefinition;

        public ExProduction(
            Production production,
            IExNonTerminal exLeftNonTerminal,
            IReadOnlyList<IExTerm> exProductionDefinition)
        {
            Production = production;
            ExLeftNonTerminal = exLeftNonTerminal;
            ExProductionDefinition = exProductionDefinition;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(ExLeftNonTerminal);
            sb.Append(" → ");
            sb.Append(string.Join(" ", ExProductionDefinition));
            return sb.ToString();
        }
    }
}
