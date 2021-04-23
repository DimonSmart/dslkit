using DSLKIT.Parser;
using System.Collections.Generic;

namespace DSLKIT.Test
{
    public static class ExtendedGrammarToText
    {
        public static string Transfort(IEnumerable<ExtendedGrammarProduction> extendedGrammar)
        {
            return string.Join(System.Environment.NewLine, extendedGrammar);
        }
    }
}