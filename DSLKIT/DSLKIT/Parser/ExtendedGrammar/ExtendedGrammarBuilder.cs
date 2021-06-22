using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Parser
{
    public static class ExtendedGrammarBuilder
    {
        public static IEnumerable<ExProduction> Build(TranslationTable translationTable)
        {
            foreach (var set in translationTable.GetAllSets())
            {
                foreach (var rule in set.Rules.Where(i => i.DotPosition == 0))
                {
                    yield return CreateExtendedGrammarProduction(set, rule.Production, translationTable);
                }
            }
        }

        public static ExProduction CreateExtendedGrammarProduction(RuleSet set, Production production, TranslationTable translationTable)
        {
            translationTable.TryGetValue(production.LeftNonTerminal, set, out RuleSet rs);

            var exProductionDefinition = new List<IExTerm>();

            var currentSet = set;
            foreach (var term in production.ProductionDefinition)
            {
                translationTable.TryGetValue(term, currentSet, out RuleSet nextSet);

               

                exProductionDefinition.Add(term.ToExTerm(currentSet, nextSet));
                currentSet = nextSet;
                if (currentSet == null)
                {
                    throw new System.Exception($"CreateExtendedGrammarProduction failed for set:{set.SetNumber}, Production:{production}");
                }
            }
            return new ExProduction(production, production.LeftNonTerminal.ToExNonTerminal(set, rs), exProductionDefinition);
        }
    }
}