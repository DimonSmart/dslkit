using System.Diagnostics;
using System.Linq;
using DSLKIT.Terminals;
using DSLKIT.Test.Utils;
using Xunit;
using static DSLKIT.Test.LexerTestData;

namespace DSLKIT.Test
{
    public class LexerTests
    {
        [Fact]
        public void LexerStreamEofTest()
        {
            var source = new StringSourceStream("A");
            var lexerData = new LexerSettings { new KeywordTerminal("A") };

            new Lexer.Lexer(lexerData)
                .GetTokens(source)
                .Dump();
        }

        [Fact]
        public void LexerComplexTest()
        {
            new Lexer.Lexer(GetSampleLexerSettings())
                .GetTokens(new StringSourceStream(SampleCode))
                .Dump();
        }

        [Fact]
        public void LexerSpeedTest()
        {
            var source = new StringSourceStream(SampleCode);
            var lexerData = GetSampleLexerSettings();

            Debug.WriteLine("Lexer speed test");
            LexerTestRun(source, new Lexer.Lexer(lexerData));
        }

        private static void LexerTestRun(StringSourceStream source, Lexer.Lexer lexer)
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