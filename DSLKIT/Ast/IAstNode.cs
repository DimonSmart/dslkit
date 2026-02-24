using System.Collections.Generic;

namespace DSLKIT.Ast
{
    public interface IAstNode
    {
        IReadOnlyList<IAstNode> Children { get; }
        string DisplayName => GetType().Name;
        string? Description => null;
        AstChildrenDisplayMode ChildrenDisplayMode => AstChildrenDisplayMode.Auto;
    }
}
