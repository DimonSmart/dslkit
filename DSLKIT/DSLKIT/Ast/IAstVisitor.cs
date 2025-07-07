namespace DSLKIT.Ast
{
    /// <summary>
    /// Visitor interface for AST nodes.
    /// User defined visitors can overload Visit methods for specific node types.
    /// </summary>
    public interface IAstVisitor
    {
        void Visit(AstNode node);
    }
}

