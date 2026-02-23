using DSLKIT.Lexer;
using DSLKIT.Terminals;
using DSLKIT.Test.Utils;
using System.IO;
using System.Reflection;

namespace DSLKIT.Test.LexerTests
{
    public static class LexerTestData
    {
        public static string SampleCode = File.ReadAllText(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "LexerTests",
            "TestData",
            "LexerTestData.txt"))
            .NormalizeLineEndings();

        public static LexerSettings GetSampleLexerSettings()
        {
            return
            [
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
            ];
        }
    }
}