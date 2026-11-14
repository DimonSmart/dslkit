using System;
using System.Linq;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.ParserTests
{
    public class GrammarBuilderInlinePatternTests
    {
        [Fact]
        public void Choice_InsideRule_ShouldExpandWithOptionalWithoutSyntheticNonTerminals()
        {
            var gb = new GrammarBuilder();
            gb.Rule("Start").CanBe(
                "SELECT",
                gb.Choice("ALL", "DISTINCT"),
                gb.Optional("FROM", "t"));

            var grammar = gb.BuildGrammar();
            var definitions = grammar.Productions
                .Select(production => string.Join(" ", production.ProductionDefinition.Select(term => term.Name)));

            definitions.Should().Equal(
                "SELECT ALL",
                "SELECT ALL FROM t",
                "SELECT DISTINCT",
                "SELECT DISTINCT FROM t");
            grammar.NonTerminals.Select(term => term.Name).Should().Equal("Start");
            grammar.Root.Name.Should().Be("Start");
        }

        [Fact]
        public void Choice_NestedInSequenceAndOneOf_ShouldExpandRecursively()
        {
            var gb = new GrammarBuilder();
            gb.Rule("Start").OneOf(
                gb.Seq("a", gb.Choice("b", gb.Seq("c", gb.Optional("d")))),
                "e");

            var definitions = gb.BuildGrammar().Productions
                .Select(production => string.Join(" ", production.ProductionDefinition.Select(term => term.Name)));

            definitions.Should().Equal("a b", "a c", "a c d", "e");
        }

        [Fact]
        public void Choice_ExpandedProductions_ShouldPreserveAstBinding()
        {
            var gb = new GrammarBuilder();
            gb.Prod("Start")
                .Ast(_ => throw new InvalidOperationException())
                .Define("a", gb.Choice("b", "c"));

            var grammar = gb.BuildGrammar();
            grammar.Productions.Should().HaveCount(2);
            foreach (var production in grammar.Productions)
            {
                grammar.AstBindings.TryGet(production, out var binding).Should().BeTrue();
                binding.Should().NotBeNull();
            }
        }

        [Theory]
        [InlineData("begin end", true)]
        [InlineData("begin x y end", true)]
        [InlineData("begin x y x y end", true)]
        [InlineData("begin x end", false)]
        public void Many_ShouldRepeatWholeSequence(string input, bool expected)
        {
            var gb = new GrammarBuilder().AddTerminal(new SpaceTerminal());
            gb.Rule("Start").CanBe("begin", gb.Many("x", "y"), "end");

            var grammar = gb.BuildGrammar();
            grammar.Root.Name.Should().Be("Start");
            grammar.NonTerminals.Should().HaveCount(2);
            Parses(grammar, input).Should().Be(expected);
        }

        [Theory]
        [InlineData("red stop", true)]
        [InlineData("red blue red stop", true)]
        [InlineData("stop", false)]
        [InlineData("red blue", false)]
        public void OneOrMore_ShouldRepeatInlineAlternatives(string input, bool expected)
        {
            var gb = new GrammarBuilder().AddTerminal(new SpaceTerminal());
            gb.Rule("Start").CanBe(gb.OneOrMore(gb.Choice("red", "blue")), "stop");

            var grammar = gb.BuildGrammar();
            grammar.Root.Name.Should().Be("Start");
            Parses(grammar, input).Should().Be(expected);
        }

        [Theory]
        [InlineData("items x y", true)]
        [InlineData("items x y , x y", true)]
        [InlineData("items", false)]
        [InlineData("items x y ,", false)]
        public void SeparatedBy_ShouldRequireItemsBetweenDelimiters(string input, bool expected)
        {
            var gb = new GrammarBuilder().AddTerminal(new SpaceTerminal());
            gb.Rule("Start").CanBe("items", gb.SeparatedBy(",", gb.Seq("x", "y")));

            var grammar = gb.BuildGrammar();
            Parses(grammar, input).Should().Be(expected);
        }

        [Theory]
        [InlineData("list", true)]
        [InlineData("list x", true)]
        [InlineData("list x , x", true)]
        [InlineData("list , x", false)]
        public void SeparatedBy_WithinOptional_ShouldPermitEmptyList(string input, bool expected)
        {
            var gb = new GrammarBuilder().AddTerminal(new SpaceTerminal());
            gb.Rule("Start").CanBe("list", gb.Optional(gb.SeparatedBy(",", "x")));

            var grammar = gb.BuildGrammar();
            Parses(grammar, input).Should().Be(expected);
        }

        [Fact]
        public void ReusingRepetitionPattern_ShouldReuseSingleGeneratedNonTerminal()
        {
            var gb = new GrammarBuilder();
            var items = gb.Many("x");
            gb.Rule("Start").CanBe(items, "middle", items);

            var grammar = gb.BuildGrammar();
            grammar.NonTerminals.Should().HaveCount(2);
            grammar.Root.Name.Should().Be("Start");
            grammar.Productions.Should().HaveCount(3);
        }

        [Fact]
        public void Repetitions_ShouldRejectEmptyMatchingPatterns()
        {
            var gb = new GrammarBuilder();

            Action empty = () => gb.Many();
            Action optional = () => gb.OneOrMore(gb.Optional("x"));
            Action emptyChoice = () => gb.Many(gb.Choice("x", gb.Seq()));
            Action emptyDelimiter = () => gb.SeparatedBy(gb.Optional(","), "x");

            empty.Should().Throw<ArgumentException>();
            optional.Should().Throw<ArgumentException>();
            emptyChoice.Should().Throw<ArgumentException>();
            emptyDelimiter.Should().Throw<ArgumentException>();
        }

        private static bool Parses(Grammar grammar, string source)
        {
            var settings = new LexerSettings();
            foreach (var terminal in grammar.Terminals)
            {
                settings.Add(terminal);
            }

            var lexer = new Lexer.Lexer(settings);
            var tokens = lexer.GetTokens(new StringSourceStream(source))
                .Where(token => token.Terminal.Flags != TermFlags.Space)
                .ToList();

            return new SyntaxParser(grammar).Parse(tokens).IsSuccess;
        }
    }
}
