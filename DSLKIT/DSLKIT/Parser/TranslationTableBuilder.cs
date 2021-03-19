using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;

namespace DSLKIT.Parser
{
    public static class TranslationTableBuilder
    {
        public static Dictionary<KeyValuePair<ITerm, RuleSet>, RuleSet> Build(IEnumerable<RuleSet> ruleSets)
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

            return dict;
        }
    }
}