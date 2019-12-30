using DSLKIT.Terminals;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DSLKIT.Terminals.KeywordTerminal;

namespace DSLKIT.Test
{
    [TestClass]
    public class RegExpTerminalTests
    {
        private RegExpTerminal GetIntegerRegexpTerminal()
        {
            return new RegExpTerminal("Integer", @"\G\d+", null, TermFlags.None);
        }


        [TestMethod]
        public void NormalUseTest()
        {
            var s = new StringSourceStream("123");
            IToken token;
            Assert.AreEqual(true, GetIntegerRegexpTerminal().TryMatch(s, out token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(3, token.Length);
        }

        [TestMethod]
        public void NotMatchedUseTest()
        {
            var s = new StringSourceStream("ABC");
            IToken token;
            Assert.AreEqual(false, GetIntegerRegexpTerminal().TryMatch(s, out token));
            Assert.AreEqual(null, token);
        }

        [TestMethod]
        public void StringToTerminalTest()
        {
            var s = new StringSourceStream("ABCD");
            var rt = CreateTerminal("ABC");
            IToken token;
            Assert.AreEqual(true, rt.TryMatch(s, out token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(3, token.Length);
        }
    }
}