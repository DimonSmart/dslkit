using System.Collections.Generic;
using System.Linq;
using DSLKIT.Ast;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using DSLKIT.Tokens;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.ParserTests
{
    public class AstBuilderTests
    {
        [Fact]
        public void BuildAst_SecondPass_ShouldCreateExpectedNodes()
        {
            var identifierTerminal = new IdentifierTerminal();
            var integerTerminal = new IntegerTerminal();

            var gb = new GrammarBuilder()
                .WithGrammarName("ast-demo")
                .AddTerminal(identifierTerminal)
                .AddTerminal(integerTerminal);

            var stmt = gb.NT("Stmt");
            var expr = gb.NT("Expr");
            var call = gb.NT("Call");
            var argList = gb.NT("ArgList").Ast<ArgumentListNode>();
            var varIdentifier = gb.NT("VarIdentifier").Ast<IdentifierNode>();
            var funcIdentifier = gb.NT("FuncIdentifier").Ast<IdentifierNode>();

            gb.Prod("Start").Is(stmt);
            gb.Prod("Stmt").Ast<VarDeclNode>().Is("var", varIdentifier, "=", expr);
            gb.Prod("Expr").Is(call);
            gb.Prod("Call").Ast<CallNode>().Is(funcIdentifier, "(", argList, ")");
            gb.Prod("ArgList").Is(integerTerminal, ",", integerTerminal, ",", integerTerminal, ",", integerTerminal, ",", integerTerminal);
            gb.Prod("VarIdentifier").Is(identifierTerminal);
            gb.Prod("FuncIdentifier").Is(identifierTerminal);

            var grammar = gb.BuildGrammar("Start");
            const string sourceText = "var x = MAX(1,2,3,4,5)";
            var terminals = grammar.Terminals.ToDictionary(i => i.Name);
            var tokens = new List<IToken>
            {
                new Token(0, 3, "var", "var", terminals["var"]),
                new Token(4, 1, "x", "x", terminals["Id"]),
                new Token(6, 1, "=", "=", terminals["="]),
                new Token(8, 3, "MAX", "MAX", terminals["Id"]),
                new Token(11, 1, "(", "(", terminals["("]),
                new Token(12, 1, "1", 1, terminals["Integer"]),
                new Token(13, 1, ",", ",", terminals[","]),
                new Token(14, 1, "2", 2, terminals["Integer"]),
                new Token(15, 1, ",", ",", terminals[","]),
                new Token(16, 1, "3", 3, terminals["Integer"]),
                new Token(17, 1, ",", ",", terminals[","]),
                new Token(18, 1, "4", 4, terminals["Integer"]),
                new Token(19, 1, ",", ",", terminals[","]),
                new Token(20, 1, "5", 5, terminals["Integer"]),
                new Token(21, 1, ")", ")", terminals[")"])
            };

            var parseResult = new SyntaxParser(grammar).Parse(tokens);

            parseResult.IsSuccess.Should().BeTrue(parseResult.Error?.Message);
            parseResult.ParseTree.Should().NotBeNull();
            parseResult.ParseTree.Should().BeOfType<NonTerminalNode>();
            ((NonTerminalNode)parseResult.ParseTree).Production.Should().NotBeNull();

            var ast = new AstBuilder(grammar.AstBindings).Build(parseResult.ParseTree, sourceText);

            ast.Should().BeOfType<VarDeclNode>();
            var varDecl = (VarDeclNode)ast;
            varDecl.Name.Should().Be("x");
            varDecl.Value.Should().BeOfType<CallNode>();

            var callNode = (CallNode)varDecl.Value;
            callNode.Name.Should().Be("MAX");
            callNode.Arguments.Args.Should().HaveCount(5);
        }

        private sealed class VarDeclNode : AstNodeBase
        {
            public VarDeclNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Name = context.AstChild<IdentifierNode>(1).Name;
                Value = context.AstChild<IAstNode>(3);
            }

            public string Name { get; }
            public IAstNode Value { get; }
        }

        private sealed class CallNode : AstNodeBase
        {
            public CallNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Name = context.AstChild<IdentifierNode>(0).Name;
                Arguments = context.AstChild<ArgumentListNode>(2);
            }

            public string Name { get; }
            public ArgumentListNode Arguments { get; }
        }

        private sealed class IdentifierNode : AstNodeBase
        {
            public IdentifierNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Name = context.AstChild<AstTokenNode>(0).Text;
            }

            public string Name { get; }
        }

        private sealed class NumberNode : IAstNode
        {
            public NumberNode(int value)
            {
                Value = value;
            }

            public int Value { get; }
            public IReadOnlyList<IAstNode> Children { get; } = [];
        }

        private sealed class ArgumentListNode : AstNodeBase
        {
            public ArgumentListNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Args = children
                    .OfType<AstTokenNode>()
                    .Where(i => i.TerminalName == "Integer")
                    .Select(i => new NumberNode((int)i.Value))
                    .ToList();
            }

            public IReadOnlyList<NumberNode> Args { get; }
        }
    }
}
