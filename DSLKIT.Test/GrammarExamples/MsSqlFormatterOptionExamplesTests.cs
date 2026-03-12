using System;
using System.Collections.Generic;
using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.GrammarExamples.MsSql.Formatting;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class MsSqlFormatterOptionExamplesTests
    {
        [Theory]
        [MemberData(nameof(OptionExamples))]
        public void TryFormat_ShouldFormatAllSqlFormatterOptionExamples(string optionId, string sourceSql)
        {
            var result = ModernMsSqlFormatter.TryFormat(sourceSql);

            result.IsSuccess.Should().BeTrue(
                $"option example '{optionId}' should format, but failed with: {result.ErrorMessage}");
            result.FormattedSql.Should().NotBeNullOrWhiteSpace();

            var reparsed = ModernMsSqlGrammarExample.ParseBatch(result.FormattedSql!);
            reparsed.IsSuccess.Should().BeTrue(
                $"formatted SQL for option '{optionId}' should parse, but failed with: {reparsed.Error}");
        }

        [Theory]
        [MemberData(nameof(ProblematicOptionExamples))]
        public void TryFormat_ShouldProduceDifferentOutput_ForProblematicOptionExamples(
            string optionId,
            string sourceSql,
            SqlFormattingOptions firstOptions,
            SqlFormattingOptions secondOptions)
        {
            var firstResult = FormatOrThrow(optionId, sourceSql, firstOptions);
            var secondResult = FormatOrThrow(optionId, sourceSql, secondOptions);

            firstResult.Should().NotBe(
                secondResult,
                $"option example '{optionId}' should show a visible formatting difference for the tested values.");
        }

        [Theory]
        [MemberData(nameof(InactiveDependentOptionExamples))]
        public void TryFormat_ShouldKeepSameOutput_ForInactiveDependentOptionExamples(
            string optionId,
            string sourceSql,
            SqlFormattingOptions firstOptions,
            SqlFormattingOptions secondOptions)
        {
            var firstResult = FormatOrThrow(optionId, sourceSql, firstOptions);
            var secondResult = FormatOrThrow(optionId, sourceSql, secondOptions);

            firstResult.Should().Be(
                secondResult,
                $"option example '{optionId}' should keep the same output while its parent mode is inactive.");
        }

        public static IEnumerable<object[]> OptionExamples()
        {
            foreach (var optionExample in OptionExampleSqlByOptionId)
            {
                yield return new object[] { optionExample.Key, optionExample.Value };
            }
        }

        public static IEnumerable<object[]> ProblematicOptionExamples()
        {
            yield return
            [
                "sql-predicates-logical-break",
                PredicatesExampleSql,
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true,
                        LogicalOperatorLineBreak = SqlLogicalOperatorLineBreakMode.BeforeOperator
                    }
                },
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true,
                        LogicalOperatorLineBreak = SqlLogicalOperatorLineBreakMode.AfterOperator
                    }
                }
            ];

            yield return
            [
                "sql-predicates-mixed-and-or-break-or-groups",
                PredicatesExampleSql,
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true,
                        MixedAndOrParentheses = new SqlMixedAndOrParenthesesOptions
                        {
                            BreakOrGroups = false
                        }
                    }
                },
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true,
                        MixedAndOrParentheses = new SqlMixedAndOrParenthesesOptions
                        {
                            BreakOrGroups = true
                        }
                    }
                }
            ];

            yield return
            [
                "sql-joins-multiline-threshold",
                JoinsExampleSql,
                new SqlFormattingOptions
                {
                    Joins = new SqlJoinsFormattingOptions
                    {
                        NewlinePerJoin = true,
                        OnNewLine = true,
                        MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                        {
                            MaxTokensSingleLine = 0,
                            BreakOnAnd = true,
                            BreakOnOr = true
                        }
                    }
                },
                new SqlFormattingOptions
                {
                    Joins = new SqlJoinsFormattingOptions
                    {
                        NewlinePerJoin = true,
                        OnNewLine = true,
                        MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                        {
                            MaxTokensSingleLine = 12,
                            BreakOnAnd = true,
                            BreakOnOr = true
                        }
                    }
                }
            ];

            yield return
            [
                "sql-predicates-inline-max-line-length",
                PredicatesInlineExampleSql,
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true,
                        InlineSimplePredicate = new SqlInlineSimplePredicateOptions
                        {
                            MaxConditions = 4,
                            MaxLineLength = 40,
                            AllowOnlyAnd = true
                        }
                    }
                },
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true,
                        InlineSimplePredicate = new SqlInlineSimplePredicateOptions
                        {
                            MaxConditions = 4,
                            MaxLineLength = 80,
                            AllowOnlyAnd = true
                        }
                    }
                }
            ];

            yield return
            [
                "sql-predicates-inline-allow-only-and",
                PredicateAllowOnlyAndExampleSql,
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true,
                        InlineSimplePredicate = new SqlInlineSimplePredicateOptions
                        {
                            MaxConditions = 2,
                            MaxLineLength = 120,
                            AllowOnlyAnd = true
                        }
                    }
                },
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true,
                        InlineSimplePredicate = new SqlInlineSimplePredicateOptions
                        {
                            MaxConditions = 2,
                            MaxLineLength = 120,
                            AllowOnlyAnd = false
                        }
                    }
                }
            ];

            yield return
            [
                "sql-short-queries-max-select-items",
                ShortQuerySelectItemsExampleSql,
                new SqlFormattingOptions
                {
                    ShortQueries = new SqlShortQueriesFormattingOptions
                    {
                        Enabled = true,
                        MaxLineLength = 120,
                        MaxSelectItems = 1,
                        MaxPredicateConditions = 1
                    }
                },
                new SqlFormattingOptions
                {
                    ShortQueries = new SqlShortQueriesFormattingOptions
                    {
                        Enabled = true,
                        MaxLineLength = 120,
                        MaxSelectItems = 2,
                        MaxPredicateConditions = 1
                    }
                }
            ];

            yield return
            [
                "sql-short-queries-max-predicate-conditions",
                ShortQueryPredicateThresholdExampleSql,
                new SqlFormattingOptions
                {
                    ShortQueries = new SqlShortQueriesFormattingOptions
                    {
                        Enabled = true,
                        MaxLineLength = 120,
                        MaxSelectItems = 1,
                        MaxPredicateConditions = 2
                    }
                },
                new SqlFormattingOptions
                {
                    ShortQueries = new SqlShortQueriesFormattingOptions
                    {
                        Enabled = true,
                        MaxLineLength = 120,
                        MaxSelectItems = 1,
                        MaxPredicateConditions = 3
                    }
                }
            ];

            yield return
            [
                "sql-case-threshold-max-line",
                ExpressionsCaseExampleSql,
                new SqlFormattingOptions
                {
                    Expressions = new SqlExpressionsFormattingOptions
                    {
                        CaseStyle = SqlCaseStyle.CompactWhenShort,
                        CompactCaseThreshold = new SqlCompactCaseThresholdOptions
                        {
                            MaxWhenClauses = 0,
                            MaxTokens = 0,
                            MaxLineLength = 40
                        }
                    }
                },
                new SqlFormattingOptions
                {
                    Expressions = new SqlExpressionsFormattingOptions
                    {
                        CaseStyle = SqlCaseStyle.CompactWhenShort,
                        CompactCaseThreshold = new SqlCompactCaseThresholdOptions
                        {
                            MaxWhenClauses = 0,
                            MaxTokens = 0,
                            MaxLineLength = 120
                        }
                    }
                }
            ];

            yield return
            [
                "sql-inline-short-max-tokens",
                InlineShortExpressionExampleSql,
                new SqlFormattingOptions
                {
                    Joins = new SqlJoinsFormattingOptions
                    {
                        NewlinePerJoin = true,
                        OnNewLine = true,
                        MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                        {
                            MaxTokensSingleLine = 12,
                            BreakOnAnd = true,
                            BreakOnOr = false
                        }
                    },
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true
                    },
                    Expressions = new SqlExpressionsFormattingOptions
                    {
                        InlineShortExpression = new SqlInlineShortExpressionOptions
                        {
                            MaxTokens = 12,
                            MaxDepth = 0,
                            MaxLineLength = 120,
                            ForContexts = [SqlInlineExpressionContext.SelectItem, SqlInlineExpressionContext.On, SqlInlineExpressionContext.Where]
                        }
                    }
                },
                new SqlFormattingOptions
                {
                    Joins = new SqlJoinsFormattingOptions
                    {
                        NewlinePerJoin = true,
                        OnNewLine = true,
                        MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                        {
                            MaxTokensSingleLine = 12,
                            BreakOnAnd = true,
                            BreakOnOr = false
                        }
                    },
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true
                    },
                    Expressions = new SqlExpressionsFormattingOptions
                    {
                        InlineShortExpression = new SqlInlineShortExpressionOptions
                        {
                            MaxTokens = 20,
                            MaxDepth = 0,
                            MaxLineLength = 120,
                            ForContexts = [SqlInlineExpressionContext.SelectItem, SqlInlineExpressionContext.On, SqlInlineExpressionContext.Where]
                        }
                    }
                }
            ];

            yield return
            [
                "sql-inline-short-max-line",
                InlineShortExpressionExampleSql,
                new SqlFormattingOptions
                {
                    Joins = new SqlJoinsFormattingOptions
                    {
                        NewlinePerJoin = true,
                        OnNewLine = true,
                        MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                        {
                            MaxTokensSingleLine = 12,
                            BreakOnAnd = true,
                            BreakOnOr = false
                        }
                    },
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true
                    },
                    Expressions = new SqlExpressionsFormattingOptions
                    {
                        InlineShortExpression = new SqlInlineShortExpressionOptions
                        {
                            MaxTokens = 20,
                            MaxDepth = 0,
                            MaxLineLength = 40,
                            ForContexts = [SqlInlineExpressionContext.SelectItem, SqlInlineExpressionContext.On, SqlInlineExpressionContext.Where]
                        }
                    }
                },
                new SqlFormattingOptions
                {
                    Joins = new SqlJoinsFormattingOptions
                    {
                        NewlinePerJoin = true,
                        OnNewLine = true,
                        MultilineOnThreshold = new SqlJoinMultilineOnThresholdOptions
                        {
                            MaxTokensSingleLine = 12,
                            BreakOnAnd = true,
                            BreakOnOr = false
                        }
                    },
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = true
                    },
                    Expressions = new SqlExpressionsFormattingOptions
                    {
                        InlineShortExpression = new SqlInlineShortExpressionOptions
                        {
                            MaxTokens = 20,
                            MaxDepth = 0,
                            MaxLineLength = 120,
                            ForContexts = [SqlInlineExpressionContext.SelectItem, SqlInlineExpressionContext.On, SqlInlineExpressionContext.Where]
                        }
                    }
                }
            ];

            yield return
            [
                "sql-statement-blank-lines",
                DmlAndDdlExampleSql,
                new SqlFormattingOptions
                {
                    Statement = new SqlStatementFormattingOptions
                    {
                        TerminateWithSemicolon = SqlStatementTerminationMode.Always,
                        BlankLinesBetweenStatements = 0
                    }
                },
                new SqlFormattingOptions
                {
                    Statement = new SqlStatementFormattingOptions
                    {
                        TerminateWithSemicolon = SqlStatementTerminationMode.Always,
                        BlankLinesBetweenStatements = 1
                    }
                }
            ];

            yield return
            [
                "sql-wrap-column",
                ListsExampleSql,
                new SqlFormattingOptions
                {
                    Layout = new SqlLayoutFormattingOptions
                    {
                        WrapColumn = 60
                    },
                    Lists = new SqlListsFormattingOptions
                    {
                        SelectItems = SqlListLayoutStyle.WrapByWidth,
                        GroupByItems = SqlListLayoutStyle.WrapByWidth,
                        OrderByItems = SqlListLayoutStyle.WrapByWidth,
                        InListItems = SqlInListItemsStyle.WrapByWidth
                    }
                },
                new SqlFormattingOptions
                {
                    Layout = new SqlLayoutFormattingOptions
                    {
                        WrapColumn = 120
                    },
                    Lists = new SqlListsFormattingOptions
                    {
                        SelectItems = SqlListLayoutStyle.WrapByWidth,
                        GroupByItems = SqlListLayoutStyle.WrapByWidth,
                        OrderByItems = SqlListLayoutStyle.WrapByWidth,
                        InListItems = SqlInListItemsStyle.WrapByWidth
                    }
                }
            ];

            yield return
            [
                "sql-select-compact-max-items",
                SelectCompactThresholdExampleSql,
                new SqlFormattingOptions
                {
                    Lists = new SqlListsFormattingOptions
                    {
                        SelectItems = SqlListLayoutStyle.OnePerLine,
                        SelectCompactThreshold = new SqlSelectCompactThresholdOptions
                        {
                            MaxItems = 1,
                            MaxLineLength = 120
                        }
                    }
                },
                new SqlFormattingOptions
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
                }
            ];

            yield return
            [
                "sql-select-compact-max-line-length",
                SelectCompactLineLengthExampleSql,
                new SqlFormattingOptions
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
                },
                new SqlFormattingOptions
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
                }
            ];
        }

        public static IEnumerable<object[]> InactiveDependentOptionExamples()
        {
            yield return
            [
                "sql-predicates-logical-break",
                PredicatesExampleSql,
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = false,
                        LogicalOperatorLineBreak = SqlLogicalOperatorLineBreakMode.BeforeOperator
                    }
                },
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = false,
                        LogicalOperatorLineBreak = SqlLogicalOperatorLineBreakMode.AfterOperator
                    }
                }
            ];

            yield return
            [
                "sql-predicates-mixed-and-or-break-or-groups",
                PredicatesExampleSql,
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = false,
                        MixedAndOrParentheses = new SqlMixedAndOrParenthesesOptions
                        {
                            BreakOrGroups = false
                        }
                    }
                },
                new SqlFormattingOptions
                {
                    Predicates = new SqlPredicatesFormattingOptions
                    {
                        MultilineWhere = false,
                        MixedAndOrParentheses = new SqlMixedAndOrParenthesesOptions
                        {
                            BreakOrGroups = true
                        }
                    }
                }
            ];
        }

        private const string KeywordCaseExampleSql =
            @"-- Keyword case: watch SELECT/FROM/WHERE keyword casing after formatting.
select o.CustomerId as customerAlias,o.Region as regionAlias from dbo.Orders as o where o.CustomerId=@customerId;";

        private const string SemicolonAndEofExampleSql =
            @"-- Statement terminator and EOF newline: watch the semicolon and final trailing newline.
select 1";

        private const string AlignAliasesExampleSql =
            @"-- Align aliases: compare how alias columns line up in the SELECT list.
select o.CustomerId as customer_id,o.TotalAmount as very_long_total_amount,o.Region as region from dbo.Orders as o;";

        private const string LayoutClausesExampleSql =
            @"-- Clause layout: inspect SELECT/FROM/WHERE/GROUP BY/HAVING/ORDER BY line breaks.
select o.CustomerId,o.Region,sum(o.TotalAmount) as total_amount
from dbo.Orders as o
where o.TotalAmount>=100
group by o.CustomerId,o.Region
having sum(o.TotalAmount)>150
order by o.CustomerId;";

        private const string LayoutWithClauseExampleSql =
            @"-- WITH clause newline: toggle whether WITH stays after AS or starts a new line.
create view dbo.v_sales as with sales_cte as (select o.CustomerId,o.TotalAmount,o.Region from dbo.Orders as o)
select CustomerId,Region,sum(TotalAmount) as total_amount
from sales_cte
where TotalAmount>=100
group by CustomerId,Region
having sum(TotalAmount)>150
order by CustomerId;";

        private const string LayoutOptionClauseExampleSql =
            @"-- OPTION clause newline: check whether OPTION moves to a dedicated line.
select o.CustomerId,o.TotalAmount from dbo.Orders as o option (recompile);";

        private const string JoinsExampleSql =
            @"-- JOIN layout: try ON token limits such as 0, 12, 18 and toggle break on AND/OR.
select a.Id,a.Region
from dbo.A as a
inner join dbo.B as b on a.Id=b.Id and b.Flag=1
left join dbo.C as c on a.Id=c.Id and c.Flag=1 and c.Region=a.Region
left join dbo.D as d on a.Id=d.Id and d.Flag=1 and d.Region=a.Region or d.Kind=a.Kind;";

        private const string PredicatesExampleSql =
            @"-- Predicate layout: inspect WHERE logical breaks and mixed AND/OR grouping.
select a.Id
from dbo.A as a
where a.Status=1 and a.Region='EU' or a.Region='US' and a.Score>10 or a.PriorityScore>3;";

        private const string PredicatesInlineExampleSql =
            @"-- Inline predicate threshold: compare 0, 2, 4 conditions and short vs longer line limits.
select a.Id
from dbo.A as a
where a.Status=1 and a.Region='EU' and a.Score>10 and a.Flag=1;";

        private const string PredicateAllowOnlyAndExampleSql =
            @"-- Inline predicate AND-only: compare OR staying multiline unless OR is explicitly allowed.
select a.Id
from dbo.A as a
where a.Status=1 or a.Flag=1;";

        private const string ExpressionsCaseExampleSql =
            @"-- CASE thresholds: compare token limits such as 10, 14, and 20 with compact CASE enabled.
set @grade = case when @score>9 then 1 when @score>5 then 2 else 3 end;";

        private const string InlineShortExpressionExampleSql =
            @"-- Inline short expression: compare token limits such as 0, 12, and 20 plus SELECT/ON/WHERE contexts.
select a.Price+a.Tax as total,a.Id as id
from dbo.A as a
inner join dbo.B as b on a.Id+b.Id+a.LegacyId>10 and b.Flag=1
where a.Score+a.Bonus+a.Penalty>0 and a.IsActive=1;";

        private const string ShortQueriesExampleSql =
            @"select 1 from dbo.A where X=3 and Y=4;
select a.Id
from dbo.A as a
where exists (select 1 from dbo.B as b where b.AId=a.Id and b.IsActive=1);
select q.Id
from (select a.Id from dbo.A as a where a.Status=1 and a.Flag=1) as q;
select a.Id
from dbo.A as a
inner join dbo.B as b on a.Id=b.AId
where a.Status=1;";

        private const string ShortQuerySelectItemsExampleSql =
            @"select a.Id,a.Region from dbo.A as a where a.Status=1;";

        private const string ShortQueryPredicateThresholdExampleSql =
            @"select 1 from dbo.A where X=3 and Y=4 and Z=5;";

        private const string ListsExampleSql =
            @"-- Wrap-by-width layout: compare widths such as 30, 60, and 120 across SELECT/IN/GROUP BY/ORDER BY lists.
select rf.CurrentQuarterRevenue,rf.ProjectedAnnualRevenue,rf.TrailingTwelveMonthRevenue
from dbo.RevenueForecasts as rf
where rf.RegionCode in('NorthEurope','WestEurope','CentralUS')
group by rf.CurrentQuarterRevenue,rf.ProjectedAnnualRevenue,rf.TrailingTwelveMonthRevenue
order by rf.CurrentQuarterRevenue,rf.ProjectedAnnualRevenue,rf.TrailingTwelveMonthRevenue;";

        private const string SelectCompactThresholdExampleSql =
            @"SELECT a+b AS c, d AS f FROM dbo.t AS t";

        private const string SelectCompactLineLengthExampleSql =
            @"SELECT currentQuarterRevenue AS current_quarter_revenue, projectedAnnualRevenue AS projected_annual_revenue FROM dbo.t AS t";

        private const string InListThresholdExampleSql =
            @"-- Inline IN threshold: inspect when IN(...) remains inline versus multiline.
select a.Id from dbo.A as a where a.Id in(1,2,3,4,5,6,7,8,9,10,11,12);";

        private const string DmlAndDdlExampleSql =
            @"-- DML/DDL layout: check UPDATE/INSERT lists and CREATE PROC block formatting.
update dbo.A set Region='EU',Status=1,Score=10 where Id=@id;
insert into dbo.AuditEntries(EntryId,Region,Status,Score) values(@id,'EU',1,10);
create proc p as begin select 1 end;";

        private const string CommentsExampleSql =
            @"-- Comment whitespace: compare original spacing with safe whitespace normalization.
select a.Id /*    keep   spacing   */ as id_alias --line    comment
from dbo.A as a;";

        private const string SpacingExampleSql =
            @"-- Spacing options: inspect spaces after commas, around operators, and parentheses.
select(a.Id+a.Score),a.Region from dbo.A as a where a.Id=1 and a.Score>=10;";

        private static readonly IReadOnlyDictionary<string, string> OptionExampleSqlByOptionId = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["sql-keyword-case"] = KeywordCaseExampleSql,
            ["sql-statement-semicolon"] = SemicolonAndEofExampleSql,
            ["sql-statement-blank-lines"] = DmlAndDdlExampleSql,
            ["sql-eof-newline"] = SemicolonAndEofExampleSql,
            ["sql-align-select-aliases"] = AlignAliasesExampleSql,
            ["sql-indent-size"] = LayoutClausesExampleSql,
            ["sql-wrap-column"] = ListsExampleSql,
            ["sql-blank-line-between-clauses"] = LayoutClausesExampleSql,
            ["sql-newline-with"] = LayoutWithClauseExampleSql,
            ["sql-newline-select"] = LayoutClausesExampleSql,
            ["sql-newline-from"] = LayoutClausesExampleSql,
            ["sql-newline-where"] = LayoutClausesExampleSql,
            ["sql-newline-group-by"] = LayoutClausesExampleSql,
            ["sql-newline-having"] = LayoutClausesExampleSql,
            ["sql-newline-order-by"] = LayoutClausesExampleSql,
            ["sql-newline-option"] = LayoutOptionClauseExampleSql,
            ["sql-joins-newline-per-join"] = JoinsExampleSql,
            ["sql-joins-on-new-line"] = JoinsExampleSql,
            ["sql-joins-multiline-threshold"] = JoinsExampleSql,
            ["sql-joins-break-on-and"] = JoinsExampleSql,
            ["sql-joins-break-on-or"] = JoinsExampleSql,
            ["sql-predicates-multiline-where"] = PredicatesExampleSql,
            ["sql-predicates-logical-break"] = PredicatesExampleSql,
            ["sql-predicates-inline-max-conditions"] = PredicatesInlineExampleSql,
            ["sql-predicates-inline-max-line-length"] = PredicatesInlineExampleSql,
            ["sql-predicates-inline-allow-only-and"] = PredicateAllowOnlyAndExampleSql,
            ["sql-predicates-mixed-and-or-parenthesize-or-groups"] = PredicatesExampleSql,
            ["sql-predicates-mixed-and-or-break-or-groups"] = PredicatesExampleSql,
            ["sql-expressions-case-style"] = ExpressionsCaseExampleSql,
            ["sql-case-threshold-max-when"] = ExpressionsCaseExampleSql,
            ["sql-case-threshold-max-line"] = ExpressionsCaseExampleSql,
            ["sql-inline-short-max-tokens"] = InlineShortExpressionExampleSql,
            ["sql-inline-short-max-line"] = InlineShortExpressionExampleSql,
            ["sql-inline-short-select-item"] = InlineShortExpressionExampleSql,
            ["sql-inline-short-on"] = InlineShortExpressionExampleSql,
            ["sql-inline-short-where"] = InlineShortExpressionExampleSql,
            ["sql-short-queries-enabled"] = ShortQueriesExampleSql,
            ["sql-short-queries-max-line-length"] = ShortQueriesExampleSql,
            ["sql-short-queries-max-select-items"] = ShortQuerySelectItemsExampleSql,
            ["sql-short-queries-max-predicate-conditions"] = ShortQueryPredicateThresholdExampleSql,
            ["sql-short-queries-apply-to-parenthesized-subqueries"] = ShortQueriesExampleSql,
            ["sql-short-queries-allow-single-join"] = ShortQueriesExampleSql,
            ["sql-comma-style"] = ListsExampleSql,
            ["sql-select-items-style"] = ListsExampleSql,
            ["sql-group-by-items-style"] = ListsExampleSql,
            ["sql-order-by-items-style"] = ListsExampleSql,
            ["sql-select-compact-max-items"] = SelectCompactThresholdExampleSql,
            ["sql-select-compact-max-line-length"] = SelectCompactLineLengthExampleSql,
            ["sql-in-list-items-style"] = ListsExampleSql,
            ["sql-inline-in-list-max-items"] = InListThresholdExampleSql,
            ["sql-inline-in-list-max-line"] = InListThresholdExampleSql,
            ["sql-update-set-style"] = DmlAndDdlExampleSql,
            ["sql-insert-columns-style"] = DmlAndDdlExampleSql,
            ["sql-insert-values-style"] = DmlAndDdlExampleSql,
            ["sql-create-proc-layout"] = DmlAndDdlExampleSql,
            ["sql-comments-preserve-attachment"] = CommentsExampleSql,
            ["sql-comments-formatting"] = CommentsExampleSql,
            ["sql-inside-parentheses"] = SpacingExampleSql,
            ["sql-spaces-after-comma"] = SpacingExampleSql,
            ["sql-spaces-around-binary-operators"] = SpacingExampleSql,
            ["sql-spaces-before-semicolon"] = SpacingExampleSql
        };

        private static string FormatOrThrow(string optionId, string sourceSql, SqlFormattingOptions options)
        {
            var result = ModernMsSqlFormatter.TryFormat(sourceSql, options);
            result.IsSuccess.Should().BeTrue(
                $"option example '{optionId}' should format with the tested options, but failed with: {result.ErrorMessage}");
            return NormalizeLineEndings(result.FormattedSql!);
        }

        private static string NormalizeLineEndings(string text) => text.Replace("\r\n", "\n");
    }
}
