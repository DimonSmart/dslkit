using System.Collections.Generic;
using System.Text;
using DSLKIT.Parser;

namespace DSLKIT.Test.Transformers
{
    public static class RuleSets2Text
    {
        public static string Transform(IReadOnlyCollection<RuleSet> ruleSets)
        {
            var sb = new StringBuilder();
            foreach (var set in ruleSets)
            {
                sb.AppendLine(set.ToString());
            }

            return sb.ToString();
        }
    }
}