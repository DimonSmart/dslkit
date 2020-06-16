using System;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using DSLKIT.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DSLKIT.Parser.Constants;

namespace DSLKIT.Test
{
    [TestClass]
    public class GrammarTests
    {
        [TestMethod]
        public void GrammarA_Create_Test()
        {
            ShowGrammar(GetGrammarA());
        }

        [TestMethod]
        public void GrammarB_Create_Test()
        {
            ShowGrammar(GetGrammarB());
        }

        [TestMethod]
        public void GrammarFirstsAndFollow_Create_Test()
        {
            ShowGrammar(GetGrammarFirstsAndFollow());
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
                .AddProductionDefinition("MUL", "(", Integer, ",", Integer, ")")
                .AddProduction("Division")
                .AddProductionDefinition("DIV", "(", Integer, ",", Integer, ")")
                .BuildGrammar();
        }

        private static Grammar GetGrammarFirstsAndFollow()
        {
            return new GrammarBuilder()
                .WithGrammarName("Firsts & Follow grammar")
                .AddProduction("E")
                .AddProductionDefinition("T".AsNonTerminal(), "E'".AsNonTerminal())
                .AddProduction("E'")
                .AddProductionDefinition("+", "T".AsNonTerminal(), "E'".AsNonTerminal())
                .AddProduction("E'")
                .AddProductionDefinition(Empty)
                .AddProduction("T")
                .AddProductionDefinition("F".AsNonTerminal(), "T'".AsNonTerminal())
                .AddProduction("T'")
                .AddProductionDefinition("*", "F".AsNonTerminal(), "T'".AsNonTerminal())
                .AddProduction("T'")
                .AddProductionDefinition(Empty)
                .AddProduction("F")
                .AddProductionDefinition("(", "E".AsNonTerminal(), ")")
                .AddProduction("F")
                .AddProductionDefinition(Identifier)
                .BuildGrammar();
        }


        private static void ShowGrammar(IGrammar grammar)
        {
            Console.WriteLine(GrammarVisualizer.DumpGrammar(grammar));
        }
    }
}