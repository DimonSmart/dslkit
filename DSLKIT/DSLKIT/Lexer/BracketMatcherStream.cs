using System.Collections.Generic;
using DSLKIT.Terminals;
using DSLKIT.Tokens;

namespace DSLKIT
{
    public class BracketMatcherStream : LexerStreamBase
    {
        public BracketMatcherStream(IEnumerable<IToken> sourceStream) : base(sourceStream)
        {
        }

        protected override IEnumerable<IToken> Filter(IEnumerable<IToken> sourceStream)
        {
            var bracesStack = new Stack<IToken>();

            foreach (var token in sourceStream)
            {
                if (token.Terminal.Flags == TermFlags.OpenBrace)
                {
                    bracesStack.Push(token);
                }

                if (token.Terminal.Flags == TermFlags.CloseBrace)
                {
                    if (bracesStack.Count == 0)
                    {
                        yield return new ErrorToken("Close brace without open");
                        yield break;
                    }
                    bracesStack.Pop();
                }
                yield return token;
            }

            if (bracesStack.Count != 0)
            {
                yield return new ErrorToken("Open and close braces count not the same");
            }
        }
    }
}