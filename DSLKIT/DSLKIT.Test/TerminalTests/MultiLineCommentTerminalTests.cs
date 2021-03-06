using DSLKIT.Terminals;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSLKIT.Test.TerminalTests
{
    [TestClass]
    public class MultiLineCommentTerminalTests
    {
        [TestMethod]
        public void MultiLineCommentTerminalTest()
        {
            const string commentText = " XXX commented ";
            var s = new StringSourceStream($"/*{commentText}*/ other text");
            var terminal = new MultiLineCommentTerminal(@"/*", @"*/");
            Assert.AreEqual(true, terminal.TryMatch(s, out IToken token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(commentText, token.Value);
        }
    }
}