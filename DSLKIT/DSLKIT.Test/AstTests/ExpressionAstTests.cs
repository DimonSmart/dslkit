using DSLKIT.Ast;
using DSLKIT.Lexer;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using DSLKIT.Test.Common;
using DSLKIT.Tokens;
using FluentAssertions;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test.AstTests
{
    public class ExpressionAstTests : GrammarTestsBase
    {
        public ExpressionAstTests(ITestOutputHelper output) : base(output) { }

        private static Grammar BuildGrammar()
        {
            var builder = new GrammarBuilder()
                .WithGrammarName("expr")
                .AddTerminal(Constants.Integer);

            // Expr -> Expr + Term
            builder.AddProduction("Expr").AddProductionDefinition("Expr".AsNonTerminal(), "+", "Term".AsNonTerminal());
            // Expr -> Term
            builder.AddProduction("Expr").AddProductionDefinition("Term".AsNonTerminal());

            // Term -> Term * Factor
            builder.AddProduction("Term").AddProductionDefinition("Term".AsNonTerminal(), "*", "Factor".AsNonTerminal());
            // Term -> Factor
            builder.AddProduction("Term").AddProductionDefinition("Factor".AsNonTerminal());

            // Factor -> Integer
            builder.AddProduction("Factor").AddProductionDefinition(Constants.Integer);
            // Factor -> ( Expr )
            builder.AddProduction("Factor").AddProductionDefinition("(", "Expr".AsNonTerminal(), ")");

            return builder.WithAugmentedGrammar().BuildGrammar();
        }

        private class NumberNode : AstNode
        {
            public int Value { get; private set; }
            public override void Init(ParseTreeNode parseNode, System.Collections.Generic.IEnumerable<AstNode> children)
            {
                base.Init(parseNode, children);
                Value = (int)((IntegerToken)((TerminalNode)parseNode).Token).Value;
            }
        }

        private class BinaryNode : AstNode
        {
            public string Op { get; private set; }
            public AstNode Left => Children.First();
            public AstNode Right => Children.Last();
            public override void Init(ParseTreeNode parseNode, System.Collections.Generic.IEnumerable<AstNode> children)
            {
                base.Init(parseNode, children);
                Op = ((TerminalNode)parseNode.Children[1]).Token.OriginalString;
            }
        }

        [Fact]
        public void Ast_ShouldReflectOperatorPrecedence()
        {
            var grammar = BuildGrammar();
            var lexerSettings = new LexerSettings();
            lexerSettings.AddRange(grammar.Terminals);
            var lexer = new Lexer.Lexer(lexerSettings);
            var parser = new SyntaxParser(grammar);

            var tokens = lexer.GetTokens(new StringSourceStream("2+3*4")).ToList();
            var parseResult = parser.Parse(tokens);
            parseResult.IsSuccess.Should().BeTrue();

            var builder = new AstBuilder();
            var intTerm = grammar.Terminals.First(t => t.Name == "Integer");
            builder.Register<NumberNode>(intTerm);
            var exprNt = grammar.NonTerminals.First(nt => nt.Name == "Expr");
            builder.Register(exprNt, (node, children) =>
            {
                if (children.Count() == 1)
                {
                    return children.First();
                }
                var bin = new BinaryNode();
                bin.Init(node, children);
                return bin;
            });
            builder.Register(grammar.NonTerminals.First(nt => nt.Name == "Term"), (node, children) =>
            {
                if (children.Count() == 1)
                {
                    return children.First();
                }
                var bin = new BinaryNode();
                bin.Init(node, children);
                return bin;
            });
            builder.Register(grammar.NonTerminals.First(nt => nt.Name == "Factor"), (node, children) =>
            {
                if (children.Count() == 1)
                {
                    return children.First();
                }
                return children.ElementAt(1);
            });

            var root = builder.Build(parseResult.ParseTree);
            root.Should().NotBeNull();

            // root should be '+' with left=Number(2) and right='*' node
            var plus = root.Should().BeOfType<BinaryNode>().Subject;
            plus.Op.Should().Be("+");

            var leftNum = plus.Left.Should().BeOfType<NumberNode>().Subject;
            leftNum.Value.Should().Be(2);

            var star = plus.Right.Should().BeOfType<BinaryNode>().Subject;
            star.Op.Should().Be("*");
            var starLeft = star.Left.Should().BeOfType<NumberNode>().Subject;
            starLeft.Value.Should().Be(3);
            var starRight = star.Right.Should().BeOfType<NumberNode>().Subject;
            starRight.Value.Should().Be(4);
        }
    }
}
