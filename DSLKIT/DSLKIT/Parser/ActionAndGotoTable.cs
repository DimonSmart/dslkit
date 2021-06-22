using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.NonTerminals;

namespace DSLKIT.Parser
{
    public class ActionAndGotoTable
    {
        private readonly INonTerminal _root;

        public readonly Dictionary<KeyValuePair<ITerm, RuleSet>, IActionItem> ActionTable =
            new Dictionary<KeyValuePair<ITerm, RuleSet>, IActionItem>();

        public readonly Dictionary<KeyValuePair<INonTerminal, RuleSet>, RuleSet> GotoTable =
            new Dictionary<KeyValuePair<INonTerminal, RuleSet>, RuleSet>();

        public IEnumerable<INonTerminal> GetGotoColumns()
        {
            return _root.Union(GotoTable.Keys.Select(i => i.Key)).Distinct();
        }

        public IEnumerable<ITerm> GetActionColumns()
        {
            return ActionTable.Keys.Select(i => i.Key).Distinct();
        }

        public ActionAndGotoTable(INonTerminal root)
        {
            _root = root;
        }

        public IEnumerable<RuleSet> GetAllSets()
        {
            return ActionTable.Keys.Select(i => i.Value).Union(GotoTable.Keys.Select(i => i.Value)).Distinct();
        }

        public bool TryGetActionValue(ITerm x, RuleSet y, out IActionItem result)
        {
            return ActionTable.TryGetValue(new KeyValuePair<ITerm, RuleSet>(x, y), out result);
        }

        public bool TryGetGotoValue(INonTerminal x, RuleSet y, out RuleSet result)
        {
            return GotoTable.TryGetValue(new KeyValuePair<INonTerminal, RuleSet>(x, y), out result);
        }
    }
}