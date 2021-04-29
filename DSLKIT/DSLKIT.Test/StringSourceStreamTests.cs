using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSLKIT.Test
{
    [TestClass]
    public class StringSourceStreamTests
    {
        [TestMethod]
        public void NormalUseTest()
        {
            ISourceStream s = new StringSourceStream("AB");
            Assert.AreEqual('A', s.Read());
            Assert.AreEqual('B', s.Read());
        }

        [TestMethod]
        public void DoublePeekTest()
        {
            ISourceStream s = new StringSourceStream("AB");
            Assert.AreEqual('A', s.Peek());
            Assert.AreEqual('A', s.Peek());
        }

        [TestMethod]
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