using System;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Parser.ExtendedGrammar
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

        public static ExProduction CreateExtendedGrammarProduction(RuleSet set, Production production,
            TranslationTable translationTable)
        {
            translationTable.TryGetValue(production.LeftNonTerminal, set, out var startRuleSet);

            var exProductionDefinition = new List<IExTerm>();

            var currentSet = set;
            foreach (var term in production.ProductionDefinition)
            {
                translationTable.TryGetValue(term, currentSet, out var nextSet);
                exProductionDefinition.Add(term.ToExTerm(currentSet, nextSet));
                if (nextSet == null)
                {
                    throw new Exception(
                        $"CreateExtendedGrammarProduction failed for set:{set.SetNumber}, Production:{production}");
                }

                currentSet = nextSet;
            }

            return new ExProduction(production, production.LeftNonTerminal.ToExNonTerminal(set, startRuleSet),
                exProductionDefinition);
        }
    }
}
