using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DSLKIT.GrammarExamples.MsSql;
using Xunit;
using Xunit.Abstractions;

namespace DSLKIT.Test.GrammarExamples
{
    public class MsSqlFormatterSqlServerSamplesTests
    {
        private readonly ITestOutputHelper testOutput;

        public MsSqlFormatterSqlServerSamplesTests(ITestOutputHelper testOutput)
        {
            this.testOutput = testOutput;
        }

        [Fact]
        public void ParseSelectBatchesSplitByGo_ShouldProcessSqlServerSamples_AndWriteDetailedReport()
        {
            var repositoryRoot = ResolveRepositoryRoot();
            var sqlSamplesRoot = Path.Combine(repositoryRoot, "sql-server-samples");
            var sqlFiles = Directory
                .EnumerateFiles(sqlSamplesRoot, "*.sql", SearchOption.AllDirectories)
                .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var successful = new List<SqlScriptRunResult>();
            var failed = new List<SqlScriptRunResult>();
            var totalBatchCount = 0;
            var parsedSelectBatchCount = 0;
            var skippedWithoutSelectCount = 0;

            foreach (var sqlFilePath in sqlFiles)
            {
                var relativePath = Path.GetRelativePath(sqlSamplesRoot, sqlFilePath);
                var script = File.ReadAllText(sqlFilePath);
                var batches = SplitScriptByGo(script);
                totalBatchCount += batches.Count;

                foreach (var batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch.Text))
                    {
                        continue;
                    }

                    if (!ContainsSelect(batch.Text))
                    {
                        skippedWithoutSelectCount++;
                        continue;
                    }

                    parsedSelectBatchCount++;
                    var batchPath = $"{relativePath} [batch {batch.Index}]";

                    try
                    {
                        var parseResult = ModernMsSqlGrammarExample.ParseScript(batch.Text);
                        if (!parseResult.IsSuccess || parseResult.ParseTree == null)
                        {
                            failed.Add(SqlScriptRunResult.CreateFailure(
                                batchPath,
                                batch.Text.Length,
                                "Parse",
                                NormalizeErrorMessage(
                                    $"Position: {parseResult.Error?.ErrorPosition}; Message: {parseResult.Error?.Message ?? "Parse failed."}")));
                            continue;
                        }

                        successful.Add(SqlScriptRunResult.CreateSuccess(batchPath, batch.Text.Length));
                    }
                    catch (Exception exception)
                    {
                        failed.Add(SqlScriptRunResult.CreateFailure(
                            batchPath,
                            batch.Text.Length,
                            "Exception",
                            NormalizeErrorMessage($"{exception.GetType().Name}: {exception.Message}")));
                    }
                }
            }

            var reportPath = Path.Combine(
                repositoryRoot,
                "test-results",
                "mssql-parser-select-batches-sql-server-samples-report.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            var report = BuildReport(
                sqlSamplesRoot,
                sqlFiles.Count,
                totalBatchCount,
                parsedSelectBatchCount,
                skippedWithoutSelectCount,
                successful,
                failed);
            File.WriteAllText(reportPath, report, Encoding.UTF8);

            testOutput.WriteLine($"Total files: {sqlFiles.Count}");
            testOutput.WriteLine($"Total batches: {totalBatchCount}");
            testOutput.WriteLine($"Parsed SELECT batches: {parsedSelectBatchCount}");
            testOutput.WriteLine($"Skipped non-SELECT batches: {skippedWithoutSelectCount}");
            testOutput.WriteLine($"Successful SELECT batches: {successful.Count}");
            testOutput.WriteLine($"Failed SELECT batches: {failed.Count}");
            testOutput.WriteLine($"Detailed report: {reportPath}");

            var crashFailures = failed.Where(result => result.Stage == "Exception").ToList();
            Assert.True(
                crashFailures.Count == 0,
                $"Unhandled exceptions count: {crashFailures.Count}. See report: {reportPath}");
        }

        private static string BuildReport(
            string sqlSamplesRoot,
            int totalFileCount,
            int totalBatchCount,
            int parsedSelectBatchCount,
            int skippedWithoutSelectCount,
            IReadOnlyCollection<SqlScriptRunResult> successful,
            IReadOnlyCollection<SqlScriptRunResult> failed)
        {
            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine($"Generated (UTC): {DateTimeOffset.UtcNow:O}");
            reportBuilder.AppendLine($"SQL samples root: {sqlSamplesRoot}");
            reportBuilder.AppendLine($"Total files: {totalFileCount}");
            reportBuilder.AppendLine($"Total batches (split by GO): {totalBatchCount}");
            reportBuilder.AppendLine($"Parsed SELECT batches: {parsedSelectBatchCount}");
            reportBuilder.AppendLine($"Skipped non-SELECT batches: {skippedWithoutSelectCount}");
            reportBuilder.AppendLine($"Successful SELECT batches: {successful.Count}");
            reportBuilder.AppendLine($"Failed SELECT batches: {failed.Count}");
            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Successful batches:");
            foreach (var success in successful)
            {
                reportBuilder.AppendLine($"- {success.RelativePath} ({success.CharactersCount} chars)");
            }

            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Failed batches:");
            foreach (var failure in failed)
            {
                reportBuilder.AppendLine(
                    $"- {failure.RelativePath} ({failure.CharactersCount} chars) | Stage: {failure.Stage} | Error: {failure.ErrorMessage}");
            }

            return reportBuilder.ToString();
        }

        private static string ResolveRepositoryRoot()
        {
            var startingPoints = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
            foreach (var startingPoint in startingPoints)
            {
                var current = new DirectoryInfo(startingPoint);
                while (current != null)
                {
                    var hasSolution = File.Exists(Path.Combine(current.FullName, "DSLKIT.sln"));
                    var hasSamples = Directory.Exists(Path.Combine(current.FullName, "sql-server-samples"));
                    if (hasSolution && hasSamples)
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }
            }

            throw new DirectoryNotFoundException("Could not locate repository root with DSLKIT.sln and sql-server-samples folder.");
        }

        private static string NormalizeErrorMessage(string message)
        {
            return message
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        private static IReadOnlyList<SqlBatch> SplitScriptByGo(string script)
        {
            var normalized = script.Replace("\r\n", "\n");
            var lines = normalized.Split('\n');
            var batches = new List<SqlBatch>();
            var currentBatchBuilder = new StringBuilder();
            var batchIndex = 1;

            foreach (var line in lines)
            {
                if (IsGoSeparatorLine(line))
                {
                    var batchText = currentBatchBuilder.ToString().TrimEnd('\n');
                    batches.Add(new SqlBatch(batchIndex, batchText));
                    batchIndex++;
                    currentBatchBuilder.Clear();
                    continue;
                }

                currentBatchBuilder.Append(line);
                currentBatchBuilder.Append('\n');
            }

            var lastBatchText = currentBatchBuilder.ToString().TrimEnd('\n');
            batches.Add(new SqlBatch(batchIndex, lastBatchText));
            return batches;
        }

        private static bool IsGoSeparatorLine(string line)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("GO", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (trimmed.Length == 2)
            {
                return true;
            }

            var tail = trimmed[2..].Trim();
            return tail.Length == 0 ||
                tail.StartsWith("--", StringComparison.Ordinal) ||
                int.TryParse(tail, out _);
        }

        private static bool ContainsSelect(string sqlBatch)
        {
            return Regex.IsMatch(sqlBatch, @"\bSELECT\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private sealed class SqlScriptRunResult
        {
            private SqlScriptRunResult(string relativePath, int charactersCount, bool isSuccess, string stage, string errorMessage)
            {
                RelativePath = relativePath;
                CharactersCount = charactersCount;
                IsSuccess = isSuccess;
                Stage = stage;
                ErrorMessage = errorMessage;
            }

            public string RelativePath { get; }

            public int CharactersCount { get; }

            public bool IsSuccess { get; }

            public string Stage { get; }

            public string ErrorMessage { get; }

            public static SqlScriptRunResult CreateSuccess(string relativePath, int charactersCount)
            {
                return new SqlScriptRunResult(relativePath, charactersCount, true, "None", string.Empty);
            }

            public static SqlScriptRunResult CreateFailure(string relativePath, int charactersCount, string stage, string errorMessage)
            {
                return new SqlScriptRunResult(relativePath, charactersCount, false, stage, errorMessage);
            }
        }

        private sealed class SqlBatch
        {
            public SqlBatch(int index, string text)
            {
                Index = index;
                Text = text;
            }

            public int Index { get; }

            public string Text { get; }
        }
    }
}
