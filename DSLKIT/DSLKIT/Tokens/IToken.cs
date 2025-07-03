using DSLKIT.Formatting;

namespace DSLKIT.Tokens
{
    public interface IToken : ITokenBase
    {
        FormattingTrivia Trivia { get; }

        // TODO: THis should be extension or something like this
        IToken WithTrivia(FormattingTrivia trivia);
    }
}