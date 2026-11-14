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
    public class GrammarBuilderNestedOptionalTests
    {
        [Fact]
        public void Optional_InsideRule_ShouldExpandIndependentSegmentsWithoutSyntheticNonTerminals()
        {
            var gb = new GrammarBuilder();
            gb.Rule("Start").CanBe(
                "UPDATE", "t", "SET", "x", "=", "1",
                gb.Optional("FROM", "s"),
                gb.Optional("WHERE", "p"));

            var grammar = gb.BuildGrammar("Start");
            var alternatives = grammar.Productions.Select(production =>
                string.Join(" ", production.ProductionDefinition.Select(term => term.Name))).ToList();

            alternatives.Should().Equal(
                "UPDATE t SET x = 1",
                "UPDATE t SET x = 1 WHERE p",
                "UPDATE t SET x = 1 FROM s",
                "UPDATE t SET x = 1 FROM s WHERE p");
            grammar.NonTerminals.Select(term => term.Name).Should().Equal("Start");
            grammar.Root.Name.Should().Be("Start");
        }

        [Fact]
        public void Optional_InsideNestedSequenceAndProduction_ShouldExpandRecursively()
        {
            var gb = new GrammarBuilder();
            gb.Prod("Start").Is(
                gb.Seq("a", gb.Optional(gb.Seq("b", gb.Optional("c")))), "d");

            var alternatives = gb.BuildGrammar("Start").Productions.Select(production =>
                string.Join(" ", production.ProductionDefinition.Select(term => term.Name)));

            alternatives.Should().Equal("a d", "a b d", "a b c d");
        }

        [Fact]
        public void Optional_InsideOneOf_ShouldApplyOnlyToItsAlternative()
        {
            var gb = new GrammarBuilder();
            gb.Rule("Start").OneOf(gb.Seq("a", gb.Optional("b")), "c");

            var alternatives = gb.BuildGrammar("Start").Productions.Select(production =>
                string.Join(" ", production.ProductionDefinition.Select(term => term.Name)));

            alternatives.Should().Equal("a", "a b", "c");
        }

        [Fact]
        public void Optional_AsWholeRule_ShouldUseEmptyTermAndRemoveDuplicateAlternatives()
        {
            var gb = new GrammarBuilder();
            gb.Rule("Start").CanBe(gb.Optional(gb.Optional("a")));

            var productions = gb.BuildGrammar("Start").Productions.ToList();

            productions.Should().HaveCount(2);
            productions[0].ProductionDefinition.Should().ContainSingle()
                .Which.Should().BeOfType<EmptyTerm>();
            productions[1].ProductionDefinition.Should().ContainSingle()
                .Which.Name.Should().Be("a");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Optional_ExpandedProductions_ShouldAllKeepAstBinding(bool bindBeforeDefine)
        {
            var gb = new GrammarBuilder();
            var builder = gb.Prod("Start");

            if (bindBeforeDefine)
            {
                builder.Ast(_ => throw new InvalidOperationException());
            }

            builder.Define("a", gb.Optional("b"));

            if (!bindBeforeDefine)
            {
                builder.Ast(_ => throw new InvalidOperationException());
            }

            var grammar = gb.BuildGrammar("Start");
            grammar.Productions.Should().HaveCount(2);
            foreach (var production in grammar.Productions)
            {
                grammar.AstBindings.TryGet(production, out var binding).Should().BeTrue();
                binding.Should().NotBeNull();
            }
        }

        [Theory]
        [InlineData("a c", true)]
        [InlineData("a b c", true)]
        [InlineData("a b b c", false)]
        public void Optional_InsideRule_ShouldParseExpectedInputs(string input, bool expectedSuccess)
        {
            var gb = new GrammarBuilder().AddTerminal(new SpaceTerminal());
            gb.Rule("Start").CanBe("a", gb.Optional("b"), "c");

            var grammar = gb.BuildGrammar("Start");
            var lexerSettings = new LexerSettings();
            foreach (var terminal in grammar.Terminals)
            {
                lexerSettings.Add(terminal);
            }

            var lexer = new Lexer.Lexer(lexerSettings);
            var tokens = lexer.GetTokens(new StringSourceStream(input))
                .Where(token => token.Terminal.Flags != TermFlags.Space)
                .ToList();

            var result = new SyntaxParser(grammar).Parse(tokens);
            result.IsSuccess.Should().Be(expectedSuccess);
        }

        [Fact]
        public void Optional_WithNoTerms_ShouldThrowArgumentException()
        {
            var gb = new GrammarBuilder();
            Action act = () => gb.Optional();

            act.Should().Throw<ArgumentException>();
        }
    }
}
