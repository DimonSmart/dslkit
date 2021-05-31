using System.Collections.Generic;
using System.Linq;
using ConsoleTableExt;
using DSLKIT.Parser;

namespace DSLKIT.Test.Transformers
{
    public static class ActionAndGotoTable2Text
    {
        public static string Transform(ActionAndGotoTable agTable)
        {
            var data = new List<List<object>>();
            var actionColumns = agTable.ActionTable.Keys.Select(i => i.Key.Name);
            var gotoColumns = agTable.GetGotoColumns().Select(i => i.Name);


            foreach (var set in agTable.GetAllSets().OrderBy(i => i.SetNumber))
            {
                var row = new List<object> { set.SetNumber };

                foreach (var column in agTable.GetActionColumns())
                {
                    row.Add(agTable.TryGetActionValue(column, set, out var action)
                        ? action.ToString()
                        : string.Empty);
                }

                foreach (var column in agTable.GetGotoColumns())
                {
                    row.Add(agTable.TryGetGotoValue(column, set, out var ruleSet)
                        ? ruleSet.SetNumber.ToString()
                        : string.Empty);
                }
                data.Add(row);
            }

            return ConsoleTableBuilder.From(data)
                .WithColumn(new List<string> { "" }.Union(actionColumns).Union(gotoColumns).ToList())
                .Export().ToString();
        }

    }
}