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
            var data = new List<List<object>> { new List<object> { "123" } };
            var columns = agTable.ActionTable.Keys.Select(i => i.Key.SetNumber.ToString());
            return ConsoleTableBuilder.From(data)
                .WithColumn(new List<string> { "" }.Union(columns).ToList())
                .Export().ToString();
        }

    }
}