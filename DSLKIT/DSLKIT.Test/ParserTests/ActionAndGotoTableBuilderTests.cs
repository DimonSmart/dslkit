using DSLKIT.Parser;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using DSLKIT.Test.Common;
using FluentAssertions;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test.ParserTests
{
    public class ActionAndGotoTableBuilderTests : GrammarTestsBase
    {
        public ActionAndGotoTableBuilderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void ActionTable_SimpleGrammar_ContainsExpectedActions()
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("simple")
                .AddProductionFromString("S → x")
                .BuildGrammar();

            grammar.ActionAndGotoTable.Should().NotBeNull();
            grammar.ActionAndGotoTable.ActionTable.Should().NotBeEmpty();
            grammar.ActionAndGotoTable.ActionTable.Values.OfType<AcceptAction>().Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void ActionTable_GrammarWithOperators_ContainsShiftAndReduceActions()
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("operators")
                .AddProductionFromString("S → E")
                .AddProductionFromString("E → E + E")
                .AddProductionFromString("E → x")
                .BuildGrammar();

            var actionTable = grammar.ActionAndGotoTable.ActionTable;
            actionTable.Values.OfType<ShiftAction>().Should().HaveCountGreaterThan(0);
            actionTable.Values.OfType<ReduceAction>().Should().HaveCountGreaterThan(0);
            actionTable.Values.OfType<AcceptAction>().Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void GotoTable_NonTerminals_ContainsTransitions()
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("nonterminals")
                .AddProductionFromString("S → E")
                .AddProductionFromString("E → x")
                .BuildGrammar();

            grammar.ActionAndGotoTable.GotoTable.Should().NotBeEmpty();
        }

        [Theory]
        // Simple grammar has at least accept action
        [InlineData("S → x", 1)]
        // Grammar with non-terminal should have more actions
        [InlineData("S → E; E → x", 2)]
        // Recursive grammar should have even more actions
        [InlineData("S → E; E → E + x; E → x", 3)]
        public void ActionTable_DifferentGrammars_HasMinimumActionCount(string grammarDefinition, int minActionCount)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("test")
                .AddProductionsFromString(grammarDefinition)
                .BuildGrammar();

            grammar.ActionAndGotoTable.ActionTable.Should().HaveCountGreaterOrEqualTo(minActionCount);
        }

        [Fact]
        public void ReductionSteps_CalledDuringBuild()
        {
            var step0Called = false;
            var step1Called = false;

            var grammar = new GrammarBuilder()
                .WithGrammarName("callbacks")
                .AddProductionFromString("S → E")
                .AddProductionFromString("E → x")
                .WithOnReductionStep0(_ => step0Called = true)
                .WithOnReductionStep1(_ => step1Called = true)
                .BuildGrammar();

            step0Called.Should().BeTrue();
            step1Called.Should().BeTrue();
        }

        [Fact]
        public void ConflictDetection_ShiftReduceConflict_DoesNotThrow()
        {
            // Grammar that might produce shift/reduce conflict
            var grammar = new GrammarBuilder()
                .WithGrammarName("conflict")
                .AddProductionFromString("S → E")
                .AddProductionFromString("E → E + E")
                .AddProductionFromString("E → E * E")
                .AddProductionFromString("E → x")
                .BuildGrammar();

            // Should not throw, conflicts are handled gracefully
            grammar.ActionAndGotoTable.Should().NotBeNull();
        }

        [Fact]
        public void AcceptAction_StartingProduction_PlacedCorrectly()
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("accept")
                .AddProductionFromString("S → x")
                .BuildGrammar();

            var acceptActions = grammar.ActionAndGotoTable.ActionTable
                .Where(kvp => kvp.Value is AcceptAction)
                .ToList();

            acceptActions.Should().HaveCount(1);
            acceptActions.Single().Key.Key.Should().BeOfType<EofTerminal>();
        }

        [Fact]
        public void ReduceAction_CorrectProductionLength()
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("reduce_length")
                .AddProductionFromString("S → E")
                .AddProductionFromString("E → x + y")
                .BuildGrammar();

            var reduceActions = grammar.ActionAndGotoTable.ActionTable.Values
                .OfType<ReduceAction>()
                .ToList();

            reduceActions.Should().NotBeEmpty();
            // Production "E → x + y" should have PopLength 3
            reduceActions.Should().Contain(r => r.PopLength == 3);
        }

        [Fact]
        public void ShiftAction_PointsToCorrectState()
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("shift_state")
                .AddProductionFromString("S → x y")
                .BuildGrammar();

            var shiftActions = grammar.ActionAndGotoTable.ActionTable
                .Where(kvp => kvp.Value is ShiftAction)
                .ToList();

            shiftActions.Should().NotBeEmpty();
            foreach (var kvp in shiftActions)
            {
                ((ShiftAction)kvp.Value).RuleSet.Should().NotBeNull();
            }
        }

        [Theory]
        [InlineData("S → x", "x")]
        [InlineData("S → a b c", "a")]
        [InlineData("S → + - *", "+")]
        public void FirstTerminal_CreatesShiftAction(string production, string expectedTerminal)
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("first_terminal")
                .AddProductionFromString(production)
                .BuildGrammar();

            var actionTable = grammar.ActionAndGotoTable.ActionTable;
            var terminalActions = actionTable
                .Where(kvp => kvp.Key.Key.Name == expectedTerminal && kvp.Value is ShiftAction)
                .ToList();

            terminalActions.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void EmptyProduction_HandledCorrectly()
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("empty")
                .AddProductionFromString("S → A")
                .AddProductionFromString("A → ε")
                .BuildGrammar();

            // Should not throw and should create valid tables
            grammar.ActionAndGotoTable.Should().NotBeNull();
            grammar.ActionAndGotoTable.ActionTable.Should().NotBeEmpty();
        }

        [Fact]
        public void AddProductionFromString_WithAngleBrackets_Works()
        {
            var grammar = new GrammarBuilder()
                .WithGrammarName("brackets")
                .AddProductionFromString("<S> → <A>")
                .AddProductionFromString("<A> → a")
                .BuildGrammar();

            grammar.Productions.Should().Contain(p => p.LeftNonTerminal.Name == "S");
            grammar.Productions.Should().Contain(p => p.LeftNonTerminal.Name == "A");
        }
    }
}
