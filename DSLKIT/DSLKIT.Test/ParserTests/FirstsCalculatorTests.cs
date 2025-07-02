using System.Collections.Generic;
using System.Linq;
using DSLKIT.Terminals;
using DSLKIT.Test.BaseTests;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test.ParserTests
{
    public class FirstsCalculatorTests : GrammarTestsBase
    {
        public FirstsCalculatorTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory]
        // http://user.it.uu.se/~kostis/Teaching/KT1-12/Slides/lecture06.pdf
        [InlineData("kostis", "E",
            "E → T X; T → ( E ); T → int Y; X → + E; X → ε; Y → * T; Y → ε",
            "E=(,int;T=(,int;X=+,Empty;Y=*,Empty")]
        // https://www.jambe.co.nz/UNI/FirstAndFollowSets.html
        [InlineData("jambe", "E",
            "E → T E'; E' → + T E'; E' → ε; T → F T';T' → * F T'; T' → ε; F → ( E ); F → id",
            "E=(,id;E'=+,Empty;F=(,id;T=(,id;T'=*,Empty")]
        [InlineData("sjackson", "S", "S → N;N → V = E;N → E;E → V;V → x;V → * E;",
            "E=*,x;N=*,x;S=*,x;V=*,x")]
        // https://www.youtube.com/watch?v=UXYqQ_CJsVE&list=LL&index=18
        [InlineData("gate", "S", "S → A B C;A → a;A → b;A →  ε;B → c;B → d;B → ε;C → e;C → f;C → ε;",
            "A=a,b,Empty;B=c,d,Empty;C=e,Empty,f;S=a,b,c,d,e,Empty,f")]
        // https://www.youtube.com/watch?v=UXYqQ_CJsVE&list=LL&index=18
        [InlineData("gate1", "S", "S → A B C;A → a;A → b;A →  ε;B → c;B → d;B → ε;C → e;C → f;C → ε;",
            "A=a,b,Empty;B=c,d,Empty;C=e,Empty,f;S=a,b,c,d,e,Empty,f")]
        public void FirstsSetCreation(string grammarName, string rootProductionName, string grammarDefinition,
            string expectedFirsts)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName(grammarName)
                .AddProductionsFromString(grammarDefinition)
                .WithOnFirstsCreated(f =>
                {
                    var fext = f.Select(i => new KeyValuePair<string, string>(
                        i.Key.Term.Name,
                        string.Join(",", i.Value.Select(j => j.Name).OrderBy(j => j))
                    )).Distinct().ToList();

                    var keys = fext.Select(i => i.Key).Distinct();
                    foreach (var key in keys)
                    {
                        fext.Where(i => i.Key == key).Distinct().Should().HaveCount(1);
                    }

                    var firstsAsText = string.Join(";", fext.OrderBy(s => s.Key).Select(d => $"{d.Key}={d.Value}"));
                    firstsAsText
                        .Should()
                        .BeEquivalentTo(expectedFirsts);
                })
                .BuildGrammar(rootProductionName);
        }
    }
}