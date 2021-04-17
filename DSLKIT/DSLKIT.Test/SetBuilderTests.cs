using DSLKIT.Parser;
using DSLKIT.Terminals;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test
{
    public class ItemSetsBuilderTests : GrammarTestsBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ItemSetsBuilderTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        // https://web.cs.dal.ca/~sjackson/lalr1.html
        [InlineData("S → N;N → V = E;N → E;E → V;V → x;V → * E", "S", "sjackson", "x = * S N E V", "0=0 1=4 2=3 3=5 4=1 5=2 6=8 7=6 8=7 9=9")]
        // https://www.cs.colostate.edu/~mstrout/CS453Spring11/Slides/19-LR-table-build.ppt.pdf
        [InlineData("S' → S e;S → ( S );S → i", "S'", "mstrout")]
        // https://www.javatpoint.com/lalr-1-parsing
        [InlineData("S' → S; S → A A;A → a A;A → b", "S'", "javatpoint")]
        // https://web.cs.dal.ca/~sjackson/lalr1.html with epsilon
        [InlineData("S → N;N → V = E;N → E;E → V;V → x;V → * E;V → ε", "S", "sjackson_with_ε")]

        public void SetBuilderTest(string grammarDefinition, string rootName, string graphFileName, string order = null, string subst = null)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName(graphFileName)
                .AddProductionsFromString(grammarDefinition)
                .BuildGrammar(rootName);
            ShowGrammar(grammar);

            var setBuilder = new ItemSetsBuilder(grammar);
            setBuilder.StepEvent += SetBuilder_StepEvent;
            var sets = setBuilder.Build().ToList();
            var translationTable = TranslationTableBuilder.Build(sets);

            var sb = new StringBuilder();
            foreach (var set in sets)
            {
                sb.AppendLine(set.ToString());
            }

            _testOutputHelper.WriteLine(sb.ToString());
            File.WriteAllText($"{graphFileName}.txt", sb.ToString());
            File.WriteAllText($"{graphFileName}.dot", Sets2Dot.Transform(sets, subst));
            File.WriteAllText($"{graphFileName}_Table.txt", TranslationTable2Text.Transform(translationTable, order, subst));
        }

        private static void SetBuilder_StepEvent(object sender, IEnumerable<RuleSet> sets, string grammarName)
        {
        }
    }
}