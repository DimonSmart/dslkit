using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.Helpers;
using DSLKIT.NonTerminals;

namespace DSLKIT.Parser
{
    public class ActionAndGotoTable
    {
        private readonly INonTerminal _root;
        private readonly Dictionary<KeyValuePair<ITerm, RuleSet>, IActionItem> _actionTable = [];
        private readonly Dictionary<KeyValuePair<INonTerminal, RuleSet>, RuleSet> _gotoTable = [];

        public IReadOnlyDictionary<KeyValuePair<ITerm, RuleSet>, IActionItem> ActionTable => _actionTable;
        public IReadOnlyDictionary<KeyValuePair<INonTerminal, RuleSet>, RuleSet> GotoTable => _gotoTable;
        internal Dictionary<KeyValuePair<ITerm, RuleSet>, IActionItem> MutableActionTable => _actionTable;
        internal Dictionary<KeyValuePair<INonTerminal, RuleSet>, RuleSet> MutableGotoTable => _gotoTable;

        public ActionAndGotoTable(INonTerminal root)
        {
            _root = root;
        }

        public IEnumerable<INonTerminal> GetGotoColumns()
        {
            return _root.Union(_gotoTable.Keys.Select(i => i.Key)).Distinct();
        }

        public IEnumerable<ITerm> GetActionColumns()
        {
            return _actionTable.Keys.Select(i => i.Key).Distinct();
        }

        public IEnumerable<RuleSet> GetAllSets()
        {
            return _actionTable.Keys.Select(i => i.Value).Union(_gotoTable.Keys.Select(i => i.Value)).Distinct();
        }

        public bool TryGetActionValue(ITerm x, RuleSet y, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IActionItem? result)
        {
            return _actionTable.TryGetValue(new KeyValuePair<ITerm, RuleSet>(x, y), out result);
        }

        public bool TryGetGotoValue(INonTerminal x, RuleSet y, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out RuleSet? result)
        {
            return _gotoTable.TryGetValue(new KeyValuePair<INonTerminal, RuleSet>(x, y), out result);
        }
    }
}
