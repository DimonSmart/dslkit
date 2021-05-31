﻿using DSLKIT.Parser;
using DSLKIT.Terminals;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSLKIT.Test.Transformers;
using DSLKIT.Test.Utils;
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
        public void SetBuilderTest(string grammarDefinition, string rootName, string grammarFileName, string order = null, string subst = null)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName(grammarFileName)
                .AddProductionsFromString(grammarDefinition)
                .BuildGrammar(rootName);
            ShowGrammar(grammar);

            var setBuilder = new ItemSetsBuilder(grammar);
            setBuilder.StepEvent += SetBuilder_StepEvent;
            var ruleSets = setBuilder.Build().ToList();
            var substDictionary = NumberingUtils.CreateSubstFromString(subst);
            foreach (var set in ruleSets)
            {
                set.SetNumber = NumberingUtils.GetSubst(substDictionary, set.SetNumber);
            }

            var translationTable = TranslationTableBuilder.Build(ruleSets);
            var extendedGrammar = ExtendedGrammarBuilder.Build(translationTable).ToList();


            var actionAndGotoTable = new ActionAndGotoTableBuilder(grammar, ruleSets).ActionAndGotoTable;
            File.WriteAllText($"{grammarFileName}_RuleSets.txt", RuleSets2Text.Transform(ruleSets));
            File.WriteAllText($"{grammarFileName}_RuleSetsInGraphvizFormat.dot", RuleSets2GraphVizDotFormat.Transform(ruleSets));
            File.WriteAllText($"{grammarFileName}_TranslationTable.txt", TranslationTable2Text.Transform(translationTable, order));
            File.WriteAllText($"{grammarFileName}_ExtendedGrammar.txt", ExtendedGrammar2Text.Transform(extendedGrammar));
            File.WriteAllText($"{grammarFileName}_ActionAndGotoTable.txt", ActionAndGotoTable2Text.Transform(actionAndGotoTable));
        }

        private static void SetBuilder_StepEvent(object sender, IEnumerable<RuleSet> sets, string grammarName)
        {
        }
    }
}