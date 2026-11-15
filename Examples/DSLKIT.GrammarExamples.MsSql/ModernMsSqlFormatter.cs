using System;
using System.Collections.Generic;
using DSLKIT.GrammarExamples.MsSql.Formatting;
using DSLKIT.Parser;

namespace DSLKIT.GrammarExamples.MsSql
{
    public static class ModernMsSqlFormatter
    {
        public static SqlFormattingResult TryFormat(string source, SqlFormattingOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            var formattingOptions = options ?? new SqlFormattingOptions();
            var documentParseResult = ModernMsSqlGrammarExample.ParseDocument(source, formattingOptions.Dialect);
            if (!documentParseResult.IsSuccess || documentParseResult.Document == null)
            {
                return documentParseResult.Error != null
                    ? SqlFormattingResult.Failure(documentParseResult.Error)
                    : SqlFormattingResult.Failure("Parse failed.");
            }

            var formattedSegments = new List<string>(documentParseResult.Document.Segments.Count);
            foreach (var segment in documentParseResult.Document.Segments)
            {
                if (segment is SqlBatchDocumentNode batchNode)
                {
                    var batchResult = TryFormatBatch(
                        batchNode.ParseResult,
                        formattingOptions,
                        batchNode.Text,
                        batchNode.StartPosition);
                    if (!batchResult.IsSuccess)
                    {
                        return batchResult;
                    }

                    var formattedBatch = TrimTrailingLineBreaks(batchResult.FormattedSql);
                    if (!string.IsNullOrEmpty(formattedBatch))
                    {
                        formattedSegments.Add(formattedBatch);
                    }

                    continue;
                }

                formattedSegments.Add(segment.Text.Trim());
            }

            var formattedScript = string.Join(Environment.NewLine, formattedSegments);
            if (formattingOptions.Eof.Newline && !string.IsNullOrEmpty(formattedScript))
            {
                formattedScript = $"{formattedScript}{Environment.NewLine}";
            }

            return SqlFormattingResult.Success(formattedScript);
        }

        private static SqlFormattingResult TryFormatBatch(
            ParseResult parseResult,
            SqlFormattingOptions options,
            string sourceText,
            int startPosition = 0)
        {
            if (!parseResult.IsSuccess || parseResult.ParseTree == null)
            {
                if (parseResult.Error != null)
                {
                    var error = startPosition == 0
                        ? parseResult.Error
                        : parseResult.Error with
                        {
                            ErrorPosition = parseResult.Error.ErrorPosition + startPosition
                        };
                    return SqlFormattingResult.Failure(error);
                }

                return SqlFormattingResult.Failure("Parse failed.");
            }

            if (!HasTerminalNodes(parseResult.ParseTree))
            {
                return SqlFormattingResult.Success(
                    string.IsNullOrWhiteSpace(sourceText)
                        ? string.Empty
                        : TrimTrailingLineBreaks(sourceText));
            }

            var formattedSql = FormatParseTree(parseResult.ParseTree, options);

            // Removing a separator can make a previously valid batch invalid or change
            // how the grammar groups statements. In that case preserve its separators.
            if (options.Statement.TerminateWithSemicolon == SqlStatementTerminationMode.Never)
            {
                var reparsed = ModernMsSqlGrammarExample.ParseBatch(formattedSql, options.Dialect);
                if (!reparsed.IsSuccess || reparsed.ParseTree == null ||
                    CountStatements(reparsed.ParseTree) != CountStatements(parseResult.ParseTree))
                {
                    var preservingOptions = options with
                    {
                        Statement = options.Statement with
                        {
                            TerminateWithSemicolon = SqlStatementTerminationMode.ExistingOnly
                        }
                    };
                    formattedSql = FormatParseTree(parseResult.ParseTree, preservingOptions);
                }
            }

            return SqlFormattingResult.Success(formattedSql);
        }

        private static string FormatParseTree(ParseTreeNode parseTree, SqlFormattingOptions options)
        {
            var formattingVisitor = new SqlFormattingVisitor(options, parseTree);
            formattingVisitor.Visit(parseTree);
            return formattingVisitor.GetFormattedSql();
        }

        private static int CountStatements(ParseTreeNode node)
        {
            var count = node is NonTerminalNode nonTerminal &&
                nonTerminal.NonTerminal.Name is "Statement" or "ImplicitStatementNoLeadingWith" ? 1 : 0;
            foreach (var child in node.Children)
            {
                count += CountStatements(child);
            }

            return count;
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

        private static string TrimTrailingLineBreaks(string? text)
        {
            return text?.TrimEnd('\r', '\n') ?? string.Empty;
        }
    }
}
