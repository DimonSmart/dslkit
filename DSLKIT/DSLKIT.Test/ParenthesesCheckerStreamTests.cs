using System.Linq;
using DSLKIT.Lexer;
using DSLKIT.Test.Utils;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace DSLKIT.Test
{
    [TestClass]
    public class ParenthesesCheckerStreamTests
    {
        [Fact]
        public void MissedCloseParenthesisTest()
        {
            ParenthesesCheckerAssertOnError(@"(");
        }

        [Fact]
        public void NestedParenthesisMissedCloseTest()
        {
            ParenthesesCheckerAssertOnError(@"(<)");
        }

        [Fact]
        public void MissedOpenParenthesisTest()
        {
            ParenthesesCheckerAssertOnError(@")");
        }

        [Fact]
        public void WrongParenthesesOrderTest()
        {
            ParenthesesCheckerAssertOnError(@")(");
        }

        [Fact]
        public void WrongParenthesesTypes()
        {
            ParenthesesCheckerAssertOnError(@"(}");
        }

        private static void ParenthesesCheckerAssertOnError(string src)
        {
            var stream = new Lexer.Lexer(LexerTestData.GetSampleLexerSettings())
                .GetTokens(new StringSourceStream(src))
                .WithParenthesesChecking()
                .ToList();
            stream.Dump();
            Assert.IsTrue(stream.Any(token => token.GetType() == typeof(ErrorToken)),
                $"Erroneous input handled as valid for:{src}");
        }
    }
}