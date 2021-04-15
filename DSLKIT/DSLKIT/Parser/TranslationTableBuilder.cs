using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;

namespace DSLKIT.Parser
{
    public class TranslationTable
    {
        private readonly Dictionary<KeyValuePair<ITerm, RuleSet>, RuleSet> _table;

        public TranslationTable(Dictionary<KeyValuePair<ITerm, RuleSet>, RuleSet> table)
        {
            _table = table;
        }

        public RuleSet this[ITerm x, RuleSet y] => _table[new KeyValuePair<ITerm, RuleSet>(x, y)];

        public bool TryGetValue(ITerm x, RuleSet y, out RuleSet result)
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
}