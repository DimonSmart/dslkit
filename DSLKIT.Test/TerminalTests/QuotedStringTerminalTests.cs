using DSLKIT.Lexer;
using DSLKIT.Terminals;
using Xunit;

namespace DSLKIT.Test.TerminalTests
{
    public class QuotedStringTerminalTests
    {
        [Fact]
        public void IniQuotedProfile_ShouldMatchSingleQuotedBackslashString()
        {
            var source = new StringSourceStream("'it\\'s'");
            var terminal = new QuotedStringTerminal(style: StringStyle.IniQuoted);

            var matched = terminal.TryMatch(source, out var token);

            Assert.True(matched);
            Assert.NotNull(token);
            Assert.Equal("'it\\'s'", token.OriginalString);
            Assert.Equal("it\\'s", token.Value);
        }

        [Fact]
        public void SqlSingleQuotedProfile_ShouldMatchNPrefixAndEscapedQuote()
        {
            var source = new StringSourceStream("N'it''s'");
            var terminal = new QuotedStringTerminal(style: StringStyle.SqlSingleQuoted);

            var matched = terminal.TryMatch(source, out var token);

            Assert.True(matched);
            Assert.NotNull(token);
            Assert.Equal("N'it''s'", token.OriginalString);
            Assert.Equal("it''s", token.Value);
        }

        [Fact]
        public void JsonStringProfile_ShouldRejectMultilineString()
        {
            var source = new StringSourceStream("\"line1\nline2\"");
            var terminal = new QuotedStringTerminal(style: StringStyle.JsonString);

            var matched = terminal.TryMatch(source, out var token);

            Assert.False(matched);
            Assert.Null(token);
        }
    }
}
