using System;
using DSLKIT.Parser;

namespace DSLKIT.GrammarExamples.MsSql.Formatting
{
    public abstract class SqlParseTreeVisitorBase : ISqlParseTreeVisitor
    {
        public void Visit(ParseTreeNode node)
        {
            ArgumentNullException.ThrowIfNull(node);

            switch (node)
            {
                case NonTerminalNode nonTerminalNode:
                    VisitNonTerminal(nonTerminalNode);
                    break;
                case TerminalNode terminalNode:
                    VisitTerminal(terminalNode);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported parse tree node type: {node.GetType().FullName}");
            }
        }

        protected virtual void VisitNonTerminal(NonTerminalNode node)
        {
            VisitChildren(node);
        }

        protected virtual void VisitTerminal(TerminalNode node)
        {
        }

        protected void VisitChildren(ParseTreeNode node)
        {
            foreach (var child in node.Children)
            {
                Visit(child);
            }
        }
    }
}
