using System;

namespace DSLKIT.Ast
{
    public sealed class AstNodeBinding
    {
        public AstNodeBinding(Type nodeType)
        {
            if (!typeof(IAstNode).IsAssignableFrom(nodeType))
            {
                throw new ArgumentException($"Type '{nodeType.FullName}' must implement {nameof(IAstNode)}.", nameof(nodeType));
            }

            NodeType = nodeType;
        }

        public AstNodeBinding(Func<AstBuildContext, IAstNode> factory)
        {
            Factory = factory;
        }

        public Type? NodeType { get; }
        public Func<AstBuildContext, IAstNode>? Factory { get; }
    }
}
