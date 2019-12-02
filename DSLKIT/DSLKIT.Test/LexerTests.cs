using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DSLKIT.Terminals;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DSLKIT.Test.LexerTestData;

namespace DSLKIT.Test
{
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
            var source = new StringSourceStream(SampleText);
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
                new BracketMatcherStream(new Lexer(GetSampleLexerData()).GetTokens(new StringSourceStream(src)))
                    .ToList();
            PrintTokens(stream);
            Assert.IsTrue(stream.Any(token => token.GetType() == typeof(ErrorToken)));
        }


        [TestMethod]
        public void LexerSpeedTest()
        {
            var source = new StringSourceStream(SampleText);
            var lexerData = GetSampleLexerData();

            Debug.WriteLine("Lexer speed test");
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
            var elapsedTime = $"Elapsed time: {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
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


        private static void PrintTokens(IEnumerable<IToken> tokens)
        {
            foreach (var token in tokens)
            {
                if (token is ErrorToken)
                {
                    Debug.WriteLine("Error");
                    break;
                }

                var tokenDescription = token.Terminal.GetType().Name + Environment.NewLine +
                                       GetNonEmptyString("OriginalString: ", token.OriginalString) +
                                       GetNonEmptyString("Value: ", token.Value) +
                                       GetNonEmptyString("Token: ", token.ToString()) +
                                       "Pos: " + token.Position + Environment.NewLine +
                                       "Len: " + token.Length + Environment.NewLine;

                Debug.WriteLine(tokenDescription);
            }
        }

        private static string GetNonEmptyString(string key, object value)
        {
            var s = value?.ToString();

            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            return key + s + Environment.NewLine;
        }
    }
}