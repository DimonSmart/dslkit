using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSLKIT.Base;
using DSLKIT.NonTerminals;

namespace DSLKIT.Test.Transformers
{
    public static class Follow2Text
    {
        public static string Transform(IReadOnlyDictionary<INonTerminal, IList<ITerm>> follow)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Follow: {follow.Count}");
            foreach (var followItem in follow)
            {
                var followSet = string.Join(",", followItem.Value.Select(i => i.Name));
                sb.AppendLine($"{followItem.Key.Name} : \t{followSet}");
            }

            return sb.ToString();
        }
    }
}