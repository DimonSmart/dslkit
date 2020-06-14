using System.Diagnostics;
using System.Linq;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DSLKIT.Terminals.KeywordTerminal;

namespace DSLKIT.Test
{
    [TestClass]
    public class GrammarTests
    {
        [TestMethod]
        public void GrammarExpressionConstruction()
        {
            var grammar = GetTestGrammar();
            Debug.WriteLine(grammar.ToString());
        }

        private Grammar GetTestGrammar()
        {
            return new GrammarBuilder()
                .WithName("Test grammar")
                .AddTerminal(new KeywordTerminal("="))
                .AddTerminal(new KeywordTerminal("TEST"))
                .AddTerminal(new KeywordTerminal("("))
                .AddTerminal(new IntegerTerminal())
                .AddTerminal(new StringTerminal())
                .AddTerminal(new KeywordTerminal(")"))
                .Build();
        }
    }
}