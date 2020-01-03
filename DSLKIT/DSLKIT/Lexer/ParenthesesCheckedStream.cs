using System.Collections.Generic;
using DSLKIT.Terminals;
using DSLKIT.Tokens;

namespace DSLKIT
{
    public class ParenthesesCheckedStream : LexerStreamBase
    {
        public ParenthesesCheckedStream(IEnumerable<IToken> sourceStream) : base(sourceStream)
        {
        }

        protected override IEnumerable<IToken> Filter(IEnumerable<IToken> sourceStream)
        {
            var bracesStack = new Stack<IToken>();

            foreach (var token in sourceStream)
            {
                switch (token.Terminal.Flags)
                {
                    case TermFlags.OpenBrace:
                        bracesStack.Push(token);
                        break;
                    case TermFlags.CloseBrace when bracesStack.Count == 0:
                        yield return new ErrorToken("Close brace without open");
                        yield break;
                    case TermFlags.CloseBrace:
                    {
                        var openBrace = bracesStack.Pop();
                        var expectedCloseBracket = ParenthesesKeywordConstants.ParenthesisPairs[token.Terminal];
                        if (openBrace.Terminal != expectedCloseBracket)
                        {
                            yield return new ErrorToken(
                                $"Close brace type:{token.Terminal.Name} do not match with open one:{openBrace.Terminal.Name}");
                            yield break;
                        }

                        break;
                    }
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