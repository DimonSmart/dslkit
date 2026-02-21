using DSLKIT.Terminals;
using DSLKIT.Formatting;

namespace DSLKIT.Tokens
{
    public record Token(
        int Position,
        int Length, 
        string OriginalString,
        object? Value,
        ITerminal Terminal,
        FormattingTrivia? Trivia = null) : IToken
    {
        public FormattingTrivia Trivia { get; init; } = Trivia ?? FormattingTrivia.Empty;

        /// <summary>
        /// Creates a new token with the specified trivia
        /// </summary>
        public IToken WithTrivia(FormattingTrivia trivia)
        {
            return this with { Trivia = trivia ?? FormattingTrivia.Empty };
        }
    }
}
