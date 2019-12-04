using System.Diagnostics;
using System.Linq;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DSLKIT.Test
{
    [TestClass]
    public class GrammarTests
    {
        [TestMethod]
        public void GrammarExpressionConstruction()
        {
            var grammar = new TestGrammar();
            Debug.WriteLine(grammar.Root.Rule.Data.ToString());
        }


        [TestMethod]
        public void GrammarNonTerminalCollecting()
        {
            var grammar = new TestGrammar();
            Debug.WriteLine(grammar.Root.Rule.Data.ToString());
            var nonTerminals = GrammarDataBuilder.GetAllNonTerminals(grammar.Root).ToList();
            for (var i = 0; i < nonTerminals.Count; i++)
            {
                var nonTerminal = nonTerminals[i];
                Debug.WriteLine("{0:d3}: {1}", i, nonTerminal.Name);
            }
        }

        public class TestGrammar : Grammar
        {
            public TestGrammar()
            {
                var intTerm = new IntegerTerminal();
                var stringTerm = new StringTerminal(@"'");

                Root = ToTerm("=") + ToTerm("TEST") +
                       ToTerm("(") + (intTerm | stringTerm) +
                       ToTerm(")");
            }
        }
    }
}