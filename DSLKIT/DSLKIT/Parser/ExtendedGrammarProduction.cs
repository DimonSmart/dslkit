using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public class ExtendedGrammarProduction
    {
        public readonly FromTo LeftNonTerminalFromTo;
        public readonly IList<FromTo> ProductionDefinitionFromTo;
        public readonly Production Production;

        public ExtendedGrammarProduction(Production production, FromTo leftNonTerminalFromTo, IList<FromTo> productionDefinitionFromTo)
        {
            ProductionDefinitionFromTo = new List<FromTo>(production.ProductionDefinition.Count);
            Production = production;
            LeftNonTerminalFromTo = leftNonTerminalFromTo;
            ProductionDefinitionFromTo = productionDefinitionFromTo;
        }

        public override string ToString()
        {
            return Production.ProductionToString(-1, (pos, str) =>
            {
                if (pos == -1)
                {
                    var from = LeftNonTerminalFromTo.From.SetNumber;
                    var to = LeftNonTerminalFromTo.To?.SetNumber == null ? "$" : LeftNonTerminalFromTo.To.SetNumber.ToString();
                    return $"{from}_{Production.LeftNonTerminal}_{to}";
                }

                var pFrom = ProductionDefinitionFromTo[pos].From.SetNumber;
                var pTo = ProductionDefinitionFromTo[pos].To.SetNumber;
                return $"{pFrom}_{Production.ProductionDefinition[pos]}_{pTo}";
            });
        }
    }
}