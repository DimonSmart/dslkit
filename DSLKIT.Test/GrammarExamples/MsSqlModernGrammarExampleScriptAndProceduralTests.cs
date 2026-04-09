using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Fact]
        public void ParseScript_ShouldParseIfDeclareSetAndCreateView()
        {
            const string script = """
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

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseEmptyBeginEndBlock()
        {
            const string script = """
                IF 1 = 1
                BEGIN
                END;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseIfExistsWithDropProcedureAndHexBitmask()
        {
            const string script = """
                if exists (select * from sysobjects where id = object_id('dbo.SalesByCategory') and sysstat & 0xf = 4)
                	drop procedure "dbo"."SalesByCategory"
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData(
            "WITH WAITFOR AS (SELECT 1 AS X) SELECT X FROM WAITFOR;",
            "CTE names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "WITH cte (WAITFOR) AS (SELECT 1 AS X) SELECT X FROM cte;",
            "CTE column lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "WITH XMLNAMESPACES ('http://example.com' AS WAITFOR) SELECT 1;",
            "XML namespace aliases should not accept contextual keywords through broad IdentifierTerm fallback.")]
        public void ParseScript_ShouldRejectScriptIdentifiers_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Fact]
        public void ParseScript_ShouldParseGotoAndLabelStatements()
        {
            const string script = """
                BEGIN TRANSACTION;
                GOTO EndSave;
                QuitWithRollback:
                    IF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION;
                EndSave:
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCursorStatements()
        {
            const string script = """
                DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT 1 AS Id;
                OPEN c;
                FETCH NEXT FROM c;
                CLOSE c;
                DEALLOCATE c;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData("DECLARE GRAPH CURSOR FOR SELECT 1;")]
        [InlineData("OPEN GRAPH;")]
        [InlineData("GOTO GRAPH;")]
        [InlineData("GRAPH: PRINT 1;")]
        public void ParseScript_ShouldRejectProceduralIdentifiers_WithContextualKeywords(string script)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("procedural identifiers should not accept unrelated contextual keywords.");
        }

        [Fact]
        public void ParseScript_ShouldParseSystemFunctionWithDoubleColonPrefix()
        {
            const string script = """
                IF EXISTS (SELECT * FROM ::fn_listextendedproperty('SnapshotFolder', 'user', 'dbo', 'table', 'UIProperties', null, null))
                    SELECT 1;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseWithXmlNamespacesPrefix()
        {
            const string script = """
                WITH XMLNAMESPACES ('http://schemas.microsoft.com/sqlserver/DMF/2007/08' AS DMF)
                INSERT INTO policy.PolicyHistoryDetail
                SELECT Res.Expr.value('(../DMF:TargetQueryExpression)[1]', 'nvarchar(150)')
                FROM policy.PolicyHistory AS PH
                CROSS APPLY EvaluationResults.nodes('//TargetQueryExpression') AS Res(Expr);
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseIfWithElse_InsideProcBody()
        {
            var r0 = ModernMsSqlGrammarExample.ParseBatch(
                "IF (1=1) DELETE dbo.T ELSE TRUNCATE TABLE dbo.T");
            r0.IsSuccess.Should().BeTrue($"DELETE ELSE TRUNCATE: {r0.Error?.ErrorPosition}: {r0.Error?.Message}");

            var r1 = ModernMsSqlGrammarExample.ParseBatch(
                "IF @x < 500 SET @c = 'A'; ELSE IF @x < 1000 SET @c = 'B'; ELSE SET @c = 'C'");
            r1.IsSuccess.Should().BeTrue($"IF SET; ELSE IF: {r1.Error?.ErrorPosition}: {r1.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldBindElseToNearestIf_InElseIfChain()
        {
            const string script = "IF @x < 500 SET @c = 'A'; ELSE IF @x < 1000 SET @c = 'B'; ELSE SET @c = 'C'";

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var elsePaths = FindTerminalPaths(parseResult.ParseTree!, "ELSE");
            elsePaths.Should().HaveCount(2);

            CountPathSegment(elsePaths[0], "IfStatement").Should().Be(1);
            CountPathSegment(elsePaths[1], "IfStatement").Should().Be(2);
        }

        [Fact]
        public void ParseScript_ShouldParseTryCatchBodies_WithImplicitKeywordBoundaries()
        {
            const string script = """
                BEGIN TRY
                    DECLARE @message NVARCHAR(100)
                    SET @message = N'work'
                    PRINT @message;
                END TRY
                BEGIN CATCH
                    DECLARE @error_message NVARCHAR(4000)
                    SET @error_message = ERROR_MESSAGE()
                    PRINT @error_message;
                END CATCH;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseImplicitSelectAfterKeywordLedStatement()
        {
            const string script = """
                BEGIN TRY
                    DECLARE @message NVARCHAR(100)
                    SET @message = N'work'
                    SELECT @message AS MessageText
                END TRY
                BEGIN CATCH
                    PRINT ERROR_MESSAGE();
                END CATCH;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseNamedCommitAndRollbackInsideTryCatch()
        {
            const string script = """
                BEGIN TRANSACTION tran1
                BEGIN TRY
                    UPDATE [SalesLT].[Customer]
                    SET [CompanyName] = 'TranCompany'
                    WHERE [CustomerID] = 1

                    UPDATE [SalesLT].[Customer]
                    SET [LastName] = 'Kowalski'
                    WHERE [CustomerID] = 2

                    ;THROW 60000, 'Wojciecherroormsg', 1

                    COMMIT TRANSACTION tran1
                END TRY
                BEGIN CATCH
                    SELECT ERROR_MESSAGE() AS ErrorMessage;
                    ROLLBACK TRANSACTION tran1
                END CATCH
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }
    }
}
