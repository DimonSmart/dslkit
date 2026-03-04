using System;
using System.Linq;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.ParserTests
{
    public class GrammarBuilderOptionalRuleTests
    {
        [Fact]
        public void Opt_WithSingleTerm_ShouldCreateExpectedProductions()
        {
            var grammarBuilder = new GrammarBuilder()
                .WithGrammarName("opt-single-term");

            var whereClause = grammarBuilder.NT("WhereClause");
            var whereOpt = grammarBuilder.NT("WhereOpt");

            grammarBuilder.Prod("Start").Is(whereOpt);
            grammarBuilder.Prod("WhereClause").Is("WHERE", "x", "=", "1");
            grammarBuilder.Opt(whereOpt, whereClause);

            var grammar = grammarBuilder.BuildGrammar("Start");
            var whereOptProductions = grammar.Productions
                .Where(i => i.LeftNonTerminal.Name == "WhereOpt")
                .ToList();

            whereOptProductions.Should().HaveCount(2);
            whereOptProductions.Should().Contain(i =>
                i.ProductionDefinition.Count == 1 &&
                i.ProductionDefinition[0] is EmptyTerm);
            whereOptProductions.Should().Contain(i =>
                i.ProductionDefinition.Count == 1 &&
                i.ProductionDefinition[0].Name == "WhereClause");
        }

        [Fact]
        public void Opt_WithSequence_ShouldCreateExpectedProductions()
        {
            var grammarBuilder = new GrammarBuilder()
                .WithGrammarName("opt-sequence");

            var orderByClause = grammarBuilder.NT("OrderByClause");
            var offsetFetchClause = grammarBuilder.NT("OffsetFetchClause");
            var orderByAndOffsetOpt = grammarBuilder.NT("OrderByAndOffsetOpt");

            grammarBuilder.Prod("Start").Is(orderByAndOffsetOpt);
            grammarBuilder.Prod("OrderByClause").Is("ORDER", "BY", "x");
            grammarBuilder.Prod("OffsetFetchClause").Is("OFFSET", "1", "ROWS");
            grammarBuilder.Opt(orderByAndOffsetOpt, orderByClause, offsetFetchClause);

            var grammar = grammarBuilder.BuildGrammar("Start");
            var productions = grammar.Productions
                .Where(i => i.LeftNonTerminal.Name == "OrderByAndOffsetOpt")
                .ToList();

            productions.Should().HaveCount(2);
            productions.Should().Contain(i =>
                i.ProductionDefinition.Count == 1 &&
                i.ProductionDefinition[0] is EmptyTerm);
            productions.Should().Contain(i =>
                i.ProductionDefinition.Count == 2 &&
                i.ProductionDefinition[0].Name == "OrderByClause" &&
                i.ProductionDefinition[1].Name == "OffsetFetchClause");
        }

        [Fact]
        public void Opt_WithoutTerms_ShouldThrowArgumentException()
        {
            var grammarBuilder = new GrammarBuilder()
                .WithGrammarName("opt-invalid");

            var act = () => grammarBuilder.Opt("WhereOpt");

            act.Should().Throw<ArgumentException>();
        }
    }
}
