using System.Collections.Generic;
using DSLKIT.Parser.ExtendedGrammar;

namespace DSLKIT.Test.Transformers
{
    public static class ExtendedGrammar2Text
    {
        public static string Transform(IEnumerable<ExProduction> exProductions)
        {
            return string.Join(System.Environment.NewLine, exProductions);
        }
    }
}