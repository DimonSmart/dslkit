using DSLKIT.Tokens;
using System.Collections.Generic;

namespace DSLKIT.Formatting
{
    public record FormattingTrivia(
        IReadOnlyList<IToken> LeadingTrivia,
        IReadOnlyList<IToken> TrailingTrivia)
    {
        public static FormattingTrivia Empty => new([], []);
    }
}