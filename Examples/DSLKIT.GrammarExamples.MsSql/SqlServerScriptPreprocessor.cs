using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal enum SqlScriptSegmentKind
    {
        Batch,
        BatchSeparator,
        SqlcmdCommand
    }

    internal readonly record struct SqlScriptSegment(
        SqlScriptSegmentKind Kind,
        string Text,
        int StartPosition,
        int? BatchRepeatCount = null);

    internal static class SqlServerScriptPreprocessor
    {
        private static readonly Regex GoSeparatorPattern = new(
            @"^\s*(?:/\*.*?\*/\s*)*GO(?:\s+(?<count>\d+))?(?:\s*/\*.*?\*/\s*)*(?:--.*)?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex SqlcmdCommandPattern = new(
            @"^\s*:[a-z_][a-z0-9_]*(?:[ \t].*)?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static IReadOnlyList<SqlScriptSegment> Split(string source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var segments = new List<SqlScriptSegment>();
            var currentBatchBuilder = new StringBuilder();
            var currentBatchStart = 0;
            var hasBatchLines = false;

            foreach (var line in EnumerateLines(source))
            {
                if (TryCreateControlSegment(line, out var controlSegment))
                {
                    FlushBatch(segments, currentBatchBuilder, currentBatchStart, hasBatchLines);
                    hasBatchLines = false;
                    currentBatchStart = line.StartPosition + line.LengthWithLineBreak;
                    segments.Add(controlSegment);
                    continue;
                }

                if (!hasBatchLines)
                {
                    currentBatchStart = line.StartPosition;
                    hasBatchLines = true;
                }

                currentBatchBuilder.Append(line.Text);
                currentBatchBuilder.Append(line.LineBreak);
            }

            FlushBatch(segments, currentBatchBuilder, currentBatchStart, hasBatchLines);
            return segments;
        }

        private static void FlushBatch(
            ICollection<SqlScriptSegment> segments,
            StringBuilder currentBatchBuilder,
            int currentBatchStart,
            bool hasBatchLines)
        {
            if (!hasBatchLines)
            {
                return;
            }

            segments.Add(new SqlScriptSegment(
                SqlScriptSegmentKind.Batch,
                currentBatchBuilder.ToString(),
                currentBatchStart));
            currentBatchBuilder.Clear();
        }

        private static bool TryCreateControlSegment(LineInfo line, out SqlScriptSegment segment)
        {
            if (TryParseGoSeparator(line.Text, out var batchRepeatCount))
            {
                segment = new SqlScriptSegment(
                    SqlScriptSegmentKind.BatchSeparator,
                    line.Text,
                    line.StartPosition,
                    batchRepeatCount);
                return true;
            }

            if (IsSqlcmdCommandLine(line.Text))
            {
                segment = new SqlScriptSegment(
                    SqlScriptSegmentKind.SqlcmdCommand,
                    line.Text,
                    line.StartPosition);
                return true;
            }

            segment = default;
            return false;
        }

        private static bool TryParseGoSeparator(string line, out int? batchRepeatCount)
        {
            var match = GoSeparatorPattern.Match(line);
            if (!match.Success)
            {
                batchRepeatCount = null;
                return false;
            }

            batchRepeatCount = match.Groups["count"].Success
                ? int.Parse(match.Groups["count"].Value)
                : null;
            return true;
        }

        private static bool IsSqlcmdCommandLine(string line)
        {
            return SqlcmdCommandPattern.IsMatch(line);
        }

        private static IEnumerable<LineInfo> EnumerateLines(string source)
        {
            var lineStart = 0;
            while (lineStart < source.Length)
            {
                var index = lineStart;
                while (index < source.Length &&
                    source[index] != '\r' &&
                    source[index] != '\n')
                {
                    index++;
                }

                var lineBreakLength = 0;
                if (index < source.Length)
                {
                    lineBreakLength = source[index] == '\r' &&
                        index + 1 < source.Length &&
                        source[index + 1] == '\n'
                        ? 2
                        : 1;
                }

                var lineText = source.Substring(lineStart, index - lineStart);
                var lineBreak = lineBreakLength == 0
                    ? string.Empty
                    : source.Substring(index, lineBreakLength);
                yield return new LineInfo(
                    lineText,
                    lineBreak,
                    lineStart,
                    lineText.Length + lineBreakLength);
                lineStart = index + lineBreakLength;
            }
        }

        private readonly record struct LineInfo(
            string Text,
            string LineBreak,
            int StartPosition,
            int LengthWithLineBreak);
    }
}
