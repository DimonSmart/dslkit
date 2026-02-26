using System.Linq;
using DSLKIT.Ast;
using DSLKIT.GrammarExamples;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class ExpressionGrammarExampleTests
    {
        [Theory]
        [InlineData("x=2+3*4;x+1", 15.0)]
        [InlineData("a=2;b=a**3;b-1", 7.0)]
        [InlineData("v=2**3**2;v", 512.0)]
        [InlineData("(2+3)*4", 20.0)]
        [InlineData("5/2", 2.5)]
        [InlineData("a=1.25;b=a+2;b", 3.25)]
        [InlineData("sin(0)", 0.0)]
        [InlineData("cos(0)", 1.0)]
        [InlineData("tg(0)", 0.0)]
        [InlineData("ctg(1)", 0.6420926159343306)]
        [InlineData("round(2.5)", 3.0)]
        [InlineData("round(-2.5)", -3.0)]
        [InlineData("trunc(2.9)", 2.0)]
        [InlineData("trunc(-2.9)", -2.0)]
        [InlineData("x=1;y=sin(x)**2+cos(x)**2;round(y)", 1.0)]
        [InlineData("x=0.75;y=tg(x)*ctg(x);round(y)", 1.0)]
        [InlineData("x=1;a=sin(x)**2+cos(x)**2;b=tg(x)*ctg(x);c=round(2.5)+trunc(3.9)-trunc(-3.9);a+b+c", 11.0)]
        [InlineData("p=2;q=3;r=(p+q)*(p**q)-round(2.5)+trunc(3.9);r", 40.0)]
        [InlineData("a=5;b=2;c=a/b;d=trunc(c)+round(c)+trunc(-c);d", 3.0)]
        [InlineData("round((sin(1)**2+cos(1)**2+tg(1)*ctg(1))*5/2)", 5.0)]
        public void Evaluate_ShouldReturnExpectedResult(string source, double expectedResult)
        {
            var actualResult = ExpressionGrammarExample.Evaluate(source);

            actualResult.Should().BeApproximately(expectedResult, 1e-9);
        }

        [Fact]
        public void BuildAst_ShouldExposeSemanticMetadataForVisualizer()
        {
            const string source = "x=2+3*4;x+1";
            var grammar = ExpressionGrammarExample.BuildGrammar();
            var lexer = new DSLKIT.Lexer.Lexer(ExpressionGrammarExample.CreateLexerSettings(grammar));
            var parser = new SyntaxParser(grammar);

            var tokens = lexer.GetTokens(new StringSourceStream(source))
                .Where(token => token.Terminal.Flags != TermFlags.Space && token.Terminal.Flags != TermFlags.Comment)
                .ToList();

            var parseResult = parser.Parse(tokens);
            parseResult.IsSuccess.Should().BeTrue();
            parseResult.ParseTree.Should().NotBeNull();

            var astRoot = new AstBuilder(grammar.AstBindings).Build(parseResult.ParseTree!);

            astRoot.DisplayName.Should().Be("Program");
            astRoot.Description.Should().Be("Statements: 2");
            astRoot.Children.Should().HaveCount(2);

            var assignmentNode = astRoot.Children[0];
            assignmentNode.DisplayName.Should().Be("x =");
            assignmentNode.Children.Should().HaveCount(1);
            assignmentNode.Children[0].DisplayName.Should().Be("+");

            var additionNode = assignmentNode.Children[0];
            additionNode.Children.Should().HaveCount(2);
            additionNode.Children[0].DisplayName.Should().Be("2");
            additionNode.Children[0].ChildrenDisplayMode.Should().Be(AstChildrenDisplayMode.Hide);
            additionNode.Children[1].DisplayName.Should().Be("*");

            var secondStatementNode = astRoot.Children[1];
            secondStatementNode.DisplayName.Should().Be("+");
            secondStatementNode.Children.Should().HaveCount(2);
            secondStatementNode.Children[0].DisplayName.Should().Be("x");
            secondStatementNode.Children[0].ChildrenDisplayMode.Should().Be(AstChildrenDisplayMode.Hide);
            secondStatementNode.Children[1].DisplayName.Should().Be("1");
        }
    }
}
