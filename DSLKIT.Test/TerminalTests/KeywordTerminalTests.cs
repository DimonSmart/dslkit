using DSLKIT.Lexer;
using DSLKIT.Terminals;
using Xunit;

namespace DSLKIT.Test.TerminalTests
{
    public class KeywordTerminalTests
    {
        [Fact]
        public void WholeWord_ShouldRejectKeywordPrefixInsideWord()
        {
            var source = new StringSourceStream("SELECTED");
            var terminal = new KeywordTerminal("SELECT", wholeWord: true, ignoreCase: true);

            var matched = terminal.TryMatch(source, out var token);

            Assert.False(matched);
            Assert.Null(token);
        }

        [Fact]
        public void WholeWord_ShouldMatchStandaloneKeyword()
        {
            var source = new StringSourceStream("SELECT ");
            var terminal = new KeywordTerminal("SELECT", wholeWord: true, ignoreCase: true);

            var matched = terminal.TryMatch(source, out var token);

            Assert.True(matched);
            Assert.NotNull(token);
            Assert.Equal("SELECT", token.OriginalString);
        }

        [Fact]
        public void IgnoreCase_ShouldMatchDifferentCase()
        {
            var source = new StringSourceStream("select");
            var terminal = new KeywordTerminal("SELECT", ignoreCase: true);

            var matched = terminal.TryMatch(source, out var token);

            Assert.True(matched);
            Assert.NotNull(token);
            Assert.Equal("select", token.OriginalString);
        }
    }
}
