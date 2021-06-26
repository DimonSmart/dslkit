using System.Linq;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test
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
            "0_E_$=(,int;0_T_1=(,int;1_X_4=+,Empty;2_E_7=(,int;2_T_1=(,int;3_Y_8=*,Empty;5_E_11=(,int;5_T_1=(,int;9_T_13=(,int")]

        // https://www.jambe.co.nz/UNI/FirstAndFollowSets.html
        [InlineData("jambe", "E",
            "E → T E'; E' → + T E'; E' → ε; T → F T';T' → * F T'; T' → ε; F → ( E ); F → id",
            "0_E_$=(,id;0_F_2=(,id;0_T_1=(,id;1_E'_5=+,Empty;12_E'_15=+,Empty;13_T'_16=*,Empty;2_T'_8=*,Empty;3_E_11=(,id;3_F_2=(,id;3_T_1=(,id;6_F_2=(,id;6_T_12=(,id;9_F_13=(,id")]
        [InlineData("sjackson_with", "S", "S → N;N → V = E;N → E;E → V;V → x;V → * E;",
            "0_E_3=*,x;0_N_1=*,x;0_S_$=*,x;0_V_2=*,x;5_E_7=*,x;5_V_8=*,x;6_E_9=*,x;6_V_8=*,x")]
        public void FirstsSetCreation(string grammarName, string rootProductionName, string grammarDefinition,
            string expectedFirsts)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName(grammarName)
                .AddProductionsFromString(grammarDefinition)
                .WithOnFirstsCreated(f =>
                {
                    var firstsAsString =
                    string.Join(";",
                    f.ToDictionary(
                        i => i.Key.ToString(),
                        i => string.Join(",", i.Value.Select(j => j.Name).OrderBy(j => j))
                    )
                    .Select(d => $"{d.Key}={d.Value}")
                    .OrderBy(s => s));
                    firstsAsString.Should().BeEquivalentTo(expectedFirsts);
                })
                .BuildGrammar(rootProductionName);


        }
    }
}