using System.Diagnostics;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSLKIT.Test
{
    [TestClass]
    public class GrammarTests
    {
        private static readonly IntegerTerminal _integer = new IntegerTerminal();

        [TestMethod]
        public void GrammarA_Create_Test()
        {
            var grammar = GetGrammarA();
            Debug.WriteLine(grammar.ToString());
        }

        [TestMethod]
        public void GrammarB_Create_Test()
        {
            var grammar = GetGrammarB();
            Debug.WriteLine(grammar.ToString());
        }

        private static Grammar GetGrammarA()
        {
            return new GrammarBuilder()
                .WithGrammarName("Test grammar")
                .AddTerminal(new KeywordTerminal("="))
                .AddTerminal(new KeywordTerminal("TEST"))
                .AddTerminal(new KeywordTerminal("("))
                .AddTerminal(new IntegerTerminal())
                .AddTerminal(new StringTerminal())
                .AddTerminal(new KeywordTerminal(")"))
                .BuildGrammar();
        }

        private static Grammar GetGrammarB()
        {
            return new GrammarBuilder()
                .WithGrammarName("Test grammar")
                .AddProduction("Multiplication")
                    .AddProductionDefinition("MUL", "(", _integer, ",", _integer, ")")
                .AddProduction("Division")
                    .AddProductionDefinition("DIV", "(", _integer, ",", _integer, ")")
                .BuildGrammar();
        }
    }
}