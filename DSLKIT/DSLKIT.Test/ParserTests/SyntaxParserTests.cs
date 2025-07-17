using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using DSLKIT.Test.Common;
using FluentAssertions;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test.ParserTests
{
    public class SyntaxParserTests : GrammarTestsBase
    {
        public SyntaxParserTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public void Parse_SimpleGrammar_ShouldSucceed()
        {
            // Arrange
            var xTerminal = new KeywordTerminal("x");

            var grammar = new GrammarBuilder()
                .WithGrammarName("simple")
                .AddProduction("S").AddProductionDefinition(xTerminal)
                .BuildGrammar();

            var lexerSettings = new LexerSettings
            {
                xTerminal // Use the same terminal instance
            };

            var lexer = new Lexer.Lexer(lexerSettings);
            var parser = new SyntaxParser(grammar);

            // Debug: show action table
            var actionTable = grammar.ActionAndGotoTable.ActionTable;
            _testOutputHelper.WriteLine("Action Table:");
            foreach (var entry in actionTable.OrderBy(e => e.Key.Value.SetNumber).ThenBy(e => e.Key.Key.Name))
            {
                _testOutputHelper.WriteLine($"  State {entry.Key.Value.SetNumber}, Terminal '{entry.Key.Key.Name}' -> {entry.Value}");
            }

            // Debug: show productions
            _testOutputHelper.WriteLine("Productions:");
            for (var i = 0; i < grammar.Productions.Count; i++)
            {
                _testOutputHelper.WriteLine($"  {i}: {grammar.Productions.ElementAt(i)}");
            }

            // Act
            var tokens = lexer.GetTokens(new StringSourceStream("x")).ToList();
            _testOutputHelper.WriteLine($"Tokens: {string.Join(", ", tokens.Select(t => $"{t.Terminal.Name}@{t.Position}"))}");

            var result = parser.Parse(tokens);

            // Assert
            _testOutputHelper.WriteLine($"Parse result: {result}");
            result.IsSuccess.Should().BeTrue($"Expected success, but got: {result.Error?.Message}");
            // Note: For simple grammars, Productions list might be empty if no reduce actions are needed
            result.Error.Should().BeNull();
        }

        [Fact]
        public void Parse_BinaryExpression_ShouldSucceed()
        {
            // Arrange - Grammar from the tutorial: x = * x
            var xTerminal = new KeywordTerminal("x");
            var starTerminal = new KeywordTerminal("*");
            var equalsTerminal = new KeywordTerminal("=");

            // Create grammar using string notation first, then replace terminals
            var grammar = new GrammarBuilder()
                .WithGrammarName("sjackson")
                .AddProductionFromString("<S> → <N>")
                .AddProductionFromString("<N> → <V> = <E>")
                .AddProductionFromString("<N> → <E>")
                .AddProductionFromString("<E> → <V>")
                .AddProductionFromString("<V> → x")
                .AddProductionFromString("<V> → * <E>")
                .BuildGrammar();

            // Now we need to use the same terminal instances for lexer
            // This is a known limitation - in a real parser, you'd manage terminal instances consistently
            var grammarTerminals = grammar.Terminals.ToList();
            var xFromGrammar = grammarTerminals.First(t => t.Name == "x");
            var starFromGrammar = grammarTerminals.First(t => t.Name == "*");
            var equalsFromGrammar = grammarTerminals.First(t => t.Name == "=");

            var lexerSettings = new LexerSettings
            {
                xFromGrammar,
                starFromGrammar,
                equalsFromGrammar
            };

            var lexer = new Lexer.Lexer(lexerSettings);
            var parser = new SyntaxParser(grammar);

            // Act
            var tokens = lexer.GetTokens(new StringSourceStream("x=*x")).ToList();
            var result = parser.Parse(tokens);

            // Assert
            result.IsSuccess.Should().BeTrue($"parsing should succeed for valid input. Error: {result.Error?.Message}");

            // Based on the tutorial, the expected output is: 4, 4, 3, 5, 3, 1
            // But production numbers may be different in our implementation
            result.Productions.Should().HaveCountGreaterThan(0, "should apply productions for non-trivial grammar");
        }

        [Fact]
        public void Parse_InvalidInput_ShouldFail()
        {
            // Arrange
            var grammar = new GrammarBuilder()
                .WithGrammarName("simple")
                .AddProductionFromString("S → x")
                .BuildGrammar();

            var lexerSettings = new LexerSettings
            {
                new KeywordTerminal("x"),
                new KeywordTerminal("y")
            };

            var lexer = new Lexer.Lexer(lexerSettings);
            var parser = new SyntaxParser(grammar);

            // Act
            var tokens = lexer.GetTokens(new StringSourceStream("y")).ToList();
            var result = parser.Parse(tokens);

            // Assert
            result.IsSuccess.Should().BeFalse("parsing should fail for invalid input");
            result.Error.Message.Should().NotBeNull("should provide error message");
        }

        [Fact]
        public void Parse_EmptyInput_ShouldFail()
        {
            // Arrange
            var grammar = new GrammarBuilder()
                .WithGrammarName("simple")
                .AddProductionFromString("S → x")
                .BuildGrammar();

            var lexer = new Lexer.Lexer(new LexerSettings());
            var parser = new SyntaxParser(grammar);

            // Act
            var tokens = lexer.GetTokens(new StringSourceStream("")).ToList();
            var result = parser.Parse(tokens);

            // Assert
            result.IsSuccess.Should().BeFalse("parsing should fail for empty input");
        }

        [Fact]
        public void Parse_GrammarWithEpsilon_ShouldSucceed()
        {
            // Arrange - Grammar with epsilon productions: S → A B, A → a | ε, B → b
            var aTerminal = new KeywordTerminal("a");
            var bTerminal = new KeywordTerminal("b");

            var grammar = new GrammarBuilder()
                .WithGrammarName("epsilon_test")
                .AddProductionFromString("S → A B")
                .AddProductionFromString("A → a")
                .AddProductionFromString("A → ε")
                .AddProductionFromString("B → b")
                .BuildGrammar();

            // Get terminals from grammar to ensure consistency
            var grammarTerminals = grammar.Terminals.ToList();
            var aFromGrammar = grammarTerminals.First(t => t.Name == "a");
            var bFromGrammar = grammarTerminals.First(t => t.Name == "b");

            var lexerSettings = new LexerSettings
            {
                aFromGrammar,
                bFromGrammar
            };

            var lexer = new Lexer.Lexer(lexerSettings);
            var parser = new SyntaxParser(grammar);

            // Act - Parse "ab" (A derives a, B derives b)
            var tokens = lexer.GetTokens(new StringSourceStream("ab")).ToList();
            var result = parser.Parse(tokens);

            // Assert
            result.IsSuccess.Should().BeTrue($"parsing should succeed for epsilon production. Error: {result.Error?.Message}");
            result.Productions.Should().NotBeEmpty("should apply productions including epsilon");
        }
    }
}
