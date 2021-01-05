using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using Xunit;
using static DSLKIT.Test.Constants;

namespace DSLKIT.Test
{
    public class GrammarBuilderTests : GrammarTestsBase
    {
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
                .AddProductionDefinition(Integer)
                .AddProduction("Value")
                .AddProductionDefinition(Constants.String)
                .BuildGrammar();
        }

        private static Grammar GetGrammarB()
        {
            return new GrammarBuilder()
                .WithGrammarName("Test grammar")
                .AddProduction("Multiplication")
                .AddProductionDefinition("MUL", "(", Integer, ",", Integer, ")")
                .AddProduction("Division")
                .AddProductionDefinition("DIV", "(", Integer, ",", Integer, ")")
                .BuildGrammar();
        }
    }
}