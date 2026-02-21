using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DSLKIT.Base;

namespace DSLKIT.Parser
{
    public class TranslationTable
    {
        private readonly Dictionary<KeyValuePair<ITerm, RuleSet>, RuleSet> _table;

        public RuleSet this[ITerm x, RuleSet y] => _table[new KeyValuePair<ITerm, RuleSet>(x, y)];

        public TranslationTable(Dictionary<KeyValuePair<ITerm, RuleSet>, RuleSet> table)
        {
            _table = table;
        }

        public IReadOnlyDictionary<KeyValuePair<ITerm, RuleSet>, RuleSet> GetAllRecords()
        {
            return new ReadOnlyDictionary<KeyValuePair<ITerm, RuleSet>, RuleSet>(_table);
        }

        public bool TryGetValue(ITerm x, RuleSet y, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out RuleSet? result)
        {
            return _table.TryGetValue(new KeyValuePair<ITerm, RuleSet>(x, y), out result);
        }

        public IEnumerable<ITerm> GetAllTerms()
        {
            return _table.Keys.Select(i => i.Key);
        }

        public IEnumerable<RuleSet> GetSourceSets()
        {
            return _table.Keys.Select(i => i.Value);
        }

        public IEnumerable<RuleSet> GetDestinationSets()
        {
            return _table.Values;
        }

        public IEnumerable<RuleSet> GetAllSets()
        {
            return GetSourceSets().Union(GetDestinationSets()).Distinct();
        }
    }
}
