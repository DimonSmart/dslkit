using System.Collections.Generic;
using System.Text;
using DSLKIT.Parser.ExtendedGrammar;

namespace DSLKIT.Test.Transformers
{
    public static class ExtendedGrammar2Text
    {
        public static string Transform(IEnumerable<ExProduction> exProductions)
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