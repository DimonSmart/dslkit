using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.Visualizer.App.Components.SqlFormatting;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.Visualizer;

public class SqlFormattingStateTests
{
    [Fact]
    public void TrySwitchDialectDemoSql_ShouldReplaceSqlServerDemo_WhenSnowflakeDialectSelected()
    {
        var state = new SqlFormattingState
        {
            Dialect = SqlDialect.Snowflake,
            SourceSql = SqlFormattingExamples.DemoSql
        };

        var result = state.TrySwitchDialectDemoSql();

        result.Should().BeTrue();
        state.SourceSql.Should().Be(SqlFormattingExamples.SnowflakeDemoSql);
    }

    [Fact]
    public void TrySwitchDialectDemoSql_ShouldReplaceSnowflakeDemo_WhenSqlServerDialectSelected()
    {
        var state = new SqlFormattingState
        {
            Dialect = SqlDialect.SqlServer,
            SourceSql = SqlFormattingExamples.SnowflakeDemoSql
        };

        var result = state.TrySwitchDialectDemoSql();

        result.Should().BeTrue();
        state.SourceSql.Should().Be(SqlFormattingExamples.DemoSql);
    }

    [Fact]
    public void TrySwitchDialectDemoSql_ShouldNotReplaceCustomSql()
    {
        var state = new SqlFormattingState
        {
            Dialect = SqlDialect.Snowflake,
            SourceSql = "select 1;"
        };

        var result = state.TrySwitchDialectDemoSql();

        result.Should().BeFalse();
        state.SourceSql.Should().Be("select 1;");
    }
}
