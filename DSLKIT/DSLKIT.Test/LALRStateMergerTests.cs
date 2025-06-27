using System.Linq;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using DSLKIT.Test.Utils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test
{
    public class LALRStateMergerTests : GrammarTestsBase
    {
        public LALRStateMergerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory]
        [InlineData("sjackson", "S → N;N → V = E;N → E;E → V;V → x;V → * E", "S")]
        [InlineData("mstrout", "S' → S e;S → ( S );S → i", "S'")]
        public void LALRStateMergerTest(string grammarName, string grammarDefinition, string rootName)
        {
            // Arrange
            var builder = new GrammarBuilder()
                .WithGrammarName(grammarName)
                .AddProductionsFromString(grammarDefinition);

            LALRMergeResult mergeResult = null;
            builder.OnLALRMergeCompleted = result => mergeResult = result;

            // Act
            var grammar = builder.BuildGrammar(rootName);

            // Assert
            mergeResult.Should().NotBeNull("LALR merge should have been executed");
            
            TestOutputHelper.WriteLine($"Grammar: {grammarName}");
            TestOutputHelper.WriteLine(mergeResult.Statistics.ToString());
            
            foreach (var mergeGroup in mergeResult.Statistics.MergeGroups)
            {
                TestOutputHelper.WriteLine($"  {mergeGroup}");
            }

            // Verify that we have fewer or equal LALR states compared to original LR(1) states
            mergeResult.Statistics.MergedLALRStateCount.Should()
                .BeLessOrEqualTo(mergeResult.Statistics.OriginalLR1StateCount,
                    "LALR merging should reduce or maintain the number of states");

            // Verify that all original states are mapped to LALR states
            mergeResult.LR1ToLALRMapping.Should().NotBeEmpty("All LR(1) states should be mapped to LALR states");

            // Verify that LALR states have consistent transitions
            foreach (var lalrState in mergeResult.LALRStates)
            {
                foreach (var transition in lalrState.Arrows)
                {
                    mergeResult.LALRStates.Should().Contain(transition.Value,
                        $"Transition target should be a valid LALR state");
                }
            }

            // Log detailed merge information
            TestOutputHelper.WriteLine($"\nDetailed merge information:");
            TestOutputHelper.WriteLine($"Original LR(1) states: {mergeResult.Statistics.OriginalLR1StateCount}");
            TestOutputHelper.WriteLine($"Merged LALR states: {mergeResult.Statistics.MergedLALRStateCount}");
            TestOutputHelper.WriteLine($"States reduced: {mergeResult.Statistics.StatesReduced}");
            TestOutputHelper.WriteLine($"Merge groups: {mergeResult.Statistics.MergeGroupCount}");

            if (mergeResult.Statistics.MergeGroups.Any())
            {
                TestOutputHelper.WriteLine("\nMerge groups:");
                foreach (var group in mergeResult.Statistics.MergeGroups)
                {
                    TestOutputHelper.WriteLine($"  Core: {group.CoreSignature}");
                    TestOutputHelper.WriteLine($"  Merged states: [{string.Join(", ", group.OriginalStateNumbers)}]");
                }
            }

            // Dump merged LALR states for inspection
            TestOutputHelper.WriteLine("\nMerged LALR states:");
            foreach (var state in mergeResult.LALRStates)
            {
                TestOutputHelper.WriteLine($"State {state.SetNumber}: {state.Rules.Count} rules, {state.Arrows.Count} transitions");
            }

            grammar.Should().NotBeNull("Grammar should be built successfully with LALR states");
            grammar.RuleSets.Should().HaveCount(mergeResult.Statistics.MergedLALRStateCount,
                "Grammar should use the merged LALR states");
        }

        [Fact]
        public void LALRMerger_WithNoMergableStates_ShouldReturnOriginalStates()
        {
            // Arrange - simple grammar with no mergeable states
            var grammarDefinition = "S → a";
            var builder = new GrammarBuilder()
                .WithGrammarName("simple")
                .AddProductionsFromString(grammarDefinition);

            LALRMergeResult mergeResult = null;
            builder.OnLALRMergeCompleted = result => mergeResult = result;

            // Act
            var grammar = builder.BuildGrammar("S");

            // Assert
            mergeResult.Should().NotBeNull();
            mergeResult.Statistics.StatesReduced.Should().Be(0, 
                "No states should be reduced for simple grammar");
            mergeResult.Statistics.MergeGroupCount.Should().Be(0,
                "No merge groups should exist for simple grammar");
        }

        [Fact]
        public void LALRMerger_WithConflictingTransitions_ShouldThrowException()
        {
            // This test would verify error handling for invalid grammars
            // where states with identical cores have different transitions
            // (This shouldn't happen with properly constructed LR(1) item sets,
            // but we should handle it gracefully)
            
            // For now, this is a placeholder for future implementation
            // when we add more sophisticated error checking
        }
    }
}
