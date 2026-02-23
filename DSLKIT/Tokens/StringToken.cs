using DSLKIT.Terminals;
using DSLKIT.Formatting;

namespace DSLKIT.Tokens
{
    public record StringToken(
        int Position,
        int Length,
        string OriginalString,
        object? Value,
        ITerminal Terminal,
        FormattingTrivia? Trivia = null) : StringTokenBase(Position, Length, OriginalString, Value, Terminal, Trivia);
}
