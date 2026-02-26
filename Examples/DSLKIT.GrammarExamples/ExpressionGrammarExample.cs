using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DSLKIT.Ast;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Terminals;

namespace DSLKIT.GrammarExamples
{
    /// <summary>
    /// Example arithmetic language with assignments and expression evaluation.
    /// </summary>
    public static class ExpressionGrammarExample
    {
        public static double Evaluate(string source)
        {
            var grammar = BuildGrammar();
            var lexer = new Lexer.Lexer(CreateLexerSettings(grammar));
            var parser = new SyntaxParser(grammar);

            var tokens = lexer.GetTokens(new StringSourceStream(source)).ToList();
            var parseResult = parser.Parse(tokens);
            if (!parseResult.IsSuccess || parseResult.ParseTree == null)
            {
                throw new InvalidOperationException($"Parse failed for '{source}'. Error: {parseResult.Error?.Message}");
            }

            var ast = new AstBuilder(grammar.AstBindings).Build(parseResult.ParseTree, source);
            if (ast is not ProgramNode programNode)
            {
                throw new InvalidOperationException($"Unexpected AST root type: {ast.GetType().Name}");
            }

            var evaluator = new EvaluationVisitor();
            return programNode.Accept(evaluator);
        }

        public static IGrammar BuildGrammar()
        {
            var number = new RegExpTerminal(
                "Number",
                @"\G(?:\d+(\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?",
                previewChar: null,
                flags: TermFlags.Const);
            var identifier = new IdentifierTerminal();

            var gb = new GrammarBuilder()
                .WithGrammarName("expression-demo")
                .AddTerminal(number)
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
            var numberLiteral = gb.NT("NumberLiteral").Ast<NumberNode>();
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
            gb.Prod("UnaryExpr").Ast<UnaryNode>().Is("+", unaryExpr);
            gb.Prod("UnaryExpr").Ast<UnaryNode>().Is("sin", "(", expr, ")");
            gb.Prod("UnaryExpr").Ast<UnaryNode>().Is("cos", "(", expr, ")");
            gb.Prod("UnaryExpr").Ast<UnaryNode>().Is("tg", "(", expr, ")");
            gb.Prod("UnaryExpr").Ast<UnaryNode>().Is("ctg", "(", expr, ")");
            gb.Prod("UnaryExpr").Ast<UnaryNode>().Is("round", "(", expr, ")");
            gb.Prod("UnaryExpr").Ast<UnaryNode>().Is("trunc", "(", expr, ")");
            gb.Prod("UnaryExpr").Is(primary);

            gb.Prod("Primary").Is(numberLiteral);
            gb.Prod("Primary").Is(idLiteral);
            gb.Prod("Primary").Ast(context => context.AstChild<IAstNode>(1)).Is("(", expr, ")");

            gb.Prod("NumberLiteral").Is(number);
            gb.Prod("IdLiteral").Is(identifier);

            return gb.BuildGrammar("Start");
        }

        public static LexerSettings CreateLexerSettings(IGrammar grammar)
        {
            var settings = new LexerSettings();
            foreach (var terminal in grammar.Terminals)
            {
                settings.Add(terminal);
            }

            return settings;
        }

        public static string RenderParseTree(ParseTreeNode root)
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
                : base(context, BuildSemanticChildren(children))
            {
                Statements = context.AstChild<StatementListNode>(0).Statements;
            }

            public IReadOnlyList<DemoAstNode> Statements { get; }
            public override string DisplayName => "Program";
            public override string? Description => $"Statements: {Statements.Count}";

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitProgram(this);
            }

            private static IReadOnlyList<IAstNode> BuildSemanticChildren(IReadOnlyList<IAstNode> children)
            {
                if (children.Count == 1 && children[0] is StatementListNode statementListNode)
                {
                    return statementListNode.Statements.Cast<IAstNode>().ToList();
                }

                throw new InvalidOperationException("Unexpected program production shape.");
            }
        }

        private sealed class StatementListNode : AstNodeBase
        {
            public StatementListNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, Array.Empty<IAstNode>())
            {
                Statements = BuildStatements(children);
            }

            public IReadOnlyList<DemoAstNode> Statements { get; }
            public override AstChildrenDisplayMode ChildrenDisplayMode => AstChildrenDisplayMode.Hide;

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
                : base(context, BuildSemanticChildren(children))
            {
                Name = context.AstChild<IdentifierNode>(0).Name;
                Value = context.AstChild<DemoAstNode>(2);
            }

            public string Name { get; }
            public DemoAstNode Value { get; }
            public override string DisplayName => $"{Name} =";

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitAssignment(this);
            }

            private static IReadOnlyList<IAstNode> BuildSemanticChildren(IReadOnlyList<IAstNode> children)
            {
                if (children.Count == 3)
                {
                    return [children[2]];
                }

                throw new InvalidOperationException("Unexpected assignment production shape.");
            }
        }

        private sealed class BinaryNode : DemoAstNode
        {
            public BinaryNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, BuildSemanticChildren(children))
            {
                Left = context.AstChild<DemoAstNode>(0);
                Operator = context.AstChild<AstTokenNode>(1).Text;
                Right = context.AstChild<DemoAstNode>(2);
            }

            public DemoAstNode Left { get; }
            public string Operator { get; }
            public DemoAstNode Right { get; }
            public override string DisplayName => Operator;

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitBinary(this);
            }

            private static IReadOnlyList<IAstNode> BuildSemanticChildren(IReadOnlyList<IAstNode> children)
            {
                if (children.Count == 3)
                {
                    return [children[0], children[2]];
                }

                throw new InvalidOperationException("Unexpected binary production shape.");
            }
        }

        private sealed class UnaryNode : DemoAstNode
        {
            public UnaryNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, BuildSemanticChildren(children))
            {
                if (children.Count == 2)
                {
                    Operator = context.AstChild<AstTokenNode>(0).Text;
                    Operand = context.AstChild<DemoAstNode>(1);
                    return;
                }

                if (children.Count == 4)
                {
                    Operator = context.AstChild<AstTokenNode>(0).Text;
                    Operand = context.AstChild<DemoAstNode>(2);
                    return;
                }

                throw new InvalidOperationException("Unexpected unary production shape.");
            }

            public string Operator { get; }
            public DemoAstNode Operand { get; }
            public override string DisplayName => Operator;

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitUnary(this);
            }

            private static IReadOnlyList<IAstNode> BuildSemanticChildren(IReadOnlyList<IAstNode> children)
            {
                if (children.Count == 2)
                {
                    return [children[1]];
                }

                if (children.Count == 4)
                {
                    return [children[2]];
                }

                throw new InvalidOperationException("Unexpected unary production shape.");
            }
        }

        private sealed class NumberNode : DemoAstNode
        {
            public NumberNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, Array.Empty<IAstNode>())
            {
                var literal = context.AstChild<AstTokenNode>(0).Text;
                if (!double.TryParse(literal, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
                {
                    throw new InvalidOperationException($"Cannot parse number literal '{literal}'.");
                }

                Value = parsedValue;
            }

            public double Value { get; }
            public override string DisplayName => Value.ToString(CultureInfo.InvariantCulture);
            public override AstChildrenDisplayMode ChildrenDisplayMode => AstChildrenDisplayMode.Hide;

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitNumber(this);
            }
        }

        private sealed class IdentifierNode : DemoAstNode
        {
            public IdentifierNode(AstBuildContext context, IReadOnlyList<IAstNode> children)
                : base(context, Array.Empty<IAstNode>())
            {
                Name = context.AstChild<AstTokenNode>(0).Text;
            }

            public string Name { get; }
            public override string DisplayName => Name;
            public override AstChildrenDisplayMode ChildrenDisplayMode => AstChildrenDisplayMode.Hide;

            public override TResult Accept<TResult>(IAstVisitor<TResult> visitor)
            {
                return visitor.VisitIdentifier(this);
            }
        }

        private sealed class EvaluationVisitor : IAstVisitor<double>
        {
            private readonly Dictionary<string, double> _variables = new(StringComparer.Ordinal);

            public double VisitProgram(ProgramNode node)
            {
                var result = 0d;
                foreach (var statement in node.Statements)
                {
                    result = statement.Accept(this);
                }

                return result;
            }

            public double VisitAssignment(AssignmentNode node)
            {
                var value = node.Value.Accept(this);
                _variables[node.Name] = value;
                return value;
            }

            public double VisitBinary(BinaryNode node)
            {
                var left = node.Left.Accept(this);
                var right = node.Right.Accept(this);

                return node.Operator switch
                {
                    "+" => left + right,
                    "-" => left - right,
                    "*" => left * right,
                    "/" => left / right,
                    "**" => Math.Pow(left, right),
                    _ => throw new InvalidOperationException($"Unsupported binary operator '{node.Operator}'.")
                };
            }

            public double VisitUnary(UnaryNode node)
            {
                var value = node.Operand.Accept(this);
                return node.Operator switch
                {
                    "-" => -value,
                    "+" => value,
                    "sin" => Math.Sin(value),
                    "cos" => Math.Cos(value),
                    "tg" => Math.Tan(value),
                    "ctg" => Cotangent(value),
                    "round" => Math.Round(value, MidpointRounding.AwayFromZero),
                    "trunc" => Math.Truncate(value),
                    _ => throw new InvalidOperationException($"Unsupported unary operator '{node.Operator}'.")
                };
            }

            public double VisitNumber(NumberNode node)
            {
                return node.Value;
            }

            public double VisitIdentifier(IdentifierNode node)
            {
                if (!_variables.TryGetValue(node.Name, out var value))
                {
                    throw new InvalidOperationException($"Variable '{node.Name}' is not assigned.");
                }

                return value;
            }

            private static double Cotangent(double value)
            {
                var tangent = Math.Tan(value);
                if (Math.Abs(tangent) < 1e-12)
                {
                    throw new InvalidOperationException("ctg(x) is undefined for this value.");
                }

                return 1d / tangent;
            }
        }
    }
}
