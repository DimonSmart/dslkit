using DSLKIT.Lexer;
using DSLKIT.Terminals;
using Xunit;

namespace DSLKIT.Test.TerminalTests
{
    public class NumberTerminalTests
    {
        [Fact]
        public void IniNumberProfile_ShouldRejectLeadingZeroNumbers()
        {
            var source = new StringSourceStream("012");
            var terminal = new NumberTerminal(style: NumberStyle.IniNumber);

            var matched = terminal.TryMatch(source, out var token);

            Assert.False(matched);
            Assert.Null(token);
        }

        [Fact]
        public void IniNumberProfile_ShouldMatchFraction()
        {
            var source = new StringSourceStream("12.5");
            var terminal = new NumberTerminal(style: NumberStyle.IniNumber);

            var matched = terminal.TryMatch(source, out var token);

            Assert.True(matched);
            Assert.NotNull(token);
            Assert.Equal("12.5", token.OriginalString);
        }

        [Fact]
        public void SqlNumberProfile_ShouldMatchLeadingDotAndExponent()
        {
            var source = new StringSourceStream(".5e-2");
            var terminal = new NumberTerminal(style: NumberStyle.SqlNumber);

            var matched = terminal.TryMatch(source, out var token);

            Assert.True(matched);
            Assert.NotNull(token);
            Assert.Equal(".5e-2", token.OriginalString);
        }
    }
}
