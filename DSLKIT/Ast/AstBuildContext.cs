using System;
using System.Collections.Generic;
using DSLKIT.Parser;

namespace DSLKIT.Ast
{
    public sealed class AstBuildContext
    {
        public AstBuildContext(ParseTreeNode parseNode, IReadOnlyList<IAstNode>? astChildren, string? sourceText = null)
        {
            ParseNode = parseNode;
            ParseChildren = parseNode.Children ?? Array.Empty<ParseTreeNode>();
            AstChildren = astChildren ?? Array.Empty<IAstNode>();
            SourceText = sourceText;
            NonTerminalNode = parseNode as NonTerminalNode;
            Production = NonTerminalNode?.Production;
        }

        public ParseTreeNode ParseNode { get; }
        public NonTerminalNode? NonTerminalNode { get; }
        public Production? Production { get; }
        public IReadOnlyList<ParseTreeNode> ParseChildren { get; }
        public IReadOnlyList<IAstNode> AstChildren { get; }
        public string? SourceText { get; }

        public T AstChild<T>(int index)
            where T : class, IAstNode
        {
            if (index < 0 || index >= AstChildren.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"AST child index is out of range. Count: {AstChildren.Count}.");
            }

            if (AstChildren[index] is not T typedNode)
            {
                throw new InvalidOperationException(
                    $"AST child at index {index} has type '{AstChildren[index].GetType().Name}', expected '{typeof(T).Name}'.");
            }

            return typedNode;
        }
    }
}
