using DSLKIT.Terminals;
using DSLKIT.Test.Common;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test.ParserTests
{
    public class AugmentedGrammarTests : GrammarTestsBase
    {
        public AugmentedGrammarTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory]
        [InlineData(true, "S'")]
        [InlineData(false, "S")]
        public void Grammar_AugmentationFlag_ProducesExpectedRootSymbol(bool augmented, string expectedRoot)
        {
            // Arrange & Act
            var grammar = new GrammarBuilder()
                .WithGrammarName("flag_test")
                .WithAugmentedGrammar(augmented)
                .AddProductionFromString("S → x")
                .BuildGrammar();

            // Assert
            grammar.Root.Name.Should().Be(expectedRoot);
        }
    }
}
