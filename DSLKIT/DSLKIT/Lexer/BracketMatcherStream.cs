using System.Collections.Generic;
using DSLKIT.Terminals;
using DSLKIT.Tokens;

namespace DSLKIT
{
    public class BracketMatcherStream : LexerStreamBase
    {
        public BracketMatcherStream(IEnumerable<Token> sourceStream) : base(sourceStream)
        {
        }

        protected override IEnumerable<Token> Filter(IEnumerable<Token> sourceStream)
        {
            var bracesStack = new Stack<Token>();

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