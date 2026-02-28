using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSLKIT.Parser;
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
            "APPLY",
            "ON"
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

        private const int WrapByWidthLineLength = 120;

        private readonly SqlFormattingOptions _options;
        private readonly IndentedSqlTextWriter _writer;

        private string? _previousToken;
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
            if (TryWriteStructuredList(node, nonTerminalName))
            {
                return;
            }

            VisitChildren(node);
        }

        protected override void VisitTerminal(TerminalNode node)
        {
            var rawToken = node.Token.OriginalString;
            if (string.IsNullOrEmpty(rawToken))
            {
                return;
            }

            var isKeyword = node.Token is KeywordToken;
            var tokenForRules = isKeyword
                ? rawToken.ToUpperInvariant()
                : rawToken;

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

            if (ShouldWriteSpaceBefore(tokenForRules))
            {
                _writer.WriteSpace();
            }

            _writer.WriteToken(FormatToken(rawToken, tokenForRules, isKeyword));

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

            return false;
        }

        private void WriteSelectItemList(NonTerminalNode node)
        {
            var itemNodes = ExtractDelimitedListItems(node, "SelectItemList");
            var itemTexts = itemNodes.Select(RenderNodeInline).ToList();
            var useMultiline = ShouldUseMultilineSelectLayout(itemNodes, itemTexts);

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

            if (!threshold.AllowExpressions && itemNodes.Any(itemNode => !IsSimpleSelectItem(itemNode)))
            {
                return true;
            }

            var inlineLineLength = "SELECT ".Length + string.Join(GetInlineCommaSeparator(), itemTexts).Length;
            return inlineLineLength > Math.Max(1, threshold.MaxLineLength);
        }

        private bool ShouldUseMultilineLayout(SqlListLayoutStyle listLayoutStyle, IReadOnlyList<string> itemTexts, int clausePrefixLength)
        {
            if (listLayoutStyle == SqlListLayoutStyle.OnePerLine)
            {
                return true;
            }

            var inlineLength = clausePrefixLength + string.Join(GetInlineCommaSeparator(), itemTexts).Length;
            return inlineLength > WrapByWidthLineLength;
        }

        private bool IsSimpleSelectItem(ParseTreeNode itemNode)
        {
            var terminals = CollectTerminalNodes(itemNode);
            if (terminals.Count == 0)
            {
                return false;
            }

            foreach (var terminalNode in terminals)
            {
                var rawToken = terminalNode.Token.OriginalString;
                if (string.IsNullOrWhiteSpace(rawToken))
                {
                    continue;
                }

                if (terminalNode.Token is KeywordToken keywordToken)
                {
                    var keyword = keywordToken.OriginalString.ToUpperInvariant();
                    if (!string.Equals(keyword, "AS", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    continue;
                }

                if (char.IsDigit(rawToken[0]) || rawToken[0] == '\'')
                {
                    return false;
                }

                if (string.Equals(rawToken, "(", StringComparison.Ordinal) ||
                    string.Equals(rawToken, ")", StringComparison.Ordinal))
                {
                    return false;
                }

                if (IsBinaryOperatorToken(rawToken))
                {
                    return false;
                }
            }

            return true;
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
            var builder = new StringBuilder();
            string? previousToken = null;
            var previousTokenWasKeyword = false;

            foreach (var terminalNode in CollectTerminalNodes(node))
            {
                var rawToken = terminalNode.Token.OriginalString;
                if (string.IsNullOrEmpty(rawToken))
                {
                    continue;
                }

                var isKeyword = terminalNode.Token is KeywordToken;
                var tokenForRules = isKeyword ? rawToken.ToUpperInvariant() : rawToken;
                if (ShouldWriteSpaceBeforeToken(previousToken, previousTokenWasKeyword, tokenForRules))
                {
                    builder.Append(' ');
                }

                builder.Append(FormatToken(rawToken, tokenForRules, isKeyword));
                previousToken = tokenForRules;
                previousTokenWasKeyword = isKeyword;
            }

            return builder.ToString();
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

            var isKeyword = lastTerminalNode.Token is KeywordToken;
            _previousToken = isKeyword ? rawToken.ToUpperInvariant() : rawToken;
            _previousTokenWasKeyword = isKeyword;
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

            if (JoinPrefixKeywords.Contains(token) && JoinPrefixKeywords.Contains(_previousToken))
            {
                return false;
            }

            if (IsMajorClause(clauseKind))
            {
                return IsClauseNewlineEnabled(clauseKind);
            }

            return JoinStartKeywords.Contains(token);
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
            if (!_writer.HasContent && previousToken == null)
            {
                return false;
            }

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
            _previousToken = null;
            _previousTokenWasKeyword = false;
        }
    }
}
