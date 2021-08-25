using DSLKIT.Terminals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace DSLKIT.Test.TerminalTests
{
    [TestClass]
    public class IntegerTerminalTests
    {
        [Fact]
        public void NormalUseTest()
        {
            var s = new StringSourceStream("123");
            var it = new IntegerTerminal();
            Assert.AreEqual(true, it.TryMatch(s, out var token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(3, token.Length);
        }
    }
}