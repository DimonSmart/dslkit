﻿using System.Collections.Generic;
using ConsoleTableExt;
using DSLKIT.Base;
using DSLKIT.Parser.ExtendedGrammar;

namespace DSLKIT.Test.Transformers
{
    public static class Rule2FollowSet2Text
    {
        public static string Transform(Dictionary<ExProduction, IList<ITerm>> rule2FollowSet)
        {
            var data = new List<List<object>>();
            var i = 0;

            foreach (var rule2Follow in rule2FollowSet)
            {
                var row = new List<object> { i++, rule2Follow.Key, string.Join(", ", rule2Follow.Value) };
                data.Add(row);
            }

            return ConsoleTableBuilder.From(data)
                .WithColumn(new List<string> { "Number", "Rule", "Follow Set" })
                .Export().ToString();
        }
    }
}