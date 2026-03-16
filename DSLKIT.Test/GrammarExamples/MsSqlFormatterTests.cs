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
            result.ParseError.Should().NotBeNull();
            result.ParseError!.ActualTokenText.Should().Be("FROM");
            result.ParseError.ExpectedTokens.Should().NotBeEmpty();
            result.FormattedSql.Should().BeNull();
        }

        [Fact]
        public void TryFormat_ShouldApplyKeywordCaseOnlyToKeywords()
        {
            const string sourceSql = "select o.CustomerId as customerAlias from dbo.Orders as o where o.CustomerId=@customerId";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, new SqlFormattingOptions
            {
                KeywordCase = SqlKeywordCase.Upper
            });

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql);
            formattedSql.Should().Contain("SELECT");
            formattedSql.Should().Contain("AS");
            formattedSql.Should().Contain("FROM");
            formattedSql.Should().Contain("WHERE");
            formattedSql.Should().Contain("o.CustomerId");
            formattedSql.Should().Contain("customerAlias");
            formattedSql.Should().Contain("dbo.Orders");
            formattedSql.Should().Contain("@customerId");
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
            result.FormattedSql.Should().Contain("SELECT ( a ) AS A,b AS B");
            result.FormattedSql.Should().Contain("WHERE a=1 ;");
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
                    WrapColumn = 40,
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
            formattedSql.Should().Contain("\n  a AS A,");
            formattedSql.Should().Contain("FROM dbo.t AS t\n\nWHERE");
            formattedSql.Should().Contain("WHERE x = 1\n\nORDER BY");
        }

        [Fact]
        public void TryFormat_ShouldUseConfiguredWrapColumn_ForWrapByWidthLists()
        {
            const string sourceSql = "SELECT a AS A, b AS B, c AS C FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Layout = new SqlLayoutFormattingOptions
                {
                    WrapColumn = 20
                },
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.WrapByWidth
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("SELECT\n");
            formattedSql.Should().Contain("\n    a AS A,\n");
            formattedSql.Should().Contain("\n    b AS B,\n");
        }

        [Fact]
        public void TryFormat_ShouldToggleNewlineBeforeWithClause()
        {
            const string sourceSql =
                "CREATE VIEW dbo.v_sales AS WITH sales_cte AS (SELECT o.CustomerId FROM dbo.Orders AS o) SELECT CustomerId FROM sales_cte";

            var withNewlineOptions = new SqlFormattingOptions
            {
                Layout = new SqlLayoutFormattingOptions
                {
                    NewlineBeforeClause = new SqlClauseNewlineOptions
                    {
                        With = true,
                        Select = false,
                        From = false,
                        Where = false,
                        GroupBy = false,
                        Having = false,
                        OrderBy = false,
                        Option = false
                    }
                }
            };

            var withoutNewlineOptions = new SqlFormattingOptions
            {
                Layout = new SqlLayoutFormattingOptions
                {
                    NewlineBeforeClause = new SqlClauseNewlineOptions
                    {
                        With = false,
                        Select = false,
                        From = false,
                        Where = false,
                        GroupBy = false,
                        Having = false,
                        OrderBy = false,
                        Option = false
                    }
                }
            };

            var withNewlineResult = ModernMsSqlFormatter.TryFormat(sourceSql, withNewlineOptions);
            var withoutNewlineResult = ModernMsSqlFormatter.TryFormat(sourceSql, withoutNewlineOptions);

            withNewlineResult.IsSuccess.Should().BeTrue();
            withoutNewlineResult.IsSuccess.Should().BeTrue();

            NormalizeLineEndings(withNewlineResult.FormattedSql).Should().Contain("AS\nWITH sales_cte AS");
            NormalizeLineEndings(withoutNewlineResult.FormattedSql).Should().Contain("AS WITH sales_cte AS");
        }

        [Fact]
        public void TryFormat_ShouldIndentCteBody_WithoutMovingClosingParenthesis_WhenConfigured()
        {
            const string sourceSql =
                "CREATE VIEW dbo.v_recent AS WITH seed AS (SELECT TOP (1) o.CustomerId FROM dbo.Orders AS o) SELECT CustomerId FROM seed";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, new SqlFormattingOptions
            {
                Layout = new SqlLayoutFormattingOptions
                {
                    IndentCteBody = true
                }
            });

            result.IsSuccess.Should().BeTrue();
            NormalizeLineEndings(result.FormattedSql!).Should().Be(
                """
                CREATE VIEW dbo.v_recent AS
                WITH seed AS (
                    SELECT TOP (1)
                        o.CustomerId
                    FROM dbo.Orders AS o)
                SELECT
                    CustomerId
                FROM seed
                """);
        }

        [Fact]
        public void TryFormat_ShouldPlaceMultipleCtesOnSeparateLines_WhenCteBodyIsIndented()
        {
            const string sourceSql =
                "WITH first_cte AS (SELECT a.Id FROM dbo.A AS a), second_cte AS (SELECT b.Id FROM dbo.B AS b) SELECT first_cte.Id, second_cte.Id FROM first_cte INNER JOIN second_cte ON first_cte.Id = second_cte.Id";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, new SqlFormattingOptions
            {
                Layout = new SqlLayoutFormattingOptions
                {
                    IndentCteBody = true
                }
            });

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            formattedSql.Should().Contain(
                """
                WITH first_cte AS (
                    SELECT
                        a.Id
                    FROM dbo.A AS a),
                second_cte AS (
                    SELECT
                        b.Id
                    FROM dbo.B AS b)
                """);
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
            formattedSql.Should().MatchRegex("SUM\\s*\\(x\\)\\s{2,}AS\\s+Total");
            formattedSql.Should().MatchRegex("\\n\\s*,\\s*COUNT\\s*\\(\\s*\\*\\s*\\)\\s+AS\\s+Cnt");
        }

        [Fact]
        public void TryFormat_ShouldCompactShortSelect_WhenThresholdAllowsIt()
        {
            const string sourceSql = "SELECT a+b AS c, d AS f FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.OnePerLine,
                    SelectCompactThreshold = new SqlSelectCompactThresholdOptions
                    {
                        MaxItems = 2,
                        MaxLineLength = 120
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("SELECT a + b AS c, d AS f");
        }

        [Fact]
        public void TryFormat_ShouldKeepLongSelectExpanded_WhenCompactThresholdLineLengthIsExceeded()
        {
            const string sourceSql = "SELECT currentQuarterRevenue AS current_quarter_revenue, projectedAnnualRevenue AS projected_annual_revenue FROM dbo.t AS t";
            var options = new SqlFormattingOptions
            {
                Lists = new SqlListsFormattingOptions
                {
                    SelectItems = SqlListLayoutStyle.OnePerLine,
                    SelectCompactThreshold = new SqlSelectCompactThresholdOptions
                    {
                        MaxItems = 2,
                        MaxLineLength = 40
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("SELECT\n");
            formattedSql.Should().Contain(",\n");
        }

        [Fact]
        public void TryFormat_ShouldCompactWholeShortQuery_WhenPolicyAllowsIt()
        {
            const string sourceSql = "SELECT 1 FROM dbo.A WHERE X=3 AND Y=4";
            var options = new SqlFormattingOptions
            {
                ShortQueries = new SqlShortQueriesFormattingOptions
                {
                    Enabled = true,
                    MaxLineLength = 120,
                    MaxSelectItems = 1,
                    MaxPredicateConditions = 2
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("SELECT 1 FROM dbo.A WHERE X = 3 AND Y = 4");
            formattedSql.Should().NotContain("\nFROM dbo.A");
        }

        [Fact]
        public void TryFormat_ShouldKeepWholeQueryExpanded_WhenShortQueryPredicateThresholdIsExceeded()
        {
            const string sourceSql = "SELECT 1 FROM dbo.A WHERE X=3 AND Y=4";
            var options = new SqlFormattingOptions
            {
                ShortQueries = new SqlShortQueriesFormattingOptions
                {
                    Enabled = true,
                    MaxLineLength = 120,
                    MaxSelectItems = 1,
                    MaxPredicateConditions = 1
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().NotContain("SELECT 1 FROM dbo.A WHERE X = 3 AND Y = 4");
            formattedSql.Should().Contain("\nFROM dbo.A\nWHERE X = 3 AND Y = 4");
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
                        MaxConditionsSingleLine = 2,
                        BreakOnAnd = true,
                        BreakOnOr = false
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("INNER JOIN dbo.B AS b");
            formattedSql.Should().MatchRegex("\\n\\s+ON\\s+a\\.Id\\s*=\\s*b\\.Id");
            formattedSql.Should().MatchRegex("\\n\\s+AND\\s+a\\.TYPE\\s*=\\s*b\\.TYPE");
            formattedSql.Should().MatchRegex("\\n\\s+AND\\s+a\\.IsActive\\s*=\\s*1");
        }

        [Fact]
        public void TryFormat_ShouldKeepParenthesizedOnGroupInline_WhenConditionThresholdAllowsIt()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.A AS a INNER JOIN dbo.B AS b ON a.Id=b.Id AND (a.Region=b.Region OR a.Kind=b.Kind)";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, new SqlFormattingOptions
            {
                Joins = new SqlJoinsFormattingOptions
                {
                    NewlinePerJoin = true,
                    OnNewLine = true,
                    MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                    {
                        MaxConditionsSingleLine = 2,
                        BreakOnAnd = true,
                        BreakOnOr = true
                    }
                }
            });

            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("ON a.Id = b.Id AND (a.Region = b.Region OR a.Kind = b.Kind)");
            formattedSql.Should().NotContain("\n        AND");
        }

        [Fact]
        public void TryFormat_ShouldGroupMixedOnPredicateByPrecedence()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.A AS a INNER JOIN dbo.B AS b ON a.Id=b.Id AND a.Region=b.Region OR a.Flag=b.Flag";

            var andOnlyOptions = new SqlFormattingOptions
            {
                Joins = new SqlJoinsFormattingOptions
                {
                    NewlinePerJoin = true,
                    OnNewLine = true,
                    MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                    {
                        MaxConditionsSingleLine = 1,
                        BreakOnAnd = true,
                        BreakOnOr = false
                    }
                }
            };

            var andOnlyResult = ModernMsSqlFormatter.TryFormat(sourceSql, andOnlyOptions);
            var andOnlyFormattedSql = NormalizeLineEndings(andOnlyResult.FormattedSql);

            andOnlyResult.IsSuccess.Should().BeTrue();
            andOnlyFormattedSql.Should().Contain("ON (\n");
            andOnlyFormattedSql.Should().MatchRegex("\\n\\s+AND\\s+a\\.Region\\s*=\\s*b\\.Region\\n");
            andOnlyFormattedSql.Should().Contain(") OR a.Flag = b.Flag");
            andOnlyFormattedSql.Should().NotContain("AND a.Region = b.Region OR a.Flag = b.Flag");

            var andOrOptions = new SqlFormattingOptions
            {
                Joins = new SqlJoinsFormattingOptions
                {
                    NewlinePerJoin = true,
                    OnNewLine = true,
                    MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                    {
                        MaxConditionsSingleLine = 1,
                        BreakOnAnd = true,
                        BreakOnOr = true
                    }
                }
            };

            var andOrResult = ModernMsSqlFormatter.TryFormat(sourceSql, andOrOptions);
            var andOrFormattedSql = NormalizeLineEndings(andOrResult.FormattedSql);

            andOrResult.IsSuccess.Should().BeTrue();
            andOrFormattedSql.Should().Contain("ON (\n");
            andOrFormattedSql.Should().MatchRegex("\\n\\s+OR\\s+a\\.Flag\\s*=\\s*b\\.Flag");
        }

        [Fact]
        public void TryFormat_ShouldKeepOriginalMixedAndOrPredicateShape()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.t AS t WHERE a = 1 AND b = 2 OR c = 3 AND d = 4";
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
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            NormalizeSql(formattedSql!).Should().Be(NormalizeSql(sourceSql));
            formattedSql.Should().NotContain("AND (b = 2 OR c = 3) AND");
        }

        [Fact]
        public void TryFormat_ShouldPreserveOriginalParentheses_InMixedAndOrPredicate()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.t AS t WHERE a = 1 AND (b = 2 OR c = 3)";
            var options = new SqlFormattingOptions
            {
                Predicates = new SqlPredicatesFormattingOptions
                {
                    MultilineWhere = true,
                    LogicalOperatorLineBreak = SqlLogicalOperatorLineBreakMode.BeforeOperator
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            NormalizeSql(formattedSql!).Should().Be(NormalizeSql(sourceSql));
            formattedSql.Should().Contain("AND (b = 2 OR c = 3)");
        }

        [Fact]
        public void TryFormat_ShouldParenthesizeMixedAndOrGroups_WhenEnabled()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.t AS t WHERE a = 1 AND b = 2 OR c = 3 AND d = 4";
            var options = new SqlFormattingOptions
            {
                Predicates = new SqlPredicatesFormattingOptions
                {
                    MultilineWhere = false,
                    MixedAndOrParentheses = new SqlMixedAndOrParenthesesOptions
                    {
                        ParenthesizeOrGroups = true
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("WHERE (a = 1 AND b = 2) OR (c = 3 AND d = 4)");
        }

        [Fact]
        public void TryFormat_ShouldBreakMixedAndOrGroups_WhenEnabled()
        {
            const string sourceSql = "SELECT a AS A FROM dbo.t AS t WHERE a = 1 AND b = 2 OR c = 3 AND d = 4 OR e = 5";
            var options = new SqlFormattingOptions
            {
                Predicates = new SqlPredicatesFormattingOptions
                {
                    MultilineWhere = true,
                    LogicalOperatorLineBreak = SqlLogicalOperatorLineBreakMode.BeforeOperator,
                    MixedAndOrParentheses = new SqlMixedAndOrParenthesesOptions
                    {
                        BreakOrGroups = true,
                        ParenthesizeOrGroups = true
                    }
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("WHERE\n    (a = 1 AND b = 2)\n    OR (c = 3 AND d = 4)\n    OR e = 5");
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
            formattedSql.Should().Contain("WHERE a = 1");
        }

        [Fact]
        public void TryFormat_ShouldApplyStage6CaseLayoutSettings()
        {
            const string sourceSql = "SET @grade = CASE WHEN @score=100 THEN 'A' WHEN @score=80 THEN 'B' ELSE 'C' END;";
            var compactOptions = new SqlFormattingOptions
            {
                Expressions = new SqlExpressionsFormattingOptions
                {
                    CaseStyle = SqlCaseStyle.CompactWhenShort,
                    CompactCaseThreshold = new SqlCompactCaseThresholdOptions
                    {
                        MaxWhenClauses = 2,
                        MaxTokens = 30,
                        MaxLineLength = 120
                    }
                }
            };
            var multilineOptions = new SqlFormattingOptions
            {
                Expressions = new SqlExpressionsFormattingOptions
                {
                    CaseStyle = SqlCaseStyle.Multiline
                }
            };

            var compactResult = ModernMsSqlFormatter.TryFormat(sourceSql, compactOptions);
            var multilineResult = ModernMsSqlFormatter.TryFormat(sourceSql, multilineOptions);
            var compactSql = NormalizeLineEndings(compactResult.FormattedSql);
            var multilineSql = NormalizeLineEndings(multilineResult.FormattedSql);

            compactResult.IsSuccess.Should().BeTrue();
            multilineResult.IsSuccess.Should().BeTrue();
            compactSql.Should().Contain("SET @grade = CASE WHEN @score = 100 THEN 'A' WHEN @score = 80 THEN 'B' ELSE 'C' END;");
            multilineSql.Should().Contain("CASE\n");
            multilineSql.Should().Contain("\n    WHEN @score = 100 THEN 'A'");
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
            formattedSql.Should().Contain("SELECT a + b + c + d AS s");
            formattedSql.Should().Contain("\nFROM dbo.t AS t");
        }

        [Fact]
        public void TryFormat_ShouldCompactShortParenthesizedSubquery_WhenPolicyAllowsIt()
        {
            const string sourceSql = "SELECT q.Id FROM (SELECT a.Id FROM dbo.A AS a WHERE a.Status=1 AND a.Flag=1) AS q";
            var disabledOptions = new SqlFormattingOptions
            {
                ShortQueries = new SqlShortQueriesFormattingOptions
                {
                    Enabled = true,
                    MaxLineLength = 120,
                    MaxSelectItems = 1,
                    MaxPredicateConditions = 2,
                    ApplyToParenthesizedSubqueries = false
                }
            };
            var enabledOptions = disabledOptions with
            {
                ShortQueries = disabledOptions.ShortQueries with
                {
                    ApplyToParenthesizedSubqueries = true
                }
            };

            var disabledResult = ModernMsSqlFormatter.TryFormat(sourceSql, disabledOptions);
            var enabledResult = ModernMsSqlFormatter.TryFormat(sourceSql, enabledOptions);
            var disabledSql = NormalizeLineEndings(disabledResult.FormattedSql);
            var enabledSql = NormalizeLineEndings(enabledResult.FormattedSql);

            disabledResult.IsSuccess.Should().BeTrue();
            enabledResult.IsSuccess.Should().BeTrue();
            disabledSql.Should().NotContain("FROM (SELECT a.Id FROM dbo.A AS a WHERE a.Status = 1 AND a.Flag = 1) AS q");
            enabledSql.Should().Contain("FROM (SELECT a.Id FROM dbo.A AS a WHERE a.Status = 1 AND a.Flag = 1) AS q");
        }

        [Fact]
        public void TryFormat_ShouldKeepShortSubqueriesInline_InsidePredicates()
        {
            const string sourceSql = """
                SELECT a.Id
                FROM dbo.A AS a
                WHERE EXISTS (SELECT 1 FROM dbo.B AS b WHERE b.AId = a.Id AND b.IsActive = 1)
                    AND a.Id IN (SELECT c.AId FROM dbo.C AS c WHERE c.IsReady = 1);
                """;
            var options = new SqlFormattingOptions
            {
                Predicates = new SqlPredicatesFormattingOptions
                {
                    MultilineWhere = true
                },
                ShortQueries = new SqlShortQueriesFormattingOptions
                {
                    Enabled = true,
                    MaxLineLength = 120,
                    MaxSelectItems = 1,
                    MaxPredicateConditions = 2,
                    ApplyToParenthesizedSubqueries = true
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            result.IsSuccess.Should().BeTrue(result.ErrorMessage);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            formattedSql.Should().Contain("EXISTS (SELECT 1 FROM dbo.B AS b WHERE b.AId = a.Id AND b.IsActive = 1)");
            formattedSql.Should().Contain("IN (SELECT c.AId FROM dbo.C AS c WHERE c.IsReady = 1)");
        }

        [Fact]
        public void TryFormat_ShouldCompactShortQueryWithSingleJoin_OnlyWhenAllowed()
        {
            const string sourceSql = "SELECT a.Id FROM dbo.A AS a INNER JOIN dbo.B AS b ON a.Id=b.AId WHERE a.Status=1";
            var disallowOptions = new SqlFormattingOptions
            {
                ShortQueries = new SqlShortQueriesFormattingOptions
                {
                    Enabled = true,
                    MaxLineLength = 120,
                    MaxSelectItems = 1,
                    MaxPredicateConditions = 1,
                    AllowSingleJoin = false
                }
            };
            var allowOptions = disallowOptions with
            {
                ShortQueries = disallowOptions.ShortQueries with
                {
                    AllowSingleJoin = true
                }
            };

            var disallowResult = ModernMsSqlFormatter.TryFormat(sourceSql, disallowOptions);
            var allowResult = ModernMsSqlFormatter.TryFormat(sourceSql, allowOptions);
            var disallowSql = NormalizeLineEndings(disallowResult.FormattedSql);
            var allowSql = NormalizeLineEndings(allowResult.FormattedSql);

            disallowResult.IsSuccess.Should().BeTrue();
            allowResult.IsSuccess.Should().BeTrue();
            disallowSql.Should().NotContain("SELECT a.Id FROM dbo.A AS a INNER JOIN dbo.B AS b ON a.Id = b.AId WHERE a.Status = 1");
            allowSql.Should().Contain("SELECT a.Id FROM dbo.A AS a INNER JOIN dbo.B AS b ON a.Id = b.AId WHERE a.Status = 1");
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
                    InsertColumnsStyle = SqlDmlListStyle.OnePerLine,
                    InsertValuesStyle = SqlDmlListStyle.OnePerLine
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
            formattedSql.Should().Contain("UPDATE dbo.t\nSET\n");
            formattedSql.Should().Contain("\n    a = 1,\n");
            formattedSql.Should().Contain("\nWHERE id = @id;\n");
            formattedSql.Should().Contain("CREATE PROC p\nAS\nBEGIN\n");
            formattedSql.Should().Contain("\n    SELECT\n        1\nEND");
        }

        [Fact]
        public void TryFormat_ShouldInsertConfiguredBlankLinesBetweenStatements()
        {
            const string sourceSql = "UPDATE dbo.t SET a=1 WHERE id=@id; CREATE PROC p AS BEGIN SELECT 1 END";
            var options = new SqlFormattingOptions
            {
                Ddl = new SqlDdlFormattingOptions
                {
                    CreateProcLayout = SqlCreateProcLayout.Expanded
                },
                Statement = new SqlStatementFormattingOptions
                {
                    TerminateWithSemicolon = SqlStatementTerminationMode.Always,
                    BlankLinesBetweenStatements = 1
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            result.IsSuccess.Should().BeTrue();
            formattedSql.Should().Contain("WHERE id = @id;\n\nCREATE PROC p\nAS\nBEGIN\n");
        }

        [Fact]
        public void TryFormat_ShouldApplyInsertColumnsAndValuesStyles_ForOutputAndMultiRowValues()
        {
            const string sourceSql = "INSERT INTO dbo.TargetRows (A, B) OUTPUT INSERTED.A VALUES (1,2),(3,4);";
            var options = new SqlFormattingOptions
            {
                Dml = new SqlDmlFormattingOptions
                {
                    InsertColumnsStyle = SqlDmlListStyle.OnePerLine,
                    InsertValuesStyle = SqlDmlListStyle.OnePerLine
                },
                Statement = new SqlStatementFormattingOptions
                {
                    TerminateWithSemicolon = SqlStatementTerminationMode.Always
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            result.IsSuccess.Should().BeTrue(result.ErrorMessage);
            var formattedSql = NormalizeLineEndings(result.FormattedSql);

            formattedSql.Should().Contain("INSERT INTO dbo.TargetRows (\n    A,\n    B\n) OUTPUT INSERTED.A\nVALUES\n");
            formattedSql.Should().Contain("(\n        1,\n        2\n    ),\n");
            formattedSql.Should().Contain("(\n        3,\n        4\n    );");
        }

        [Fact]
        public void TryFormat_ShouldWriteOneInsertRowPerLine_WhenInsertValuesStyleIsRowsPerLine()
        {
            const string sourceSql = "INSERT INTO dbo.TargetRows (Id, Region, TypeId, Score) VALUES (@id,'EU',1,10),(@id+1,'US',2,20);";
            var options = new SqlFormattingOptions
            {
                Dml = new SqlDmlFormattingOptions
                {
                    InsertValuesStyle = SqlDmlListStyle.RowsPerLine
                },
                Statement = new SqlStatementFormattingOptions
                {
                    TerminateWithSemicolon = SqlStatementTerminationMode.Always
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            result.IsSuccess.Should().BeTrue(result.ErrorMessage);

            var formattedSql = NormalizeLineEndings(result.FormattedSql);
            formattedSql.Should().Contain("VALUES\n");
            formattedSql.Should().Contain("    (@id, 'EU', 1, 10),\n");
            formattedSql.Should().Contain("    (@id + 1, 'US', 2, 20);");
            formattedSql.Should().NotContain("(\n        @id");
        }

        [Fact]
        public void TryFormat_ShouldStartInsertColumnsOnNewLine_WhenOptionEnabled()
        {
            const string sourceSql = "INSERT INTO dbo.TargetRows (A, B) VALUES (1, 2);";
            var options = new SqlFormattingOptions
            {
                Dml = new SqlDmlFormattingOptions
                {
                    InsertColumnsStyle = SqlDmlListStyle.OnePerLine,
                    InsertColumnsStartOnNewLine = true
                },
                Statement = new SqlStatementFormattingOptions
                {
                    TerminateWithSemicolon = SqlStatementTerminationMode.Always
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            result.IsSuccess.Should().BeTrue(result.ErrorMessage);

            var formattedSql = NormalizeLineEndings(result.FormattedSql);
            formattedSql.Should().Contain("INSERT INTO dbo.TargetRows\n(\n    A,\n    B\n)\nVALUES");
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

        [Fact]
        public void TryFormat_ShouldKeepSingleLineCommentOnDedicatedLine_WhenRenderingInlineNodes()
        {
            const string sourceSql = """
                SELECT
                    -- keep   spacing
                    c.CustomerId /*  keep   spacing */ AS customer_id,
                    c.Region AS region
                FROM dbo.Customers AS c;
                """;

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

            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            formattedSql.Should().Contain("-- keep spacing\n");
            formattedSql.Should().Contain("/* keep spacing */");
            formattedSql.Should().NotContain("-- keep spacing c.CustomerId");
        }

        [Fact]
        public void TryFormat_ShouldPreserveTrailingCommentBeforeEof()
        {
            const string sourceSql = "select 1 -- tail";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, new SqlFormattingOptions
            {
                KeywordCase = SqlKeywordCase.Upper
            });

            result.IsSuccess.Should().BeTrue();

            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(NormalizeSql("SELECT 1 -- tail"));
            formattedSql.Should().Contain("-- tail");
        }

        [Fact]
        public void TryFormat_ShouldPreserveCommentOnlyBatch()
        {
            const string sourceSql = "-- comment only batch";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            NormalizeLineEndings(result.FormattedSql!).Should().Be(sourceSql);
        }

        [Fact]
        public void TryFormat_ShouldPreserveCommentsInsideSplitMultiKeywordConstructs()
        {
            const string sourceSql = """
                CREATE VIEW dbo.vTrivia AS
                SELECT 1 AS A
                WITH /*check-before*/ CHECK /*option-before*/ OPTION;

                SELECT ProductID
                FROM Product FOR /*system-time-before*/ SYSTEM_TIME AS OF '2015-07-28 13:20:00';

                SELECT 1
                FROM Product FOR /*path-before*/ PATH p;
                """;

            var options = new SqlFormattingOptions
            {
                Comments = new SqlCommentsFormattingOptions
                {
                    PreserveAttachment = true,
                    Formatting = SqlCommentsFormattingMode.Keep
                }
            };

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);

            result.IsSuccess.Should().BeTrue();

            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(NormalizeSql(sourceSql));
            formattedSql.Should().Contain("WITH /*check-before*/ CHECK");
            formattedSql.Should().Contain("/*option-before*/");
            formattedSql.Should().Contain("FOR /*system-time-before*/ SYSTEM_TIME AS OF");
            formattedSql.Should().Contain("FOR /*path-before*/ PATH p");
        }

        [Fact]
        public void TryFormat_ShouldFormatCreateDatabase_WithPopularOptions()
        {
            const string sourceSql = """
                CREATE DATABASE Sales
                CONTAINMENT = NONE
                ON PRIMARY
                (
                    NAME = SalesData,
                    FILENAME = 'C:\data\sales.mdf',
                    SIZE = 64MB,
                    MAXSIZE = 512MB,
                    FILEGROWTH = 64MB
                ),
                FILEGROUP FG_Archive CONTAINS FILESTREAM DEFAULT
                (
                    NAME = ArchiveFs,
                    FILENAME = 'C:\data\archive'
                ),
                LOG ON
                (
                    NAME = SalesLog,
                    FILENAME = 'C:\data\sales.ldf',
                    FILEGROWTH = 10%
                )
                COLLATE Latin1_General_100_CI_AS
                WITH
                DEFAULT_LANGUAGE = us_english,
                NESTED_TRIGGERS = ON,
                TRUSTWORTHY OFF,
                LEDGER = OFF;
                GO
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            formattedSql.Should().Contain("CREATE DATABASE Sales");
            formattedSql.Should().Contain("FILEGROUP FG_Archive CONTAINS FILESTREAM DEFAULT");
            formattedSql.Should().Contain("DEFAULT_LANGUAGE = us_english");
            formattedSql.Should().Contain("GO");
        }

        [Fact]
        public void TryFormat_ShouldFormatCreateRole_WithAuthorization()
        {
            const string sourceSql = "CREATE ROLE [Plains Sales] AUTHORIZATION [dbo];";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql("CREATE ROLE [Plains Sales] AUTHORIZATION [dbo];"),
                "formatted CREATE ROLE should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldFormatUseStatement()
        {
            const string sourceSql = """
                USE [Clinic];
                GO
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted USE should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldFormatCreateSchema_BasicAndAuthorization()
        {
            const string sourceSql = """
                CREATE SCHEMA ext;
                GO
                CREATE SCHEMA [sales] AUTHORIZATION [dbo];
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted CREATE SCHEMA should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldReturnError_ForInlineGoBatchSeparator()
        {
            const string sourceSql = "USE [Clinic]; GO";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void TryFormat_ShouldFormatGoBatchRepeat_OnDedicatedLine()
        {
            const string sourceSql = """
                SELECT 1;
                GO 5
                SELECT 2;
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            NormalizeSql(result.FormattedSql!).Should().Be(
                NormalizeSql(sourceSql),
                "formatted GO batch repeat should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldFormatSqlcmdPreprocessorCommands_OnDedicatedLines()
        {
            const string sourceSql = """
                :r .\setup.sql
                :setvar JobOwner sa
                :on error exit
                PRINT N'after sqlcmd preprocessor commands';
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            NormalizeSql(result.FormattedSql!).Should().Be(
                NormalizeSql(sourceSql),
                "formatted SQLCMD control lines should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldReturnError_ForInlineSqlcmdPreprocessorCommand()
        {
            const string sourceSql = "PRINT N'before'; :setvar JobOwner sa";

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void TryFormat_ShouldFormatIfDeclareSetAndCreateView()
        {
            const string sourceSql = """
                IF EXISTS (SELECT 1 FROM dbo.TestTable)
                BEGIN
                    DECLARE @counter INT = 1;
                    SET @counter = @counter + 1;
                    PRINT @counter;
                    SELECT @counter;
                END;
                GO
                CREATE VIEW dbo.vTest AS SELECT 1 AS A;
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted output should preserve significant SQL tokens for IF/DECLARE/SET/CREATE VIEW.");
        }

        [Fact]
        public void TryFormat_ShouldFormatExecuteStatement_Variants()
        {
            const string sourceSql = """
                DECLARE @policy_id INT
                EXEC msdb.dbo.sp_syspolicy_add_policy @name=N'Policy', @enabled=True, @policy_id=@policy_id OUTPUT
                SELECT @policy_id;

                EXECUTE @return_code = dbo.usp_DoWork @arg1 = DEFAULT, @arg2 = @policy_id OUT WITH RECOMPILE;
                EXECUTE ('SELECT 1' + N' AS Value') AS USER = 'dbo';
                EXECUTE (N'SELECT * FROM dbo.T WHERE Id = ?', @policy_id OUTPUT) AT DATA_SOURCE [RemoteSource];
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted EXEC/EXECUTE variants should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldFormatInsertExec_Variants()
        {
            const string sourceSql = """
                INSERT INTO dbo.TargetTable EXEC dbo.usp_FillTarget;
                INSERT dbo.TargetTable (A, B) EXECUTE dbo.usp_FillTargetByParams @a = 1, @b = DEFAULT;
                INSERT INTO [dbo].[models]
                EXEC sp_execute_external_script
                    @language = N'R',
                    @script = N'SELECT 1';
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue();
            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted INSERT EXEC variants should preserve significant SQL tokens.");
        }

        [Fact]
        public void TryFormat_ShouldFormatSnowflakeViewSyntax_WhenSnowflakeDialectSelected()
        {
            const string sourceSql = """
                CREATE OR REPLACE VIEW ADP_DX_DSE_PO_BOT.PRODUCT.V_CURRENT_USER_DIRECT_REPORTS AS
                WITH me AS (
                    SELECT PERSONNEL_NR
                    FROM CDP_CORPORATE_DSG_S4_USER.PRODUCT.V_DIM_HR_COMMUNICATION
                    WHERE CURRENT_DATE() BETWEEN START_DT::DATE AND END_DT::DATE
                )
                SELECT PERSONNEL_NR
                FROM me
                QUALIFY ROW_NUMBER() OVER (ORDER BY PERSONNEL_NR DESC) = 1;
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, new SqlFormattingOptions
            {
                Dialect = SqlDialect.Snowflake
            });

            result.IsSuccess.Should().BeTrue(result.ErrorMessage);

            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted Snowflake SQL should preserve significant tokens.");
            formattedSql.Should().Contain("START_DT::DATE");
            formattedSql.Should().Contain("\nQUALIFY ROW_NUMBER() OVER");
            formattedSql.Should().Contain("CREATE OR REPLACE VIEW");
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
