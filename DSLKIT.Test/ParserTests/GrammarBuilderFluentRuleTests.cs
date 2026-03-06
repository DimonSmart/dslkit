using System.Linq;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.ParserTests
{
    public class GrammarBuilderFluentRuleTests
    {
        [Fact]
        public void Rule_OneOfAndKeywords_ShouldCreateExpectedProductions()
        {
            var grammarBuilder = new GrammarBuilder()
                .WithGrammarName("fluent-one-of");

            var statement = grammarBuilder.NT("Statement");
            var setQuantifier = grammarBuilder.NT("SetQuantifier");

            grammarBuilder.Rule("Start").CanBe(statement);
            grammarBuilder.Rule(statement)
                .CanBe("SELECT")
                .Or(grammarBuilder.Seq("INSERT", "INTO"))
                .Or("DELETE");
            grammarBuilder.Rule(setQuantifier)
                .CanBe("ALL")
                .OrKeywords("DISTINCT");

            var grammar = grammarBuilder.BuildGrammar("Start");

            var statementProductions = grammar.Productions
                .Where(i => i.LeftNonTerminal.Name == "Statement")
                .Select(i => string.Join(" ", i.ProductionDefinition.Select(term => term.Name)))
                .ToList();

            statementProductions.Should().BeEquivalentTo(
                ["SELECT", "INSERT INTO", "DELETE"]);

            var setQuantifierProductions = grammar.Productions
                .Where(i => i.LeftNonTerminal.Name == "SetQuantifier")
                .Select(i => i.ProductionDefinition.Single().Name)
                .ToList();

            setQuantifierProductions.Should().BeEquivalentTo(["ALL", "DISTINCT"]);
        }

        [Fact]
        public void Rule_PlusAndSeparatedBy_ShouldCreateExpectedProductions()
        {
            var grammarBuilder = new GrammarBuilder()
                .WithGrammarName("fluent-lists");

            var item = grammarBuilder.NT("Item");
            var list = grammarBuilder.NT("List");
            var delimitedList = grammarBuilder.NT("DelimitedList");

            grammarBuilder.Rule("Start").CanBe(list);
            grammarBuilder.Rule(item).CanBe("x");
            grammarBuilder.Rule(list).Plus(item);
            grammarBuilder.Rule(delimitedList).SeparatedBy(",", item);

            var grammar = grammarBuilder.BuildGrammar("Start");

            var listProductions = grammar.Productions
                .Where(i => i.LeftNonTerminal.Name == "List")
                .Select(i => string.Join(" ", i.ProductionDefinition.Select(term => term.Name)))
                .ToList();

            listProductions.Should().BeEquivalentTo(["Item", "List Item"]);

            var delimitedListProductions = grammar.Productions
                .Where(i => i.LeftNonTerminal.Name == "DelimitedList")
                .Select(i => string.Join(" ", i.ProductionDefinition.Select(term => term.Name)))
                .ToList();

            delimitedListProductions.Should().BeEquivalentTo(["Item", "DelimitedList , Item"]);
        }

        [Fact]
        public void WithKeywordPolicy_ShouldApplyToWordKeywordsButNotPunctuation()
        {
            var identifier = new IdentifierTerminal(allowDot: false);

            var grammarBuilder = new GrammarBuilder()
                .WithGrammarName("keyword-policy")
                .WithKeywordPolicy(wholeWord: true, ignoreCase: true)
                .AddTerminal(new SpaceTerminal())
                .AddTerminal(identifier);

            grammarBuilder.Rule("Start").CanBe("SELECT", identifier, ".", identifier);

            var grammar = grammarBuilder.BuildGrammar("Start");
            var lexerSettings = new LexerSettings();
            foreach (var terminal in grammar.Terminals)
            {
                lexerSettings.Add(terminal);
            }

            var lexer = new Lexer.Lexer(lexerSettings);
            var parser = new SyntaxParser(grammar);
            var tokens = lexer.GetTokens(new StringSourceStream("select Foo.bar"))
                .Where(token => token.Terminal.Flags != TermFlags.Space)
                .ToList();

            var parseResult = parser.Parse(tokens);

            parseResult.IsSuccess.Should().BeTrue(
                $"expected 'select Foo.bar' to parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }
    }
}
