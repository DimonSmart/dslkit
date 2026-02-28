using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;

namespace DSLKIT.Parser
{
    public static class TranslationTableBuilder
    {
        public static TranslationTable Build(IEnumerable<RuleSet> ruleSets)
        {
            var sets = ruleSets.ToList();
            var recordCapacity = sets.Sum(set => set.Arrows.Count);
            var dict = new Dictionary<KeyValuePair<ITerm, RuleSet>, RuleSet>(recordCapacity);
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
