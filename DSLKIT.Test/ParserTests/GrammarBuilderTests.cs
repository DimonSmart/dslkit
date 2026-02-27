using System.Linq;
using DSLKIT.Lexer;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using DSLKIT.Test.Common;
using DSLKIT.Tokens;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test.ParserTests
{
    public class GrammarBuilderTests : GrammarTestsBase
    {
        public GrammarBuilderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void GrammarA_Create_Test()
        {
            ShowGrammar(GetGrammarA());
        }

        [Fact]
        public void GrammarB_Create_Test()
        {
            ShowGrammar(GetGrammarB());
        }

        [Fact]
        public void SameNameTerminals_WithDifferentDictionaryKeys_AreStoredSeparately()
        {
            const string dictionaryKeyA = "SameName[A]";
            const string dictionaryKeyB = "SameName[B]";

            var first = new SameNameTestTerminal(dictionaryKeyA);
            var second = new SameNameTestTerminal(dictionaryKeyB);

            var grammar = new GrammarBuilder()
                .WithGrammarName("same-name-keys")
                .AddProduction("Root")
                .AddProductionDefinition(first)
                .AddProduction("Root")
                .AddProductionDefinition(second)
                .BuildGrammar();

            var sameNameTerminals = grammar.Terminals
                .Where(t => t.Name == "SameName")
                .ToList();

            Assert.Equal(2, sameNameTerminals.Count);
            Assert.Contains(sameNameTerminals, t => t.DictionaryKey == dictionaryKeyA);
            Assert.Contains(sameNameTerminals, t => t.DictionaryKey == dictionaryKeyB);
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
                .AddProductionDefinition(Constants.Integer)
                .AddProduction("Value")
                .AddProductionDefinition(Constants.String)
                .BuildGrammar();
        }

        private static Grammar GetGrammarB()
        {
            return new GrammarBuilder()
                .WithGrammarName("Test grammar")
                .AddProduction("Multiplication")
                .AddProductionDefinition("MUL", "(", Constants.Integer, ",", Constants.Integer, ")")
                .AddProduction("Division")
                .AddProductionDefinition("DIV", "(", Constants.Integer, ",", Constants.Integer, ")")
                .BuildGrammar();
        }

        private sealed class SameNameTestTerminal(string dictionaryKey) : ITerminal
        {
            public string Name => "SameName";
            public string DictionaryKey => dictionaryKey;
            public TermFlags Flags => TermFlags.None;
            public TerminalPriority Priority => TerminalPriority.Low;
            public bool CanStartWith(char c) => false;

            public bool TryMatch(ISourceStream source, out IToken token)
            {
                token = null;
                return false;
            }
        }
    }
}
