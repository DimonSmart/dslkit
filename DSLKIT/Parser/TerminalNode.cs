using DSLKIT.Base;
using DSLKIT.Tokens;
using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public sealed class TerminalNode : ParseTreeNode
    {
        public override ITerm Term => Token.Terminal;

        public IToken Token { get; }

        public TerminalNode(IToken token, IEnumerable<ParseTreeNode>? children = null)
            : base(children)
        {
            Token = token;
        }
    }
}
