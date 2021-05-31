using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.SpecialTerms;

namespace DSLKIT.Parser
{
    public class ActionAndGotoTableBuilder
    {
        protected readonly Grammar Grammar;
        protected readonly IEnumerable<RuleSet> RuleSets;
        protected readonly TranslationTable TranslationTable;
        public readonly ActionAndGotoTable ActionAndGotoTable;

        public ActionAndGotoTableBuilder(Grammar grammar, IEnumerable<RuleSet> ruleSets,
            TranslationTable translationTable)
        {
            Grammar = grammar;
            RuleSets = ruleSets;
            TranslationTable = translationTable;
            ActionAndGotoTable = new ActionAndGotoTable(Grammar);
            Stage1();
            Stage2();
        }

        /// <summary>
        /// Add a column for the end of input, labeled $.
        /// Place an "accept" in the $ column whenever the item set contains an item where the pointer is at the end of the starting rule
        /// (in our example "S → N •").
        /// </summary>
        public void Stage1()
        {
            foreach (var ruleSet in RuleSets)
            {
                if (ContainStartingRuleWithPointerAtTheEnd(ruleSet))
                {
                    ActionAndGotoTable.ActionTable[new KeyValuePair<ITerm, RuleSet>(EofTerminal.Instance, ruleSet)] = AcceptAction.Instance;
                }
            }
        }

        /// <summary>
        /// Directly copy the Translation Table's non-terminal columns as GOTOs
        /// </summary>
        public void Stage2()
        {
            foreach (var record in TranslationTable.GetAllRecords())
            {
                if (record.Key.Key is INonTerminal nonTerminal)
                {
                    ActionAndGotoTable.GotoTable[new KeyValuePair<INonTerminal, RuleSet>(nonTerminal, record.Key.Value)] = record.Value;
                }
            }
        }


        private bool ContainStartingRuleWithPointerAtTheEnd(RuleSet ruleSet)
        {
            return ruleSet.Rules.Any(rule => rule.IsFinished && rule.Production.LeftNonTerminal == Grammar.Root);
        }
    }
}