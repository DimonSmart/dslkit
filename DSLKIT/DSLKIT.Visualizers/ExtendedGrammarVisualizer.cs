using System.Collections.Generic;
using System.Text;
using DSLKIT.Parser.ExtendedGrammar;

namespace DSLKIT.Visualizers
{
    public static class ExtendedGrammarVisualizer
    {
        public static string Visualize(IEnumerable<ExProduction> exProductions)
        {
            var sb = new StringBuilder();
            var i = 0;
            foreach (var exProduction in exProductions)
            {
                sb.AppendLine($"{i++}. {exProduction}");
            }

            return sb.ToString();
        }
    }
}
