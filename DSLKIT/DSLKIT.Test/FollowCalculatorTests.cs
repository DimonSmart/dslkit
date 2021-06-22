using System.Linq;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test
{
    public class FollowCalculatorTests : GrammarTestsBase
    {
        public FollowCalculatorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory]
        // http://user.it.uu.se/~kostis/Teaching/KT1-12/Slides/lecture06.pdf
        [InlineData(
            "kostis", "E",
            "E → T X; T → ( E ); T → int Y; X → + E; X → ε; Y → * T; Y → ε",
            "X → $ ); E → ) $; T → + ) $; Y → + ) $;")]

        // https://www.jambe.co.nz/UNI/FirstAndFollowSets.html
        [InlineData(
            "jambe", "E",
            "E → T E'; E' → + T E'; E' → ε; T → F T';T' → * F T'; T' → ε; F → ( E ); F → id",
            "E → $ ); E' → $ ); T → + $ ); T' → + $ ); F → * + $ )")]
        [InlineData("sjackson_with_ε", "S", "N → V = E;S → N;N → E;E → V;V → x;V → * E;V → ε",
            "E → $ =; N → $; V → $; S → $")]
        public void FollowSetCreation_Test(string grammarName, string rootProductionName, string grammarDefinition,
            string expectedFollows)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName(grammarName)
                .AddProductionsFromString(grammarDefinition)
                .BuildGrammar(rootProductionName);
            ShowGrammar(grammar);

            var allGrammarTerminals = grammar.Terminals.ToDictionary(i => i.Name, i => i);
            var follow = grammar.Follows.ToDictionary(i => i.Key.NonTerminal.Name, i => i.Value.ToList());
            grammar.Follows.Keys.Should().BeEquivalentTo(grammar.NonTerminals);
            follow.Should().BeEquivalentTo(GetSet(allGrammarTerminals, expectedFollows));
        }
    }
}