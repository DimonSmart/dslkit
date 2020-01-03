using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DSLKIT.Terminals.KeywordTerminal;
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
                CreateKeywordTerminal("A")
            };

            new Lexer(lexerData)
                .GetTokens(source)
                .Dump();
        }

        [TestMethod]
        public void LexerComplexTest()
        {
            new Lexer(GetSampleLexerSettings())
                .GetTokens(new StringSourceStream(SampleCode))
                .Dump();
        }

        [TestMethod]
        public void LexerSpeedTest()
        {
            var source = new StringSourceStream(SampleCode);
            var lexerData = GetSampleLexerSettings();

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
    }
}