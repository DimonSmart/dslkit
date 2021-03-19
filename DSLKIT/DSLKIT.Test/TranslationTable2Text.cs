using System.Collections.Generic;
using System.Linq;
using ConsoleTableExt;
using DSLKIT.Base;
using DSLKIT.Parser;

namespace DSLKIT.Test
{
    public static class TranslationTable2Text
    {
        public static string Transform(Dictionary<KeyValuePair<ITerm, RuleSet>, RuleSet> sets, string order)
        {
            var tempColumns = sets.Keys.Select(i => i.Key).Distinct().ToList();
            var examples = order.Split(' ');
            var columns = new List<ITerm>();
            foreach (var example in examples)
            {
                ITerm term = tempColumns.SingleOrDefault(i => i.Name == example);
                if (term != null)
                {
                    columns.Add(term);
                    tempColumns.Remove(term);
                }
            }

            columns.AddRange(tempColumns);   

            var data = new List<List<object>>();
            foreach (var set in sets)
            {
                var row = new List<object>();
                row.Add(set.Value.SetNumber);
                foreach (var column in columns)
                {
                    if (sets.TryGetValue(new KeyValuePair<ITerm, RuleSet>(column, set.Value), out var destinationSet))
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