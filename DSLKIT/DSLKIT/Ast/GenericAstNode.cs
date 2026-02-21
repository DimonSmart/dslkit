using System.Collections.Generic;
using DSLKIT.Base;

namespace DSLKIT.Ast
{
    public class GenericAstNode : AstNodeBase
    {
        public GenericAstNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
            : base(context, children)
        {
        }

        public ITerm Term => ParseNode.Term;
        public string Name => Term.Name;
    }
}
