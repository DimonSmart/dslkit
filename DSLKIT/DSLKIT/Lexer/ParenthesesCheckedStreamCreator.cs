using DSLKIT.Tokens;
using System.Collections.Generic;

namespace DSLKIT
{
    public static class ParenthesesCheckedStreamCreator
    {
        public static ParenthesesCheckedStream WithParenthesesChecking(this IEnumerable<IToken> sourceStream)
        {
            return new ParenthesesCheckedStream(sourceStream);
        }
    }
}