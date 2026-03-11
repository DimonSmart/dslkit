using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DSLKIT.Parser;
using DSLKIT.Terminals;
using DSLKIT.Tokens;

namespace DSLKIT.GrammarExamples.MsSql.Formatting
{
    internal sealed class SqlFormattingVisitor : SqlParseTreeVisitorBase
    {
        private enum ClauseKind
        {
            None,
            With,
            Select,
            From,
            Where,
            GroupBy,
            Having,
            OrderBy,
            Option
        }

        private static readonly HashSet<string> JoinStartKeywords = new(StringComparer.Ordinal)
        {
            "INNER",
            "LEFT",
            "RIGHT",
            "FULL",
            "CROSS",
            "OUTER",
            "JOIN",
            "APPLY"
        };

        private static readonly HashSet<string> JoinPrefixKeywords = new(StringComparer.Ordinal)
        {
            "INNER",
            "LEFT",
            "RIGHT",
            "FULL",
            "CROSS",
            "OUTER"
        };

        private static readonly HashSet<string> TokensWithoutLeadingSpace = new(StringComparer.Ordinal)
        {
            ",",
            ")",
            ";",
            "."
        };

        private static readonly HashSet<string> TokensWithoutTrailingSpace = new(StringComparer.Ordinal)
        {
            "(",
            "."
        };

        private static readonly HashSet<string> BinaryOperatorTokens = new(StringComparer.Ordinal)
        {
            "=",
            "<>",
            "!=",
            "<",
            "<=",
            ">",
            ">=",
            "AND",
            "OR",
            "LIKE",
            "IN",
            "IS"
        };

        private static readonly HashSet<string> AllLogicalOperators = new(StringComparer.Ordinal)
        {
            "AND",
            "OR"
        };

        private static readonly HashSet<string> AndLogicalOperators = new(StringComparer.Ordinal)
        {
            "AND"
        };

        private static readonly HashSet<string> OrLogicalOperators = new(StringComparer.Ordinal)
        {
            "OR"
        };

        private readonly SqlFormattingOptions _options;
        private readonly IndentedSqlTextWriter _writer;
        private readonly Stack<string> _nonTerminalNameStack = new();

        private string? _previousToken;
        private string? _tokenBeforePrevious;
        private bool _previousTokenWasKeyword;
        private ClauseKind _currentClause = ClauseKind.None;
        private ClauseKind? _lastMajorClause;

        public SqlFormattingVisitor(SqlFormattingOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            var indentUnit = new string(' ', Math.Max(1, _options.Layout.IndentSize));
            _writer = new IndentedSqlTextWriter(new StringBuilder(), indentUnit);
        }

        public string GetFormattedSql()
        {
            var formattedSql = _writer.ToString();
            if (_options.Statement.TerminateWithSemicolon == SqlStatementTerminationMode.Always &&
                !string.IsNullOrWhiteSpace(formattedSql) &&
                !formattedSql.TrimEnd().EndsWith(";", StringComparison.Ordinal))
            {
                var semicolonPrefix = _options.Spaces.BeforeSemicolon ? " ;" : ";";
                formattedSql = $"{formattedSql.TrimEnd()}{semicolonPrefix}";
            }

            if (_options.Eof.Newline && !string.IsNullOrEmpty(formattedSql))
            {
                formattedSql = $"{formattedSql}{Environment.NewLine}";
            }

            return formattedSql;
        }

        protected override void VisitNonTerminal(NonTerminalNode node)
        {
            var nonTerminalName = node.NonTerminal.Name;
            _nonTerminalNameStack.Push(nonTerminalName);
            try
            {
                if (TryWriteCreateProcStatement(node, nonTerminalName))
                {
                    return;
                }

                if (TryWriteUpdateStatement(node, nonTerminalName))
                {
                    return;
                }

                if (TryWriteInsertStatement(node, nonTerminalName))
                {
                    return;
                }

                if (TryWriteShortQueryExpression(node, nonTerminalName))
                {
                    return;
                }

                if (TryWriteSubqueryQueryPrimary(node, nonTerminalName))
                {
                    return;
                }

                if (TryWriteCaseExpression(node, nonTerminalName))
                {
                    return;
                }

                if (TryWriteStructuredList(node, nonTerminalName))
                {
                    return;
                }

                if (TryWriteSearchCondition(node, nonTerminalName))
                {
                    return;
                }

                VisitChildren(node);
            }
            finally
            {
                _nonTerminalNameStack.Pop();
            }
        }

        protected override void VisitTerminal(TerminalNode node)
        {
            var rawToken = node.Token.OriginalString;
            if (string.IsNullOrEmpty(rawToken))
            {
                return;
            }

            var isKeyword = IsKeywordToken(node.Token);
            var tokenForRules = isKeyword
                ? rawToken.ToUpperInvariant()
                : rawToken;
            WriteTriviaComments(node.Token.Trivia.LeadingTrivia);

            if (string.Equals(tokenForRules, "GO", StringComparison.Ordinal))
            {
                if (_writer.HasContent && !_writer.IsLineStart)
                {
                    _writer.WriteLine();
                }

                _writer.WriteToken(FormatToken(rawToken, tokenForRules, isKeyword));
                WriteTriviaComments(node.Token.Trivia.TrailingTrivia);
                HandleStatementBoundary();
                return;
            }

            if (string.Equals(tokenForRules, ";", StringComparison.Ordinal) &&
                _options.Statement.TerminateWithSemicolon == SqlStatementTerminationMode.Never)
            {
                HandleStatementBoundary();
                return;
            }

            var clauseKind = GetClauseKindForToken(tokenForRules);
            if (ShouldStartNewLineBefore(tokenForRules, clauseKind))
            {
                _writer.WriteLine();
                if (ShouldInsertBlankLine(clauseKind))
                {
                    _writer.WriteLine();
                }
            }

            if (string.Equals(tokenForRules, "ON", StringComparison.Ordinal) &&
                _options.Joins.OnNewLine &&
                _writer.IsLineStart)
            {
                _writer.SetNextLineIndentOffset(1);
            }

            if (ShouldWriteSpaceBefore(tokenForRules))
            {
                _writer.WriteSpace();
            }

            _writer.WriteToken(FormatToken(rawToken, tokenForRules, isKeyword));
            WriteTriviaComments(node.Token.Trivia.TrailingTrivia);

            if (string.Equals(tokenForRules, ";", StringComparison.Ordinal))
            {
                HandleStatementBoundary();
                return;
            }

            if (IsMajorClause(clauseKind))
            {
                _lastMajorClause = clauseKind;
            }

            if (clauseKind != ClauseKind.None)
            {
                _currentClause = clauseKind;
            }

            _tokenBeforePrevious = _previousToken;
            _previousToken = tokenForRules;
            _previousTokenWasKeyword = isKeyword;
        }

        private bool TryWriteStructuredList(NonTerminalNode node, string nonTerminalName)
        {
            if (string.Equals(nonTerminalName, "SelectItemList", StringComparison.Ordinal))
            {
                WriteSelectItemList(node);
                return true;
            }

            if (string.Equals(nonTerminalName, "OrderItemList", StringComparison.Ordinal))
            {
                WriteOrderByItemList(node);
                return true;
            }

            if (string.Equals(nonTerminalName, "ExpressionList", StringComparison.Ordinal) &&
                _currentClause == ClauseKind.GroupBy &&
                string.Equals(_previousToken, "BY", StringComparison.Ordinal))
            {
                WriteGroupByExpressionList(node);
                return true;
            }

            if (string.Equals(nonTerminalName, "ExpressionList", StringComparison.Ordinal) &&
                string.Equals(_previousToken, "(", StringComparison.Ordinal) &&
                string.Equals(_tokenBeforePrevious, "IN", StringComparison.Ordinal))
            {
                WriteInListExpressionList(node);
                return true;
            }

            if (string.Equals(nonTerminalName, "UpdateSetList", StringComparison.Ordinal))
            {
                WriteUpdateSetList(node);
                return true;
            }

            if (string.Equals(nonTerminalName, "InsertColumnList", StringComparison.Ordinal))
            {
                WriteInsertColumnList(node);
                return true;
            }

            if (string.Equals(nonTerminalName, "InsertValueList", StringComparison.Ordinal))
            {
                WriteInsertValueList(node);
                return true;
            }

            return false;
        }

        private bool TryWriteCreateProcStatement(NonTerminalNode node, string nonTerminalName)
        {
            if (!string.Equals(nonTerminalName, "CreateProcStatement", StringComparison.Ordinal))
            {
                return false;
            }

            if (_options.Ddl.CreateProcLayout == SqlCreateProcLayout.Compact)
            {
                if (_writer.HasContent && !_writer.IsLineStart)
                {
                    _writer.WriteSpace();
                }

                _writer.WriteToken(RenderNodeInline(node));
                UpdatePreviousTokenFromNode(node);
                return true;
            }

            if (_writer.HasContent && !_writer.IsLineStart)
            {
                _writer.WriteSpace();
            }

            if (TryWriteExpandedCreateProcStructured(node))
            {
                UpdatePreviousTokenFromNode(node);
                return true;
            }

            if (TryWriteExpandedCreateProcLegacy(node))
            {
                UpdatePreviousTokenFromNode(node);
                return true;
            }

            _writer.WriteToken(RenderNodeInline(node));
            UpdatePreviousTokenFromNode(node);
            return true;
        }

        private bool TryWriteExpandedCreateProcStructured(NonTerminalNode node)
        {
            if (node.Children.Count != 3 ||
                node.Children[2] is not NonTerminalNode createProcBodyNode)
            {
                return false;
            }

            var createHeadText = RenderNodeInline(node.Children[0]);
            var signatureText = RenderNodeInline(node.Children[1]);
            _writer.WriteToken($"{createHeadText} {signatureText}");

            if (TryWriteExpandedCreateProcBody(createProcBodyNode))
            {
                return true;
            }

            _writer.WriteLine();
            _writer.WriteToken(RenderNodeInline(createProcBodyNode));
            return true;
        }

        private bool TryWriteExpandedCreateProcLegacy(NonTerminalNode node)
        {
            if (node.Children.Count < 7)
            {
                return false;
            }

            var createText = RenderNodeInline(node.Children[0]);
            var procKeywordText = RenderNodeInline(node.Children[1]);
            var procNameText = RenderNodeInline(node.Children[2]);
            var asKeywordText = RenderNodeInline(node.Children[3]);
            var beginKeywordText = RenderNodeInline(node.Children[4]);
            var procStatementsNode = node.Children[5];
            var endKeywordText = RenderNodeInline(node.Children[6]);

            _writer.WriteToken($"{createText} {procKeywordText} {procNameText}");
            _writer.WriteLine();
            _writer.WriteToken(asKeywordText);
            _writer.WriteLine();
            _writer.WriteToken(beginKeywordText);
            _writer.WriteLine();

            using (_writer.PushIndent())
            {
                Visit(procStatementsNode);
            }

            if (!_writer.IsLineStart)
            {
                _writer.WriteLine();
            }

            _writer.WriteToken(endKeywordText);
            return true;
        }

        private bool TryWriteExpandedCreateProcBody(NonTerminalNode createProcBodyNode)
        {
            if (createProcBodyNode.Children.Count != 2 ||
                !TryGetTerminalText(createProcBodyNode.Children[0], out var asKeywordText) ||
                createProcBodyNode.Children[1] is not NonTerminalNode createProcBodyBlockNode)
            {
                return false;
            }

            _writer.WriteLine();
            _writer.WriteToken(asKeywordText);

            if (!TryExtractBeginEndProcBlock(createProcBodyBlockNode, out var beginKeywordText, out var procStatementsNode, out var endKeywordText))
            {
                _writer.WriteLine();
                using (_writer.PushIndent())
                {
                    Visit(createProcBodyBlockNode);
                }

                return true;
            }

            _writer.WriteLine();
            _writer.WriteToken(beginKeywordText);
            _writer.WriteLine();
            using (_writer.PushIndent())
            {
                Visit(procStatementsNode);
            }

            if (!_writer.IsLineStart)
            {
                _writer.WriteLine();
            }

            _writer.WriteToken(endKeywordText);
            return true;
        }

        private static bool TryExtractBeginEndProcBlock(
            NonTerminalNode createProcBodyBlockNode,
            out string beginKeywordText,
            out ParseTreeNode procStatementsNode,
            out string endKeywordText)
        {
            beginKeywordText = string.Empty;
            endKeywordText = string.Empty;
            procStatementsNode = null!;

            if (!string.Equals(createProcBodyBlockNode.NonTerminal.Name, "CreateProcBodyBlock", StringComparison.Ordinal) ||
                createProcBodyBlockNode.Children.Count < 3)
            {
                return false;
            }

            if (!TryGetTerminalText(createProcBodyBlockNode.Children[0], out beginKeywordText) ||
                !string.Equals(beginKeywordText, "BEGIN", StringComparison.OrdinalIgnoreCase) ||
                !TryGetTerminalText(createProcBodyBlockNode.Children[^1], out endKeywordText) ||
                !string.Equals(endKeywordText, "END", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            procStatementsNode = createProcBodyBlockNode.Children[1];
            return true;
        }

        private static bool TryGetTerminalText(ParseTreeNode node, out string text)
        {
            text = string.Empty;
            if (node is not TerminalNode terminalNode || string.IsNullOrEmpty(terminalNode.Token.OriginalString))
            {
                return false;
            }

            text = terminalNode.Token.OriginalString;
            return true;
        }

        private bool TryWriteUpdateStatement(NonTerminalNode node, string nonTerminalName)
        {
            if (!string.Equals(nonTerminalName, "UpdateStatement", StringComparison.Ordinal))
            {
                return false;
            }

            if (node.Children.Count < 4)
            {
                return false;
            }

            if (_writer.HasContent && !_writer.IsLineStart)
            {
                _writer.WriteLine();
            }

            var updateKeyword = FormatToken("UPDATE", "UPDATE", isKeyword: true);
            var setKeyword = FormatToken("SET", "SET", isKeyword: true);
            var tableText = RenderNodeInline(node.Children[1]);
            var setListNode = node.Children[3];

            _writer.WriteToken($"{updateKeyword} {tableText}");
            _writer.WriteLine();
            _writer.WriteToken(setKeyword);
            UpdatePreviousToken(CreateKeywordToken("SET"));
            Visit(setListNode);

            if (node.Children.Count >= 6)
            {
                var whereKeyword = FormatToken("WHERE", "WHERE", isKeyword: true);
                _writer.WriteLine();
                _writer.WriteToken(whereKeyword);
                UpdatePreviousToken(CreateKeywordToken("WHERE"));
                Visit(node.Children[5]);
            }

            UpdatePreviousTokenFromNode(node);
            return true;
        }

        private bool TryWriteInsertStatement(NonTerminalNode node, string nonTerminalName)
        {
            if (!string.Equals(nonTerminalName, "InsertStatement", StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryGetInsertValuesStatementParts(
                    node,
                    out var insertTargetNode,
                    out var dmlOutputClauseNode,
                    out var rowValueListNode) ||
                !TryExtractInsertTargetLayoutParts(
                    insertTargetNode,
                    out var targetPrefixText,
                    out var columnListNode,
                    out var targetSuffixText) ||
                !TryExtractInsertRowValues(rowValueListNode, out var valueListNodes))
            {
                return false;
            }

            if (_writer.HasContent && !_writer.IsLineStart)
            {
                _writer.WriteLine();
            }

            var insertKeyword = FormatToken("INSERT", "INSERT", isKeyword: true);
            var valuesKeyword = FormatToken("VALUES", "VALUES", isKeyword: true);
            var multilineColumns = false;
            if (columnListNode != null)
            {
                var columnItemNodes = ExtractDelimitedListItems(columnListNode, "InsertColumnList");
                var columnItemTexts = columnItemNodes.Select(RenderNodeInline).ToList();
                multilineColumns = ShouldUseMultilineDmlList(_options.Dml.InsertColumnsStyle, columnItemTexts, "(".Length);
            }

            var rowValueLayoutInfos = valueListNodes
                .Select(valueListNode =>
                {
                    var valueItemNodes = ExtractDelimitedListItems(valueListNode, "InsertValueList");
                    var valueItemTexts = valueItemNodes.Select(RenderNodeInline).ToList();
                    var isMultiline = ShouldUseMultilineDmlList(_options.Dml.InsertValuesStyle, valueItemTexts, "(".Length);
                    return new InsertRowValueLayoutInfo(valueListNode, isMultiline);
                })
                .ToList();
            var useMultilineValues = rowValueLayoutInfos.Any(layoutInfo => layoutInfo.IsMultiline);
            var useMultilineRowLayout = rowValueLayoutInfos.Count > 1 &&
                (_options.Dml.InsertValuesStyle == SqlDmlListStyle.OnePerLine ||
                 _options.Dml.InsertValuesStyle == SqlDmlListStyle.RowsPerLine ||
                 useMultilineValues);

            _writer.WriteToken($"{insertKeyword} {targetPrefixText}");
            if (columnListNode != null)
            {
                if (_options.Dml.InsertColumnsStartOnNewLine)
                {
                    _writer.WriteLine();
                }
                else
                {
                    _writer.WriteSpace();
                }

                WriteParenthesizedList(columnListNode, multilineColumns);
            }

            if (!string.IsNullOrWhiteSpace(targetSuffixText))
            {
                _writer.WriteSpace();
                _writer.WriteToken(targetSuffixText);
            }

            if (dmlOutputClauseNode != null)
            {
                _writer.WriteSpace();
                _writer.WriteToken(RenderNodeInline(dmlOutputClauseNode));
            }

            if (multilineColumns || useMultilineValues || useMultilineRowLayout)
            {
                _writer.WriteLine();
            }
            else
            {
                _writer.WriteSpace();
            }

            _writer.WriteToken(valuesKeyword);
            WriteInsertRowValueList(rowValueLayoutInfos, useMultilineRowLayout);

            UpdatePreviousTokenFromNode(node);
            return true;
        }

        private static bool TryGetInsertValuesStatementParts(
            NonTerminalNode insertStatementNode,
            out NonTerminalNode insertTargetNode,
            out NonTerminalNode? dmlOutputClauseNode,
            out NonTerminalNode rowValueListNode)
        {
            insertTargetNode = null!;
            dmlOutputClauseNode = null;
            rowValueListNode = null!;

            if (insertStatementNode.Children.Count == 4 &&
                insertStatementNode.Children[1] is NonTerminalNode simpleInsertTargetNode &&
                insertStatementNode.Children[2] is TerminalNode simpleValuesTerminal &&
                string.Equals(simpleValuesTerminal.Token.OriginalString, "VALUES", StringComparison.OrdinalIgnoreCase) &&
                insertStatementNode.Children[3] is NonTerminalNode simpleRowValueListNode &&
                string.Equals(simpleRowValueListNode.NonTerminal.Name, "RowValueList", StringComparison.Ordinal))
            {
                insertTargetNode = simpleInsertTargetNode;
                rowValueListNode = simpleRowValueListNode;
                return true;
            }

            if (insertStatementNode.Children.Count == 5 &&
                insertStatementNode.Children[1] is NonTerminalNode outputInsertTargetNode &&
                insertStatementNode.Children[2] is NonTerminalNode outputClauseNode &&
                string.Equals(outputClauseNode.NonTerminal.Name, "DeleteOutputClause", StringComparison.Ordinal) &&
                insertStatementNode.Children[3] is TerminalNode outputValuesTerminal &&
                string.Equals(outputValuesTerminal.Token.OriginalString, "VALUES", StringComparison.OrdinalIgnoreCase) &&
                insertStatementNode.Children[4] is NonTerminalNode outputRowValueListNode &&
                string.Equals(outputRowValueListNode.NonTerminal.Name, "RowValueList", StringComparison.Ordinal))
            {
                insertTargetNode = outputInsertTargetNode;
                dmlOutputClauseNode = outputClauseNode;
                rowValueListNode = outputRowValueListNode;
                return true;
            }

            return false;
        }

        private bool TryExtractInsertTargetLayoutParts(
            NonTerminalNode insertTargetNode,
            out string targetPrefixText,
            out NonTerminalNode? columnListNode,
            out string targetSuffixText)
        {
            targetPrefixText = RenderNodeInline(insertTargetNode);
            columnListNode = null;
            targetSuffixText = string.Empty;

            var columnListIndex = -1;
            for (var childIndex = 0; childIndex < insertTargetNode.Children.Count; childIndex++)
            {
                if (insertTargetNode.Children[childIndex] is NonTerminalNode candidateColumnListNode &&
                    string.Equals(candidateColumnListNode.NonTerminal.Name, "InsertColumnList", StringComparison.Ordinal))
                {
                    columnListIndex = childIndex;
                    columnListNode = candidateColumnListNode;
                    break;
                }
            }

            if (columnListIndex < 0)
            {
                return true;
            }

            if (columnListIndex == 0 ||
                columnListIndex >= insertTargetNode.Children.Count - 1 ||
                !TryGetTerminalText(insertTargetNode.Children[columnListIndex - 1], out var openParenthesis) ||
                !string.Equals(openParenthesis, "(", StringComparison.Ordinal) ||
                !TryGetTerminalText(insertTargetNode.Children[columnListIndex + 1], out var closeParenthesis) ||
                !string.Equals(closeParenthesis, ")", StringComparison.Ordinal))
            {
                columnListNode = null;
                targetSuffixText = string.Empty;
                targetPrefixText = RenderNodeInline(insertTargetNode);
                return false;
            }

            targetPrefixText = RenderNodesInline(insertTargetNode.Children.Take(columnListIndex - 1));
            targetSuffixText = RenderNodesInline(insertTargetNode.Children.Skip(columnListIndex + 2));
            return true;
        }

        private static bool TryExtractInsertRowValues(
            NonTerminalNode rowValueListNode,
            out IReadOnlyList<NonTerminalNode> valueListNodes)
        {
            var extractedValueListNodes = new List<NonTerminalNode>();
            foreach (var rowValueNodeCandidate in ExtractDelimitedListItems(rowValueListNode, "RowValueList"))
            {
                if (rowValueNodeCandidate is not NonTerminalNode rowValueNode ||
                    !string.Equals(rowValueNode.NonTerminal.Name, "RowValue", StringComparison.Ordinal) ||
                    rowValueNode.Children.Count != 3 ||
                    rowValueNode.Children[0] is not TerminalNode openTerminal ||
                    !string.Equals(openTerminal.Token.OriginalString, "(", StringComparison.Ordinal) ||
                    rowValueNode.Children[1] is not NonTerminalNode insertValueListNode ||
                    !string.Equals(insertValueListNode.NonTerminal.Name, "InsertValueList", StringComparison.Ordinal) ||
                    rowValueNode.Children[2] is not TerminalNode closeTerminal ||
                    !string.Equals(closeTerminal.Token.OriginalString, ")", StringComparison.Ordinal))
                {
                    valueListNodes = [];
                    return false;
                }

                extractedValueListNodes.Add(insertValueListNode);
            }

            valueListNodes = extractedValueListNodes;
            return true;
        }

        private void WriteInsertRowValueList(
            IReadOnlyList<InsertRowValueLayoutInfo> rowValueLayoutInfos,
            bool useMultilineRowLayout)
        {
            if (!useMultilineRowLayout)
            {
                for (var rowIndex = 0; rowIndex < rowValueLayoutInfos.Count; rowIndex++)
                {
                    _writer.WriteSpace();
                    WriteParenthesizedList(rowValueLayoutInfos[rowIndex].ValueListNode, rowValueLayoutInfos[rowIndex].IsMultiline);
                    if (rowIndex >= rowValueLayoutInfos.Count - 1)
                    {
                        continue;
                    }

                    _writer.WriteToken(",");
                }

                return;
            }

            _writer.WriteLine();
            using (_writer.PushIndent())
            {
                for (var rowIndex = 0; rowIndex < rowValueLayoutInfos.Count; rowIndex++)
                {
                    _writer.WriteSpace();
                    WriteParenthesizedList(rowValueLayoutInfos[rowIndex].ValueListNode, rowValueLayoutInfos[rowIndex].IsMultiline);
                    if (rowIndex >= rowValueLayoutInfos.Count - 1)
                    {
                        continue;
                    }

                    _writer.WriteToken(",");
                    _writer.WriteLine();
                }
            }
        }

        private void WriteParenthesizedList(NonTerminalNode listNode, bool multiline)
        {
            _writer.WriteToken("(");
            UpdatePreviousToken(CreateSymbolToken("("));
            Visit(listNode);
            if (multiline && !_writer.IsLineStart)
            {
                _writer.WriteLine();
            }

            _writer.WriteToken(")");
            UpdatePreviousToken(CreateSymbolToken(")"));
        }

        private bool TryWriteShortQueryExpression(NonTerminalNode node, string nonTerminalName)
        {
            var isSupportedNode = string.Equals(nonTerminalName, "QueryExpression", StringComparison.Ordinal) ||
                string.Equals(nonTerminalName, "ImplicitQueryExpression", StringComparison.Ordinal);
            if (!isSupportedNode)
            {
                return false;
            }

            if (IsParenthesizedShortQueryContext() && !_options.ShortQueries.ApplyToParenthesizedSubqueries)
            {
                return false;
            }

            if (!TryGetShortQueryInlineText(node, out var inlineText))
            {
                return false;
            }

            if (_writer.HasContent && !_writer.IsLineStart && ShouldWriteSpaceBefore("SELECT"))
            {
                _writer.WriteSpace();
            }

            _writer.WriteToken(inlineText);
            UpdatePreviousTokenFromNode(node);
            return true;
        }

        private bool TryWriteSubqueryQueryPrimary(NonTerminalNode node, string nonTerminalName)
        {
            var isSupportedNode = string.Equals(nonTerminalName, "QueryPrimary", StringComparison.Ordinal) ||
                string.Equals(nonTerminalName, "PrimaryExpression", StringComparison.Ordinal) ||
                string.Equals(nonTerminalName, "TableFactor", StringComparison.Ordinal);
            if (!isSupportedNode)
            {
                return false;
            }

            if (node.Children.Count < 3 ||
                node.Children[0] is not TerminalNode openTerminal ||
                !string.Equals(openTerminal.Token.OriginalString, "(", StringComparison.Ordinal) ||
                node.Children[1] is not NonTerminalNode queryExpressionNode ||
                !string.Equals(queryExpressionNode.NonTerminal.Name, "QueryExpression", StringComparison.Ordinal) ||
                node.Children[2] is not TerminalNode closeTerminal ||
                !string.Equals(closeTerminal.Token.OriginalString, ")", StringComparison.Ordinal))
            {
                return false;
            }

            if (_writer.HasContent && !_writer.IsLineStart && ShouldWriteSpaceBefore("("))
            {
                _writer.WriteSpace();
            }

            if (_options.ShortQueries.ApplyToParenthesizedSubqueries &&
                TryGetShortQueryInlineText(queryExpressionNode, out var inlineText))
            {
                _writer.WriteToken("(");
                _writer.WriteToken(inlineText);
                _writer.WriteToken(")");
                UpdatePreviousToken(CreateSymbolToken(")"));

                for (var childIndex = 3; childIndex < node.Children.Count; childIndex++)
                {
                    Visit(node.Children[childIndex]);
                }

                return true;
            }

            _writer.WriteToken("(");
            _writer.WriteLine();
            using (_writer.PushIndent())
            {
                Visit(queryExpressionNode);
            }

            if (!_writer.IsLineStart)
            {
                _writer.WriteLine();
            }

            _writer.WriteToken(")");
            UpdatePreviousToken(CreateSymbolToken(")"));

            for (var childIndex = 3; childIndex < node.Children.Count; childIndex++)
            {
                Visit(node.Children[childIndex]);
            }

            return true;
        }

        private bool TryGetShortQueryInlineText(NonTerminalNode queryExpressionNode, out string inlineText)
        {
            inlineText = string.Empty;
            if (!_options.ShortQueries.Enabled)
            {
                return false;
            }

            if (!TryGetShortQuerySpecificationNode(queryExpressionNode, out var querySpecificationNode))
            {
                return false;
            }

            if (!TryFindFirstNonTerminal(querySpecificationNode, "SelectItemList", out var selectItemListNode))
            {
                return false;
            }

            if (ContainsComment(queryExpressionNode) ||
                ContainsDescendantNonTerminal(querySpecificationNode, "QuerySpecificationGroupByClause") ||
                ContainsDescendantNonTerminal(querySpecificationNode, "QueryExpression") ||
                ContainsDescendantNonTerminal(querySpecificationNode, "ImplicitQueryExpression"))
            {
                return false;
            }

            var maxSelectItems = Math.Max(1, _options.ShortQueries.MaxSelectItems);
            var selectItemCount = ExtractDelimitedListItems(selectItemListNode, "SelectItemList").Count;
            if (selectItemCount > maxSelectItems)
            {
                return false;
            }

            var joinCount = CountJoinClauses(querySpecificationNode);
            if (joinCount > 0 && (!_options.ShortQueries.AllowSingleJoin || joinCount > 1))
            {
                return false;
            }

            if (TryGetWhereSearchCondition(querySpecificationNode, out var whereSearchConditionNode))
            {
                var maxPredicateConditions = Math.Max(1, _options.ShortQueries.MaxPredicateConditions);
                if (CountPredicateConditions(whereSearchConditionNode) > maxPredicateConditions)
                {
                    return false;
                }
            }

            inlineText = RenderNodeInline(queryExpressionNode);
            return inlineText.Length <= Math.Max(10, _options.ShortQueries.MaxLineLength);
        }

        private bool TryGetShortQuerySpecificationNode(
            NonTerminalNode queryExpressionNode,
            out NonTerminalNode querySpecificationNode)
        {
            querySpecificationNode = null!;
            if (string.Equals(queryExpressionNode.NonTerminal.Name, "QueryExpression", StringComparison.Ordinal))
            {
                return TryGetExplicitShortQuerySpecificationNode(queryExpressionNode, out querySpecificationNode);
            }

            if (string.Equals(queryExpressionNode.NonTerminal.Name, "ImplicitQueryExpression", StringComparison.Ordinal))
            {
                return TryGetImplicitShortQuerySpecificationNode(queryExpressionNode, out querySpecificationNode);
            }

            return false;
        }

        private static bool TryGetExplicitShortQuerySpecificationNode(
            NonTerminalNode queryExpressionNode,
            out NonTerminalNode querySpecificationNode)
        {
            querySpecificationNode = null!;
            if (queryExpressionNode.Children.Count != 2 ||
                queryExpressionNode.Children[0] is not NonTerminalNode queryUnionNode ||
                queryExpressionNode.Children[1] is not NonTerminalNode queryExpressionTailNode ||
                HasTerminalNodes(queryExpressionTailNode))
            {
                return false;
            }

            if (queryUnionNode.Children.Count != 1 ||
                queryUnionNode.Children[0] is not NonTerminalNode queryIntersectNode ||
                queryIntersectNode.Children.Count != 1 ||
                queryIntersectNode.Children[0] is not NonTerminalNode queryPrimaryNode)
            {
                return false;
            }

            return TryGetQuerySpecificationFromQueryPrimary(queryPrimaryNode, out querySpecificationNode);
        }

        private static bool TryGetImplicitShortQuerySpecificationNode(
            NonTerminalNode queryExpressionNode,
            out NonTerminalNode querySpecificationNode)
        {
            querySpecificationNode = null!;
            if (queryExpressionNode.Children.Count != 2 ||
                queryExpressionNode.Children[0] is not NonTerminalNode queryUnionNode ||
                queryExpressionNode.Children[1] is not NonTerminalNode queryExpressionTailNode ||
                HasTerminalNodes(queryExpressionTailNode))
            {
                return false;
            }

            if (queryUnionNode.Children.Count != 1 ||
                queryUnionNode.Children[0] is not NonTerminalNode queryIntersectNode ||
                queryIntersectNode.Children.Count != 1 ||
                queryIntersectNode.Children[0] is not NonTerminalNode querySpecificationNodeCandidate ||
                !string.Equals(querySpecificationNodeCandidate.NonTerminal.Name, "QuerySpecification", StringComparison.Ordinal))
            {
                return false;
            }

            querySpecificationNode = querySpecificationNodeCandidate;
            return true;
        }

        private static bool TryGetQuerySpecificationFromQueryPrimary(
            NonTerminalNode queryPrimaryNode,
            out NonTerminalNode querySpecificationNode)
        {
            querySpecificationNode = null!;
            if (queryPrimaryNode.Children.Count != 1 ||
                queryPrimaryNode.Children[0] is not NonTerminalNode querySpecificationNodeCandidate ||
                !string.Equals(querySpecificationNodeCandidate.NonTerminal.Name, "QuerySpecification", StringComparison.Ordinal))
            {
                return false;
            }

            querySpecificationNode = querySpecificationNodeCandidate;
            return true;
        }

        private static bool TryFindFirstNonTerminal(
            ParseTreeNode node,
            string nonTerminalName,
            out NonTerminalNode result)
        {
            result = null!;
            if (node is NonTerminalNode nonTerminalNode &&
                string.Equals(nonTerminalNode.NonTerminal.Name, nonTerminalName, StringComparison.Ordinal))
            {
                result = nonTerminalNode;
                return true;
            }

            foreach (var childNode in node.Children)
            {
                if (TryFindFirstNonTerminal(childNode, nonTerminalName, out result))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsDescendantNonTerminal(ParseTreeNode node, string nonTerminalName)
        {
            foreach (var childNode in node.Children)
            {
                if (childNode is NonTerminalNode nonTerminalNode &&
                    (string.Equals(nonTerminalNode.NonTerminal.Name, nonTerminalName, StringComparison.Ordinal) ||
                    ContainsDescendantNonTerminal(nonTerminalNode, nonTerminalName)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasTerminalNodes(ParseTreeNode node)
        {
            if (node is TerminalNode)
            {
                return true;
            }

            foreach (var childNode in node.Children)
            {
                if (HasTerminalNodes(childNode))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsComment(ParseTreeNode node)
        {
            return CollectTokenInfos(node).Any(static tokenInfo => tokenInfo.IsComment);
        }

        private static int CountJoinClauses(ParseTreeNode node)
        {
            var joinCount = 0;
            foreach (var terminalNode in CollectTerminalNodes(node))
            {
                var rawToken = terminalNode.Token.OriginalString;
                if (string.Equals(rawToken, "JOIN", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rawToken, "APPLY", StringComparison.OrdinalIgnoreCase))
                {
                    joinCount++;
                }
            }

            return joinCount;
        }

        private static bool TryGetWhereSearchCondition(ParseTreeNode querySpecificationNode, out NonTerminalNode searchConditionNode)
        {
            searchConditionNode = null!;
            if (!TryFindFirstNonTerminal(querySpecificationNode, "QuerySpecificationWhereClause", out var whereClauseNode))
            {
                return false;
            }

            return TryFindFirstNonTerminal(whereClauseNode, "SearchCondition", out searchConditionNode);
        }

        private static int CountPredicateConditions(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminalNode)
            {
                return 0;
            }

            return nonTerminalNode.NonTerminal.Name switch
            {
                "SearchCondition" => nonTerminalNode.Children.Count == 1
                    ? CountPredicateConditions(nonTerminalNode.Children[0])
                    : 1,
                "BooleanOrExpression" => CountLogicalPredicateConditions(nonTerminalNode, "OR"),
                "BooleanAndExpression" => CountLogicalPredicateConditions(nonTerminalNode, "AND"),
                "BooleanNotExpression" => CountBooleanNotConditions(nonTerminalNode),
                "BooleanPrimary" => CountBooleanPrimaryConditions(nonTerminalNode),
                _ => nonTerminalNode.Children.Count == 1
                    ? CountPredicateConditions(nonTerminalNode.Children[0])
                    : 1
            };
        }

        private static int CountLogicalPredicateConditions(NonTerminalNode logicalExpressionNode, string logicalOperator)
        {
            if (logicalExpressionNode.Children.Count == 3 &&
                TryGetTerminalText(logicalExpressionNode.Children[1], out var operatorText) &&
                string.Equals(operatorText, logicalOperator, StringComparison.OrdinalIgnoreCase))
            {
                return CountPredicateConditions(logicalExpressionNode.Children[0]) +
                    CountPredicateConditions(logicalExpressionNode.Children[2]);
            }

            return logicalExpressionNode.Children.Count == 1
                ? CountPredicateConditions(logicalExpressionNode.Children[0])
                : 1;
        }

        private static int CountBooleanNotConditions(NonTerminalNode booleanNotExpressionNode)
        {
            if (booleanNotExpressionNode.Children.Count == 2 &&
                TryGetTerminalText(booleanNotExpressionNode.Children[0], out var notText) &&
                string.Equals(notText, "NOT", StringComparison.OrdinalIgnoreCase))
            {
                return CountPredicateConditions(booleanNotExpressionNode.Children[1]);
            }

            return booleanNotExpressionNode.Children.Count == 1
                ? CountPredicateConditions(booleanNotExpressionNode.Children[0])
                : 1;
        }

        private static int CountBooleanPrimaryConditions(NonTerminalNode booleanPrimaryNode)
        {
            if (booleanPrimaryNode.Children.Count == 3 &&
                TryGetTerminalText(booleanPrimaryNode.Children[0], out var openText) &&
                string.Equals(openText, "(", StringComparison.Ordinal) &&
                booleanPrimaryNode.Children[1] is NonTerminalNode nestedSearchConditionNode &&
                string.Equals(nestedSearchConditionNode.NonTerminal.Name, "SearchCondition", StringComparison.Ordinal))
            {
                return CountPredicateConditions(nestedSearchConditionNode);
            }

            return 1;
        }

        private bool IsParenthesizedShortQueryContext()
        {
            var parentNonTerminalName = GetParentNonTerminalName();
            return string.Equals(parentNonTerminalName, "BooleanPrimary", StringComparison.Ordinal) ||
                string.Equals(parentNonTerminalName, "CteDefinition", StringComparison.Ordinal) ||
                string.Equals(parentNonTerminalName, "InPredicateValue", StringComparison.Ordinal) ||
                string.Equals(parentNonTerminalName, "PrimaryExpression", StringComparison.Ordinal) ||
                string.Equals(parentNonTerminalName, "QueryPrimary", StringComparison.Ordinal) ||
                string.Equals(parentNonTerminalName, "TableFactor", StringComparison.Ordinal);
        }

        private string? GetParentNonTerminalName()
        {
            return _nonTerminalNameStack.Skip(1).FirstOrDefault();
        }

        private bool TryWriteCaseExpression(NonTerminalNode node, string nonTerminalName)
        {
            if (!string.Equals(nonTerminalName, "CaseExpression", StringComparison.Ordinal))
            {
                return false;
            }

            var tokenInfos = CollectTokenInfos(node);
            var inlineCaseText = RenderTokensInline(tokenInfos);
            var whenCount = CountCaseWhenClauses(node);
            if (ShouldUseCompactCaseLayout(whenCount, tokenInfos.Count, inlineCaseText.Length))
            {
                if (_writer.HasContent && !_writer.IsLineStart && ShouldWriteSpaceBefore("CASE"))
                {
                    _writer.WriteSpace();
                }

                _writer.WriteToken(inlineCaseText);
                UpdatePreviousToken(tokenInfos[^1]);
                return true;
            }

            WriteCaseExpressionMultiline(node);
            return true;
        }

        private bool ShouldUseCompactCaseLayout(int whenCount, int tokenCount, int lineLength)
        {
            if (_options.Expressions.CaseStyle == SqlCaseStyle.Multiline)
            {
                return false;
            }

            var threshold = _options.Expressions.CompactCaseThreshold;
            if (threshold.MaxWhenClauses > 0 && whenCount > threshold.MaxWhenClauses)
            {
                return false;
            }

            if (threshold.MaxTokens > 0 && tokenCount > threshold.MaxTokens)
            {
                return false;
            }

            return lineLength <= Math.Max(1, threshold.MaxLineLength);
        }

        private static int CountCaseWhenClauses(NonTerminalNode caseExpressionNode)
        {
            var caseWhenListNode = caseExpressionNode.Children
                .OfType<NonTerminalNode>()
                .FirstOrDefault(nonTerminalNode =>
                    string.Equals(nonTerminalNode.NonTerminal.Name, "CaseWhenList", StringComparison.Ordinal));
            if (caseWhenListNode == null)
            {
                return 0;
            }

            return ExtractCaseWhenNodes(caseWhenListNode).Count;
        }

        private static IReadOnlyList<NonTerminalNode> ExtractCaseWhenNodes(NonTerminalNode caseWhenListNode)
        {
            var nodes = new List<NonTerminalNode>();
            CollectCaseWhenNodes(caseWhenListNode, nodes);
            return nodes;
        }

        private static void CollectCaseWhenNodes(ParseTreeNode node, List<NonTerminalNode> output)
        {
            if (node is not NonTerminalNode nonTerminalNode)
            {
                return;
            }

            if (string.Equals(nonTerminalNode.NonTerminal.Name, "CaseWhen", StringComparison.Ordinal))
            {
                output.Add(nonTerminalNode);
                return;
            }

            if (!string.Equals(nonTerminalNode.NonTerminal.Name, "CaseWhenList", StringComparison.Ordinal))
            {
                return;
            }

            foreach (var child in nonTerminalNode.Children)
            {
                CollectCaseWhenNodes(child, output);
            }
        }

        private void WriteCaseExpressionMultiline(NonTerminalNode caseExpressionNode)
        {
            if (!TryExtractCaseComponents(caseExpressionNode, out var inputExpressionNode, out var caseWhenListNode, out var elseExpressionNode))
            {
                _writer.WriteToken(RenderNodeInline(caseExpressionNode));
                UpdatePreviousTokenFromNode(caseExpressionNode);
                return;
            }

            var caseKeyword = FormatToken("CASE", "CASE", isKeyword: true);
            var endKeyword = FormatToken("END", "END", isKeyword: true);

            if (_writer.HasContent && !_writer.IsLineStart)
            {
                _writer.WriteSpace();
            }

            _writer.WriteToken(caseKeyword);
            if (inputExpressionNode != null)
            {
                _writer.WriteSpace();
                _writer.WriteToken(RenderNodeInline(inputExpressionNode));
            }

            var caseWhenNodes = ExtractCaseWhenNodes(caseWhenListNode);
            if (caseWhenNodes.Count == 0)
            {
                _writer.WriteSpace();
                _writer.WriteToken(endKeyword);
                UpdatePreviousTokenFromNode(caseExpressionNode);
                return;
            }

            _writer.WriteLine();
            using (_writer.PushIndent())
            {
                for (var whenIndex = 0; whenIndex < caseWhenNodes.Count; whenIndex++)
                {
                    WriteCaseWhenLine(caseWhenNodes[whenIndex]);
                    if (whenIndex < caseWhenNodes.Count - 1 || elseExpressionNode != null)
                    {
                        _writer.WriteLine();
                    }
                }

                if (elseExpressionNode != null)
                {
                    var elseKeyword = FormatToken("ELSE", "ELSE", isKeyword: true);
                    _writer.WriteToken($"{elseKeyword} {RenderNodeInline(elseExpressionNode)}");
                }
            }

            if (!_writer.IsLineStart)
            {
                _writer.WriteLine();
            }

            _writer.WriteToken(endKeyword);
            UpdatePreviousTokenFromNode(caseExpressionNode);
        }

        private static bool TryExtractCaseComponents(
            NonTerminalNode caseExpressionNode,
            out ParseTreeNode? inputExpressionNode,
            out NonTerminalNode caseWhenListNode,
            out ParseTreeNode? elseExpressionNode)
        {
            inputExpressionNode = null;
            elseExpressionNode = null;
            caseWhenListNode = null!;

            if (caseExpressionNode.Children.Count < 3)
            {
                return false;
            }

            var caseWhenListIndex = -1;
            for (var childIndex = 0; childIndex < caseExpressionNode.Children.Count; childIndex++)
            {
                if (caseExpressionNode.Children[childIndex] is NonTerminalNode candidate &&
                    string.Equals(candidate.NonTerminal.Name, "CaseWhenList", StringComparison.Ordinal))
                {
                    caseWhenListNode = candidate;
                    caseWhenListIndex = childIndex;
                    break;
                }
            }

            if (caseWhenListIndex < 0)
            {
                return false;
            }

            if (caseWhenListIndex > 1)
            {
                inputExpressionNode = caseExpressionNode.Children[1];
            }

            if (caseWhenListIndex + 2 < caseExpressionNode.Children.Count &&
                caseExpressionNode.Children[caseWhenListIndex + 1] is TerminalNode elseTerminal &&
                string.Equals(elseTerminal.Token.OriginalString, "ELSE", StringComparison.OrdinalIgnoreCase))
            {
                elseExpressionNode = caseExpressionNode.Children[caseWhenListIndex + 2];
            }

            return true;
        }

        private void WriteCaseWhenLine(NonTerminalNode caseWhenNode)
        {
            if (caseWhenNode.Children.Count < 4)
            {
                _writer.WriteToken(RenderNodeInline(caseWhenNode));
                return;
            }

            var whenKeyword = FormatToken("WHEN", "WHEN", isKeyword: true);
            var thenKeyword = FormatToken("THEN", "THEN", isKeyword: true);
            var whenConditionText = RenderNodeInline(caseWhenNode.Children[1]);
            var whenResultText = RenderNodeInline(caseWhenNode.Children[3]);
            _writer.WriteToken($"{whenKeyword} {whenConditionText} {thenKeyword} {whenResultText}");
        }

        private bool ShouldInlineShortExpression(ParseTreeNode expressionNode, SqlInlineExpressionContext context, int clausePrefixLength)
        {
            var inlineOptions = _options.Expressions.InlineShortExpression;
            if (inlineOptions.MaxTokens <= 0 || !ShouldInlineShortExpressionContextEnabled(context, inlineOptions))
            {
                return false;
            }

            var tokenInfos = CollectTokenInfos(expressionNode);
            if (tokenInfos.Count == 0 || tokenInfos.Count > inlineOptions.MaxTokens)
            {
                return false;
            }

            if (inlineOptions.MaxDepth > 0 && CalculateNodeDepth(expressionNode) > inlineOptions.MaxDepth)
            {
                return false;
            }

            var inlineLength = clausePrefixLength + RenderTokensInline(tokenInfos).Length;
            return inlineLength <= Math.Max(1, inlineOptions.MaxLineLength);
        }

        private static bool ShouldInlineShortExpressionContextEnabled(
            SqlInlineExpressionContext context,
            SqlInlineShortExpressionOptions inlineOptions)
        {
            if (inlineOptions.ForContexts.Count == 0)
            {
                return false;
            }

            return inlineOptions.ForContexts.Contains(context);
        }

        private static int CalculateNodeDepth(ParseTreeNode node)
        {
            if (node.Children.Count == 0)
            {
                return 1;
            }

            var maxChildDepth = 0;
            foreach (var child in node.Children)
            {
                maxChildDepth = Math.Max(maxChildDepth, CalculateNodeDepth(child));
            }

            return maxChildDepth + 1;
        }

        private bool TryWriteSearchCondition(NonTerminalNode node, string nonTerminalName)
        {
            if (!string.Equals(nonTerminalName, "SearchCondition", StringComparison.Ordinal))
            {
                return false;
            }

            if (_previousToken == null)
            {
                return false;
            }

            var anchorKeyword = _previousToken;
            var isOnAnchor = string.Equals(anchorKeyword, "ON", StringComparison.Ordinal);
            var isWhereOrHavingAnchor = string.Equals(anchorKeyword, "WHERE", StringComparison.Ordinal) ||
                string.Equals(anchorKeyword, "HAVING", StringComparison.Ordinal);
            if (!isOnAnchor && !isWhereOrHavingAnchor)
            {
                return false;
            }

            if (ShouldDeferSearchConditionFormattingToChildNodes(node))
            {
                return false;
            }

            var shortQueryReplacements = isWhereOrHavingAnchor &&
                _options.ShortQueries.ApplyToParenthesizedSubqueries
                ? CollectShortQueryReplacements(node)
                : [];
            var tokenInfos = CollectTokenInfos(node);
            if (tokenInfos.Count == 0)
            {
                return true;
            }

            if (isWhereOrHavingAnchor)
            {
                var forceInlineByShortExpression = ShouldInlineShortExpression(
                    node,
                    SqlInlineExpressionContext.Where,
                    anchorKeyword.Length + 1);
                WriteWhereOrHavingPredicate(anchorKeyword, tokenInfos, shortQueryReplacements, forceInlineByShortExpression);
            }
            else
            {
                var forceInlineByShortExpression = ShouldInlineShortExpression(
                    node,
                    SqlInlineExpressionContext.On,
                    "ON ".Length);
                WriteOnPredicate(tokenInfos, forceInlineByShortExpression);
            }

            UpdatePreviousToken(tokenInfos[^1]);
            return true;
        }

        private bool ShouldDeferSearchConditionFormattingToChildNodes(ParseTreeNode searchConditionNode)
        {
            if (!ContainsInPredicateList(searchConditionNode))
            {
                return false;
            }

            if (_options.Lists.InListItems != SqlInListItemsStyle.Inline)
            {
                return true;
            }

            return _options.Lists.InlineInListThreshold.MaxItemsInline > 0;
        }

        private bool ContainsInPredicateList(ParseTreeNode searchConditionNode)
        {
            var tokenInfos = CollectTokenInfos(searchConditionNode);
            for (var tokenIndex = 0; tokenIndex < tokenInfos.Count - 1; tokenIndex++)
            {
                var currentToken = tokenInfos[tokenIndex];
                var nextToken = tokenInfos[tokenIndex + 1];
                if (string.Equals(currentToken.TokenForRules, "IN", StringComparison.Ordinal) &&
                    string.Equals(nextToken.TokenForRules, "(", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void WriteWhereOrHavingPredicate(
            string anchorKeyword,
            IReadOnlyList<SqlTokenInfo> tokenInfos,
            IReadOnlyList<ShortQueryReplacement> shortQueryReplacements,
            bool forceInlineByShortExpression)
        {
            var mixedAndOrOptions = _options.Predicates.MixedAndOrParentheses;
            var hasMixedTopLevelAndOr = TryGetMixedTopLevelOrSplit(tokenInfos, out var mixedOrSplit);
            var useMixedOrGrouping = hasMixedTopLevelAndOr &&
                (mixedAndOrOptions.ParenthesizeOrGroups || mixedAndOrOptions.BreakOrGroups);
            var shouldMultiline = _options.Predicates.MultilineWhere;
            if (shouldMultiline && ShouldInlineSimplePredicate(anchorKeyword, tokenInfos, shortQueryReplacements))
            {
                shouldMultiline = false;
            }

            if (forceInlineByShortExpression)
            {
                shouldMultiline = false;
            }

            if (!shouldMultiline)
            {
                _writer.WriteSpace();
                _writer.WriteToken(useMixedOrGrouping
                    ? RenderMixedOrGroupsInline(mixedOrSplit, mixedAndOrOptions.ParenthesizeOrGroups, shortQueryReplacements)
                    : RenderTokensInline(tokenInfos, 0, shortQueryReplacements));
                return;
            }

            _writer.WriteLine();
            using (_writer.PushIndent())
            {
                if (useMixedOrGrouping)
                {
                    WriteMixedOrGroupPredicateLines(
                        mixedOrSplit,
                        mixedAndOrOptions.ParenthesizeOrGroups,
                        _options.Predicates.LogicalOperatorLineBreak,
                        shortQueryReplacements);
                    return;
                }

                var split = SplitByTopLevelLogicalOperators(tokenInfos, AllLogicalOperators);
                if (split.Operators.Count == 0)
                {
                    _writer.WriteToken(RenderTokensInline(tokenInfos, 0, shortQueryReplacements));
                    return;
                }

                WriteLogicalPredicateLines(split, _options.Predicates.LogicalOperatorLineBreak, shortQueryReplacements);
            }
        }

        private void WriteOnPredicate(IReadOnlyList<SqlTokenInfo> tokenInfos, bool forceInlineByShortExpression)
        {
            var threshold = _options.Joins.MultilineOnThreshold;
            var breakOnAnd = threshold.BreakOnAnd;
            var breakOnOr = threshold.BreakOnOr;
            var breakOperators = new HashSet<string>(StringComparer.Ordinal);
            if (breakOnAnd)
            {
                breakOperators.Add("AND");
            }

            if (breakOnOr)
            {
                breakOperators.Add("OR");
            }

            if (breakOperators.Count == 0)
            {
                _writer.WriteSpace();
                _writer.WriteToken(RenderTokensInline(tokenInfos));
                return;
            }

            var split = SplitByTopLevelLogicalOperators(tokenInfos, breakOperators);
            var shouldUseMultiline = threshold.MaxTokensSingleLine > 0 &&
                CountSignificantTokens(tokenInfos) > threshold.MaxTokensSingleLine &&
                split.Operators.Count > 0;
            if (forceInlineByShortExpression)
            {
                shouldUseMultiline = false;
            }

            if (!shouldUseMultiline)
            {
                _writer.WriteSpace();
                _writer.WriteToken(RenderTokensInline(tokenInfos));
                return;
            }

            _writer.WriteSpace();
            _writer.WriteToken(RenderTokensInline(split.Segments[0]));
            using (_writer.PushIndent())
            {
                for (var splitIndex = 0; splitIndex < split.Operators.Count; splitIndex++)
                {
                    _writer.WriteLine();
                    var logicalOperator = split.Operators[splitIndex];
                    var operatorText = FormatToken(logicalOperator.Raw, logicalOperator.TokenForRules, logicalOperator.IsKeyword);
                    var rightSegmentText = RenderTokensInline(split.Segments[splitIndex + 1]);
                    _writer.WriteToken($"{operatorText} {rightSegmentText}");
                }
            }
        }

        private void WriteLogicalPredicateLines(
            LogicalSplitResult split,
            SqlLogicalOperatorLineBreakMode lineBreakMode,
            IReadOnlyList<ShortQueryReplacement> shortQueryReplacements)
        {
            if (lineBreakMode == SqlLogicalOperatorLineBreakMode.AfterOperator)
            {
                for (var splitIndex = 0; splitIndex < split.Operators.Count; splitIndex++)
                {
                    var leftSegmentText = RenderTokensInline(
                        split.Segments[splitIndex],
                        split.SegmentStartIndexes[splitIndex],
                        shortQueryReplacements);
                    var logicalOperator = split.Operators[splitIndex];
                    var operatorText = FormatToken(logicalOperator.Raw, logicalOperator.TokenForRules, logicalOperator.IsKeyword);
                    _writer.WriteToken($"{leftSegmentText} {operatorText}");
                    _writer.WriteLine();
                }

                _writer.WriteToken(RenderTokensInline(
                    split.Segments[^1],
                    split.SegmentStartIndexes[^1],
                    shortQueryReplacements));
                return;
            }

            _writer.WriteToken(RenderTokensInline(
                split.Segments[0],
                split.SegmentStartIndexes[0],
                shortQueryReplacements));
            for (var splitIndex = 0; splitIndex < split.Operators.Count; splitIndex++)
            {
                _writer.WriteLine();
                var logicalOperator = split.Operators[splitIndex];
                var operatorText = FormatToken(logicalOperator.Raw, logicalOperator.TokenForRules, logicalOperator.IsKeyword);
                var rightSegmentText = RenderTokensInline(
                    split.Segments[splitIndex + 1],
                    split.SegmentStartIndexes[splitIndex + 1],
                    shortQueryReplacements);
                _writer.WriteToken($"{operatorText} {rightSegmentText}");
            }
        }

        private bool ShouldInlineSimplePredicate(
            string anchorKeyword,
            IReadOnlyList<SqlTokenInfo> tokenInfos,
            IReadOnlyList<ShortQueryReplacement> shortQueryReplacements)
        {
            var inlineOptions = _options.Predicates.InlineSimplePredicate;
            if (inlineOptions.MaxConditions <= 0)
            {
                return false;
            }

            var split = SplitByTopLevelLogicalOperators(tokenInfos, AllLogicalOperators);
            if (split.Segments.Count > inlineOptions.MaxConditions)
            {
                return false;
            }

            if (inlineOptions.AllowOnlyAnd &&
                split.Operators.Any(logicalOperator =>
                    !string.Equals(logicalOperator.TokenForRules, "AND", StringComparison.Ordinal)))
            {
                return false;
            }

            var predicateText = RenderTokensInline(tokenInfos, 0, shortQueryReplacements);
            var maxLineLength = Math.Max(1, inlineOptions.MaxLineLength);
            return anchorKeyword.Length + 1 + predicateText.Length <= maxLineLength;
        }

        private bool TryGetMixedTopLevelOrSplit(IReadOnlyList<SqlTokenInfo> tokenInfos, out LogicalSplitResult orSplit)
        {
            orSplit = SplitByTopLevelLogicalOperators(tokenInfos, OrLogicalOperators);
            if (orSplit.Operators.Count == 0)
            {
                return false;
            }

            var split = SplitByTopLevelLogicalOperators(tokenInfos, AllLogicalOperators);
            var hasAnd = split.Operators.Any(logicalOperator =>
                string.Equals(logicalOperator.TokenForRules, "AND", StringComparison.Ordinal));
            var hasOr = split.Operators.Any(logicalOperator =>
                string.Equals(logicalOperator.TokenForRules, "OR", StringComparison.Ordinal));
            return hasAnd && hasOr;
        }

        private string RenderMixedOrGroupsInline(
            LogicalSplitResult split,
            bool parenthesizeOrGroups,
            IReadOnlyList<ShortQueryReplacement> shortQueryReplacements)
        {
            var builder = new StringBuilder();
            for (var splitIndex = 0; splitIndex < split.Segments.Count; splitIndex++)
            {
                if (splitIndex > 0)
                {
                    var logicalOperator = split.Operators[splitIndex - 1];
                    var operatorText = FormatToken(logicalOperator.Raw, logicalOperator.TokenForRules, logicalOperator.IsKeyword);
                    builder.Append(' ');
                    builder.Append(operatorText);
                    builder.Append(' ');
                }

                builder.Append(RenderMixedOrGroupText(
                    split.Segments[splitIndex],
                    split.SegmentStartIndexes[splitIndex],
                    parenthesizeOrGroups,
                    shortQueryReplacements));
            }

            return builder.ToString();
        }

        private void WriteMixedOrGroupPredicateLines(
            LogicalSplitResult split,
            bool parenthesizeOrGroups,
            SqlLogicalOperatorLineBreakMode lineBreakMode,
            IReadOnlyList<ShortQueryReplacement> shortQueryReplacements)
        {
            if (lineBreakMode == SqlLogicalOperatorLineBreakMode.AfterOperator)
            {
                for (var splitIndex = 0; splitIndex < split.Operators.Count; splitIndex++)
                {
                    var leftSegmentText = RenderMixedOrGroupText(
                        split.Segments[splitIndex],
                        split.SegmentStartIndexes[splitIndex],
                        parenthesizeOrGroups,
                        shortQueryReplacements);
                    var logicalOperator = split.Operators[splitIndex];
                    var operatorText = FormatToken(logicalOperator.Raw, logicalOperator.TokenForRules, logicalOperator.IsKeyword);
                    _writer.WriteToken($"{leftSegmentText} {operatorText}");
                    _writer.WriteLine();
                }

                _writer.WriteToken(RenderMixedOrGroupText(
                    split.Segments[^1],
                    split.SegmentStartIndexes[^1],
                    parenthesizeOrGroups,
                    shortQueryReplacements));
                return;
            }

            _writer.WriteToken(RenderMixedOrGroupText(
                split.Segments[0],
                split.SegmentStartIndexes[0],
                parenthesizeOrGroups,
                shortQueryReplacements));
            for (var splitIndex = 0; splitIndex < split.Operators.Count; splitIndex++)
            {
                _writer.WriteLine();
                var logicalOperator = split.Operators[splitIndex];
                var operatorText = FormatToken(logicalOperator.Raw, logicalOperator.TokenForRules, logicalOperator.IsKeyword);
                var rightSegmentText = RenderMixedOrGroupText(
                    split.Segments[splitIndex + 1],
                    split.SegmentStartIndexes[splitIndex + 1],
                    parenthesizeOrGroups,
                    shortQueryReplacements);
                _writer.WriteToken($"{operatorText} {rightSegmentText}");
            }
        }

        private string RenderMixedOrGroupText(
            IReadOnlyList<SqlTokenInfo> tokenInfos,
            int startIndex,
            bool parenthesizeOrGroups,
            IReadOnlyList<ShortQueryReplacement> shortQueryReplacements)
        {
            var groupText = RenderTokensInline(tokenInfos, startIndex, shortQueryReplacements);
            if (!parenthesizeOrGroups || !ShouldParenthesizeMixedOrGroup(tokenInfos))
            {
                return groupText;
            }

            if (IsWrappedInSingleTopLevelParentheses(tokenInfos))
            {
                return groupText;
            }

            return WrapInlineTextInParentheses(groupText);
        }

        private bool ShouldParenthesizeMixedOrGroup(IReadOnlyList<SqlTokenInfo> tokenInfos)
        {
            var split = SplitByTopLevelLogicalOperators(tokenInfos, AndLogicalOperators);
            return split.Operators.Count > 0;
        }

        private static bool IsWrappedInSingleTopLevelParentheses(IReadOnlyList<SqlTokenInfo> tokenInfos)
        {
            var startIndex = 0;
            while (startIndex < tokenInfos.Count && tokenInfos[startIndex].IsComment)
            {
                startIndex++;
            }

            var endIndex = tokenInfos.Count - 1;
            while (endIndex >= startIndex && tokenInfos[endIndex].IsComment)
            {
                endIndex--;
            }

            if (startIndex >= endIndex ||
                !string.Equals(tokenInfos[startIndex].TokenForRules, "(", StringComparison.Ordinal) ||
                !string.Equals(tokenInfos[endIndex].TokenForRules, ")", StringComparison.Ordinal))
            {
                return false;
            }

            var parenthesisDepth = 0;
            for (var tokenIndex = startIndex; tokenIndex <= endIndex; tokenIndex++)
            {
                var token = tokenInfos[tokenIndex].TokenForRules;
                if (string.Equals(token, "(", StringComparison.Ordinal))
                {
                    parenthesisDepth++;
                }
                else if (string.Equals(token, ")", StringComparison.Ordinal))
                {
                    parenthesisDepth--;
                    if (parenthesisDepth == 0 && tokenIndex < endIndex)
                    {
                        return false;
                    }
                }
            }

            return parenthesisDepth == 0;
        }

        private string WrapInlineTextInParentheses(string text)
        {
            return _options.Spaces.InsideParentheses == SqlParenthesesSpacing.Always
                ? $"( {text} )"
                : $"({text})";
        }

        private static LogicalSplitResult SplitByTopLevelLogicalOperators(IReadOnlyList<SqlTokenInfo> tokenInfos, HashSet<string> breakOperators)
        {
            var segments = new List<List<SqlTokenInfo>> { new List<SqlTokenInfo>() };
            var segmentStartIndexes = new List<int> { 0 };
            var operators = new List<SqlTokenInfo>();
            var parenthesisDepth = 0;

            for (var tokenIndex = 0; tokenIndex < tokenInfos.Count; tokenIndex++)
            {
                var tokenInfo = tokenInfos[tokenIndex];
                if (string.Equals(tokenInfo.TokenForRules, "(", StringComparison.Ordinal))
                {
                    parenthesisDepth++;
                    segments[^1].Add(tokenInfo);
                    continue;
                }

                if (string.Equals(tokenInfo.TokenForRules, ")", StringComparison.Ordinal))
                {
                    parenthesisDepth = Math.Max(0, parenthesisDepth - 1);
                    segments[^1].Add(tokenInfo);
                    continue;
                }

                var canSplitByOperator = parenthesisDepth == 0 &&
                    breakOperators.Contains(tokenInfo.TokenForRules) &&
                    segments[^1].Count > 0;
                if (!canSplitByOperator)
                {
                    segments[^1].Add(tokenInfo);
                    continue;
                }

                operators.Add(tokenInfo);
                segments.Add(new List<SqlTokenInfo>());
                segmentStartIndexes.Add(tokenIndex + 1);
            }

            if (segments.Count > 1 && segments[^1].Count == 0)
            {
                segments.RemoveAt(segments.Count - 1);
                segmentStartIndexes.RemoveAt(segmentStartIndexes.Count - 1);
                operators.RemoveAt(operators.Count - 1);
            }

            return new LogicalSplitResult(segments, segmentStartIndexes, operators);
        }

        private static int CountSignificantTokens(IReadOnlyList<SqlTokenInfo> tokenInfos)
        {
            return tokenInfos.Count(tokenInfo =>
                !tokenInfo.IsComment &&
                !string.Equals(tokenInfo.TokenForRules, "(", StringComparison.Ordinal) &&
                !string.Equals(tokenInfo.TokenForRules, ")", StringComparison.Ordinal));
        }

        private void WriteSelectItemList(NonTerminalNode node)
        {
            var itemNodes = ExtractDelimitedListItems(node, "SelectItemList");
            var itemTexts = itemNodes.Select(RenderNodeInline).ToList();
            var useMultiline = ShouldUseMultilineSelectLayout(itemNodes, itemTexts);
            if (useMultiline && ShouldForceInlineSelectList(itemNodes, itemTexts))
            {
                useMultiline = false;
            }

            if (useMultiline && _options.Align.SelectAliases)
            {
                itemTexts = AlignSelectAliases(itemTexts);
            }

            WriteList(itemTexts, useMultiline);
            UpdatePreviousTokenFromNode(node);
        }

        private void WriteGroupByExpressionList(NonTerminalNode node)
        {
            var itemNodes = ExtractDelimitedListItems(node, "ExpressionList");
            var itemTexts = itemNodes.Select(RenderNodeInline).ToList();
            var useMultiline = ShouldUseMultilineLayout(_options.Lists.GroupByItems, itemTexts, "GROUP BY ".Length);

            WriteList(itemTexts, useMultiline);
            UpdatePreviousTokenFromNode(node);
        }

        private void WriteOrderByItemList(NonTerminalNode node)
        {
            var itemNodes = ExtractDelimitedListItems(node, "OrderItemList");
            var itemTexts = itemNodes.Select(RenderNodeInline).ToList();
            var useMultiline = ShouldUseMultilineLayout(_options.Lists.OrderByItems, itemTexts, "ORDER BY ".Length);

            WriteList(itemTexts, useMultiline);
            UpdatePreviousTokenFromNode(node);
        }

        private void WriteInListExpressionList(NonTerminalNode node)
        {
            var itemNodes = ExtractDelimitedListItems(node, "ExpressionList");
            var itemTexts = itemNodes.Select(RenderNodeInline).ToList();
            var listStyle = _options.Lists.InListItems;

            var useMultiline = listStyle switch
            {
                SqlInListItemsStyle.OnePerLine => true,
                SqlInListItemsStyle.WrapByWidth => "IN (".Length + string.Join(GetInlineCommaSeparator(), itemTexts).Length + 1 > GetWrapColumn(),
                _ => false
            };

            var inlineThreshold = _options.Lists.InlineInListThreshold;
            if (useMultiline &&
                inlineThreshold.MaxItemsInline > 0 &&
                itemTexts.Count <= inlineThreshold.MaxItemsInline)
            {
                var inlineLength = "IN (".Length + string.Join(GetInlineCommaSeparator(), itemTexts).Length + 1;
                if (inlineLength <= Math.Max(1, inlineThreshold.MaxLineLength))
                {
                    useMultiline = false;
                }
            }

            if (useMultiline)
            {
                WriteList(itemTexts, multiline: true);
                if (!_writer.IsLineStart)
                {
                    _writer.WriteLine();
                }
            }
            else
            {
                _writer.WriteToken(string.Join(GetInlineCommaSeparator(), itemTexts));
            }

            UpdatePreviousTokenFromNode(node);
        }

        private void WriteUpdateSetList(NonTerminalNode node)
        {
            var itemNodes = ExtractDelimitedListItems(node, "UpdateSetList");
            var itemTexts = itemNodes.Select(RenderNodeInline).ToList();
            var useMultiline = ShouldUseMultilineDmlList(_options.Dml.UpdateSetStyle, itemTexts, "SET ".Length);

            WriteList(itemTexts, useMultiline);
            UpdatePreviousTokenFromNode(node);
        }

        private void WriteInsertColumnList(NonTerminalNode node)
        {
            var itemNodes = ExtractDelimitedListItems(node, "InsertColumnList");
            var itemTexts = itemNodes.Select(RenderNodeInline).ToList();
            var useMultiline = ShouldUseMultilineDmlList(_options.Dml.InsertColumnsStyle, itemTexts, "(".Length);

            if (useMultiline)
            {
                WriteList(itemTexts, multiline: true);
            }
            else
            {
                _writer.WriteToken(string.Join(GetInlineCommaSeparator(), itemTexts));
            }

            UpdatePreviousTokenFromNode(node);
        }

        private void WriteInsertValueList(NonTerminalNode node)
        {
            var itemNodes = ExtractDelimitedListItems(node, "InsertValueList");
            var itemTexts = itemNodes.Select(RenderNodeInline).ToList();
            var useMultiline = ShouldUseMultilineDmlList(_options.Dml.InsertValuesStyle, itemTexts, "(".Length);

            if (useMultiline)
            {
                WriteList(itemTexts, multiline: true);
            }
            else
            {
                _writer.WriteToken(string.Join(GetInlineCommaSeparator(), itemTexts));
            }

            UpdatePreviousTokenFromNode(node);
        }

        private void WriteList(IReadOnlyList<string> itemTexts, bool multiline)
        {
            if (itemTexts.Count == 0)
            {
                return;
            }

            if (!multiline)
            {
                if (_writer.HasContent && !_writer.IsLineStart)
                {
                    _writer.WriteSpace();
                }

                _writer.WriteToken(string.Join(GetInlineCommaSeparator(), itemTexts));
                return;
            }

            _writer.WriteLine();
            using (_writer.PushIndent())
            {
                for (var itemIndex = 0; itemIndex < itemTexts.Count; itemIndex++)
                {
                    var itemText = itemTexts[itemIndex];
                    var isLastItem = itemIndex == itemTexts.Count - 1;

                    if (_options.Lists.CommaStyle == SqlCommaStyle.Trailing)
                    {
                        var textToWrite = isLastItem ? itemText : $"{itemText},";
                        _writer.WriteToken(textToWrite);
                    }
                    else
                    {
                        if (itemIndex == 0)
                        {
                            _writer.WriteToken(itemText);
                        }
                        else
                        {
                            var prefix = _options.Spaces.AfterComma ? ", " : ",";
                            _writer.WriteToken($"{prefix}{itemText}");
                        }
                    }

                    if (!isLastItem)
                    {
                        _writer.WriteLine();
                    }
                }
            }
        }

        private bool ShouldUseMultilineSelectLayout(IReadOnlyList<ParseTreeNode> itemNodes, IReadOnlyList<string> itemTexts)
        {
            var useMultiline = ShouldUseMultilineLayout(_options.Lists.SelectItems, itemTexts, "SELECT ".Length);
            if (!useMultiline)
            {
                return false;
            }

            var threshold = _options.Lists.SelectCompactThreshold;
            if (threshold.MaxItems <= 0 || itemNodes.Count > threshold.MaxItems)
            {
                return true;
            }

            var inlineLineLength = "SELECT ".Length + string.Join(GetInlineCommaSeparator(), itemTexts).Length;
            return inlineLineLength > Math.Max(1, threshold.MaxLineLength);
        }

        private bool ShouldForceInlineSelectList(IReadOnlyList<ParseTreeNode> itemNodes, IReadOnlyList<string> itemTexts)
        {
            if (itemNodes.Count == 0)
            {
                return false;
            }

            if (!itemNodes.All(itemNode => ShouldInlineShortExpression(itemNode, SqlInlineExpressionContext.SelectItem, "SELECT ".Length)))
            {
                return false;
            }

            var inlineLength = "SELECT ".Length + string.Join(GetInlineCommaSeparator(), itemTexts).Length;
            var maxLineLength = Math.Max(1, _options.Expressions.InlineShortExpression.MaxLineLength);
            return inlineLength <= maxLineLength;
        }

        private bool ShouldUseMultilineLayout(SqlListLayoutStyle listLayoutStyle, IReadOnlyList<string> itemTexts, int clausePrefixLength)
        {
            if (listLayoutStyle == SqlListLayoutStyle.OnePerLine)
            {
                return true;
            }

            var inlineLength = clausePrefixLength + string.Join(GetInlineCommaSeparator(), itemTexts).Length;
            return inlineLength > GetWrapColumn();
        }

        private bool ShouldUseMultilineDmlList(SqlDmlListStyle listStyle, IReadOnlyList<string> itemTexts, int clausePrefixLength)
        {
            if (listStyle == SqlDmlListStyle.OnePerLine)
            {
                return true;
            }

            if (listStyle == SqlDmlListStyle.RowsPerLine)
            {
                return false;
            }

            var inlineLength = clausePrefixLength + string.Join(GetInlineCommaSeparator(), itemTexts).Length;
            return inlineLength > GetWrapColumn();
        }

        private int GetWrapColumn()
        {
            return Math.Max(10, _options.Layout.WrapColumn);
        }

        private List<string> AlignSelectAliases(IReadOnlyList<string> itemTexts)
        {
            var aliasParts = new List<(string ItemText, string? LeftPart, string? AliasPart)>(itemTexts.Count);
            var maxLeftPartLength = 0;

            foreach (var itemText in itemTexts)
            {
                if (!TrySplitAlias(itemText, out var leftPart, out var aliasPart))
                {
                    aliasParts.Add((itemText, null, null));
                    continue;
                }

                maxLeftPartLength = Math.Max(maxLeftPartLength, leftPart.Length);
                aliasParts.Add((itemText, leftPart, aliasPart));
            }

            if (maxLeftPartLength == 0)
            {
                return itemTexts.ToList();
            }

            var asKeyword = FormatToken("AS", "AS", isKeyword: true);
            var aligned = new List<string>(itemTexts.Count);
            foreach (var aliasPart in aliasParts)
            {
                if (aliasPart.LeftPart == null || aliasPart.AliasPart == null)
                {
                    aligned.Add(aliasPart.ItemText);
                    continue;
                }

                aligned.Add($"{aliasPart.LeftPart.PadRight(maxLeftPartLength)} {asKeyword} {aliasPart.AliasPart}");
            }

            return aligned;
        }

        private static bool TrySplitAlias(string itemText, out string leftPart, out string aliasPart)
        {
            const string aliasMarker = " AS ";
            var markerIndex = itemText.LastIndexOf(aliasMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex <= 0)
            {
                leftPart = string.Empty;
                aliasPart = string.Empty;
                return false;
            }

            leftPart = itemText[..markerIndex].TrimEnd();
            aliasPart = itemText[(markerIndex + aliasMarker.Length)..].TrimStart();
            return !string.IsNullOrWhiteSpace(leftPart) && !string.IsNullOrWhiteSpace(aliasPart);
        }

        private static IReadOnlyList<ParseTreeNode> ExtractDelimitedListItems(NonTerminalNode listNode, string listName)
        {
            var items = new List<ParseTreeNode>();
            CollectListItems(listNode, listName, items);
            return items;
        }

        private static void CollectListItems(ParseTreeNode currentNode, string listName, List<ParseTreeNode> items)
        {
            if (currentNode is not NonTerminalNode nonTerminalNode ||
                !string.Equals(nonTerminalNode.NonTerminal.Name, listName, StringComparison.Ordinal))
            {
                items.Add(currentNode);
                return;
            }

            if (nonTerminalNode.Children.Count == 1)
            {
                items.Add(nonTerminalNode.Children[0]);
                return;
            }

            if (nonTerminalNode.Children.Count == 3 &&
                nonTerminalNode.Children[1] is TerminalNode separatorNode &&
                string.Equals(separatorNode.Token.OriginalString, ",", StringComparison.Ordinal))
            {
                CollectListItems(nonTerminalNode.Children[0], listName, items);
                items.Add(nonTerminalNode.Children[2]);
                return;
            }

            items.Add(nonTerminalNode);
        }

        private string RenderNodeInline(ParseTreeNode node)
        {
            var tokenInfos = CollectTokenInfos(node);
            return RenderTokensInline(tokenInfos);
        }

        private string RenderNodesInline(IEnumerable<ParseTreeNode> nodes)
        {
            var tokenInfos = new List<SqlTokenInfo>();
            foreach (var node in nodes)
            {
                tokenInfos.AddRange(CollectTokenInfos(node));
            }

            return RenderTokensInline(tokenInfos);
        }

        private string RenderTokensInline(IReadOnlyList<SqlTokenInfo> tokenInfos)
        {
            return RenderTokensInline(tokenInfos, 0, []);
        }

        private string RenderTokensInline(
            IReadOnlyList<SqlTokenInfo> tokenInfos,
            int startIndex,
            IReadOnlyList<ShortQueryReplacement> shortQueryReplacements)
        {
            var replacementsByStartIndex = shortQueryReplacements.Count == 0
                ? null
                : shortQueryReplacements.ToDictionary(replacement => replacement.StartIndex);
            var builder = new StringBuilder();
            string? previousToken = null;
            var previousTokenWasKeyword = false;

            for (var tokenIndex = 0; tokenIndex < tokenInfos.Count; tokenIndex++)
            {
                var absoluteTokenIndex = startIndex + tokenIndex;
                if (replacementsByStartIndex != null &&
                    replacementsByStartIndex.TryGetValue(absoluteTokenIndex, out var replacement))
                {
                    var startTokenInfo = tokenInfos[tokenIndex];
                    if (ShouldWriteSpaceBeforeToken(previousToken, previousTokenWasKeyword, startTokenInfo.TokenForRules))
                    {
                        builder.Append(' ');
                    }

                    builder.Append(replacement.Text);
                    var localEndIndex = replacement.EndIndex - startIndex;
                    if (localEndIndex >= tokenIndex && localEndIndex < tokenInfos.Count)
                    {
                        previousToken = tokenInfos[localEndIndex].TokenForRules;
                        previousTokenWasKeyword = tokenInfos[localEndIndex].IsKeyword;
                        tokenIndex = localEndIndex;
                    }

                    continue;
                }

                var tokenInfo = tokenInfos[tokenIndex];
                if (ShouldWriteSpaceBeforeToken(previousToken, previousTokenWasKeyword, tokenInfo.TokenForRules))
                {
                    builder.Append(' ');
                }

                if (!tokenInfo.IsComment)
                {
                    builder.Append(FormatToken(tokenInfo.Raw, tokenInfo.TokenForRules, tokenInfo.IsKeyword));
                    previousToken = tokenInfo.TokenForRules;
                    previousTokenWasKeyword = tokenInfo.IsKeyword;
                    continue;
                }

                var commentText = FormatCommentText(tokenInfo.Raw);
                builder.Append(commentText);

                var isSingleLineComment = commentText.StartsWith("--", StringComparison.Ordinal);
                var hasLineBreak = commentText.Contains('\n') || commentText.Contains('\r');
                if (isSingleLineComment && !hasLineBreak)
                {
                    builder.Append(Environment.NewLine);
                    previousToken = null;
                    previousTokenWasKeyword = false;
                    continue;
                }

                previousToken = tokenInfo.TokenForRules;
                previousTokenWasKeyword = tokenInfo.IsKeyword;
            }

            return builder.ToString();
        }

        private List<SqlTokenInfo> CollectTokenInfos(ParseTreeNode node)
        {
            return CollectTokenInfos(node, terminalTokenIndexes: null);
        }

        private List<SqlTokenInfo> CollectTokenInfos(
            ParseTreeNode node,
            Dictionary<TerminalNode, int>? terminalTokenIndexes)
        {
            var terminalNodes = CollectTerminalNodes(node);
            var tokenInfos = new List<SqlTokenInfo>(terminalNodes.Count);
            foreach (var terminalNode in terminalNodes)
            {
                AppendCommentTriviaTokenInfos(terminalNode.Token.Trivia.LeadingTrivia, tokenInfos);

                var rawToken = terminalNode.Token.OriginalString;
                if (string.IsNullOrEmpty(rawToken))
                {
                    continue;
                }

                var isKeyword = IsKeywordToken(terminalNode.Token);
                var tokenForRules = isKeyword ? rawToken.ToUpperInvariant() : rawToken;
                terminalTokenIndexes?.Add(terminalNode, tokenInfos.Count);
                tokenInfos.Add(new SqlTokenInfo(rawToken, tokenForRules, isKeyword));

                AppendCommentTriviaTokenInfos(terminalNode.Token.Trivia.TrailingTrivia, tokenInfos);
            }

            return tokenInfos;
        }

        private static void AppendCommentTriviaTokenInfos(IReadOnlyList<IToken> triviaTokens, List<SqlTokenInfo> output)
        {
            if (triviaTokens.Count == 0)
            {
                return;
            }

            foreach (var triviaToken in triviaTokens)
            {
                if (triviaToken.Terminal.Flags != TermFlags.Comment || string.IsNullOrWhiteSpace(triviaToken.OriginalString))
                {
                    continue;
                }

                output.Add(new SqlTokenInfo(
                    triviaToken.OriginalString,
                    triviaToken.OriginalString,
                    IsKeyword: false,
                    IsComment: true));
            }
        }

        private IReadOnlyList<ShortQueryReplacement> CollectShortQueryReplacements(ParseTreeNode node)
        {
            var terminalTokenIndexes = new Dictionary<TerminalNode, int>();
            CollectTokenInfos(node, terminalTokenIndexes);

            var replacements = new List<ShortQueryReplacement>();
            CollectShortQueryReplacements(node, terminalTokenIndexes, replacements);
            return replacements
                .OrderBy(replacement => replacement.StartIndex)
                .ToList();
        }

        private void CollectShortQueryReplacements(
            ParseTreeNode node,
            IReadOnlyDictionary<TerminalNode, int> terminalTokenIndexes,
            List<ShortQueryReplacement> output)
        {
            if (TryAddShortQueryReplacement(node, terminalTokenIndexes, output))
            {
                return;
            }

            foreach (var childNode in node.Children)
            {
                CollectShortQueryReplacements(childNode, terminalTokenIndexes, output);
            }
        }

        private bool TryAddShortQueryReplacement(
            ParseTreeNode node,
            IReadOnlyDictionary<TerminalNode, int> terminalTokenIndexes,
            List<ShortQueryReplacement> output)
        {
            if (TryGetExistsSubqueryRange(node, out var existsOpenTerminal, out var existsQueryExpressionNode, out var existsCloseTerminal))
            {
                return TryAddShortQueryReplacement(
                    existsOpenTerminal,
                    existsQueryExpressionNode,
                    existsCloseTerminal,
                    terminalTokenIndexes,
                    output);
            }

            if (TryGetParenthesizedSubqueryRange(node, out var openTerminal, out var queryExpressionNode, out var closeTerminal))
            {
                return TryAddShortQueryReplacement(
                    openTerminal,
                    queryExpressionNode,
                    closeTerminal,
                    terminalTokenIndexes,
                    output);
            }

            return false;
        }

        private bool TryAddShortQueryReplacement(
            TerminalNode openTerminal,
            NonTerminalNode queryExpressionNode,
            TerminalNode closeTerminal,
            IReadOnlyDictionary<TerminalNode, int> terminalTokenIndexes,
            List<ShortQueryReplacement> output)
        {
            if (!TryGetShortQueryInlineText(queryExpressionNode, out var inlineText) ||
                !terminalTokenIndexes.TryGetValue(openTerminal, out var startIndex) ||
                !terminalTokenIndexes.TryGetValue(closeTerminal, out var endIndex))
            {
                return false;
            }

            output.Add(new ShortQueryReplacement(startIndex, endIndex, $"({inlineText})"));
            return true;
        }

        private static bool TryGetExistsSubqueryRange(
            ParseTreeNode node,
            out TerminalNode openTerminal,
            out NonTerminalNode queryExpressionNode,
            out TerminalNode closeTerminal)
        {
            openTerminal = null!;
            queryExpressionNode = null!;
            closeTerminal = null!;
            if (node is not NonTerminalNode nonTerminalNode ||
                !string.Equals(nonTerminalNode.NonTerminal.Name, "BooleanPrimary", StringComparison.Ordinal) ||
                nonTerminalNode.Children.Count != 4 ||
                !TryGetTerminalText(nonTerminalNode.Children[0], out var existsKeyword) ||
                !string.Equals(existsKeyword, "EXISTS", StringComparison.OrdinalIgnoreCase) ||
                nonTerminalNode.Children[1] is not TerminalNode existsOpenTerminal ||
                !string.Equals(existsOpenTerminal.Token.OriginalString, "(", StringComparison.Ordinal) ||
                nonTerminalNode.Children[2] is not NonTerminalNode existsQueryExpressionNode ||
                !string.Equals(existsQueryExpressionNode.NonTerminal.Name, "QueryExpression", StringComparison.Ordinal) ||
                nonTerminalNode.Children[3] is not TerminalNode existsCloseTerminal ||
                !string.Equals(existsCloseTerminal.Token.OriginalString, ")", StringComparison.Ordinal))
            {
                return false;
            }

            openTerminal = existsOpenTerminal;
            queryExpressionNode = existsQueryExpressionNode;
            closeTerminal = existsCloseTerminal;
            return true;
        }

        private static bool TryGetParenthesizedSubqueryRange(
            ParseTreeNode node,
            out TerminalNode openTerminal,
            out NonTerminalNode queryExpressionNode,
            out TerminalNode closeTerminal)
        {
            openTerminal = null!;
            queryExpressionNode = null!;
            closeTerminal = null!;
            if (node is not NonTerminalNode nonTerminalNode ||
                (nonTerminalNode.NonTerminal.Name is not "InPredicateValue" and
                    not "PrimaryExpression" and
                    not "QueryPrimary" and
                    not "TableFactor") ||
                nonTerminalNode.Children.Count != 3 ||
                nonTerminalNode.Children[0] is not TerminalNode subqueryOpenTerminal ||
                !string.Equals(subqueryOpenTerminal.Token.OriginalString, "(", StringComparison.Ordinal) ||
                nonTerminalNode.Children[1] is not NonTerminalNode subqueryQueryExpressionNode ||
                !string.Equals(subqueryQueryExpressionNode.NonTerminal.Name, "QueryExpression", StringComparison.Ordinal) ||
                nonTerminalNode.Children[2] is not TerminalNode subqueryCloseTerminal ||
                !string.Equals(subqueryCloseTerminal.Token.OriginalString, ")", StringComparison.Ordinal))
            {
                return false;
            }

            openTerminal = subqueryOpenTerminal;
            queryExpressionNode = subqueryQueryExpressionNode;
            closeTerminal = subqueryCloseTerminal;
            return true;
        }

        private void UpdatePreviousTokenFromNode(ParseTreeNode node)
        {
            var terminalNodes = CollectTerminalNodes(node);
            if (terminalNodes.Count == 0)
            {
                return;
            }

            var lastTerminalNode = terminalNodes[^1];
            var rawToken = lastTerminalNode.Token.OriginalString;
            if (string.IsNullOrEmpty(rawToken))
            {
                return;
            }

            var isKeyword = IsKeywordToken(lastTerminalNode.Token);
            _tokenBeforePrevious = _previousToken;
            _previousToken = isKeyword ? rawToken.ToUpperInvariant() : rawToken;
            _previousTokenWasKeyword = isKeyword;
        }

        private static bool IsKeywordToken(IToken token)
        {
            return token is KeywordToken && token.Terminal.Flags != TermFlags.Identifier;
        }

        private void UpdatePreviousToken(SqlTokenInfo tokenInfo)
        {
            _tokenBeforePrevious = _previousToken;
            _previousToken = tokenInfo.TokenForRules;
            _previousTokenWasKeyword = tokenInfo.IsKeyword;
        }

        private static List<TerminalNode> CollectTerminalNodes(ParseTreeNode node)
        {
            var terminalNodes = new List<TerminalNode>();
            CollectTerminalNodes(node, terminalNodes);
            return terminalNodes;
        }

        private static void CollectTerminalNodes(ParseTreeNode node, List<TerminalNode> output)
        {
            if (node is TerminalNode terminalNode)
            {
                output.Add(terminalNode);
                return;
            }

            foreach (var child in node.Children)
            {
                CollectTerminalNodes(child, output);
            }
        }

        private void WriteTriviaComments(IReadOnlyList<IToken> triviaTokens)
        {
            if (triviaTokens.Count == 0)
            {
                return;
            }

            foreach (var triviaToken in triviaTokens)
            {
                if (triviaToken.Terminal.Flags != TermFlags.Comment)
                {
                    continue;
                }

                var commentText = FormatCommentText(triviaToken.OriginalString);
                if (string.IsNullOrWhiteSpace(commentText))
                {
                    continue;
                }

                var isSingleLineComment = commentText.StartsWith("--", StringComparison.Ordinal);
                var hasLineBreak = commentText.Contains('\n') || commentText.Contains('\r');

                if (!_options.Comments.PreserveAttachment)
                {
                    if (!_writer.IsLineStart)
                    {
                        _writer.WriteLine();
                    }

                    _writer.WriteToken(commentText);
                    _writer.WriteLine();
                    continue;
                }

                if (!_writer.IsLineStart)
                {
                    _writer.WriteSpace();
                }

                _writer.WriteToken(commentText);
                if (isSingleLineComment || hasLineBreak)
                {
                    _writer.WriteLine();
                }
            }
        }

        private string FormatCommentText(string rawCommentText)
        {
            if (_options.Comments.Formatting == SqlCommentsFormattingMode.Keep)
            {
                return rawCommentText;
            }

            if (rawCommentText.StartsWith("--", StringComparison.Ordinal))
            {
                var commentBody = rawCommentText.Length > 2
                    ? rawCommentText[2..]
                    : string.Empty;
                var normalizedBody = Regex.Replace(commentBody, @"\s+", " ").Trim();
                return string.IsNullOrEmpty(normalizedBody)
                    ? "--"
                    : $"-- {normalizedBody}";
            }

            if (rawCommentText.StartsWith("/*", StringComparison.Ordinal) &&
                rawCommentText.EndsWith("*/", StringComparison.Ordinal))
            {
                var commentBody = rawCommentText[2..^2];
                var hasLineBreak = commentBody.Contains('\n') || commentBody.Contains('\r');
                if (hasLineBreak)
                {
                    return rawCommentText;
                }

                var normalizedBody = Regex.Replace(commentBody, @"\s+", " ").Trim();
                return string.IsNullOrEmpty(normalizedBody)
                    ? "/* */"
                    : $"/* {normalizedBody} */";
            }

            return rawCommentText;
        }

        private string GetInlineCommaSeparator()
        {
            return _options.Spaces.AfterComma ? ", " : ",";
        }

        private string FormatToken(string rawToken, string tokenForRules, bool isKeyword)
        {
            if (!isKeyword)
            {
                return rawToken;
            }

            return _options.KeywordCase switch
            {
                SqlKeywordCase.Upper => tokenForRules,
                SqlKeywordCase.Lower => tokenForRules.ToLowerInvariant(),
                _ => rawToken
            };
        }

        private bool ShouldStartNewLineBefore(string token, ClauseKind clauseKind)
        {
            if (!_writer.HasContent || _writer.IsLineStart || _previousToken == null)
            {
                return false;
            }

            if (string.Equals(_previousToken, ",", StringComparison.Ordinal) ||
                string.Equals(_previousToken, ";", StringComparison.Ordinal))
            {
                return false;
            }

            if (string.Equals(_previousToken, "(", StringComparison.Ordinal))
            {
                return string.Equals(token, "SELECT", StringComparison.Ordinal) ||
                       string.Equals(token, "WITH", StringComparison.Ordinal);
            }

            if (string.Equals(token, "JOIN", StringComparison.Ordinal) && JoinPrefixKeywords.Contains(_previousToken))
            {
                return false;
            }

            if (string.Equals(token, "APPLY", StringComparison.Ordinal) &&
                (string.Equals(_previousToken, "CROSS", StringComparison.Ordinal) ||
                 string.Equals(_previousToken, "OUTER", StringComparison.Ordinal)))
            {
                return false;
            }

            if (JoinPrefixKeywords.Contains(token) && JoinPrefixKeywords.Contains(_previousToken))
            {
                return false;
            }

            if (IsMajorClause(clauseKind))
            {
                return IsClauseNewlineEnabled(clauseKind);
            }

            if (string.Equals(token, "ON", StringComparison.Ordinal))
            {
                return _options.Joins.OnNewLine;
            }

            return JoinStartKeywords.Contains(token) && _options.Joins.NewlinePerJoin;
        }

        private bool ShouldInsertBlankLine(ClauseKind clauseKind)
        {
            if (_options.Layout.BlankLineBetweenClauses != SqlBlankLineBetweenClausesMode.BetweenMajorClauses)
            {
                return false;
            }

            if (!IsMajorClause(clauseKind))
            {
                return false;
            }

            if (!_lastMajorClause.HasValue)
            {
                return false;
            }

            return _lastMajorClause.Value != clauseKind;
        }

        private bool ShouldWriteSpaceBefore(string token)
        {
            return ShouldWriteSpaceBeforeToken(_previousToken, _previousTokenWasKeyword, token);
        }

        private bool ShouldWriteSpaceBeforeToken(string? previousToken, bool previousTokenWasKeyword, string token)
        {
            if (previousToken == null)
            {
                return false;
            }

            if (string.Equals(token, ";", StringComparison.Ordinal))
            {
                return _options.Spaces.BeforeSemicolon;
            }

            if (string.Equals(token, ")", StringComparison.Ordinal))
            {
                return _options.Spaces.InsideParentheses == SqlParenthesesSpacing.Always &&
                       !string.Equals(previousToken, "(", StringComparison.Ordinal);
            }

            if (TokensWithoutLeadingSpace.Contains(token))
            {
                return false;
            }

            if (string.Equals(token, "(", StringComparison.Ordinal))
            {
                return ShouldWriteSpaceBeforeOpenParenthesis(previousToken, previousTokenWasKeyword);
            }

            if (string.Equals(previousToken, ",", StringComparison.Ordinal))
            {
                return _options.Spaces.AfterComma;
            }

            if (string.Equals(previousToken, "(", StringComparison.Ordinal))
            {
                return _options.Spaces.InsideParentheses == SqlParenthesesSpacing.Always;
            }

            if (IsBinaryOperatorToken(token))
            {
                return _options.Spaces.AroundBinaryOperators;
            }

            if (IsBinaryOperatorToken(previousToken))
            {
                return _options.Spaces.AroundBinaryOperators;
            }

            if (TokensWithoutTrailingSpace.Contains(previousToken))
            {
                return false;
            }

            return true;
        }

        private bool ShouldWriteSpaceBeforeOpenParenthesis(string previousToken, bool previousTokenWasKeyword)
        {
            if (string.Equals(previousToken, "(", StringComparison.Ordinal) ||
                string.Equals(previousToken, ".", StringComparison.Ordinal))
            {
                return false;
            }

            if (previousTokenWasKeyword)
            {
                return true;
            }

            return IsBinaryOperatorToken(previousToken);
        }

        private static bool IsBinaryOperatorToken(string token)
        {
            return BinaryOperatorTokens.Contains(token.ToUpperInvariant());
        }

        private ClauseKind GetClauseKindForToken(string token)
        {
            return token switch
            {
                "WITH" => ClauseKind.With,
                "SELECT" => ClauseKind.Select,
                "FROM" => ClauseKind.From,
                "WHERE" => ClauseKind.Where,
                "GROUP" => ClauseKind.GroupBy,
                "HAVING" => ClauseKind.Having,
                "ORDER" => ClauseKind.OrderBy,
                "OPTION" => ClauseKind.Option,
                _ => ClauseKind.None
            };
        }

        private bool IsClauseNewlineEnabled(ClauseKind clauseKind)
        {
            return clauseKind switch
            {
                ClauseKind.With => _options.Layout.NewlineBeforeClause.With,
                ClauseKind.Select => _options.Layout.NewlineBeforeClause.Select,
                ClauseKind.From => _options.Layout.NewlineBeforeClause.From,
                ClauseKind.Where => _options.Layout.NewlineBeforeClause.Where,
                ClauseKind.GroupBy => _options.Layout.NewlineBeforeClause.GroupBy,
                ClauseKind.Having => _options.Layout.NewlineBeforeClause.Having,
                ClauseKind.OrderBy => _options.Layout.NewlineBeforeClause.OrderBy,
                ClauseKind.Option => _options.Layout.NewlineBeforeClause.Option,
                _ => false
            };
        }

        private static bool IsMajorClause(ClauseKind clauseKind)
        {
            return clauseKind is ClauseKind.With or
                ClauseKind.Select or
                ClauseKind.From or
                ClauseKind.Where or
                ClauseKind.GroupBy or
                ClauseKind.Having or
                ClauseKind.OrderBy or
                ClauseKind.Option;
        }

        private void HandleStatementBoundary()
        {
            if (!_writer.IsLineStart)
            {
                _writer.WriteLine();
            }

            _currentClause = ClauseKind.None;
            _lastMajorClause = null;
            _tokenBeforePrevious = null;
            _previousToken = null;
            _previousTokenWasKeyword = false;
        }

        private static SqlTokenInfo CreateKeywordToken(string keyword)
        {
            return new SqlTokenInfo(keyword, keyword.ToUpperInvariant(), IsKeyword: true);
        }

        private static SqlTokenInfo CreateSymbolToken(string symbol)
        {
            return new SqlTokenInfo(symbol, symbol, IsKeyword: false);
        }

        private sealed record LogicalSplitResult(
            List<List<SqlTokenInfo>> Segments,
            List<int> SegmentStartIndexes,
            List<SqlTokenInfo> Operators);

        private sealed record InsertRowValueLayoutInfo(
            NonTerminalNode ValueListNode,
            bool IsMultiline);

        private sealed record ShortQueryReplacement(
            int StartIndex,
            int EndIndex,
            string Text);

        private readonly record struct SqlTokenInfo(string Raw, string TokenForRules, bool IsKeyword, bool IsComment = false);
    }
}
