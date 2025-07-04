using DSLKIT.Helpers;
using DSLKIT.Terminals;
using DSLKIT.Formatting;

namespace DSLKIT.Tokens
{
    public record SpaceToken(
        int Position,
        int Length,
        string OriginalString,
        object Value,
        ITerminal Terminal,
        FormattingTrivia Trivia = null) : Token(Position, Length, OriginalString, Value, Terminal, Trivia)
    {
        public override string ToString()
        {
            var s = ((char)Value).ToString();
            return s.MakeWhiteSpaceVisible();
        }
    }
}