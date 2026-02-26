using System;
using System.Collections.Generic;
using DSLKIT.Parser;

namespace DSLKIT.Ast
{
    public abstract class AstNodeBase(AstBuildContext context, IReadOnlyList<IAstNode>? children = null) : IAstNode
    {
        protected AstBuildContext Context { get; } = context;

        public IReadOnlyList<IAstNode> Children { get; } = children ?? context.AstChildren ?? [];
        public virtual string DisplayName => GetType().Name;
        public virtual string? Description => null;
        public virtual AstChildrenDisplayMode ChildrenDisplayMode => AstChildrenDisplayMode.Auto;

        public ParseTreeNode ParseNode => Context.ParseNode;
    }
}
