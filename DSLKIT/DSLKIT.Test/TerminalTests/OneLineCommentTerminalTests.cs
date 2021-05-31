using DSLKIT.Terminals;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSLKIT.Test.TerminalTests
{
    [TestClass]
    public class SingleLineCommentTerminalTests
    {
        [TestMethod]
        public void OneLineCommentTerminalTest()
        {
            var s = new StringSourceStream("//This is a comment text");
            var it = new SingleLineCommentTerminal(@"//");
            Assert.AreEqual(true, it.TryMatch(s, out IToken token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(s.Length, token.Length);
        }

        [TestMethod]
        public void TwoSequentialLinesCommentTerminalTest()
        {
            var s = new StringSourceStream("//This is a comment text\n\r//This is a comment text");
            var it = new SingleLineCommentTerminal(@"//");
            Assert.AreEqual(true, it.TryMatch(s, out IToken token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual("//This is a comment text".Length, token.Length);
        }
    }
}