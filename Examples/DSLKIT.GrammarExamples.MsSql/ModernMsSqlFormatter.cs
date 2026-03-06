using System;
using System.Collections.Generic;
using DSLKIT.GrammarExamples.MsSql.Formatting;

namespace DSLKIT.GrammarExamples.MsSql
{
    public static class ModernMsSqlFormatter
    {
        public static SqlFormattingResult TryFormat(string source, SqlFormattingOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(source);

            var formattingOptions = options ?? new SqlFormattingOptions();
            var segments = SqlServerScriptPreprocessor.Split(source);
            if (segments.Count == 0)
            {
                return TryFormatBatch(source, formattingOptions);
            }

            if (segments.Count == 1 && segments[0].Kind == SqlScriptSegmentKind.Batch)
            {
                return TryFormatBatch(segments[0].Text, formattingOptions);
            }

            var formattedSegments = new List<string>(segments.Count);
            foreach (var segment in segments)
            {
                if (segment.Kind == SqlScriptSegmentKind.Batch)
                {
                    var batchResult = TryFormatBatch(segment.Text, formattingOptions, segment.StartPosition);
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

        private static SqlFormattingResult TryFormatBatch(string source, SqlFormattingOptions options, int startPosition = 0)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseScript(source);
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

            var formattingVisitor = new SqlFormattingVisitor(options);
            formattingVisitor.Visit(parseResult.ParseTree);
            return SqlFormattingResult.Success(formattingVisitor.GetFormattedSql());
        }

        private static string TrimTrailingLineBreaks(string? text)
        {
            return text?.TrimEnd('\r', '\n') ?? string.Empty;
        }
    }
}
