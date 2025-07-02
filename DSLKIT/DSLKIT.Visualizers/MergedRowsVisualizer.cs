using System.Collections.Generic;
using System.Linq;
using ConsoleTableExt;
using DSLKIT.Parser;

namespace DSLKIT.Visualizers
{
    public static class MergedRowsVisualizer
    {
        public static string Visualize(IEnumerable<ActionAndGotoTableBuilder.MergedRow> mergedRows)
        {
            var data = new List<List<object>>();

            foreach (var mergedRow in mergedRows)
            {
                var row = new List<object>
                {
                    mergedRow.FinalSet?.SetNumber,
                    $"({mergedRow.PreMergedRules.Count}) " +
                    string.Join(", ", mergedRow.PreMergedRules.Select(r => r.ToString())),
                    mergedRow.Production.ProductionToString(),
                    string.Join(", ", mergedRow.FollowSet.Select(f => f.ToString()))
                };
                data.Add(row);
            }

            return ConsoleTableBuilder.From(data)
                .WithColumn(new List<string> { "Final Set", "Pre-Merge Rules", "Rule", "Follow Set" })
                .Export().ToString();
        }
    }
}
