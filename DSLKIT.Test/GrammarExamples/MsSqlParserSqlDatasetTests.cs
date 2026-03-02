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
    public class MsSqlParserSqlDatasetTests
    {
        private readonly ITestOutputHelper testOutput;

        public MsSqlParserSqlDatasetTests(ITestOutputHelper testOutput)
        {
            this.testOutput = testOutput;
        }

        [Fact]
        public void ParseSqlDatasetFilesContainingSelect_ShouldProduceSuccessAndFailureReport()
        {
            var repositoryRoot = ResolveRepositoryRoot();
            var datasetRoot = Path.Combine(repositoryRoot, "sql-dataset");
            var sqlFiles = Directory
                .EnumerateFiles(datasetRoot, "*.sql", SearchOption.AllDirectories)
                .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var successfulFiles = new List<string>();
            var failedFiles = new List<SqlDatasetParseFailure>();
            var selectFilesCount = 0;
            var skippedTemplatePlaceholderFiles = 0;

            foreach (var sqlFilePath in sqlFiles)
            {
                var script = File.ReadAllText(sqlFilePath);
                if (!ContainsSelect(script))
                {
                    continue;
                }

                if (ContainsTemplatePlaceholder(script))
                {
                    skippedTemplatePlaceholderFiles++;
                    continue;
                }

                selectFilesCount++;
                var relativePath = Path.GetRelativePath(datasetRoot, sqlFilePath);
                try
                {
                    var parseResult = ModernMsSqlGrammarExample.ParseScript(script);
                    if (parseResult.IsSuccess && parseResult.ParseTree != null)
                    {
                        successfulFiles.Add(relativePath);
                        continue;
                    }

                    var parseError = parseResult.Error?.Message ?? "Parse failed.";
                    failedFiles.Add(new SqlDatasetParseFailure(
                        relativePath,
                        script.Length,
                        "Parse",
                        GetParseErrorType(parseError),
                        NormalizeErrorMessage(
                            $"Position: {parseResult.Error?.ErrorPosition}; Message: {parseError}")));
                }
                catch (Exception exception)
                {
                    failedFiles.Add(new SqlDatasetParseFailure(
                        relativePath,
                        script.Length,
                        "Exception",
                        $"Exception: {exception.GetType().Name}",
                        NormalizeErrorMessage($"{exception.GetType().Name}: {exception.Message}")));
                }
            }

            var reportPath = Path.Combine(
                repositoryRoot,
                "test-results",
                "mssql-parser-sql-dataset-select-report.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            var report = BuildReport(datasetRoot, sqlFiles.Count, selectFilesCount, skippedTemplatePlaceholderFiles, successfulFiles, failedFiles);
            File.WriteAllText(reportPath, report, Encoding.UTF8);

            testOutput.WriteLine($"Dataset SQL files: {sqlFiles.Count}");
            testOutput.WriteLine($"Files containing SELECT: {selectFilesCount}");
            testOutput.WriteLine($"Skipped (template placeholders): {skippedTemplatePlaceholderFiles}");
            testOutput.WriteLine($"Successful parse: {successfulFiles.Count}");
            testOutput.WriteLine($"Failed parse: {failedFiles.Count}");
            testOutput.WriteLine($"Detailed report: {reportPath}");

            Assert.True(selectFilesCount > 0, "Expected at least one SQL file containing SELECT in sql-dataset.");
        }

        private static string BuildReport(
            string datasetRoot,
            int totalSqlFiles,
            int selectFilesCount,
            int skippedTemplatePlaceholderFiles,
            IReadOnlyCollection<string> successfulFiles,
            IReadOnlyCollection<SqlDatasetParseFailure> failedFiles)
        {
            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine($"Generated (UTC): {DateTimeOffset.UtcNow:O}");
            reportBuilder.AppendLine($"SQL dataset root: {datasetRoot}");
            reportBuilder.AppendLine($"Total SQL files: {totalSqlFiles}");
            reportBuilder.AppendLine($"Files containing SELECT: {selectFilesCount}");
            reportBuilder.AppendLine($"Skipped (template placeholders): {skippedTemplatePlaceholderFiles}");
            reportBuilder.AppendLine($"Successful parse: {successfulFiles.Count}");
            reportBuilder.AppendLine($"Failed parse: {failedFiles.Count}");
            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Error type summary:");
            foreach (var group in failedFiles
                         .GroupBy(failure => failure.ErrorType)
                         .OrderByDescending(group => group.Count())
                         .ThenBy(group => group.Key, StringComparer.Ordinal))
            {
                reportBuilder.AppendLine($"- {group.Key}: {group.Count()}");
            }

            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Successful files:");
            foreach (var file in successfulFiles.OrderBy(file => file, StringComparer.OrdinalIgnoreCase))
            {
                reportBuilder.AppendLine($"- {file}");
            }

            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Failed files:");
            foreach (var failure in failedFiles.OrderBy(failure => failure.RelativePath, StringComparer.OrdinalIgnoreCase))
            {
                reportBuilder.AppendLine(
                    $"- {failure.RelativePath} ({failure.CharactersCount} chars) | Stage: {failure.Stage} | Type: {failure.ErrorType} | Error: {failure.ErrorMessage}");
            }

            return reportBuilder.ToString();
        }

        private static bool ContainsSelect(string sqlText)
        {
            return Regex.IsMatch(sqlText, @"\bSELECT\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static bool ContainsTemplatePlaceholder(string sqlText)
        {
            // SSMS template placeholders look like <name, sysname, value> — angle brackets with at least one comma inside
            return Regex.IsMatch(sqlText, @"<[^<>]*,[^<>]*>", RegexOptions.CultureInvariant);
        }

        private static string GetParseErrorType(string parseErrorMessage)
        {
            var terminalMatch = Regex.Match(parseErrorMessage, "terminal '([^']+)'", RegexOptions.CultureInvariant);
            if (terminalMatch.Success)
            {
                return $"Parse terminal '{terminalMatch.Groups[1].Value}'";
            }

            return "Parse other";
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
                    var hasDataset = Directory.Exists(Path.Combine(current.FullName, "sql-dataset"));
                    if (hasSolution && hasDataset)
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }
            }

            throw new DirectoryNotFoundException("Could not locate repository root with DSLKIT.sln and sql-dataset folder.");
        }

        private static string NormalizeErrorMessage(string message)
        {
            return message
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        private sealed class SqlDatasetParseFailure
        {
            public SqlDatasetParseFailure(
                string relativePath,
                int charactersCount,
                string stage,
                string errorType,
                string errorMessage)
            {
                RelativePath = relativePath;
                CharactersCount = charactersCount;
                Stage = stage;
                ErrorType = errorType;
                ErrorMessage = errorMessage;
            }

            public string RelativePath { get; }

            public int CharactersCount { get; }

            public string Stage { get; }

            public string ErrorType { get; }

            public string ErrorMessage { get; }
        }
    }
}
