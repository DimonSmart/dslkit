using DSLKIT.Terminals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace DSLKIT.Test.TerminalTests
{
    [TestClass]
    public class MultiLineCommentTerminalTests
    {
        [Fact]
        public void MultiLineCommentTerminalTest()
        {
            const string commentText = " XXX commented ";
            var s = new StringSourceStream($"/*{commentText}*/ other text");
            var terminal = new MultiLineCommentTerminal(@"/*", @"*/");
            Assert.AreEqual(true, terminal.TryMatch(s, out var token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(commentText, token.Value);
        }
    }
}