using System;
using DSLKIT.Tokens;
using DSLKIT.Parser;

namespace DSLKIT.Ast
{
    public class AstTokenNode : AstNodeBase
    {
        public AstTokenNode(TerminalNode terminalNode, string? sourceText = null)
            : base(new AstBuildContext(terminalNode, Array.Empty<IAstNode>(), sourceText), Array.Empty<IAstNode>())
        {
            Token = terminalNode.Token;
        }

        public IToken Token { get; }
        public string Text => Token.OriginalString;
        public object? Value => Token.Value;
        public string TerminalName => Token.Terminal.Name;
    }
}
