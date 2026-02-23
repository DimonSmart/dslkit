using DSLKIT.Lexer;
using DSLKIT.Terminals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace DSLKIT.Test.TerminalTests
{
    [TestClass]
    public class SingleLineCommentTerminalTests
    {
        [Fact]
        public void OneLineCommentTerminalTest()
        {
            var s = new StringSourceStream("//This is a comment text");
            var it = new SingleLineCommentTerminal(@"//");
            Assert.AreEqual(true, it.TryMatch(s, out var token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(s.Length, token.Length);
        }

        [Fact]
        public void TwoSequentialLinesCommentTerminalTest()
        {
            var s = new StringSourceStream("//This is a comment text\n\r//This is a comment text");
            var it = new SingleLineCommentTerminal(@"//");
            Assert.AreEqual(true, it.TryMatch(s, out var token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual("//This is a comment text".Length, token.Length);
        }
    }
}