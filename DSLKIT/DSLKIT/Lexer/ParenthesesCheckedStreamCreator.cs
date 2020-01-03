using System.Collections.Generic;
using DSLKIT.Tokens;

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