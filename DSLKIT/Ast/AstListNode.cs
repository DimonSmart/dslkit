using System.Collections.Generic;

namespace DSLKIT.Ast
{
    public class AstListNode : AstNodeBase
    {
        public AstListNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
            : base(context, children)
        {
        }

        public IReadOnlyList<IAstNode> Items => Children;
    }
}
