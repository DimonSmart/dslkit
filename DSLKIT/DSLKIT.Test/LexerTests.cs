using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DSLKIT.Terminals;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSLKIT.Test
{
    public static class TestHelper
    {
        public static string GetLexerSample()
        {
            return File.ReadAllText("LexerTestData.txt");
        }
    }

    [TestClass]
    public class LexerTests
    {
        [TestMethod]
        public void LexerStreamEofTest()
        {
            var source = new StringSourceStream("A");
            var lexerData = new LexerSettings
            {
                new KeywordTerminal("A")
            };

            var tokens = new Lexer(lexerData).GetTokens(source);
            PrintTokens(tokens);
        }


        [TestMethod]
        public void LexerComplexTest()
        {
            var source = new StringSourceStream(TestHelper.GetLexerSample());
            var tokens = new Lexer(GetSampleLexerData()).GetTokens(source);
            PrintTokens(tokens);
        }

        [TestMethod]
        public void LexerBraceCheckTest()
        {
            BraceCheckerAssertOnError(@"(");
            BraceCheckerAssertOnError(@"((");
            BraceCheckerAssertOnError(@"(()");
            BraceCheckerAssertOnError(@"(()()");
            BraceCheckerAssertOnError(@")");
            BraceCheckerAssertOnError(@"))");
            BraceCheckerAssertOnError(@"())");
            BraceCheckerAssertOnError(@"()())");
        }

        private static void BraceCheckerAssertOnError(string src)
        {
            var stream =
                new BracketMatcherStream(new Lexer(GetSampleLexerData()).GetTokens(new StringSourceStream(src))).ToList();
            PrintTokens(stream);
            Assert.IsTrue(stream.Any(token => token.GetType() == typeof (ErrorToken)));
        }


        [TestMethod]
        public void LexerSpeedTest()
        {
            var source = new StringSourceStream(TestHelper.GetLexerSample());
            var lexerData = GetSampleLexerData();

            Debug.WriteLine("Speed with usePreviewChar option turned ON");
            lexerData.UsePreviewChar = true;
            LexerTestRun(source, new Lexer(lexerData));

            Debug.WriteLine("Speed with usePreviewChar option turned off");
            lexerData.UsePreviewChar = false;
            LexerTestRun(source, new Lexer(lexerData));
        }

        private static void LexerTestRun(StringSourceStream source, Lexer lexer)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for (var i = 0; i < 1000; i++)
            {
                source.Position = 0;
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                lexer.GetTokens(source).ToList();
            }
            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            string elapsedTime = $"Elapsed time: {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds/10:00}";
            Debug.WriteLine(elapsedTime);
        }


        private static LexerSettings GetSampleLexerData()
        {
            var lexerData = new LexerSettings
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
            return lexerData;
        }


        private static void PrintTokens(IEnumerable<Token> tokens)
        {
            foreach (var token in tokens)
            {
                if (token is ErrorToken)
                {
                    Debug.WriteLine("Error");
                    break;
                }
                Debug.WriteLine("Token: {0}\tsValue: {1}\tvalue: {2}\ttoString:{3}\tpos:{4},len:{5}",
                    token.Terminal.GetType().Name, token.StringValue, token.Value, token, token.Position, token.Length);
            }
        }
    }
}