using System.Collections.Generic;

namespace DSLKIT.Terminals
{
    public static class ParenthesesKeywordConstants
    {
        public static KeywordTerminal OpenParenthesis = new KeywordTerminal("(");
        public static KeywordTerminal CloseParenthesis = new KeywordTerminal(")");
        public static KeywordTerminal OpenSquareBracket = new KeywordTerminal("[");
        public static KeywordTerminal CloseSquareBracket = new KeywordTerminal("]");
        public static KeywordTerminal OpenCurlyBracket = new KeywordTerminal("{");
        public static KeywordTerminal CloseCurlyBracket = new KeywordTerminal("}");
        public static KeywordTerminal OpenAngleBracket = new KeywordTerminal("<");
        public static KeywordTerminal CloseAngleBracket = new KeywordTerminal(">");

        public static Dictionary<ITerminal, ITerminal> ParenthesisPairs =
            new Dictionary<ITerminal, ITerminal>(new KeywordTerminalEqualityComparer())
            {
                [OpenParenthesis] = CloseParenthesis,
                [OpenSquareBracket] = CloseSquareBracket,
                [OpenCurlyBracket] = CloseCurlyBracket,
                [OpenAngleBracket] = CloseAngleBracket,

                [CloseParenthesis] = OpenParenthesis,
                [CloseSquareBracket] = OpenSquareBracket,
                [CloseCurlyBracket] = OpenCurlyBracket,
                [CloseAngleBracket] = OpenAngleBracket
            };

        public class KeywordTerminalEqualityComparer : IEqualityComparer<ITerminal>
        {
            public bool Equals(ITerminal x, ITerminal y)
            {
                return x?.DictionaryKey == y?.DictionaryKey;
            }

            public int GetHashCode(ITerminal obj)
            {
                return obj.DictionaryKey.GetHashCode();
            }
        }
    }
}