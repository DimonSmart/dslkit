using ConsoleTableExt;
using DSLKIT.Base;
using DSLKIT.Parser;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Test
{
    public static class TranslationTable2Text
    {
        public static string
            Transform(TranslationTable translationTable, string order)
        {
            var columns = new List<ITerm>();
            var tempColumns = translationTable.GetAllTerms()
                .Union(translationTable.GetDestinationSets()
                    .SelectMany(i => i.Rules, (i, rules) => rules.Production.LeftNonTerminal))
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
            foreach (var set in translationTable.GetAllSets()
                .OrderBy(i => i.SetNumber))
            {
                var row = new List<object> { set.SetNumber };
                foreach (var column in columns)
                {
                    if (translationTable.TryGetValue(column, set, out var destinationSet))
                    {
                        row.Add(destinationSet.SetNumber.ToString());
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