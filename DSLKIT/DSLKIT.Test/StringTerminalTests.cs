using DSLKIT.Terminals;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSLKIT.Test
{
    [TestClass]
    public class StringTerminalTests
    {
        [TestMethod]
        public void NormalUseTest()
        {
            var s = new StringSourceStream("Test".DoubleQuoteIt());
            var rt = new StringTerminal();
            IToken token;
            Assert.AreEqual(true, rt.TryMatch(s, out token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(6, token.Length);
            Assert.AreEqual(typeof (string), token.Value.GetType());
            Assert.AreEqual("Test", token.Value);
            Assert.AreEqual("Test".DoubleQuoteIt(), token.OriginalString);
        }

        [TestMethod]
        public void SingleQuoteStringTest()
        {
            DoStringTest(@"'Hello World'_rest of string", 0, @"Hello World", @"'", @"'");
            DoStringTest(@"1'Hello World'_rest of string", 1, @"Hello World", @"'", @"'");
        }

        [TestMethod]
        public void ParenthesisQuoteStringTest()
        {
            DoStringTest(@"(Hello World)_rest of string", 0, @"Hello World", @"(", @")");
            DoStringTest(@"1(Hello World)_rest of string", 1, @"Hello World", @"(", @")");
        }

        [TestMethod]
        public void CurvedBracesQuoteStringTest()
        {
            DoStringTest(@"{Hello World}'_rest of string", 0, @"Hello World", @"{", @"}");
            DoStringTest(@"1{Hello World}'_rest of string", 1, @"Hello World", @"{", @"}");
        }

        [TestMethod]
        public void BracketQuoteStringTest()
        {
            DoStringTest(@"[Hello World]'_rest of string", 0, @"Hello World", @"[", @"]");
            DoStringTest(@"1[Hello World]'_rest of string", 1, @"Hello World", @"[", @"]");
        }

        [TestMethod]
        public void PrefexedStringTest()
        {
            DoStringTest(@"$[Hello World]'_rest of string", 0, @"Hello World", @"$[", @"]");
            DoStringTest(@"$""Hello World""'_rest of string", 0, @"Hello World", @"$""", @"""");
        }


        private void DoStringTest(string stringToTest, int startPosition, string expectedString, string start,
            string end)
        {
            var stream = new StringSourceStream(stringToTest);
            stream.Seek(startPosition);

            var rt = new StringTerminal(start, end);
            IToken token;
            Assert.AreEqual(true, rt.TryMatch(stream, out token));
            Assert.AreEqual(startPosition, token.Position);
            Assert.AreEqual(expectedString.Length + start.Length + end.Length, token.Length);
            Assert.AreEqual(typeof (string), token.Value.GetType());
            Assert.AreEqual(expectedString, token.Value);
            Assert.AreEqual(start + expectedString + end, token.OriginalString);
        }
    }
}