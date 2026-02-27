using DSLKIT.Lexer;
using DSLKIT.Terminals;
using Xunit;

namespace DSLKIT.Test.TerminalTests
{
    public class NewLineTerminalTests
    {
        [Fact]
        public void DefaultMode_ShouldCollapseConsecutiveNewLines()
        {
            var source = new StringSourceStream("\r\n\nrest");
            var terminal = new NewLineTerminal();

            var matched = terminal.TryMatch(source, out var token);

            Assert.True(matched);
            Assert.NotNull(token);
            Assert.Equal(3, token.Length);
            Assert.Equal("\r\n\n", token.OriginalString);
        }

        [Fact]
        public void NonCollapsedMode_ShouldMatchSingleNewLine()
        {
            var source = new StringSourceStream("\r\n\nrest");
            var terminal = new NewLineTerminal(collapseConsecutive: false);

            var matched = terminal.TryMatch(source, out var token);

            Assert.True(matched);
            Assert.NotNull(token);
            Assert.Equal(2, token.Length);
            Assert.Equal("\r\n", token.OriginalString);
        }
    }
}
