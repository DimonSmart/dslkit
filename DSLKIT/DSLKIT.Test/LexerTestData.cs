using System.IO;
using DSLKIT.Terminals;

namespace DSLKIT.Test
{
    public static class LexerTestData
    {
        public static string SampleCode = File.ReadAllText("LexerTestData.txt");

        public static LexerSettings GetSampleLexerSettings()
        {
            return new LexerSettings
            {
                new SpaceTerminal(),
                new KeywordTerminal("SIN"),
                new KeywordTerminal("COS"),
                new KeywordTerminal("(", TermFlags.OpenBrace),
                new KeywordTerminal(")", TermFlags.CloseBrace),
                new KeywordTerminal("{", TermFlags.OpenBrace),
                new KeywordTerminal("}", TermFlags.CloseBrace),
                new KeywordTerminal(","),
                new KeywordTerminal("."),
                new KeywordTerminal(";"),
                new StringTerminal(),
                new StringTerminal("[", "]"),
                new StringTerminal("$", @""""),
                new IntegerTerminal(),
                new SingleLineCommentTerminal("//"),
                new MultiLineCommentTerminal(@"/*", @"*/"),
                new KeywordTerminal("int"),
                new KeywordTerminal("="),
                new KeywordTerminal("+"),
                new KeywordTerminal("-"),
                new KeywordTerminal("/"),
                new KeywordTerminal("*"),
                new KeywordTerminal("<"),
                new KeywordTerminal(">"),
                new KeywordTerminal("<="),
                new KeywordTerminal(">="),
                new KeywordTerminal("=="),
                new IdentifierTerminal()
            };
        }
    }
}