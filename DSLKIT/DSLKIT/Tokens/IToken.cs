using DSLKIT.Formatting;

namespace DSLKIT.Tokens
{
    public interface IToken : ITokenBase
    {
        FormattingTrivia Trivia { get; }

        IToken WithTrivia(FormattingTrivia trivia);
    }
}