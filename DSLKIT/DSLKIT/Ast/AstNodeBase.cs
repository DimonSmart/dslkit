using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Parser;

namespace DSLKIT.Ast
{
    public abstract class AstNodeBase(AstBuildContext context, IReadOnlyList<IAstNode>? children = null) : IAstNode
    {
        protected AstBuildContext Context { get; } = context;

        public IReadOnlyList<IAstNode> Children { get; } = children ?? context.AstChildren ?? [];

        public ParseTreeNode ParseNode => Context.ParseNode;

        public string GetText()
        {
            return Context.GetText();
        }

        public IEnumerable<T> FindChildren<T>()
            where T : class, IAstNode
        {
            return Children.OfType<T>();
        }

        public T FirstChild<T>()
            where T : class, IAstNode
        {
            return FindChildren<T>().First();
        }
    }
}
