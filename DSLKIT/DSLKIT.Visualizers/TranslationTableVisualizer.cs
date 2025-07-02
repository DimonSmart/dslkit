using System.Collections.Generic;
using System.Linq;
using ConsoleTableExt;
using DSLKIT.Parser;

namespace DSLKIT.Visualizers
{
    public static class TranslationTableVisualizer
    {
        public static string Visualize(TranslationTable translationTable)
        {
            var columns = translationTable.GetAllTerms()
                .Union(translationTable.GetDestinationSets()
                    .SelectMany(i => i.Rules, (i, rules) => rules.Production.LeftNonTerminal))
                .Distinct().ToList();

            var data = new List<List<object>>();
            foreach (var set in translationTable.GetAllSets()
                .OrderBy(i => i.SetNumber))
            {
                var row = new List<object> { set.SetNumber };
                foreach (var column in columns)
                {
                    row.Add(translationTable.TryGetValue(column, set, out var destinationSet)
                        ? destinationSet.SetNumber.ToString()
                        : string.Empty);
                }

                data.Add(row);
            }

            return ConsoleTableBuilder.From(data)
                .WithColumn(new List<string> { "Item Set" }.Union(columns.Select(i => i.Name)).ToList())
                .Export().ToString();
        }
    }
}
