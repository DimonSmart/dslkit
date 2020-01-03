using System.IO;
using DSLKIT.Terminals;

namespace DSLKIT.Test
{
    public static class LexerTestData
    {
        public static string SampleCode = File.ReadAllText("LexerTestData.txt");

        public static LexerSettings GetSampleLexerSettings()
        {
            var lexerData = new LexerSettings
            {
                new SpaceTerminal(),
                KeywordTerminal.CreateKeywordTerminal("SIN"),
                KeywordTerminal.CreateKeywordTerminal("COS"),
                KeywordTerminal.CreateKeywordTerminal("(", TermFlags.OpenBrace),
                KeywordTerminal.CreateKeywordTerminal(")", TermFlags.CloseBrace),
                KeywordTerminal.CreateKeywordTerminal("{", TermFlags.OpenBrace),
                KeywordTerminal.CreateKeywordTerminal("}", TermFlags.CloseBrace),
                KeywordTerminal.CreateKeywordTerminal(","),
                KeywordTerminal.CreateKeywordTerminal("."),
                KeywordTerminal.CreateKeywordTerminal(";"),
                new StringTerminal(),
                new StringTerminal("[", "]"),
                new StringTerminal("$", @""""),
                new IntegerTerminal(),
                new SingleLineCommentTerminal("//"),
                new MultiLineCommentTerminal(@"/*", @"*/"),
                KeywordTerminal.CreateKeywordTerminal("int"),
                KeywordTerminal.CreateKeywordTerminal("="),
                KeywordTerminal.CreateKeywordTerminal("+"),
                KeywordTerminal.CreateKeywordTerminal("-"),
                KeywordTerminal.CreateKeywordTerminal("/"),
                KeywordTerminal.CreateKeywordTerminal("*"),
                KeywordTerminal.CreateKeywordTerminal("<"),
                KeywordTerminal.CreateKeywordTerminal(">"),
                KeywordTerminal.CreateKeywordTerminal("<="),
                KeywordTerminal.CreateKeywordTerminal(">="),
                KeywordTerminal.CreateKeywordTerminal("=="),
                new IdentifierTerminal()
            };
            return lexerData;
        }
    }
}