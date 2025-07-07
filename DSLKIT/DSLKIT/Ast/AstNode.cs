using DSLKIT.Parser;
using System.Collections.Generic;

namespace DSLKIT.Ast
{
    /// <summary>
    /// Base class for AST nodes. User defined nodes should derive from this class.
    /// </summary>
    public abstract class AstNode
    {
        private readonly List<AstNode> _children = new();

        /// <summary>
        /// Underlying parse tree node.
        /// </summary>
        public ParseTreeNode ParseNode { get; private set; }

        /// <summary>
        /// Children AST nodes.
        /// </summary>
        public IReadOnlyList<AstNode> Children => _children;

        /// <summary>
        /// Initialize the node using a parse node and its children.
        /// </summary>
        public virtual void Init(ParseTreeNode parseNode, IEnumerable<AstNode> children)
        {
            ParseNode = parseNode;
            _children.AddRange(children);
        }

        /// <summary>
        /// Accept a visitor.
        /// </summary>
        public virtual void Accept(IAstVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}

