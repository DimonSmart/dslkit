using System.Collections.Generic;

namespace DSLKIT.Ast
{
    public interface IAstNode
    {
        IReadOnlyList<IAstNode> Children { get; }
    }
}
