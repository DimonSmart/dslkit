using DSLKIT.Lexer;
using DSLKIT.Terminals;
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
            Assert.AreEqual(true, terminal.TryMatch(s, out var token));
            Assert.AreEqual(0, token.Position);
            Assert.AreEqual("Variable", token.Value);
        }

        [Fact]
        public void IdentifierTerminal_WithAllowDotFalse_ShouldStopBeforeDot()
        {
            var source = new StringSourceStream("schema.table");
            var terminal = new IdentifierTerminal(allowDot: false);

            Assert.AreEqual(true, terminal.TryMatch(source, out var token));
            Assert.AreEqual("schema", token?.OriginalString);
        }

        [Fact]
        public void IdentifierTerminal_DifferentAllowDotSettings_ShouldHaveDifferentDictionaryKeys()
        {
            var withDot = new IdentifierTerminal(allowDot: true);
            var withoutDot = new IdentifierTerminal(allowDot: false);

            Assert.AreNotEqual(withDot.DictionaryKey, withoutDot.DictionaryKey);
        }
    }
}
