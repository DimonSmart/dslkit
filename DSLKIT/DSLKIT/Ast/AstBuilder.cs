using DSLKIT.Base;
using DSLKIT.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Ast
{
    /// <summary>
    /// Converts a parse tree into an AST using user provided node mappings.
    /// </summary>
    public class AstBuilder
    {
        private readonly Dictionary<ITerm, Func<ParseTreeNode, IEnumerable<AstNode>, AstNode>> _factories = new();

        /// <summary>
        /// Register a node type for a given term.
        /// The type must derive from <see cref="AstNode"/> and have a parameterless constructor.
        /// </summary>
        public void Register<TNode>(ITerm term) where TNode : AstNode, new()
        {
            _factories[term] = (n, c) =>
            {
                var node = new TNode();
                node.Init(n, c);
                return node;
            };
        }

        /// <summary>
        /// Register a custom factory for a term.
        /// </summary>
        public void Register(ITerm term, Func<ParseTreeNode, IEnumerable<AstNode>, AstNode> factory)
        {
            _factories[term] = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Build AST starting from the parse tree root.
        /// </summary>
        public AstNode Build(ParseTreeNode root)
        {
            return ConvertNode(root);
        }

        /// <summary>
        /// Build AST from a parse result.
        /// </summary>
        public AstResult Build(ParseResult result)
        {
            if (!result.IsSuccess)
            {
                return new AstResult { Error = result.Error };
            }

            var root = Build(result.ParseTree);
            return new AstResult { Root = root };
        }

        private AstNode ConvertNode(ParseTreeNode node)
        {
            var children = node.Children.Select(ConvertNode).ToList();

            if (_factories.TryGetValue(node.Term, out var factory))
            {
                return factory(node, children);
            }

            var defaultNode = new DefaultAstNode();
            defaultNode.Init(node, children);
            return defaultNode;
        }
    }
}

