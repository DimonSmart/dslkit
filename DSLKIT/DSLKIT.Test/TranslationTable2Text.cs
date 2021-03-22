using ConsoleTableExt;
using DSLKIT.Base;
using DSLKIT.Parser;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Test
{
    public static class TranslationTable2Text
    {
        public static string Transform(Dictionary<KeyValuePair<ITerm, RuleSet>, RuleSet> sets, string order)
        {
            var columns = new List<ITerm>();
            if (string.IsNullOrEmpty(order))
            {
                columns = sets.Keys.Select(i => i.Key).Distinct().ToList();
            }
            else
            {
                var tempColumns = sets.Keys.Select(i => i.Key).Distinct().ToList();
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
            foreach (var set in sets.Keys.Select(i => i.Value).Union(sets.Values))
            {
                var row = new List<object> { set.SetNumber };
                foreach (var column in columns)
                {
                    if (sets.TryGetValue(new KeyValuePair<ITerm, RuleSet>(column, set), out var destinationSet))
                    {
                        row.Add(destinationSet.SetNumber.ToString());
                    }
                    else
                    {
                        row.Add("-");
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