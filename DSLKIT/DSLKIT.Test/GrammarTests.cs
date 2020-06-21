using System;
using System.Collections.Generic;
using System.Linq;
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
            var grammar = GetGrammarFirstsAndFollowSetSample();
            ShowGrammar(grammar);

            var nt = grammar.NonTerminals.ToDictionary(i => i.Name, i => i);
            var firsts = grammar.Firsts.ToDictionary(i => i.Key.Name, i => i.Value.ToList());
            var t = grammar.Terminals.ToDictionary(i => i.Name, i => i);

            CollectionAssert.AreEquivalent(grammar.Firsts.Keys.ToList(), grammar.NonTerminals.ToList());
            CollectionAssert.AreEqual(firsts["E"], new List<ITerminal> {t["("], Identifier});
            CollectionAssert.AreEqual(firsts["E'"], new List<ITerminal> {t["+"], Empty});
            CollectionAssert.AreEqual(firsts["T"], new List<ITerminal> {t["("], Identifier});
            CollectionAssert.AreEqual(firsts["T'"], new List<ITerminal> {t["*"], Empty});
            CollectionAssert.AreEqual(firsts["F"], new List<ITerminal> {t["("], Identifier});
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

        /// <summary>
        /// Grammar sample source: https://www.jambe.co.nz/UNI/FirstAndFollowSets.html
        /// </summary>
        /// <returns></returns>
        private static Grammar GetGrammarFirstsAndFollowSetSample()
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