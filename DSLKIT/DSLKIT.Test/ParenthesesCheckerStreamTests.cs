using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DSLKIT.Test
{
    [TestClass]
    public class ParenthesesCheckerStreamTests
    {
        [TestMethod]
        public void MissedCloseParenthesisTest()
        {
            ParenthesesCheckerAssertOnError(@"(");
        }

        [TestMethod]
        public void NestedParenthesisMissedCloseTest()
        {
            ParenthesesCheckerAssertOnError(@"(<)");
        }

        [TestMethod]
        public void MissedOpenParenthesisTest()
        {
            ParenthesesCheckerAssertOnError(@")");
        }

        [TestMethod]
        public void WrongParenthesesOrderTest()
        {
            ParenthesesCheckerAssertOnError(@")(");
        }

        [TestMethod]
        public void WrongParenthesesTypes()
        {
            ParenthesesCheckerAssertOnError(@"(}");
        }

        private static void ParenthesesCheckerAssertOnError(string src)
        {
            var stream = new Lexer(LexerTestData.GetSampleLexerSettings())
                .GetTokens(new StringSourceStream(src))
                .WithParenthesesChecking()
                .ToList();
            stream.Dump();
            Assert.IsTrue(stream.Any(token => token.GetType() == typeof(ErrorToken)),
                $"Erroneous input handled as valid for:{src}");
        }
    }
}