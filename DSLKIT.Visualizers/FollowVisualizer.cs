using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSLKIT.Base;
using DSLKIT.Parser.ExtendedGrammar;

namespace DSLKIT.Visualizers
{
    public static class FollowVisualizer
    {
        public static string Visualize(IReadOnlyDictionary<IExNonTerminal, IList<ITerm>> follow)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Follow: {follow.Count}");
            foreach (var followItem in follow)
            {
                var followSet = string.Join(",", followItem.Value.Select(i => i.Name));
                sb.AppendLine($"{followItem.Key} : \t{followSet}");
            }

            return sb.ToString();
        }
    }
}
