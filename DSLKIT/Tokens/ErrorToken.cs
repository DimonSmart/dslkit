using DSLKIT.Terminals;
using DSLKIT.Formatting;

namespace DSLKIT.Tokens
{
    public record ErrorToken(
        string ErrorMessage,
        int Position = 0,
        int Length = 0,
        string OriginalString = "",
        object? Value = null,
        ITerminal Terminal = null!,
        FormattingTrivia? Trivia = null) : Token(Position, Length, OriginalString, Value, Terminal, Trivia)
    {
        public override string ToString()
        {
            return $"{ErrorMessage} at position: {Position}";
        }
    }
}
