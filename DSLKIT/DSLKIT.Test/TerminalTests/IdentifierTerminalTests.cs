using DSLKIT.Terminals;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSLKIT.Test.TerminalTests
{
    [TestClass]
    public class IdentifierTerminalTests
    {
        [TestMethod]
        public void IdentifierTerminalTryMatchTest()
        {
            var s = new StringSourceStream("Variable");
            var terminal = new IdentifierTerminal();
            Assert.AreEqual(true, terminal.TryMatch(s, out IToken token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual("Variable", token.Value);
        }
    }
}