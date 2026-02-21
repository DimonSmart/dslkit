using DSLKIT.Helpers;
using DSLKIT.Terminals;
using DSLKIT.Formatting;

namespace DSLKIT.Tokens
{
    public record StringTokenBase(
        int Position,
        int Length,
        string OriginalString,
        object? Value,
        ITerminal Terminal,
        FormattingTrivia? Trivia = null) : Token(Position, Length, OriginalString, Value, Terminal, Trivia)
    {
        public override string ToString()
        {
            return (Value as string ?? string.Empty).MakeWhiteSpaceVisible().DoubleQuoteIt();
        }
    }
}
