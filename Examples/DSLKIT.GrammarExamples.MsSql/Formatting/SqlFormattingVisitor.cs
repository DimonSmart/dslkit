using System;
using System.Collections.Generic;
using System.Text;
using DSLKIT.Parser;
using DSLKIT.Tokens;

namespace DSLKIT.GrammarExamples.MsSql.Formatting
{
    internal sealed class SqlFormattingVisitor : SqlParseTreeVisitorBase
    {
        private static readonly HashSet<string> ClauseStartKeywords = new(StringComparer.Ordinal)
        {
            "WITH",
            "SELECT",
            "FROM",
            "WHERE",
            "GROUP",
            "HAVING",
            "ORDER",
            "OFFSET",
            "FETCH",
            "UNION",
            "INTERSECT",
            "EXCEPT",
            "INNER",
            "LEFT",
            "RIGHT",
            "FULL",
            "CROSS",
            "OUTER",
            "JOIN",
            "ON",
            "AND",
            "OR",
            "WHEN",
            "THEN",
            "ELSE"
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

        private static readonly HashSet<string> OperatorTokens = new(StringComparer.Ordinal)
        {
            "=",
            "<>",
            "!=",
            "<",
            "<=",
            ">",
            ">=",
            "+",
            "-",
            "*",
            "/",
            "%"
        };

        private static readonly HashSet<string> IndentedNonTerminalNames = new(StringComparer.Ordinal)
        {
            "CteDefinitionList",
            "SelectItemList",
            "TableSourceList",
            "OrderItemList",
            "ExpressionList",
            "IdentifierList",
            "FunctionArgumentList",
            "CaseWhenList"
        };

        private readonly SqlFormattingOptions _options;
        private readonly IndentedSqlTextWriter _writer = new(new StringBuilder());
        private readonly HashSet<string> _activeIndentScopes = new(StringComparer.Ordinal);

        private string? _previousToken;
        private bool _previousTokenWasKeyword;

        public SqlFormattingVisitor(SqlFormattingOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string GetFormattedSql()
        {
            return _writer.ToString();
        }

        protected override void VisitNonTerminal(NonTerminalNode node)
        {
            if (!IndentedNonTerminalNames.Contains(node.NonTerminal.Name))
            {
                VisitChildren(node);
                return;
            }

            if (_activeIndentScopes.Contains(node.NonTerminal.Name))
            {
                VisitChildren(node);
                return;
            }

            _activeIndentScopes.Add(node.NonTerminal.Name);
            try
            {
                using (_writer.PushIndent())
                {
                    VisitChildren(node);
                }
            }
            finally
            {
                _activeIndentScopes.Remove(node.NonTerminal.Name);
            }
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
            var formattedToken = isKeyword && _options.UppercaseKeywords
                ? tokenForRules
                : rawToken;

            if (ShouldStartNewLineBefore(tokenForRules))
            {
                _writer.WriteLine();
            }

            if (ShouldWriteSpaceBefore(tokenForRules))
            {
                _writer.WriteSpace();
            }

            _writer.WriteToken(formattedToken);

            if (string.Equals(tokenForRules, ",", StringComparison.Ordinal) ||
                string.Equals(tokenForRules, ";", StringComparison.Ordinal))
            {
                _writer.WriteLine();
            }

            _previousToken = tokenForRules;
            _previousTokenWasKeyword = isKeyword;
        }

        private bool ShouldStartNewLineBefore(string token)
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

            return ClauseStartKeywords.Contains(token);
        }

        private bool ShouldWriteSpaceBefore(string token)
        {
            if (!_writer.HasContent || _writer.IsLineStart || _previousToken == null)
            {
                return false;
            }

            if (TokensWithoutLeadingSpace.Contains(token))
            {
                return false;
            }

            if (string.Equals(token, "(", StringComparison.Ordinal))
            {
                return ShouldWriteSpaceBeforeOpenParenthesis();
            }

            if (TokensWithoutTrailingSpace.Contains(_previousToken))
            {
                return false;
            }

            return true;
        }

        private bool ShouldWriteSpaceBeforeOpenParenthesis()
        {
            if (_previousToken == null)
            {
                return false;
            }

            if (string.Equals(_previousToken, "(", StringComparison.Ordinal) ||
                string.Equals(_previousToken, ".", StringComparison.Ordinal))
            {
                return false;
            }

            if (_previousTokenWasKeyword)
            {
                return true;
            }

            return OperatorTokens.Contains(_previousToken);
        }
    }
}
