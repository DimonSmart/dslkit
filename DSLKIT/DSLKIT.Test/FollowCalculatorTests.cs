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
            "E → Eof;E → );E → ) Eof;T → +;T → +;T → +;T → +;X → ) Eof;Y → +")]

        // https://www.jambe.co.nz/UNI/FirstAndFollowSets.html
        [InlineData(
            "jambe", "E",
            "E → T E'; E' → + T E'; E' → ε; T → F T';T' → * F T'; T' → ε; F → ( E ); F → id",
            "E → Eof;E → );E' → ) Eof;E' → ) Eof;F → *;F → *;F → *;F → *;T → +;T → +;T → +;T' → +;T' → +")]
        [InlineData("sjackson", "S", "S → N;N → V = E;N → E;E → V;V → x;V → * E;",
            "E → Eof;E → = Eof;E → Eof;N → Eof;S → Eof;V → = Eof;V → = Eof;V → Eof")]
        [InlineData("slystudy", "S1",
            "S1 → S; S → A C d; S → C b a b; S → B a; S → d; A → c; A → C B; B → S d; B → ε; C → e; C → ε;",
            "A → e;A → e;B → a;B → a e;C → a b c d e;C → d;C → a b c d e;S → d Eof;S → d;S1 → Eof")]
        public void FollowSetCreation_Test(string grammarName, string rootProductionName, string grammarDefinition,
            string expectedFollows)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName(grammarName)
                .AddProductionsFromString(grammarDefinition)
                .WithOnFollowsCreated(follows =>
                {
                    var fext = follows
                        .OrderBy(i => i.Key.NonTerminal.Name)
                        .ThenBy(i => i.Key.From.SetNumber)
                        .Select(i =>
                            new KeyValuePair<string, string>(
                                i.Key.Term.Name,
                                string.Join(" ", i.Value.Select(j => j.Name).OrderBy(j => j))
                            )).ToList();

                    var res = string
                        .Join(";", fext.OrderBy(i => i.Key).Select(i => $"{i.Key} → {i.Value}"));
                    expectedFollows.Should().Equals(res);
                })
                .BuildGrammar(rootProductionName);
            ShowGrammar(grammar);
        }
    }
}