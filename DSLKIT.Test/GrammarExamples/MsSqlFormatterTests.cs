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
                KeywordCase = SqlKeywordCase.Upper
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
                KeywordCase = SqlKeywordCase.Upper
            });

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
            result.FormattedSql.Should().BeNull();
        }

        [Fact]
        public void TryFormat_ShouldApplyStage1Settings()
        {
            const string sourceSql = "SELECT(a) AS A,b AS B FROM dbo.t AS t WHERE a=1";
            var options = new SqlFormattingOptions
            {
                Spaces = new SqlSpacesFormattingOptions
                {
                    AfterComma = false,
                    AroundBinaryOperators = false,
                    InsideParentheses = SqlParenthesesSpacing.Always,
                    BeforeSemicolon = true
                },
                Statement = new SqlStatementFormattingOptions
                {
                    TerminateWithSemicolon = SqlStatementTerminationMode.Always
                },
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.WrapByWidth
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);

            result.IsSuccess.Should().BeTrue();
            result.FormattedSql.Should().Contain("SELECT ( A ) AS A,B AS B");
            result.FormattedSql.Should().Contain("WHERE A=1 ;");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage2LayoutSettings()
        {
            const string sourceSql = "SELECT a AS A, b AS B FROM dbo.t AS t WHERE x = 1 ORDER BY a";
            var options = new SqlFormattingOptions
            {
                Layout = new SqlLayoutFormattingOptions
                {
                    IndentSize = 2,
                    BlankLineBetweenClauses = SqlBlankLineBetweenClausesMode.BetweenMajorClauses,
                    NewlineBeforeClause = new SqlClauseNewlineOptions
                    {
                        With = true,
                        Select = true,
                        From = true,
                        Where = true,
                        GroupBy = true,
                        Having = true,
                        OrderBy = true,
                        Option = true
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("\n  A AS A,");
            formattedSql.Should().Contain("FROM DBO.T AS T\n\nWHERE");
            formattedSql.Should().Contain("WHERE X = 1\n\nORDER BY");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage3ListSettings()
        {
            const string sourceSql = "SELECT SUM(x) AS Total, COUNT(*) AS Cnt FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Lists = new SqlListsFormattingOptions
                {
                    CommaStyle = SqlCommaStyle.Leading,
                    SelectItems = SqlListLayoutStyle.OnePerLine
                },
                Align = new SqlAlignFormattingOptions
                {
                    SelectAliases = true
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().MatchRegex("SUM\\s*\\(X\\)\\s{2,}AS\\s+TOTAL");
            formattedSql.Should().MatchRegex("\\n\\s*,\\s*COUNT\\s*\\(\\s*\\*\\s*\\)\\s+AS\\s+CNT");
        }

        [Fact]
        public void TryFormat_ShouldCompactShortSelect_WhenThresholdAllowsIt()
        {
            const string sourceSql = "SELECT a AS A, b AS B FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.OnePerLine,
                    SelectCompactThreshold = new SqlSelectCompactThresholdOptions
                    {
                        MaxItems = 2,
                        MaxLineLength = 120,
                        AllowExpressions = true
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("SELECT A AS A, B AS B");
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

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n");
        }
    }
}
