using System.Collections.Generic;
using System.Text;
using DSLKIT.Parser;

namespace DSLKIT.Visualizers
{
    public static class RuleSetsVisualizer
    {
        public static string Visualize(IReadOnlyCollection<RuleSet> ruleSets)
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
