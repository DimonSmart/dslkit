using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public class ExProduction
    {
        private readonly Production _production;
        public readonly IExNonTerminal ExLeftNonTerminal;
        public readonly IList<IExTerm> ExProductionDefinition;

        public ExProduction(
            Production production,
            IExNonTerminal exLeftNonTerminal,
            IList<IExTerm> exProductionDefinition)
        {
            _production = production;
            ExLeftNonTerminal = exLeftNonTerminal;
            ExProductionDefinition = exProductionDefinition;
        }

        public override string ToString()
        {
            return _production.ProductionToString(-1, (pos, str) =>
            {
                if (pos == -1)
                {
                    var from = ExLeftNonTerminal.From.SetNumber;
                    var to = ExLeftNonTerminal.To?.SetNumber == null ? "$" : ExLeftNonTerminal.To.SetNumber.ToString();
                    return $"{from}_{ExLeftNonTerminal.NonTerminal}_{to}";
                }

                var pFrom = ExProductionDefinition[pos].From.SetNumber;
                var pTo = ExProductionDefinition[pos].To.SetNumber;
                return $"{pFrom}_{ExProductionDefinition[pos]}_{pTo}";
            });
        }
    }
}