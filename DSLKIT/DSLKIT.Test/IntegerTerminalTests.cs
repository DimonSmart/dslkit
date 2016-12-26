using DSLKIT.Terminals;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSLKIT.Test
{
    [TestClass]
    public class IntegerTerminalTests
    {
        [TestMethod]
        public void NormalUseTest()
        {
            var s = new StringSourceStream("123");
            var it = new IntegerTerminal();
            Token token;
            Assert.AreEqual(true, it.TryMatch(s, out token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(3, token.Length);
        }
    }
}