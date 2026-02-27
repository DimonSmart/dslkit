using System.Collections.Generic;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using Xunit;

namespace DSLKIT.Test.ParserTests
{
    public class ProductionTests
    {
        [Fact]
        public void Equals_UsesDictionaryKey()
        {
            var root = new NonTerminal("Root");
            var productionA = new Production(root, new ITerm[] { new FakeTerm("String[double]") });
            var productionB = new Production(root, new ITerm[] { new FakeTerm("String[brackets]") });

            Assert.NotEqual(productionA, productionB);
            Assert.Equal(2, new HashSet<Production> { productionA, productionB }.Count);
        }

        private sealed class FakeTerm : ITerm
        {
            public FakeTerm(string dictionaryKey)
            {
                DictionaryKey = dictionaryKey;
            }

            public string Name => "String";
            public string DictionaryKey { get; }
        }
    }
}
