using DSLKIT.Parser;
using DSLKIT.Terminals;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;
using static DSLKIT.Test.Constants;

namespace DSLKIT.Test
{
    public class FirstsFollowSetsCalculationTests : GrammarTestsBase
    {
        [Theory(Skip = "Not finished")]
        // http://user.it.uu.se/~kostis/Teaching/KT1-12/Slides/lecture06.pdf
        [InlineData(
         "E → T X; T → ( E ); T → int Y; X → + E; X → ε; Y → * T; Y → ε",
         "T → int (; E → int (; X → + ε; Y → * ε")]

        // https://www.jambe.co.nz/UNI/FirstAndFollowSets.html
        [InlineData(
         "E → T E'; E'→ + T E'; E'→ ε; T → F T';T'→ * F T'; T'→ ε; F → ( E ); F → id",
         "E → ( id; E' → + ε; T → ( id; T' → * ε; F → ( id")]
        public void FirstsSetCreation(string grammarDefinition, string expectedFirsts)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("Firsts & Follow test grammar")
                .AddProductionsFromString(grammarDefinition)
                .BuildGrammar("E");
            ShowGrammar(grammar);

            var firsts = new FirstsCalculator(grammar.Productions).Calculate()
                .ToDictionary(i => i.Key.Name, i => i.Value.ToList());
            var terminals = grammar.Terminals.ToDictionary(i => i.Name, i => i);

            grammar.Firsts.Keys.Should().BeEquivalentTo(grammar.NonTerminals);
            firsts.Should().BeEquivalentTo(GetSet(terminals, expectedFirsts));
        }

        [Theory(Skip = "Not finished")]
        // http://user.it.uu.se/~kostis/Teaching/KT1-12/Slides/lecture06.pdf
        [InlineData(
            "E → T X; T → ( E ); T → int Y; X → + E; X → ε; Y → * T; Y → ε",
            "X → $ ); E → ) $; T → + ) $; Y → + ) $;")]

        // https://www.jambe.co.nz/UNI/FirstAndFollowSets.html
        [InlineData(
            "E → T E'; E'→ + T E'; E'→ ε; T → F T';T'→ * F T'; T'→ ε; F → ( E ); F → id",
            "E → $ ); E' → $ ); T → + $ ); T' → + $ ); F → * + $ )")]
        public void FollowSetCreation_Test(string grammarDefinition, string expectedFollows)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("Firsts & Follow test grammar")
                .AddProductionsFromString(grammarDefinition)
                .BuildGrammar("E");
            ShowGrammar(grammar);

            var follow = new FollowCalculator(grammar).Calculate()
                .ToDictionary(i => i.Key.Name, i => i.Value.ToList());
            var terminals = grammar.Terminals.ToDictionary(i => i.Name, i => i);

            grammar.Firsts.Keys.Should().BeEquivalentTo(grammar.NonTerminals);
            var exfollow = GetSet(terminals, expectedFollows);
            follow.Should().BeEquivalentTo(exfollow);
        }

        private Dictionary<string, List<ITerm>> GetSet(Dictionary<string, ITerminal> terminals, string setLines, string[] delimiter = null)
        {
            if (delimiter == null)
            {
                delimiter = new[] { Environment.NewLine, ";" };
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

        private static KeyValuePair<string, List<ITerm>> GetSetRecord(Dictionary<string, ITerminal> terminals, string setDefinition)
        {
            var pair = setDefinition.Split(new[] { "→", "->" }, StringSplitOptions.RemoveEmptyEntries);
            if (pair.Length != 2)
            {
                throw new ArgumentException($"{setDefinition} should be in form A→zxcA with → as delimiter");
            }
            var left = pair[0].Trim();
            var right = new List<ITerm>();
            foreach (var item in pair[1].Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (item == "ε")
                {
                    right.Add(EmptyTerm.Empty);
                    continue;
                }

                if (item == "$")
                {
                    right.Add(EOF);
                    continue;
                }

                right.Add(terminals[item]);
            }
            return new KeyValuePair<string, List<ITerm>>(left, right);
        }
    }
}