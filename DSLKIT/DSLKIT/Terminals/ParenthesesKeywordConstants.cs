using System.Collections.Generic;

namespace DSLKIT.Terminals
{
    public static class ParenthesesKeywordConstants
    {
        public static KeywordTerminal OpenParenthesis = KeywordTerminal.CreateKeywordTerminal("(");
        public static KeywordTerminal CloseParenthesis = KeywordTerminal.CreateKeywordTerminal(")");
        public static KeywordTerminal OpenSquareBracket = KeywordTerminal.CreateKeywordTerminal("[");
        public static KeywordTerminal CloseSquareBracket = KeywordTerminal.CreateKeywordTerminal("]");
        public static KeywordTerminal OpenCurlyBracket = KeywordTerminal.CreateKeywordTerminal("{");
        public static KeywordTerminal CloseCurlyBracket = KeywordTerminal.CreateKeywordTerminal("}");
        public static KeywordTerminal OpenAngleBracket = KeywordTerminal.CreateKeywordTerminal("<");
        public static KeywordTerminal CloseAngleBracket = KeywordTerminal.CreateKeywordTerminal(">");

        public static Dictionary<ITerminal, ITerminal> ParenthesisPairs =
            new Dictionary<ITerminal, ITerminal>
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
    }
}