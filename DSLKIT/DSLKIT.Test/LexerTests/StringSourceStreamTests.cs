using DSLKIT.Lexer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace DSLKIT.Test.LexerTests
{
    [TestClass]
    public class StringSourceStreamTests
    {
        [Fact]
        public void NormalUseTest()
        {
            ISourceStream s = new StringSourceStream("AB");
            Assert.AreEqual('A', s.Read());
            Assert.AreEqual('B', s.Read());
        }

        [Fact]
        public void DoublePeekTest()
        {
            ISourceStream s = new StringSourceStream("AB");
            Assert.AreEqual('A', s.Peek());
            Assert.AreEqual('A', s.Peek());
        }

        [Fact]
        public void PositionSlidingTest()
        {
            ISourceStream s = new StringSourceStream("AB");
            Assert.AreEqual(0, s.Position);
            Assert.AreEqual('A', s.Read());
            Assert.AreEqual(1, s.Position);
            Assert.AreEqual('B', s.Read());
            Assert.AreEqual(2, s.Position);
        }
    }
}