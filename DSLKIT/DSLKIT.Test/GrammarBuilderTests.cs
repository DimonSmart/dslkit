using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test
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