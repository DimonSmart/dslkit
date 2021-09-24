using System.IO;
using DSLKIT.Terminals;
using DSLKIT.Test.Transformers;
using DSLKIT.Test.Utils;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test
{
    public class ItemSetsBuilderTests : GrammarTestsBase
    {
        public ItemSetsBuilderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory]
        // https://web.cs.dal.ca/~sjackson/lalr1.html
        [InlineData("sjackson", "S → N;N → V = E;N → E;E → V;V → x;V → * E", "S",
            "0=0 1=4 2=3 3=5 4=1 5=2 6=8 7=6 8=7 9=9")]
        // https://www.cs.colostate.edu/~mstrout/CS453Spring11/Slides/19-LR-table-build.ppt.pdf
        [InlineData("mstrout", "S' → S e;S → ( S );S → i", "S'")]
        // https://www.javatpoint.com/lalr-1-parsing
        [InlineData("javatpoint", "S' → S; S → A A;A → a A;A → b", "S'")]
        // https://web.cs.dal.ca/~sjackson/lalr1.html with epsilon
        [InlineData("sjackson_with_ε", "S → N;N → V = E;N → E;E → V;V → x;V → * E;V → ε", "S")]
        public void SetBuilderTest(string grammarName, string grammarDefinition, string rootName,
            string subst = null)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName(grammarName)
                .AddProductionsFromString(grammarDefinition)
                .WithOnRuleSetCreated(ruleSets =>
                {
                    var substDictionary = NumberingUtils.CreateSubstFromString(subst);
                    foreach (var set in ruleSets)
                    {
                        set.SetNumber = NumberingUtils.GetSubst(substDictionary, set.SetNumber);
                    }

                    File.WriteAllText($"{grammarName}_RuleSets.txt",
                        RuleSets2Text.Transform(ruleSets));
                })
                .WithOnTranslationTableCreated(translationTable =>
                {
                    File.WriteAllText($"{grammarName}_TranslationTable.txt",
                        TranslationTable2Text.Transform(translationTable));
                })
                .WithOnExtendedGrammarCreated(exProductions =>
                {
                    File.WriteAllText($"{grammarName}_ExtendedGrammar.txt",
                        ExtendedGrammar2Text.Transform(exProductions));
                })
                .WithOnFirstsCreated(firsts =>
                {
                    File.WriteAllText($"{grammarName}_Firsts.txt",
                        Firsts2Text.Transform(firsts));
                })
                .WithOnReductionStep0(rule2FollowSet =>
                    {
                        File.WriteAllText($"{grammarName}_ReductionStep0.txt",
                            Rule2FollowSet2Text.Transform(rule2FollowSet));
                    }
                )
                .WithOnReductionStep1(mergedRows =>
                {
                    File.WriteAllText($"{grammarName}_ReductionStep1.txt",
                        MergedRows2Text.Transform(mergedRows));
                })
                .BuildGrammar(rootName);

            ShowGrammar(grammar);
            File.WriteAllText($"{grammarName}_Follow.txt", Follow2Text.Transform(grammar.Follows));
            File.WriteAllText($"{grammarName}_RuleSetsInGraphvizFormat.dot",
                RuleSets2GraphVizDotFormat.Transform(grammar.RuleSets));
            File.WriteAllText($"{grammarName}_ActionAndGotoTable.txt",
                ActionAndGotoTable2Text.Transform(grammar.ActionAndGotoTable));
        }
    }
}