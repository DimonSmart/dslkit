// ReSharper disable StringLiteralTypo

using System.Text;
using DSLKIT.Parser;

namespace DSLKIT.Visualizers
{
    public static class ParseTreeDotExporter
    {
        public static string Visualize(ParseTreeNode tree)
        {
            var sb = new StringBuilder();
            var nodeCounter = 0;
            
            sb.AppendLine(
                @"digraph ParseTree { graph[fontsize = 30 labelloc = ""t"" label = ""Parse Tree"" splines = true overlap = false rankdir = ""TB""]; ratio = auto; ");

            VisualizeNode(tree, sb, ref nodeCounter, -1);

            sb.AppendLine(@"}");
            return sb.ToString();
        }

        private static int VisualizeNode(ParseTreeNode node, StringBuilder sb, ref int nodeCounter, int parentId)
        {
            var currentId = nodeCounter++;
            
            switch (node)
            {
                case NonTerminalNode nonTerminalNode:
                    // Non-terminal nodes - round shape, blue background
                    sb.AppendLine(
                        $@"""node{currentId}"" [style = ""filled, bold"" penwidth = 2 fillcolor = ""lightblue"" fontname = ""Courier New"" shape = ""ellipse"" label = ""{EscapeLabel(nonTerminalNode.NonTerminal.Name)}"" ];");
                    
                    foreach (var child in node.Children)
                    {
                        var childId = VisualizeNode(child, sb, ref nodeCounter, currentId);
                        sb.AppendLine($@" node{currentId} -> node{childId} [ penwidth = 2 fontsize = 14 fontcolor = ""black"" ];");
                    }
                    break;
                    
                case TerminalNode terminalNode:
                    // Terminal nodes - rectangular shape, light green background
                    var tokenValue = terminalNode.Token.OriginalString ?? terminalNode.Token.Value?.ToString() ?? "";
                    var label = $"{terminalNode.Token.Terminal.Name}";
                    if (!string.IsNullOrEmpty(tokenValue))
                    {
                        label += $"\\n\"{EscapeLabel(tokenValue)}\"";
                    }
                    
                    sb.AppendLine(
                        $@"""node{currentId}"" [style = ""filled, bold"" penwidth = 2 fillcolor = ""lightgreen"" fontname = ""Courier New"" shape = ""box"" label = ""{label}"" ];");
                    
                    // Handle composite terminals with children
                    foreach (var child in node.Children)
                    {
                        var childId = VisualizeNode(child, sb, ref nodeCounter, currentId);
                        sb.AppendLine($@" node{currentId} -> node{childId} [ penwidth = 2 fontsize = 14 fontcolor = ""black"" ];");
                    }
                    break;
                    
                default:
                    // Fallback for other node types - white background
                    sb.AppendLine(
                        $@"""node{currentId}"" [style = ""filled, bold"" penwidth = 2 fillcolor = ""white"" fontname = ""Courier New"" shape = ""ellipse"" label = ""{EscapeLabel(node.Term.Name)}"" ];");
                    
                    foreach (var child in node.Children)
                    {
                        var childId = VisualizeNode(child, sb, ref nodeCounter, currentId);
                        sb.AppendLine($@" node{currentId} -> node{childId} [ penwidth = 2 fontsize = 14 fontcolor = ""black"" ];");
                    }
                    break;
            }

            return currentId;
        }

        private static string EscapeLabel(string label)
        {
            return label?.Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "\\r")
                        .Replace("\t", "\\t") ?? "";
        }
    }
}
