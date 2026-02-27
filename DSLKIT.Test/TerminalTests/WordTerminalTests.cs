using System;
using DSLKIT.Lexer;
using DSLKIT.Terminals;
using Xunit;

namespace DSLKIT.Test.TerminalTests
{
    public class WordTerminalTests
    {
        [Fact]
        public void IniWordProfile_ShouldMatchDotAndDash()
        {
            var source = new StringSourceStream("core.value-1");
            var terminal = new WordTerminal(style: WordStyle.IniWord);

            var matched = terminal.TryMatch(source, out var token);

            Assert.True(matched);
            Assert.NotNull(token);
            Assert.Equal("core.value-1", token.OriginalString);
        }

        [Fact]
        public void SqlIdentifierProfile_ShouldMatchCaseInsensitiveWithDollarAndHash()
        {
            var source = new StringSourceStream("Temp$Name#1");
            var terminal = new WordTerminal(style: WordStyle.SqlIdentifier);

            var matched = terminal.TryMatch(source, out var token);

            Assert.True(matched);
            Assert.NotNull(token);
            Assert.Equal("Temp$Name#1", token.OriginalString);
        }

        [Fact]
        public void CustomStartRule_WithoutCharacterClass_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => new WordTerminal(
                options: new WordOptions
                {
                    StartRule = WordStartRule.Custom
                }));
        }
    }
}
