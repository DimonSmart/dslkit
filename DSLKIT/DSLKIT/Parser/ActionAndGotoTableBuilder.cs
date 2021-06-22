using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class ActionAndGotoTableBuilder
    {
        private readonly INonTerminal _root;
        private readonly IEnumerable<RuleSet> _ruleSets;
        private readonly TranslationTable _translationTable;

        public ActionAndGotoTableBuilder(INonTerminal root, IEnumerable<RuleSet> ruleSets,
            TranslationTable translationTable)
        {
            _root = root;
            _ruleSets = ruleSets;
            _translationTable = translationTable;
        }

        public ActionAndGotoTable Build()
        {
            var actionAndGotoTable = Initialize();
            BuildGotos(actionAndGotoTable);
            BuildShifts(actionAndGotoTable);
            return actionAndGotoTable;
        }

        /// <summary>
        /// Add a column for the end of input, labeled $.
        /// Place an "accept" in the $ column whenever the item set contains an item where the pointer is at the end of the
        /// starting rule
        /// (in our example "S → N •").
        /// </summary>
        private ActionAndGotoTable Initialize()
        {
            var actionAndGotoTable = new ActionAndGotoTable(_root);
            foreach (var ruleSet in _ruleSets)
            {
                if (ContainStartingRuleWithPointerAtTheEnd(ruleSet))
                {
                    actionAndGotoTable.ActionTable[new KeyValuePair<ITerm, RuleSet>(EofTerminal.Instance, ruleSet)] =
                        AcceptAction.Instance;
                }
            }

            return actionAndGotoTable;
        }

        /// <summary>
        /// Directly copy the Translation Table's non-terminal columns as GOTOs
        /// </summary>
        private void BuildGotos(ActionAndGotoTable actionAndGotoTable)
        {
            foreach (var record in _translationTable.GetAllRecords())
            {
                if (record.Key.Key is INonTerminal nonTerminal)
                {
                    actionAndGotoTable.GotoTable
                        [new KeyValuePair<INonTerminal, RuleSet>(nonTerminal, record.Key.Value)] = record.Value;
                }
            }
        }

        /// <summary>
        /// Copy the terminal columns as shift actions to the number determined from the Translation Table.
        /// </summary>
        private void BuildShifts(ActionAndGotoTable actionAndGotoTable)
        {
            foreach (var record in _translationTable.GetAllRecords())
            {
                if (record.Key.Key is ITerminal terminal)
                {
                    actionAndGotoTable.ActionTable[new KeyValuePair<ITerm, RuleSet>(terminal, record.Key.Value)] =
                        new ShiftAction(record.Value);
                }
            }
        }

        private bool ContainStartingRuleWithPointerAtTheEnd(RuleSet ruleSet)
        {
            return ruleSet.Rules.Any(rule => rule.IsFinished && rule.Production.LeftNonTerminal == _root);
        }
    }
}