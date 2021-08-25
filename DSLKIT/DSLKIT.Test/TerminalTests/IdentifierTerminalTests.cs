﻿using DSLKIT.Terminals;
using DSLKIT.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace DSLKIT.Test.TerminalTests
{
    [TestClass]
    public class IdentifierTerminalTests
    {
        [Fact]
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