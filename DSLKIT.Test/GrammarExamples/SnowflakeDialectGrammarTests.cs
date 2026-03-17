using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Fact]
        public void ParseScript_ShouldRejectSnowflakeViewSyntax_WhenSqlServerDialectSelected()
        {
            const string script = """
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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, SqlDialect.SqlServer);

            parseResult.IsSuccess.Should().BeFalse("Snowflake-only syntax should stay disabled in the default SQL Server dialect.");
        }

        [Fact]
        public void ParseScript_ShouldParseSnowflakeViewSyntax_WhenSnowflakeDialectSelected()
        {
            const string script = """
                CREATE OR REPLACE VIEW ADP_DX_DSE_PO_BOT.PRODUCT.V_CURRENT_USER_DIRECT_REPORTS AS
                WITH me AS (
                    SELECT
                        PERSONNEL_NR
                    FROM CDP_CORPORATE_DSG_S4_USER.PRODUCT.V_DIM_HR_COMMUNICATION
                    WHERE CURRENT_DATE() BETWEEN START_DT::DATE AND END_DT::DATE
                )
                SELECT
                    PERSONNEL_NR
                FROM me
                QUALIFY ROW_NUMBER() OVER (ORDER BY PERSONNEL_NR DESC) = 1;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, SqlDialect.Snowflake);

            parseResult.IsSuccess.Should().BeTrue(
                $"Snowflake syntax should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseSnowflakeViewColumnAndViewComments_WhenSnowflakeDialectSelected()
        {
            const string script = """
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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, SqlDialect.Snowflake);

            parseResult.IsSuccess.Should().BeTrue(
                $"Snowflake view comments should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var commentPaths = FindTerminalPaths(parseResult.ParseTree!, "COMMENT");
            commentPaths.Should().HaveCount(3);
            commentPaths.Where(path => path.Contains("ViewColumnComment")).Count().Should().Be(2);
            commentPaths.Where(path => path.Contains("CreateViewOption")).Count().Should().Be(1);
        }

        [Fact]
        public void ParseScript_ShouldRejectSnowflakeViewColumnComments_WhenSqlServerDialectSelected()
        {
            const string script = """
                create or replace view PO_HEADER(
                    PURCHASE_ORDER_NR comment 'Purchase order number'
                )
                as
                select PURCHASE_ORDER_NR
                from SOME_SOURCE;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, SqlDialect.SqlServer);

            parseResult.IsSuccess.Should().BeFalse("Snowflake column comment syntax should stay disabled in the SQL Server dialect.");
        }
    }
}