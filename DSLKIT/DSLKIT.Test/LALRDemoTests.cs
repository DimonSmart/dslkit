using DSLKIT.Test.Utils;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test
{
    public class LALRDemoTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public LALRDemoTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void DemonstrateBasicLALRMerging()
        {
            // Use the sjackson grammar from the tutorial
            var grammarName = "sjackson_demo";
            var grammarDefinition = "S → N;N → V = E;N → E;E → V;V → x;V → * E";
            var rootName = "S";

            _testOutputHelper.WriteLine("Running LALR State Merging Demonstration...");
            LALRDemonstrator.DemonstrateAndSave(grammarName, grammarDefinition, rootName);
            _testOutputHelper.WriteLine("Demo completed. Check output files for detailed results.");
        }

        [Fact]
        public void DemonstrateMstroutGrammar()
        {
            // Use the mstrout grammar which might have more merge opportunities
            var grammarName = "mstrout_demo";
            var grammarDefinition = "S' → S e;S → ( S );S → i";
            var rootName = "S'";

            _testOutputHelper.WriteLine("Running LALR State Merging Demonstration for mstrout grammar...");
            LALRDemonstrator.DemonstrateAndSave(grammarName, grammarDefinition, rootName);
            _testOutputHelper.WriteLine("Demo completed. Check output files for detailed results.");
        }

        [Fact]
        public void DemonstrateComplexGrammar()
        {
            // Use a more complex grammar that's likely to have mergeable states
            var grammarName = "complex_demo";
            var grammarDefinition = "S' → S; S → A A;A → a A;A → b";
            var rootName = "S'";

            _testOutputHelper.WriteLine("Running LALR State Merging Demonstration for complex grammar...");
            LALRDemonstrator.DemonstrateAndSave(grammarName, grammarDefinition, rootName);
            _testOutputHelper.WriteLine("Demo completed. Check output files for detailed results.");
        }
    }
}
