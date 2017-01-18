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
                if ((token.Terminal.Flags & TermFlags.OpenBrace) != TermFlags.None)
                {
                    bracesStack.Push(token);
                }

                if ((token.Terminal.Flags & TermFlags.CloseBrace) != TermFlags.None)
                {
                    if (bracesStack.Count == 0)
                    {
                        yield return new ErrorToken();
                        yield break;
                    }
                    bracesStack.Pop();
                }
                yield return token;
            }

            if (bracesStack.Count != 0)
            {
                yield return new ErrorToken();
            }
        }
    }
}