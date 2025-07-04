using DSLKIT.Terminals;
using DSLKIT.Formatting;

namespace DSLKIT.Tokens
{
    public record KeywordToken(
        int Position,
        int Length,
        string OriginalString,
        object Value,
        ITerminal Terminal,
        FormattingTrivia Trivia = null) : StringTokenBase(Position, Length, OriginalString, Value, Terminal, Trivia);
}