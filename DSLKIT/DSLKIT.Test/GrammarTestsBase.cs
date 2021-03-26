using DSLKIT.Parser;
using System;

namespace DSLKIT.Test
{
    public class GrammarTestsBase
    {
        protected static void ShowGrammar(IGrammar grammar)
        {
            Console.WriteLine(GrammarVisualizer.DumpGrammar(grammar));
        }
    }
}