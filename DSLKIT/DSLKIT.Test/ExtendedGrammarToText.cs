using DSLKIT.Parser;
using System.Collections.Generic;

namespace DSLKIT.Test
{
    public static class ExtendedGrammarToText
    {
        public static string Transform(IEnumerable<ExtendedGrammarProduction> extendedGrammar)
        {
            return string.Join(System.Environment.NewLine, extendedGrammar);
        }
    }
}