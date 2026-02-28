using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.GrammarExamples.MsSql.Formatting;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class MsSqlFormatterTests
    {
        [Theory]
        [MemberData(nameof(ValidFormattingScripts))]
        public void TryFormat_ShouldPreserveSqlContent_WhenIgnoringWhitespaceAndCase(string scriptName, string scriptText)
        {
            var formattingOptions = new SqlFormattingOptions
            {
                UppercaseKeywords = true
            };

            var result = ModernMsSqlFormatter.TryFormat(scriptText, formattingOptions);

            result.IsSuccess.Should().BeTrue(
                $"script '{scriptName}' should format, but failed with: {result.ErrorMessage}");
            result.FormattedSql.Should().NotBeNullOrWhiteSpace();

            NormalizeSql(scriptText).Should().Be(
                NormalizeSql(result.FormattedSql!),
                $"formatted script '{scriptName}' should preserve all significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldReturnError_ForInvalidSql()
        {
            const string invalidSql = "SELECT FROM dbo.Orders;";

            var result = ModernMsSqlFormatter.TryFormat(invalidSql, new SqlFormattingOptions
            {
                UppercaseKeywords = true
            });

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            result.FormattedSql.Should().BeNull();
        }

        public static IEnumerable<object[]> ValidFormattingScripts()
        {
            var scriptsRoot = ResolveScriptsRoot();
            foreach (var filePath in Directory.EnumerateFiles(scriptsRoot, "*.sql", SearchOption.AllDirectories).OrderBy(i => i))
            {
                var scriptName = Path.GetRelativePath(scriptsRoot, filePath);
                var scriptText = File.ReadAllText(filePath);
                yield return new object[] { scriptName, scriptText };
            }
        }

        private static string ResolveScriptsRoot()
        {
            var outputPath = Path.Combine(AppContext.BaseDirectory, "GrammarExamples", "TestData", "MsSql", "Formatting", "Valid");
            if (Directory.Exists(outputPath))
            {
                return outputPath;
            }

            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), "GrammarExamples", "TestData", "MsSql", "Formatting", "Valid");
            if (Directory.Exists(projectPath))
            {
                return projectPath;
            }

            throw new DirectoryNotFoundException("Could not find SQL formatter test data folder.");
        }

        private static string NormalizeSql(string sqlText)
        {
            return new string(
                sqlText
                    .Where(symbol => !char.IsWhiteSpace(symbol))
                    .Select(char.ToUpperInvariant)
                    .ToArray());
        }
    }
}
