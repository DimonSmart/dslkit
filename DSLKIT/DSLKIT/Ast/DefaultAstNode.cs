using DSLKIT.Base;
using DSLKIT.Parser;
using DSLKIT.Tokens;

namespace DSLKIT.Ast
{
    /// <summary>
    /// Default AST node used when no user specific node is registered.
    /// Wraps the corresponding parse tree node.
    /// </summary>
    public class DefaultAstNode : AstNode
    {
        public ITerm Term => ParseNode.Term;

        public IToken Token => (ParseNode as TerminalNode)?.Token;
    }
}

