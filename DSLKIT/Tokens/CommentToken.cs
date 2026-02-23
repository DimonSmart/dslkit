using DSLKIT.Terminals;
using DSLKIT.Formatting;

namespace DSLKIT.Tokens
{
    public record CommentToken(
        int Position,
        int Length,
        string OriginalString,
        object? Value,
        ITerminal Terminal,
        FormattingTrivia? Trivia = null) : Token(Position, Length, OriginalString, Value, Terminal, Trivia);
}
