using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.GrammarExamples.MsSql.Formatting;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlFormatterTests
    {
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

        [Fact]
        public void TryFormat_ShouldFormatSnowflakeViewComments_WhenSnowflakeDialectSelected()
        {
            const string sourceSql = """
                create or replace view PO_HEADER(
                    PURCHASE_ORDER_NR comment 'Purchase order number',
                    PURCHASE_ORDER_DT comment 'Date of the purchase order'
                )
                comment = 'Purchase order header view'
                as
                select
                    PURCHASE_ORDER_NR,
                    PURCHASE_ORDER_DT
                from SOME_SOURCE;
                """;

            var result = ModernMsSqlFormatter.TryFormat(sourceSql, new SqlFormattingOptions
            {
                Dialect = SqlDialect.Snowflake
            });

            result.IsSuccess.Should().BeTrue(result.ErrorMessage);

            var formattedSql = NormalizeLineEndings(result.FormattedSql!);
            NormalizeSql(formattedSql).Should().Be(
                NormalizeSql(sourceSql),
                "formatted Snowflake CREATE VIEW comments should preserve significant SQL tokens.");
            formattedSql.Should().Contain("PURCHASE_ORDER_NR COMMENT 'Purchase order number'");
            formattedSql.Should().Contain("PURCHASE_ORDER_DT COMMENT 'Date of the purchase order'");
            formattedSql.Should().Contain("COMMENT = 'Purchase order header view'");
        }
    }
}