using System;
using System.Collections.Generic;
using DSLKIT.GrammarExamples.MsSql;
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

            var reparsed = ModernMsSqlGrammarExample.ParseScript(result.FormattedSql!);
            reparsed.IsSuccess.Should().BeTrue(
                $"formatted SQL for option '{optionId}' should parse, but failed with: {reparsed.Error}");
        }

        public static IEnumerable<object[]> OptionExamples()
        {
            foreach (var optionExample in OptionExampleSqlByOptionId)
            {
                yield return new object[] { optionExample.Key, optionExample.Value };
            }
        }

        private const string KeywordCaseExampleSql =
            @"select o.CustomerId as customerAlias,o.Region as regionAlias from dbo.Orders as o where o.CustomerId=@customerId;";

        private const string SemicolonAndEofExampleSql = "select 1";

        private const string AlignAliasesExampleSql =
            @"select o.CustomerId as customer_id,o.TotalAmount as very_long_total_amount,o.Region as region from dbo.Orders as o;";

        private const string LayoutClausesExampleSql =
            @"with sales_cte as (select o.CustomerId,o.TotalAmount,o.Region from dbo.Orders as o)
select CustomerId,Region,sum(TotalAmount) as total_amount
from sales_cte
where TotalAmount>=100
group by CustomerId,Region
having sum(TotalAmount)>150
order by CustomerId;";

        private const string LayoutOptionClauseExampleSql =
            @"select o.CustomerId,o.TotalAmount from dbo.Orders as o option (recompile);";

        private const string JoinsExampleSql =
            @"select a.Id,a.Region from dbo.A as a inner join dbo.B as b on a.Id=b.Id and a.Region=b.Region or a.IsActive=b.IsActive and a.Type=b.Type;";

        private const string PredicatesExampleSql =
            @"select a.Id from dbo.A as a where a.Status=1 and a.Region='EU' or a.Region='US' and a.Score>10;";

        private const string ExpressionsCaseExampleSql =
            @"select case when a.Score>90 then 'A' when a.Score>70 then 'B' else 'C' end as grade from dbo.A as a;";

        private const string InlineShortExpressionExampleSql =
            @"select ((a.Price+a.Tax)+a.Fee)+a.Discount as total from dbo.A as a inner join dbo.B as b on ((a.Id+b.Id)+b.ShiftAmount)>10 where ((a.Score+a.Bonus)+a.Penalty)>0;";

        private const string ListsExampleSql =
            @"select a.Id,a.Region,a.Status,a.Score from dbo.A as a where a.Id in(1,2,3,4,5,6,7,8) group by a.Id,a.Region,a.Status,a.Score order by a.Id,a.Region,a.Status,a.Score;";

        private const string SelectCompactWithExpressionsExampleSql =
            @"select a.Id,a.Region,a.Score+10 as boosted_score,a.Status from dbo.A as a;";

        private const string InListThresholdExampleSql =
            @"select a.Id from dbo.A as a where a.Id in(1,2,3,4,5,6,7,8,9,10,11,12);";

        private const string DmlAndDdlExampleSql =
            @"update dbo.A set Region='EU',Status=1,Score=10 where Id=@id;
insert into dbo.Log(Id,Region,Status,Score) values(@id,'EU',1,10);
create proc p as begin select 1 end;";

        private const string CommentsExampleSql =
            @"select a.Id /*    keep   spacing   */ as id_alias --line    comment
from dbo.A as a;";

        private const string StringLiteralsExampleSql =
            @"select 'A   B  C' as label from dbo.A as a;";

        private const string SpacingExampleSql =
            @"select(a.Id+a.Score),a.Region from dbo.A as a where a.Id=1 and a.Score>=10;";

        private static readonly IReadOnlyDictionary<string, string> OptionExampleSqlByOptionId = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["sql-keyword-case"] = KeywordCaseExampleSql,
            ["sql-statement-semicolon"] = SemicolonAndEofExampleSql,
            ["sql-eof-newline"] = SemicolonAndEofExampleSql,
            ["sql-align-select-aliases"] = AlignAliasesExampleSql,
            ["sql-indent-size"] = LayoutClausesExampleSql,
            ["sql-blank-line-between-clauses"] = LayoutClausesExampleSql,
            ["sql-newline-with"] = LayoutClausesExampleSql,
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
            ["sql-joins-break-on"] = JoinsExampleSql,
            ["sql-predicates-multiline-where"] = PredicatesExampleSql,
            ["sql-predicates-logical-break"] = PredicatesExampleSql,
            ["sql-predicates-inline-max-conditions"] = PredicatesExampleSql,
            ["sql-predicates-inline-max-line-length"] = PredicatesExampleSql,
            ["sql-predicates-inline-allow-only-and"] = PredicatesExampleSql,
            ["sql-predicates-parenthesize-mode"] = PredicatesExampleSql,
            ["sql-expressions-case-style"] = ExpressionsCaseExampleSql,
            ["sql-case-threshold-max-when"] = ExpressionsCaseExampleSql,
            ["sql-case-threshold-max-tokens"] = ExpressionsCaseExampleSql,
            ["sql-case-threshold-max-line"] = ExpressionsCaseExampleSql,
            ["sql-inline-short-max-tokens"] = InlineShortExpressionExampleSql,
            ["sql-inline-short-max-depth"] = InlineShortExpressionExampleSql,
            ["sql-inline-short-max-line"] = InlineShortExpressionExampleSql,
            ["sql-inline-short-select-item"] = InlineShortExpressionExampleSql,
            ["sql-inline-short-on"] = InlineShortExpressionExampleSql,
            ["sql-inline-short-where"] = InlineShortExpressionExampleSql,
            ["sql-comma-style"] = ListsExampleSql,
            ["sql-select-items-style"] = ListsExampleSql,
            ["sql-group-by-items-style"] = ListsExampleSql,
            ["sql-order-by-items-style"] = ListsExampleSql,
            ["sql-select-compact-max-items"] = ListsExampleSql,
            ["sql-select-compact-max-line-length"] = ListsExampleSql,
            ["sql-select-compact-allow-expressions"] = SelectCompactWithExpressionsExampleSql,
            ["sql-in-list-items-style"] = ListsExampleSql,
            ["sql-inline-in-list-max-items"] = InListThresholdExampleSql,
            ["sql-inline-in-list-max-line"] = InListThresholdExampleSql,
            ["sql-update-set-style"] = DmlAndDdlExampleSql,
            ["sql-insert-columns-style"] = DmlAndDdlExampleSql,
            ["sql-create-proc-layout"] = DmlAndDdlExampleSql,
            ["sql-comments-preserve-attachment"] = CommentsExampleSql,
            ["sql-comments-formatting"] = CommentsExampleSql,
            ["sql-preserve-string-literals"] = StringLiteralsExampleSql,
            ["sql-inside-parentheses"] = SpacingExampleSql,
            ["sql-spaces-after-comma"] = SpacingExampleSql,
            ["sql-spaces-around-binary-operators"] = SpacingExampleSql,
            ["sql-spaces-before-semicolon"] = SpacingExampleSql
        };
    }
}
