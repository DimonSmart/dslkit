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

        [Fact]
        public void TryFormat_ShouldApplyStage4JoinSettings()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.A AS a INNER JOIN dbo.B AS b ON a.Id=b.Id AND a.Type=b.Type AND a.IsActive=1";
            var options = new SqlFormattingOptions
            {
                Joins = new SqlJoinsFormattingOptions
                {
                    NewlinePerJoin = true,
                    OnNewLine = true,
                    MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                    {
                        MaxTokensSingleLine = 5,
                        BreakOn = SqlJoinMultilineBreakOnMode.AndOnly
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("INNER JOIN DBO.B AS B");
            formattedSql.Should().MatchRegex("\\n\\s+ON\\s+A\\.ID\\s*=\\s*B\\.ID");
            formattedSql.Should().MatchRegex("\\n\\s+AND\\s+A\\.TYPE\\s*=\\s*B\\.TYPE");
            formattedSql.Should().MatchRegex("\\n\\s+AND\\s+A\\.ISACTIVE\\s*=\\s*1");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage5PredicateSettings()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.t AS t WHERE a = 1 AND b = 2 OR c = 3";
            var options = new SqlFormattingOptions
            {
                Predicates = new SqlPredicatesFormattingOptions
                {
                    MultilineWhere = true,
                    LogicalOperatorLineBreak = SqlLogicalOperatorLineBreakMode.BeforeOperator,
                    InlineSimplePredicate = new SqlInlineSimplePredicateOptions
                    {
                        MaxConditions = 0,
                        MaxLineLength = 120,
                        AllowOnlyAnd = true
                    },
                    ParenthesizeMixedAndOr = new SqlParenthesizeMixedAndOrOptions
                    {
                        Mode = SqlParenthesizeMixedAndOrMode.AlwaysForOrGroups
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().MatchRegex("WHERE\\n\\s+A\\s*=\\s*1\\n\\s+AND\\s+\\(B\\s*=\\s*2\\s+OR\\s+C\\s*=\\s*3\\)");
        }

        [Fact]
        public void TryFormat_ShouldInlineSimplePredicate_WhenHeuristicAllowsIt()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.t AS t WHERE a = 1";
            var options = new SqlFormattingOptions
            {
                Predicates = new SqlPredicatesFormattingOptions
                {
                    MultilineWhere = true,
                    InlineSimplePredicate = new SqlInlineSimplePredicateOptions
                    {
                        MaxConditions = 1,
                        MaxLineLength = 120,
                        AllowOnlyAnd = true
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("WHERE A = 1");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage6CaseLayoutSettings()
        {
            const string sourceSql = "SELECT CASE WHEN x=1 THEN 'A' ELSE 'B' END AS v FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Expressions = new SqlExpressionsFormattingOptions
                {
                    CaseStyle = SqlCaseStyle.CompactWhenShort,
                    CompactCaseThreshold = new SqlCompactCaseThresholdOptions
                    {
                        MaxWhenClauses = 1,
                        MaxTokens = 20,
                        MaxLineLength = 120
                    }
                },
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.OnePerLine
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("CASE WHEN X = 1 THEN 'A' ELSE 'B' END AS V");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage6InListSettings()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.t AS t WHERE a IN (1,2,3,4)";
            var options = new SqlFormattingOptions
            {
                Lists = new SqlListsFormattingOptions
                {
                    InListItems = SqlInListItemsStyle.OnePerLine,
                    InlineInListThreshold = new SqlInlineInListThresholdOptions
                    {
                        MaxItemsInline = 0,
                        MaxLineLength = 120
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("IN (\n");
            formattedSql.Should().Contain("\n    1,\n");
            formattedSql.Should().Contain("\n    4\n");
        }

        [Fact]
        public void TryFormat_ShouldInlineShortSelectExpression_WhenStage6HeuristicAllowsIt()
        {
            const string sourceSql = "SELECT a+b+c+d AS s FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.OnePerLine
                },
                Expressions = new SqlExpressionsFormattingOptions
                {
                    InlineShortExpression = new SqlInlineShortExpressionOptions
                    {
                        MaxTokens = 16,
                        MaxDepth = 0,
                        MaxLineLength = 120,
                        ForContexts = [SqlInlineExpressionContext.SelectItem]
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("SELECT A + B + C + D AS S");
            formattedSql.Should().Contain("\nFROM DBO.T AS T");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage7DmlDdlSettings()
        {
            const string sourceSql = "UPDATE dbo.t SET a=1,b=2 WHERE id=@id; CREATE PROC p AS BEGIN SELECT 1 END";
            var options = new SqlFormattingOptions
            {
                Dml = new SqlDmlFormattingOptions
                {
                    UpdateSetStyle = SqlDmlListStyle.OnePerLine,
                    InsertColumnsStyle = SqlDmlListStyle.OnePerLine
                },
                Ddl = new SqlDdlFormattingOptions
                {
                    CreateProcLayout = SqlCreateProcLayout.Expanded
                },
                Statement = new SqlStatementFormattingOptions
                {
                    TerminateWithSemicolon = SqlStatementTerminationMode.Always
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("UPDATE DBO.T\nSET\n");
            formattedSql.Should().Contain("\n    A = 1,\n");
            formattedSql.Should().Contain("\nWHERE ID = @ID;\n");
            formattedSql.Should().Contain("CREATE PROC P\nAS\nBEGIN\n");
            formattedSql.Should().Contain("\n    SELECT\n        1\nEND");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage8CommentFormatting()
        {
            const string sourceSql = "SELECT a /*  keep   spacing */ AS b FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Comments = new SqlCommentsFormattingOptions
                {
                    PreserveAttachment = true,
                    Formatting = SqlCommentsFormattingMode.ReflowSafeOnly
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);

            result.IsSuccess.Should().BeTrue();
            result.FormattedSql.Should().Contain("/* keep spacing */");
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
