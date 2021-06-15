using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test
{
    public class FirstsFollowSetsCalculationTests : GrammarTestsBase
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
        [InlineData("sjackson_with_ε", "S", "N → V = E;S → N;N → E;E → V;V → x;V → * E;V → ε",
            "E → $ =; N → $; V → $; S → $")]
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
            var follow = grammar.Follow.ToDictionary(i => i.Key.Name, i => i.Value.ToList());
            grammar.Follow.Keys.Should().BeEquivalentTo(grammar.NonTerminals);
            follow.Should().BeEquivalentTo(GetSet(allGrammarTerminals, expectedFollows));
        }

        private static Dictionary<string, List<ITerm>> GetSet(Dictionary<string, ITerminal> terminals, string setLines,
            string[] delimiter = null)
        {
            if (delimiter == null)
            {
                delimiter = new[] {Environment.NewLine, ";"};
            }

            var result = new Dictionary<string, List<ITerm>>();
            var lines = setLines.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var record = GetSetRecord(terminals, line);
                result.Add(record.Key, record.Value);
            }

            return result;
        }

        private static KeyValuePair<string, List<ITerm>> GetSetRecord(Dictionary<string, ITerminal> terminals,
            string setDefinition)
        {
            var pair = setDefinition.Split(new[] {"→", "->"}, StringSplitOptions.RemoveEmptyEntries);
            if (pair.Length != 2)
            {
                throw new ArgumentException($"{setDefinition} should be in form A→zxcA with → as delimiter");
            }

            var left = pair[0].Trim();
            var right = new List<ITerm>();
            foreach (var item in pair[1].Trim().Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries))
            {
                if (item == "ε")
                {
                    right.Add(EmptyTerm.Empty);
                    continue;
                }

                if (item == "$")
                {
                    right.Add(EofTerminal.Instance);
                    continue;
                }

                right.Add(terminals[item]);
            }

            return new KeyValuePair<string, List<ITerm>>(left, right);
        }
    }
}