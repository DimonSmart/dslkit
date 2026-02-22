using System.Linq;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.ParserTests
{
    public class GrammarBuilderStarRuleTests
    {
        [Fact]
        public void Star_WithoutDelimiter_ShouldCreateExpectedProductions()
        {
            var grammarBuilder = new GrammarBuilder()
                .WithGrammarName("star-no-delimiter");

            var item = grammarBuilder.NT("Item");
            var list = grammarBuilder.NT("List");

            grammarBuilder.Prod("Start").Is(list);
            grammarBuilder.Prod("Item").Is("x");
            grammarBuilder.Star(list, item);

            var grammar = grammarBuilder.BuildGrammar("Start");
            var listProductions = grammar.Productions
                .Where(i => i.LeftNonTerminal.Name == "List")
                .ToList();

            listProductions.Should().HaveCount(2);
            listProductions.Should().Contain(i =>
                i.ProductionDefinition.Count == 1 &&
                i.ProductionDefinition[0] is EmptyTerm);
            listProductions.Should().Contain(i =>
                i.ProductionDefinition.Count == 2 &&
                i.ProductionDefinition[0].Name == "List" &&
                i.ProductionDefinition[1].Name == "Item");
        }

        [Fact]
        public void Star_WithDelimiter_ShouldCreateExpectedProductions()
        {
            var grammarBuilder = new GrammarBuilder()
                .WithGrammarName("star-with-delimiter");

            var item = grammarBuilder.NT("Item");
            var list = grammarBuilder.NT("List");

            grammarBuilder.Prod("Start").Is(list);
            grammarBuilder.Prod("Item").Is("x");
            grammarBuilder.Star(list, item, ",");

            var grammar = grammarBuilder.BuildGrammar("Start");
            var listProductions = grammar.Productions
                .Where(i => i.LeftNonTerminal.Name == "List")
                .ToList();

            listProductions.Should().HaveCount(3);
            listProductions.Should().Contain(i =>
                i.ProductionDefinition.Count == 1 &&
                i.ProductionDefinition[0] is EmptyTerm);
            listProductions.Should().Contain(i =>
                i.ProductionDefinition.Count == 1 &&
                i.ProductionDefinition[0].Name == "Item");
            listProductions.Should().Contain(i =>
                i.ProductionDefinition.Count == 3 &&
                i.ProductionDefinition[0].Name == "List" &&
                i.ProductionDefinition[1].Name == "," &&
                i.ProductionDefinition[2].Name == "Item");
        }
    }
}
