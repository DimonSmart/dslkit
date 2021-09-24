using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSLKIT.Base;
using DSLKIT.Parser.ExtendedGrammar;

namespace DSLKIT.Test.Transformers
{
    public static class Firsts2Text
    {
        public static string Transform(IDictionary<IExNonTerminal, IList<ITerm>> firsts)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Firsts: {firsts.Count}");
            foreach (var first in firsts)
            {
                var firstsSet = string.Join(",", first.Value.Select(i => i.Name));
                sb.AppendLine($"{first.Key} : \t{firstsSet}");
            }

            return sb.ToString();
        }
    }
}