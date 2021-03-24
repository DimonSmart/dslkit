using ConsoleTableExt;
using DSLKIT.Base;
using DSLKIT.Parser;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Test
{
    public static class TranslationTable2Text
    {
        public static string Transform(Dictionary<KeyValuePair<ITerm, RuleSet>, RuleSet> sets, string order, string subst)
        {
            var substDictionary = NuberingUtils.CreateSubstFromString(subst);
            var columns = new List<ITerm>();
            var tempColumns = sets.Keys.Select(i => i.Key)
                    .Union(sets.Values.SelectMany(i => i.Rules, (i, rules) => rules.Production.LeftNonTerminal))
                    .Distinct().ToList();
            if (string.IsNullOrEmpty(order))
            {
                columns = tempColumns;
            }
            else
            {
                foreach (var example in order.Split(' '))
                {
                    var term = tempColumns.SingleOrDefault(i => i.Name == example);
                    if (term != null)
                    {
                        columns.Add(term);
                        tempColumns.Remove(term);
                    }
                }
                columns.AddRange(tempColumns);
            }

            var data = new List<List<object>>();
            foreach (var set in sets.Keys.Select(i => i.Value).Union(sets.Values).OrderBy(i => NuberingUtils.GetSubst(substDictionary, i.SetNumber)))
            {
                var row = new List<object> { NuberingUtils.GetSubst(substDictionary, set.SetNumber) };
                foreach (var column in columns)
                {
                    if (sets.TryGetValue(new KeyValuePair<ITerm, RuleSet>(column, set), out var destinationSet))
                    {
                        row.Add(NuberingUtils.GetSubst(substDictionary, destinationSet.SetNumber).ToString());
                    }
                    else
                    {
                        row.Add(string.Empty);
                    }
                }
                data.Add(row);
            }

            return ConsoleTableBuilder.From(data)
                .WithColumn(new List<string> { "Item Set" }.Union(columns.Select(i => i.Name)).ToList())
                .Export().ToString();
        }
    }
}