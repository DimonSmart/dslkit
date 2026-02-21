using DSLKIT.Base;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Parser
{

    public abstract class ParseTreeNode
    {
        /// <summary>
        /// Child nodes. For non-terminals - the result of applying a production.
        /// For composite terminals (e.g., quoted strings) - the components of the terminal.
        /// For simple terminals - an empty collection.
        /// </summary>
        public IReadOnlyList<ParseTreeNode> Children { get; }

        public abstract ITerm Term { get; }

        protected ParseTreeNode(IEnumerable<ParseTreeNode>? children)
        {
            Children = children?.ToList() ?? [];
        }
    }
}
