using System.Collections.Generic;
using System.Linq;
using DSLKIT.Tokens;

namespace DSLKIT.Formatting
{
    public class FormattingTrivia
    {
        public IReadOnlyList<IToken> LeadingTrivia { get; }
        public IReadOnlyList<IToken> TrailingTrivia { get; }

        public FormattingTrivia(IEnumerable<IToken> leadingTrivia = null, IEnumerable<IToken> trailingTrivia = null)
        {
            LeadingTrivia = leadingTrivia?.ToList() ?? [];
            TrailingTrivia = trailingTrivia?.ToList() ?? [];
        }

        public static FormattingTrivia Empty => new();
    }
}