using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.SpecialTerms;

namespace DSLKIT.Parser
{
    public class ActionAndGotoTable
    {
        public Dictionary<KeyValuePair<RuleSet, ITerm>, IActionItem> ActionTable =
            new Dictionary<KeyValuePair<RuleSet, ITerm>, IActionItem>();

        public ActionAndGotoTable()
        {

        }
    }

    public class ActionAndGotoTableBuilder
    {
        protected readonly Grammar Grammar;
        protected readonly IEnumerable<RuleSet> RuleSets;
        public readonly ActionAndGotoTable ActionAndGotoTable = new ActionAndGotoTable();

        public ActionAndGotoTableBuilder(Grammar grammar, IEnumerable<RuleSet> ruleSets)
        {
            Grammar = grammar;
            RuleSets = ruleSets;
            Stage1();
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
                    ActionAndGotoTable.ActionTable[new KeyValuePair<RuleSet, ITerm>(ruleSet, EofTerminal.Instance)] = AcceptAction.Instance;
                }
            }
        }

        private bool ContainStartingRuleWithPointerAtTheEnd(RuleSet ruleSet)
        {
            return ruleSet.Rules.Any(rule => rule.IsFinished && rule.Production.LeftNonTerminal == Grammar.Root);
        }
    }
}