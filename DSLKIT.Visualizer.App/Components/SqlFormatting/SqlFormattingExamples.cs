namespace DSLKIT.Visualizer.App.Components.SqlFormatting;

internal static class SqlFormattingExamples
{
    public const string DemoSql = @"create view dbo.v_recent as with seed as (select top(1) o.CustomerId from dbo.Orders as o) select CustomerId from seed;
with recent as (
select top(3) o.CustomerId,o.Region,o.TotalAmount from dbo.Orders as o where o.TotalAmount>=50
)
select case when r.TotalAmount>=200 then 'vip' else 'std' end as segment,r.CustomerId /*  keep   spacing */ as customer_id,r.Region
from recent as r inner join dbo.Customers as c on c.CustomerId=r.CustomerId and c.IsActive=1 and c.Region in('EU','US','APAC')
where (r.Region='EU' or r.Region='US') and r.TotalAmount>=100 and r.CustomerId in(1,2,3,4)
group by r.CustomerId,r.Region,r.TotalAmount
order by r.CustomerId desc,r.Region;
update dbo.Customers set Region='EU',IsActive=1 where CustomerId=@customerId;
insert into dbo.AuditLog(CustomerId,Region,Amount) values(@customerId,'EU',100);
create proc p as begin select case when @flag=1 then 'Y' else 'N' end as v end;";

    public const string KeywordCaseExampleSql =
        @"-- Keyword case: watch SELECT/FROM/WHERE keyword casing after formatting.
select o.CustomerId as customerAlias,o.Region as regionAlias from dbo.Orders as o where o.CustomerId=@customerId;";

    public const string SemicolonAndEofExampleSql =
        @"-- Statement terminator and EOF newline: watch the semicolon and final trailing newline.
select 1";

    public const string AlignAliasesExampleSql =
        @"-- Align aliases: compare how alias columns line up in the SELECT list.
select o.CustomerId as customer_id,o.TotalAmount as very_long_total_amount,o.Region as region from dbo.Orders as o;";

    public const string LayoutClausesExampleSql =
        @"-- Clause layout: inspect SELECT/FROM/WHERE/GROUP BY/HAVING/ORDER BY line breaks.
select o.CustomerId,o.Region,sum(o.TotalAmount) as total_amount
from dbo.Orders as o
where o.TotalAmount>=100
group by o.CustomerId,o.Region
having sum(o.TotalAmount)>150
order by o.CustomerId;";

    public const string LayoutWithClauseExampleSql =
        @"-- WITH clause newline: toggle whether WITH stays after AS or starts a new line.
create view dbo.v_sales as with sales_cte as (select o.CustomerId,o.TotalAmount,o.Region from dbo.Orders as o)
select CustomerId,Region,sum(TotalAmount) as total_amount
from sales_cte
where TotalAmount>=100
group by CustomerId,Region
having sum(TotalAmount)>150
order by CustomerId;";

    public const string IndentCteBodyExampleSql =
        @"-- Indent CTE body: toggle whether the query inside AS (...) shifts right.
create view dbo.v_recent as with seed as (select top(1) o.CustomerId from dbo.Orders as o)
select CustomerId
from seed;";

    public const string LayoutOptionClauseExampleSql =
        @"-- OPTION clause newline: check whether OPTION moves to a dedicated line.
select o.CustomerId,o.TotalAmount from dbo.Orders as o option (recompile);";

    public const string JoinsExampleSql =
        @"-- JOIN layout: try ON condition limits such as 0, 2, 3 and compare mixed AND/OR grouping.
select a.Id,a.Region
from dbo.A as a
inner join dbo.B as b on a.Id=b.Id and b.Flag=1
left join dbo.C as c on a.Id=c.Id and c.Flag=1 and c.Region=a.Region
left join dbo.D as d on a.Id=d.Id and d.Flag=1 and d.Region=a.Region or d.Kind=a.Kind;";

    public const string PredicatesExampleSql =
        @"-- Predicate layout: inspect WHERE logical breaks and mixed AND/OR grouping.
select a.Id
from dbo.A as a
where a.Status=1 and a.Region='EU' or a.Region='US' and a.Score>10 or a.PriorityScore>3;";

    public const string PredicatesInlineExampleSql =
        @"-- Inline predicate threshold: compare 0, 2, 4 conditions and short vs longer line limits.
select a.Id
from dbo.A as a
where a.Status=1 and a.Region='EU' and a.Score>10 and a.Flag=1;";

    public const string PredicateAllowOnlyAndExampleSql =
        @"-- Inline predicate AND-only: compare OR staying multiline unless OR is explicitly allowed.
select a.Id
from dbo.A as a
where a.Status=1 or a.Flag=1;";

    public const string ExpressionsCaseExampleSql =
        @"-- CASE thresholds: compare token limits such as 10, 14, and 20 with compact CASE enabled.
set @grade = case when @score>9 then 1 when @score>5 then 2 else 3 end;";

    public const string InlineShortExpressionExampleSql =
        @"-- Inline short expression: compare token limits such as 0, 12, and 20 plus SELECT/ON/WHERE contexts.
select a.Price+a.Tax as total,a.Id as id
from dbo.A as a
inner join dbo.B as b on a.Id+b.Id+a.LegacyId>10 and b.Flag=1
where a.Score+a.Bonus+a.Penalty>0 and a.IsActive=1;";

    public const string ShortQueriesExampleSql =
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

    public const string ShortQuerySelectItemsExampleSql =
        @"select a.Id,a.Region from dbo.A as a where a.Status=1;";

    public const string ShortQueryPredicateThresholdExampleSql =
        @"select 1 from dbo.A where X=3 and Y=4 and Z=5;";

    public const string ListsExampleSql =
        @"-- Wrap-by-width layout: compare widths such as 30, 60, and 120 across SELECT/IN/GROUP BY/ORDER BY lists.
select rf.CurrentQuarterRevenue,rf.ProjectedAnnualRevenue,rf.TrailingTwelveMonthRevenue
from dbo.RevenueForecasts as rf
where rf.RegionCode in('NorthEurope','WestEurope','CentralUS')
group by rf.CurrentQuarterRevenue,rf.ProjectedAnnualRevenue,rf.TrailingTwelveMonthRevenue
order by rf.CurrentQuarterRevenue,rf.ProjectedAnnualRevenue,rf.TrailingTwelveMonthRevenue;";

    public const string SelectCompactThresholdExampleSql =
        @"SELECT a+b AS c, d AS f FROM dbo.t AS t";

    public const string SelectCompactLineLengthExampleSql =
        @"SELECT currentQuarterRevenue AS current_quarter_revenue, projectedAnnualRevenue AS projected_annual_revenue FROM dbo.t AS t";

    public const string InListThresholdExampleSql =
        @"-- Inline IN threshold: inspect when IN(...) remains inline versus multiline.
select a.Id from dbo.A as a where a.Id in(1,2,3,4,5,6,7,8,9,10,11,12);";

    public const string DmlAndDdlExampleSql =
        @"-- DML/DDL layout: check UPDATE/INSERT lists and CREATE PROC block formatting.
update dbo.A set Region='EU',Status=1,Score=10 where Id=@id;
insert into dbo.AuditEntries(EntryId,Region,Status,Score) output inserted.EntryId values(@id,'EU',1,10),(@id+1,'US',2,20);
create proc p as begin select 1 end;";

    public const string CreateProcLayoutExampleSql =
        @"-- CREATE PROC layout: compare expanded and compact procedure body formatting.
create proc p as begin select 1 as value end;";

    public const string CommentsExampleSql =
        @"-- Comment whitespace: compare original spacing with safe whitespace normalization.
select a.Id /*    keep   spacing   */ as id_alias --line    comment
from dbo.A as a;";

    public const string SpacingExampleSql =
        @"-- Spacing options: inspect spaces after commas, around operators, and parentheses.
select(a.Id+a.Score),a.Region from dbo.A as a where a.Id=1 and a.Score>=10;";

}
