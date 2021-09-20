// ReSharper disable StringLiteralTypo

using System.Collections.Generic;
using System.Text;
using DSLKIT.Parser;

namespace DSLKIT.Test.Transformers
{
    public static class RuleSets2GraphVizDotFormat
    {
        public static string Transform(IReadOnlyCollection<RuleSet> sets)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"digraph Sets { graph[fontsize = 30 labelloc = ""t"" label = """" splines = true overlap = false rankdir = ""LR""]; ratio = auto; ");

            foreach (var set in sets)
            {
                sb.AppendLine($@"""state{set.SetNumber}"" [style = ""filled, bold"" penwidth = 5 fillcolor = ""white"" fontname = ""Courier New"" shape = ""Mrecord"" label = <<table border = ""0"" cellborder = ""0"" cellpadding = ""3"" bgcolor = ""white"" > ");
                sb.AppendLine($@"<tr><td bgcolor=""black"" align=""center"" colspan=""2""><font color=""white"">Set #{set.SetNumber}</font></td></tr>");

                var form = 0;
                foreach (var rule in set.Rules)
                {
                    var ruleText = (form >= set.SetFormRules ? "+" : " ") + rule;
                    sb.AppendLine($@"<tr><td align=""left"" port=""r0"">{ruleText}</td></tr>");
                    form++;
                }
                sb.AppendLine("</table>> ];");
            }

            sb.AppendLine();

            foreach (var set in sets)
            {
                foreach (var arrow in set.Arrows)
                {
                    sb.AppendLine($@" state{set.SetNumber} -> state{arrow.Value.SetNumber} [ penwidth = 5 fontsize = 28 fontcolor = ""black"" label = ""{arrow.Key}"" ];");
                }
            }

            sb.AppendLine(@"}");
            return sb.ToString();
        }
    }
}