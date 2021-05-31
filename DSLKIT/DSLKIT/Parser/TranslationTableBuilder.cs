using System;
using DSLKIT.Base;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.SpecialTerms;

namespace DSLKIT.Parser
{
    public static class TranslationTableBuilder
    {
        public static TranslationTable Build(IEnumerable<RuleSet> ruleSets)
        {
            var sets = ruleSets.ToList();
            var dict = new Dictionary<KeyValuePair<ITerm, RuleSet>, RuleSet>();
            foreach (var set in sets)
            {
                foreach (var arrow in set.Arrows)
                {
                    dict[new KeyValuePair<ITerm, RuleSet>(arrow.Key, set)] = arrow.Value;
                }
            }

            return new TranslationTable(dict);
        }
    }



    public interface IActionItem
    {
    }

    public  class Accept : IActionItem
    {
        private Accept()
        {
        }

        public static readonly Lazy<Accept> Lazy = new Lazy<Accept>(() => new Accept());
        public static Accept Instance => Lazy.Value;
    }

    public class ActionAndGotoTableBuilder
    {
        protected readonly Grammar Grammar;
        protected readonly IEnumerable<RuleSet> RuleSets;
        public Dictionary<KeyValuePair<RuleSet, ITerm>, IActionItem> ActionTable =
            new Dictionary<KeyValuePair<RuleSet, ITerm>, IActionItem>();
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
                    ActionTable[new KeyValuePair<RuleSet, ITerm>(ruleSet, EofTerminal.Instance)] = Accept.Instance;
                }
            }
        }

        private bool ContainStartingRuleWithPointerAtTheEnd(RuleSet ruleSet)
        {
            return ruleSet.Rules.Any(rule => rule.IsFinished && rule.Production.LeftNonTerminal == Grammar.Root);
        }
    }


}