using System.Collections.Generic;
using DSLKIT.Parser;

namespace DSLKIT.Test.Transformers
{
    public static class ExtendedGrammar2Text
    {
        public static string Transform(IEnumerable<ExtendedGrammarProduction> extendedGrammar)
        {
            return string.Join(System.Environment.NewLine, extendedGrammar);
        }
    }
}