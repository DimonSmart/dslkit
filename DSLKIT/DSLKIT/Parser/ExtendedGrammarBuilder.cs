using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Parser
{
    public static class ExtendedGrammarBuilder
    {
        public static IEnumerable<ExtendedGrammarProduction> Build(TranslationTable translationTable)
        {
            foreach (var set in translationTable.GetAllSets())
            {
                foreach (var rule in set.Rules.Where(i => i.DotPosition == 0))
                {
                    yield return CreateExtendedGrammarProduction(set, rule.Production, translationTable);
                }
            }
        }

        public static ExtendedGrammarProduction CreateExtendedGrammarProduction(RuleSet set, Production production, TranslationTable translationTable)
        {
            translationTable.TryGetValue(production.LeftNonTerminal, set, out RuleSet rs);

            var productionDefinitionFromTo = new List<FromTo>();

            var currentSet = set;
            foreach (var term in production.ProductionDefinition)
            {
                RuleSet nextSet = null;
                translationTable.TryGetValue(term, currentSet, out nextSet);

                productionDefinitionFromTo.Add(new FromTo(currentSet, nextSet));
                currentSet = nextSet;
                if (currentSet == null)
                {
                    throw new System.Exception($"CreateExtendedGrammarProduction failed for set:{set.SetNumber}, Production:{production}");
                }
            }
            return new ExtendedGrammarProduction(production, new FromTo(set, rs), productionDefinitionFromTo);
        }
    }
}