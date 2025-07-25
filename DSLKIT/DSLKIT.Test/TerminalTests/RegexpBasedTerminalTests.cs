﻿using DSLKIT.Lexer;
using DSLKIT.Terminals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace DSLKIT.Test.TerminalTests
{
    [TestClass]
    public class RegExpTerminalTests
    {
        private RegExpTerminal GetIntegerRegexpTerminal()
        {
            return new RegExpTerminal("Integer", @"\G\d+", null, TermFlags.None);
        }

        [Fact]
        public void NormalUseTest()
        {
            var s = new StringSourceStream("123");
            Assert.AreEqual(true, GetIntegerRegexpTerminal().TryMatch(s, out var token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(3, token.Length);
        }

        [Fact]
        public void NotMatchedUseTest()
        {
            var s = new StringSourceStream("ABC");
            Assert.AreEqual(false, GetIntegerRegexpTerminal().TryMatch(s, out var token));
            Assert.AreEqual(null, token);
        }

        [Fact]
        public void StringToTerminalTest()
        {
            var s = new StringSourceStream("ABCD");
            var rt = new KeywordTerminal("ABC");
            Assert.AreEqual(true, rt.TryMatch(s, out var token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual(3, token.Length);
        }
    }
}