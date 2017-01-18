using DSLKIT.Terminals;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSLKIT.Test
{
    [TestClass]
    public class MultiLineCommentTerminalTests
    {
        [TestMethod]
        public void MultiLineCommentTerminalTest()
        {
            var commentText = " XXX commented ";
            var s = new StringSourceStream($"/*{commentText}*/ other text");
            var terminal = new MultiLineCommentTerminal(@"/*", @"*/");
            IToken token;
            Assert.AreEqual(true, terminal.TryMatch(s, out token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(commentText, token.Value);
        }
    }
}