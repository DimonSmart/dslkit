using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using DSLKIT.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test.ParserTests
{
    public class GrammarBuilderTests : GrammarTestsBase
    {
        public GrammarBuilderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void GrammarA_Create_Test()
        {
            ShowGrammar(GetGrammarA());
        }

        [Fact]
        public void GrammarB_Create_Test()
        {
            ShowGrammar(GetGrammarB());
        }


        [Fact]
        public void AugmentedGrammar_NameConflict_Test()
        {
            // Test when S' name conflicts
            var grammar = new GrammarBuilder()
                .WithGrammarName("Grammar with S' conflict")
                .AddProductionFromString("S → x")
                .AddProductionFromString("S' → y") // This creates a conflict
                .WithAugmentedGrammar(true)
                .BuildGrammar();

            Assert.Equal("S''", grammar.Root.Name);

            ShowGrammar(grammar);
        }

        private static Grammar GetGrammarA()
        {
            return new GrammarBuilder()
                .WithGrammarName("Test grammar")
                .AddTerminal(new IntegerTerminal())
                .AddTerminal(new StringTerminal())
                .AddProduction("Root")
                .AddProductionDefinition("=", "TEST", "(", "Value".AsNonTerminal(), ")")
                .AddProduction("Value")
                .AddProductionDefinition(Constants.Integer)
                .AddProduction("Value")
                .AddProductionDefinition(Constants.String)
                .BuildGrammar();
        }

        private static Grammar GetGrammarB()
        {
            return new GrammarBuilder()
                .WithGrammarName("Test grammar")
                .AddProduction("Multiplication")
                .AddProductionDefinition("MUL", "(", Constants.Integer, ",", Constants.Integer, ")")
                .AddProduction("Division")
                .AddProductionDefinition("DIV", "(", Constants.Integer, ",", Constants.Integer, ")")
                .BuildGrammar();
        }
    }
}