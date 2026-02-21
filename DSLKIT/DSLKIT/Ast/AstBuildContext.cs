using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Parser;

namespace DSLKIT.Ast
{
    public sealed class AstBuildContext
    {
        private readonly IReadOnlyList<TerminalNode> _terminalDescendants;

        public AstBuildContext(ParseTreeNode parseNode, IReadOnlyList<IAstNode>? astChildren, string? sourceText = null)
        {
            ParseNode = parseNode;
            ParseChildren = parseNode.Children ?? Array.Empty<ParseTreeNode>();
            AstChildren = astChildren ?? Array.Empty<IAstNode>();
            SourceText = sourceText;
            NonTerminalNode = parseNode as NonTerminalNode;
            Production = NonTerminalNode?.Production;
            _terminalDescendants = CollectTerminalDescendants(parseNode).ToList();
        }

        public ParseTreeNode ParseNode { get; }
        public NonTerminalNode? NonTerminalNode { get; }
        public Production? Production { get; }
        public IReadOnlyList<ParseTreeNode> ParseChildren { get; }
        public IReadOnlyList<IAstNode> AstChildren { get; }
        public string? SourceText { get; }

        public IEnumerable<TerminalNode> TerminalDescendants()
        {
            return _terminalDescendants;
        }

        public TerminalNode Terminal(int index)
        {
            if (index < 0 || index >= _terminalDescendants.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index,
                    $"Terminal index is out of range. Count: {_terminalDescendants.Count}.");
            }

            return _terminalDescendants[index];
        }

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

        public IEnumerable<T> AstChildrenOf<T>()
            where T : class, IAstNode
        {
            return AstChildren.OfType<T>();
        }

        public string GetText()
        {
            if (_terminalDescendants.Count == 0)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(SourceText))
            {
                var start = _terminalDescendants.Min(i => i.Token.Position);
                var end = _terminalDescendants.Max(i => i.Token.Position + i.Token.Length);

                if (start >= 0 && end >= start && end <= SourceText.Length)
                {
                    return SourceText.Substring(start, end - start);
                }
            }

            return string.Concat(_terminalDescendants.Select(i => i.Token.OriginalString));
        }

        private static IEnumerable<TerminalNode> CollectTerminalDescendants(ParseTreeNode parseNode)
        {
            if (parseNode is TerminalNode terminalNode)
            {
                yield return terminalNode;
                yield break;
            }

            foreach (var child in parseNode.Children)
            {
                foreach (var terminal in CollectTerminalDescendants(child))
                {
                    yield return terminal;
                }
            }
        }
    }
}
