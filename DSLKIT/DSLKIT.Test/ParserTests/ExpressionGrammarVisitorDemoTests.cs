using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSLKIT.Ast;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test.ParserTests
{
    public class ExpressionGrammarVisitorDemoTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ExpressionGrammarVisitorDemoTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData("x=2+3*4;x+1", 15)]
        [InlineData("a=2;b=a**3;b-1", 7)]
        [InlineData("v=2**3**2;v", 512)]
        public void Parse_BuildAst_VisitAndEvaluate_ShouldReturnExpectedResult(string source, int expectedResult)
        {
            var grammar = BuildDemoGrammar();
            var lexer = new Lexer.Lexer(CreateLexerSettings(grammar));
            var parser = new SyntaxParser(grammar);

            var tokens = lexer.GetTokens(new StringSourceStream(source)).ToList();
            var parseResult = parser.Parse(tokens);
            if (!parseResult.IsSuccess || parseResult.ParseTree == null)
            {
                throw new Xunit.Sdk.XunitException($"Parse failed for '{source}'. Error: {parseResult.Error?.Message}");
            }

            _testOutputHelper.WriteLine("Parse tree:");
            _testOutputHelper.WriteLine(RenderParseTree(parseResult.ParseTree));

            var ast = new AstBuilder(grammar.AstBindings).Build(parseResult.ParseTree, source);
            if (ast is not ProgramNode programNode)
            {
                throw new Xunit.Sdk.XunitException($"Unexpected AST root type: {ast.GetType().Name}");
            }

            var evaluator = new EvaluationVisitor();
            var actualResult = programNode.Accept(evaluator);

            actualResult.Should().Be(expectedResult);
        }

        private static LexerSettings CreateLexerSettings(IGrammar grammar)
        {
            var settings = new LexerSettings();
            foreach (var terminal in grammar.Terminals)
            {
                settings.Add(terminal);
            }

            return settings;
        }

        private static IGrammar BuildDemoGrammar()
        {
            var integer = new IntegerTerminal();
            var identifier = new IdentifierTerminal();

            var gb = new GrammarBuilder()
                .WithGrammarName("expression-demo")
                .AddTerminal(integer)
                .AddTerminal(identifier);

            var program = gb.NT("Program").Ast<ProgramNode>();
            var statements = gb.NT("Statements").Ast<StatementListNode>();
            var statement = gb.NT("Statement");
            var assignmentStmt = gb.NT("AssignmentStmt").Ast<AssignmentNode>();

            var expr = gb.NT("Expr");
            var addExpr = gb.NT("AddExpr");
            var mulExpr = gb.NT("MulExpr");
            var powExpr = gb.NT("PowExpr");
            var unaryExpr = gb.NT("UnaryExpr");
            var primary = gb.NT("Primary");
            var intLiteral = gb.NT("IntLiteral").Ast<NumberNode>();
            var idLiteral = gb.NT("IdLiteral").Ast<IdentifierNode>();

            gb.Prod("Start").Is(program);
            gb.Prod("Program").Is(statements);

            gb.Star(statements, statement, ";");

            gb.Prod("Statement").Is(assignmentStmt);
            gb.Prod("Statement").Is(expr);
            gb.Prod("AssignmentStmt").Is(idLiteral, "=", expr);

            gb.Prod("Expr").Is(addExpr);

            gb.Prod("AddExpr").Ast<BinaryNode>().Is(addExpr, "+", mulExpr);
            gb.Prod("AddExpr").Ast<BinaryNode>().Is(addExpr, "-", mulExpr);
            gb.Prod("AddExpr").Is(mulExpr);

            gb.Prod("MulExpr").Ast<BinaryNode>().Is(mulExpr, "*", powExpr);
            gb.Prod("MulExpr").Ast<BinaryNode>().Is(mulExpr, "/", powExpr);
            gb.Prod("MulExpr").Is(powExpr);

            gb.Prod("PowExpr").Ast<BinaryNode>().Is(unaryExpr, "**", powExpr);
            gb.Prod("PowExpr").Is(unaryExpr);

            gb.Prod("UnaryExpr").Ast<UnaryNode>().Is("-", unaryExpr);
            gb.Prod("UnaryExpr").Is(primary);

            gb.Prod("Primary").Is(intLiteral);
            gb.Prod("Primary").Is(idLiteral);
            gb.Prod("Primary").Ast(context => context.AstChild<IAstNode>(1)).Is("(", expr, ")");

            gb.Prod("IntLiteral").Is(integer);
            gb.Prod("IdLiteral").Is(identifier);

            return gb.BuildGrammar("Start");
        }

        private static string RenderParseTree(ParseTreeNode root)
        {
            var sb = new StringBuilder();
            WriteParseTreeNode(root, sb, 0);
            return sb.ToString();
        }

        private static void WriteParseTreeNode(ParseTreeNode node, StringBuilder sb, int depth)
        {
            var indent = new string(' ', depth * 2);

            if (node is NonTerminalNode nonTerminalNode)
            {
                sb.AppendLine($"{indent}{nonTerminalNode.NonTerminal.Name}");
            }
            else if (node is TerminalNode terminalNode)
            {
                sb.AppendLine($"{indent}{terminalNode.Token.Terminal.Name}: '{terminalNode.Token.OriginalString}'");
            }
            else
            {
                sb.AppendLine($"{indent}{node.Term.Name}");
            }

            foreach (var child in node.Children)
            {
                WriteParseTreeNode(child, sb, depth + 1);
            }
        }

        private interface IAstVisitor<out TResult>
        {
            TResult VisitProgram(ProgramNode node);
            TResult VisitAssignment(AssignmentNode node);
            TResult VisitBinary(BinaryNode node);
            TResult VisitUnary(UnaryNode node);
            TResult VisitNumber(NumberNode node);
            TResult VisitIdentifier(IdentifierNode node);
        }

        private abstract class DemoAstNode : AstNodeBase
        {
            protected DemoAstNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
            }

            public abstract TResult Accept<TResult>(IAstVisitor<TResult> visitor);
        }

        private sealed class ProgramNode : DemoAstNode
        {
            public ProgramNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Statements = context.AstChild<StatementListNode>(0).Statements;
            }

            public IReadOnlyList<DemoAstNode> Statements { get; }

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitProgram(this);
            }
        }

        private sealed class StatementListNode : AstNodeBase
        {
            public StatementListNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Statements = BuildStatements(children);
            }

            public IReadOnlyList<DemoAstNode> Statements { get; }

            private static IReadOnlyList<DemoAstNode> BuildStatements(IReadOnlyList<IAstNode> children)
            {
                if (children.Count == 1)
                {
                    if (children[0] is AstTokenNode token && token.TerminalName == "Empty")
                    {
                        return [];
                    }

                    if (children[0] is DemoAstNode statement)
                    {
                        return [statement];
                    }
                }

                if (children.Count == 3 &&
                    children[0] is StatementListNode prefix &&
                    children[2] is DemoAstNode last)
                {
                    return prefix.Statements.Concat(new[] { last }).ToList();
                }

                throw new InvalidOperationException("Unexpected statement list production shape.");
            }
        }

        private sealed class AssignmentNode : DemoAstNode
        {
            public AssignmentNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Name = context.AstChild<IdentifierNode>(0).Name;
                Value = context.AstChild<DemoAstNode>(2);
            }

            public string Name { get; }
            public DemoAstNode Value { get; }

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitAssignment(this);
            }
        }

        private sealed class BinaryNode : DemoAstNode
        {
            public BinaryNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Left = context.AstChild<DemoAstNode>(0);
                Operator = context.AstChild<AstTokenNode>(1).Text;
                Right = context.AstChild<DemoAstNode>(2);
            }

            public DemoAstNode Left { get; }
            public string Operator { get; }
            public DemoAstNode Right { get; }

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitBinary(this);
            }
        }

        private sealed class UnaryNode : DemoAstNode
        {
            public UnaryNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Operator = context.AstChild<AstTokenNode>(0).Text;
                Operand = context.AstChild<DemoAstNode>(1);
            }

            public string Operator { get; }
            public DemoAstNode Operand { get; }

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitUnary(this);
            }
        }

        private sealed class NumberNode : DemoAstNode
        {
            public NumberNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Value = (int)context.AstChild<AstTokenNode>(0).Value!;
            }

            public int Value { get; }

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitNumber(this);
            }
        }

        private sealed class IdentifierNode : DemoAstNode
        {
            public IdentifierNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, children)
            {
                Name = context.AstChild<AstTokenNode>(0).Text;
            }

            public string Name { get; }

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitIdentifier(this);
            }
        }

        private sealed class EvaluationVisitor : IAstVisitor<int>
        {
            private readonly Dictionary<string, int> _variables = new Dictionary<string, int>(StringComparer.Ordinal);

            public int VisitProgram(ProgramNode node)
            {
                var result = 0;
                foreach (var statement in node.Statements)
                {
                    result = statement.Accept(this);
                }

                return result;
            }

            public int VisitAssignment(AssignmentNode node)
            {
                var value = node.Value.Accept(this);
                _variables[node.Name] = value;
                return value;
            }

            public int VisitBinary(BinaryNode node)
            {
                var left = node.Left.Accept(this);
                var right = node.Right.Accept(this);

                return node.Operator switch
                {
                    "+" => left + right,
                    "-" => left - right,
                    "*" => left * right,
                    "/" => left / right,
                    "**" => Pow(left, right),
                    _ => throw new InvalidOperationException($"Unsupported binary operator '{node.Operator}'.")
                };
            }

            public int VisitUnary(UnaryNode node)
            {
                var value = node.Operand.Accept(this);
                return node.Operator switch
                {
                    "-" => -value,
                    _ => throw new InvalidOperationException($"Unsupported unary operator '{node.Operator}'.")
                };
            }

            public int VisitNumber(NumberNode node)
            {
                return node.Value;
            }

            public int VisitIdentifier(IdentifierNode node)
            {
                if (!_variables.TryGetValue(node.Name, out var value))
                {
                    throw new InvalidOperationException($"Variable '{node.Name}' is not assigned.");
                }

                return value;
            }

            private static int Pow(int value, int exponent)
            {
                if (exponent < 0)
                {
                    throw new InvalidOperationException("Negative powers are not supported for integer arithmetic.");
                }

                var result = 1;
                for (var i = 0; i < exponent; i++)
                {
                    result *= value;
                }

                return result;
            }
        }
    }
}
