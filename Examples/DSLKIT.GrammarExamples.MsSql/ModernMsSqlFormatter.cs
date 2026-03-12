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
                return SqlFormattingResult.Failure(documentParseResult.Error?.ToString() ?? "Parse failed.");
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
                    return SqlFormattingResult.Failure(error.ToString());
                }

                return SqlFormattingResult.Failure(parseResult.Error?.ToString() ?? "Parse failed.");
            }

            if (!HasTerminalNodes(parseResult.ParseTree))
            {
                return SqlFormattingResult.Success(
                    string.IsNullOrWhiteSpace(sourceText)
                        ? string.Empty
                        : TrimTrailingLineBreaks(sourceText));
            }

            var formattingVisitor = new SqlFormattingVisitor(options);
            formattingVisitor.Visit(parseResult.ParseTree);
            return SqlFormattingResult.Success(formattingVisitor.GetFormattedSql());
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
