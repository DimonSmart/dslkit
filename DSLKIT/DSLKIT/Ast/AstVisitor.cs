namespace DSLKIT.Ast
{
    /// <summary>
    /// Base implementation of <see cref="IAstVisitor"/> that traverses children by default.
    /// </summary>
    public abstract class AstVisitor : IAstVisitor
    {
        public virtual void Visit(AstNode node)
        {
            foreach (var child in node.Children)
            {
                child.Accept(this);
            }
        }
    }
}

