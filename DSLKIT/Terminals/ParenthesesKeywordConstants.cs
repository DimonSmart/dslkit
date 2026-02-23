using System.Collections.Generic;

namespace DSLKIT.Terminals
{
    public static class ParenthesesKeywordConstants
    {
        public static readonly KeywordTerminal OpenParenthesis = new KeywordTerminal("(");
        public static readonly KeywordTerminal CloseParenthesis = new KeywordTerminal(")");
        public static readonly KeywordTerminal OpenSquareBracket = new KeywordTerminal("[");
        public static readonly KeywordTerminal CloseSquareBracket = new KeywordTerminal("]");
        public static readonly KeywordTerminal OpenCurlyBracket = new KeywordTerminal("{");
        public static readonly KeywordTerminal CloseCurlyBracket = new KeywordTerminal("}");
        public static readonly KeywordTerminal OpenAngleBracket = new KeywordTerminal("<");
        public static readonly KeywordTerminal CloseAngleBracket = new KeywordTerminal(">");

        public static IReadOnlyDictionary<ITerminal, ITerminal> ParenthesisPairs { get; } =
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
            public bool Equals(ITerminal? x, ITerminal? y)
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
