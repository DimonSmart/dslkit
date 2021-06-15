using System.Linq;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test
{
    public class FirstsCalculatorTests : GrammarTestsBase
    {
        [Theory]
        // http://user.it.uu.se/~kostis/Teaching/KT1-12/Slides/lecture06.pdf
        [InlineData("kostis", "E",
            "E → T X; T → ( E ); T → int Y; X → + E; X → ε; Y → * T; Y → ε",
            "T → int (; E → int (; X → + ε; Y → * ε")]

        // https://www.jambe.co.nz/UNI/FirstAndFollowSets.html
        [InlineData("jambe", "E",
            "E → T E'; E' → + T E'; E' → ε; T → F T';T' → * F T'; T' → ε; F → ( E ); F → id",
            "E → ( id; E' → + ε; T → ( id; T' → * ε; F → ( id")]
        [InlineData("sjackson_with", "S", "S → N;N → V = E;N → E;E → V;V → x;V → * E;",
            "E → x *; N → x *; V → x *; S → x *")]
        public void FirstsSetCreation(string grammarName, string rootProductionName, string grammarDefinition,
            string expectedFirsts)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName(grammarName)
                .AddProductionsFromString(grammarDefinition)
                .BuildGrammar(rootProductionName);
            ShowGrammar(grammar);

            var allGrammarTerminals = grammar.Terminals.ToDictionary(i => i.Name, i => i);
            var firsts = grammar.Firsts.ToDictionary(i => i.Key.Name, i => i.Value.ToList());

            grammar.Firsts.Keys.Should().BeEquivalentTo(grammar.NonTerminals);
            firsts.Should().BeEquivalentTo(GetSet(allGrammarTerminals, expectedFirsts));
        }
    }
}