using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public void ParseAndFormat_ShouldProcessSqlServerSamples_AndWriteDetailedReport()
        {
            var repositoryRoot = ResolveRepositoryRoot();
            var sqlSamplesRoot = Path.Combine(repositoryRoot, "sql-server-samples");
            var sqlFiles = Directory
                .EnumerateFiles(sqlSamplesRoot, "*.sql", SearchOption.AllDirectories)
                .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var successful = new List<SqlScriptRunResult>();
            var failed = new List<SqlScriptRunResult>();

            foreach (var sqlFilePath in sqlFiles)
            {
                var relativePath = Path.GetRelativePath(sqlSamplesRoot, sqlFilePath);
                var script = File.ReadAllText(sqlFilePath);

                try
                {
                    var parseResult = ModernMsSqlGrammarExample.ParseScript(script);
                    if (!parseResult.IsSuccess || parseResult.ParseTree == null)
                    {
                        failed.Add(SqlScriptRunResult.CreateFailure(
                            relativePath,
                            script.Length,
                            "Parse",
                            NormalizeErrorMessage(
                                $"Position: {parseResult.Error?.ErrorPosition}; Message: {parseResult.Error?.Message ?? "Parse failed."}")));
                        continue;
                    }

                    var formatResult = ModernMsSqlFormatter.TryFormat(script);
                    if (!formatResult.IsSuccess)
                    {
                        failed.Add(SqlScriptRunResult.CreateFailure(
                            relativePath,
                            script.Length,
                            "Format",
                            NormalizeErrorMessage(formatResult.ErrorMessage ?? "Formatting failed.")));
                        continue;
                    }

                    successful.Add(SqlScriptRunResult.CreateSuccess(relativePath, script.Length));
                }
                catch (Exception exception)
                {
                    failed.Add(SqlScriptRunResult.CreateFailure(
                        relativePath,
                        script.Length,
                        "Exception",
                        NormalizeErrorMessage($"{exception.GetType().Name}: {exception.Message}")));
                }
            }

            var reportPath = Path.Combine(
                repositoryRoot,
                "test-results",
                "mssql-formatter-sql-server-samples-report.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            var report = BuildReport(sqlSamplesRoot, sqlFiles.Count, successful, failed);
            File.WriteAllText(reportPath, report, Encoding.UTF8);

            testOutput.WriteLine($"Total files: {sqlFiles.Count}");
            testOutput.WriteLine($"Successful: {successful.Count}");
            testOutput.WriteLine($"Failed: {failed.Count}");
            testOutput.WriteLine($"Detailed report: {reportPath}");

            var crashFailures = failed.Where(result => result.Stage == "Exception").ToList();
            Assert.True(
                crashFailures.Count == 0,
                $"Unhandled exceptions count: {crashFailures.Count}. See report: {reportPath}");
        }

        private static string BuildReport(
            string sqlSamplesRoot,
            int totalCount,
            IReadOnlyCollection<SqlScriptRunResult> successful,
            IReadOnlyCollection<SqlScriptRunResult> failed)
        {
            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine($"Generated (UTC): {DateTimeOffset.UtcNow:O}");
            reportBuilder.AppendLine($"SQL samples root: {sqlSamplesRoot}");
            reportBuilder.AppendLine($"Total files: {totalCount}");
            reportBuilder.AppendLine($"Successful: {successful.Count}");
            reportBuilder.AppendLine($"Failed: {failed.Count}");
            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Successful files:");
            foreach (var success in successful)
            {
                reportBuilder.AppendLine($"- {success.RelativePath} ({success.CharactersCount} chars)");
            }

            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Failed files:");
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
    }
}
