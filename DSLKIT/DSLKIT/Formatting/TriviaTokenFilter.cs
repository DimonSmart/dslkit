using DSLKIT.Terminals;
using DSLKIT.Tokens;
using System.Collections.Generic;

namespace DSLKIT.Formatting
{
    public class TriviaTokenFilter
    {
        public IEnumerable<IToken> FilterTokens(IReadOnlyList<IToken> tokens)
        {
            var result = new List<IToken>();
            var pendingTrivia = new List<IToken>();

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (IsFormattingToken(token))
                {
                    pendingTrivia.Add(token);
                }
                else
                {
                    // Find trailing trivia for this token
                    var trailingTrivia = new List<IToken>();
                    int j = i + 1;
                    while (j < tokens.Count && IsFormattingToken(tokens[j]))
                    {
                        trailingTrivia.Add(tokens[j]);
                        j++;
                    }

                    result.Add(token.WithTrivia(new FormattingTrivia(pendingTrivia, trailingTrivia)));

                    // Clear pending trivia and skip trailing trivia tokens
                    pendingTrivia.Clear();
                    i = j - 1;
                }
            }

            return result;
        }

        public static bool IsFormattingToken(IToken token)
        {
            return token?.Terminal?.Flags == TermFlags.Space ||
                   token?.Terminal?.Flags == TermFlags.Comment;
        }
    }
}