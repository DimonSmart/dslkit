using DSLKIT.GrammarExamples.SJackson;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class SJacksonGrammarExampleTests
    {
        [Theory]
        [InlineData("x=*x")]
        [InlineData("x=***x")]
        [InlineData("x")]
        public void ParseInput_ShouldParseValidExamples(string source)
        {
            var parseResult = SJacksonGrammarExample.ParseInput(source);

            parseResult.IsSuccess.Should().BeTrue(
                $"source '{source}' should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.Productions.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void ParseInput_ShouldFailForInvalidInput()
        {
            var parseResult = SJacksonGrammarExample.ParseInput("x==x");

            parseResult.IsSuccess.Should().BeFalse();
        }
    }
}
