using System.Collections.Generic;
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

        [InlineData("sjackson", "S", "S → N;N → V = E;N → E;E → V;V → x;V → * E;",
            "E → $ =; N → $; V → $; S → $")]

        [InlineData("slystudy", "S1", "S1 → S; S → A C d; S → C b a b; S → B a; S → d; A → c; A → C B; B → S d; B → ε; C → e; C → ε;",
            "E → $")]
        public void FollowSetCreation_Test(string grammarName, string rootProductionName, string grammarDefinition,
            string expectedFollows)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName(grammarName)
                .AddProductionsFromString(grammarDefinition)
                .WithOnFollowsCreated(follows => {
                    var fext = follows.Select(i => new KeyValuePair<string, string>(
                        i.Key.Term.Name,
                        string.Join(",", i.Value.Select(j => j.Name).OrderBy(j => j))
                        )).ToList().Distinct();

                    var keys = fext.Select(i => i.Key).Distinct();
                    foreach (var key in keys)
                    {
                        fext.Where(i => i.Key == key).Distinct().Should().HaveCount(1);
                    };
                })
                .BuildGrammar(rootProductionName);
            ShowGrammar(grammar);

        //    var allGrammarTerminals = grammar.Terminals.ToDictionary(i => i.Name, i => i);
        //    var follow = grammar.Follows.ToDictionary(i => i.Key.NonTerminal.Name, i => i.Value.ToList());
        //    grammar.Follows.Keys.Should().BeEquivalentTo(grammar.NonTerminals);
        //    follow.Should().BeEquivalentTo(GetSet(allGrammarTerminals, expectedFollows));
        }
    }
}