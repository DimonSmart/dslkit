using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Fact]
        public void ParseScript_ShouldParseCreateOrAlterViewAndAlterProcedure()
        {
            const string script = """
                CREATE OR ALTER VIEW dbo.vTest AS SELECT 1 AS A;
                ALTER VIEW dbo.vTest AS SELECT 2 AS A;
                ALTER PROCEDURE dbo.usp_Test AS
                BEGIN
                    RETURN 1;
                END;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateView_WithColumnsAndMultipleOptions()
        {
            const string script = """
                CREATE VIEW dbo.vCustomerSummary (CustomerId, CustomerName)
                WITH ENCRYPTION, SCHEMABINDING, VIEW_METADATA
                AS
                WITH src AS (
                    SELECT 1 AS CustomerId, N'ACME' AS CustomerName
                )
                SELECT CustomerId, CustomerName
                FROM src;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateView_WithCheckOption()
        {
            const string script = """
                CREATE VIEW dbo.vActiveCustomers (CustomerId, CustomerName)
                WITH SCHEMABINDING
                AS
                WITH src AS (
                    SELECT 1 AS CustomerId, N'ACME' AS CustomerName
                )
                SELECT CustomerId, CustomerName
                FROM src
                WITH CHECK OPTION;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectCreateView_WithContextualKeywordColumnName()
        {
            const string script = "CREATE VIEW dbo.vTest (WAITFOR) AS SELECT 1 AS A;";

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(
                "CREATE VIEW column lists should not accept contextual keywords through a generic identifier fallback.");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateProcedure_WithOptionsAndParameters()
        {
            const string script = """
                CREATE OR ALTER PROCEDURE [dbo].[usp_ProcessOrder]
                    @order_id INT = 0,
                    @message NVARCHAR(200) OUTPUT READONLY
                WITH ENCRYPTION, RECOMPILE, EXECUTE AS OWNER
                FOR REPLICATION
                AS
                BEGIN
                    DECLARE @counter INT = 0;
                    WHILE @counter < 2
                    BEGIN
                        SET @counter = @counter + 1;
                    END;
                    RETURN @counter;
                END;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateProcedure_ClrAndNativeVariants()
        {
            const string script = """
                CREATE PROCEDURE dbo.usp_ClrProc
                    @id INT = 1 OUTPUT
                WITH EXECUTE AS OWNER
                AS EXTERNAL NAME MyAssembly.MyNamespace.MyType.MyMethod;

                CREATE PROCEDURE dbo.usp_NativeProc
                    @id INT
                WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
                AS
                BEGIN ATOMIC WITH (LANGUAGE = N'us_english', TRANSACTION ISOLATION LEVEL = SNAPSHOT)
                    RETURN @id;
                END;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData(
            "CREATE PROCEDURE dbo.usp_Test WITH EXECUTE AS WAITFOR AS SELECT 1;",
            "CREATE PROCEDURE EXECUTE AS should not accept contextual keywords through a generic identifier fallback.")]
        [InlineData(
            """
            CREATE PROCEDURE dbo.usp_NativeProc
                @id INT
            WITH NATIVE_COMPILATION, SCHEMABINDING, EXECUTE AS OWNER
            AS
            BEGIN ATOMIC WITH (WAITFOR = 1)
                RETURN @id;
            END;
            """,
            "BEGIN ATOMIC WITH should not accept arbitrary identifier names as native atomic options.")]
        public void ParseScript_ShouldRejectProgrammableObjectIdentifiers_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Fact]
        public void ParseScript_ShouldParseCreateFunction_InlineTableValued()
        {
            const string script = """
                CREATE FUNCTION Integration.GenerateDateDimensionColumns(@Date date)
                RETURNS TABLE
                AS
                RETURN
                SELECT @Date AS [Date]
                     , YEAR(@Date) * 10000 + MONTH(@Date) * 0100 + DAY(@Date) AS [DateKey]
                     , N'Q' + DATENAME(quarter, @Date) AS [Quarter]
                     , CASE WHEN MONTH(@Date) BETWEEN 1 AND 3
                            THEN CAST(DATENAME(year, @Date) + N'-01-01' AS DATE)
                            ELSE CAST(DATENAME(year, @Date) + N'-04-01' AS DATE)
                       END [Beginning of Quarter Label Short];
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateOrAlterFunction_ScalarAndMstvf()
        {
            const string script = """
                CREATE OR ALTER FUNCTION dbo.ufn_customer_category(@CustomerKey INT)
                RETURNS NVARCHAR(10)
                WITH SCHEMABINDING, INLINE = OFF
                AS
                BEGIN
                    RETURN N'GENERIC';
                END;

                CREATE FUNCTION policy.pfn_ServerGroupInstances(@server_group_name NVARCHAR(128))
                RETURNS @ServerGroups TABLE
                (
                    [Server Group Name] NVARCHAR(128) NOT NULL
                )
                AS
                BEGIN
                    RETURN;
                END;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseTypeSpec_WithMaxArguments()
        {
            const string script = """
                DECLARE @payload VARBINARY(MAX);
                DECLARE @text NVARCHAR(MAX);

                CREATE FUNCTION dbo.ufn_Echo(@value NVARCHAR(MAX))
                RETURNS NVARCHAR(MAX)
                AS
                BEGIN
                    RETURN @value;
                END;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateFunction_WithPreludeStatementsBeforeRequiredReturn()
        {
            const string script = """
                CREATE FUNCTION dbo.ufn_AddOne(@value INT)
                RETURNS INT
                AS
                BEGIN
                    DECLARE @result INT = @value;
                    SET @result = @result + 1
                    RETURN @result;
                END;

                CREATE FUNCTION dbo.ufn_Numbers(@value INT)
                RETURNS @items TABLE
                (
                    ItemValue INT NOT NULL
                )
                AS
                BEGIN
                    INSERT INTO @items (ItemValue)
                    VALUES (@value)
                    RETURN;
                END;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectInvalidCreateFunctionBodyShapes()
        {
            var invalidScripts = new (string Script, string Reason)[]
            {
                ("CREATE FUNCTION dbo.f(@x int) RETURNS int AS RETURN SELECT 1;", "scalar functions must use a block body"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS int AS RETURN (SELECT 1);", "scalar functions must not use inline table-valued RETURN syntax"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS TABLE AS BEGIN RETURN; END;", "inline table-valued functions must return a query expression"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS @t TABLE (Id int) AS RETURN SELECT 1 AS Id;", "multi-statement table-valued functions must use a block body"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS int BEGIN RETURN 1; END", "scalar functions require AS before BEGIN"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS TABLE RETURN SELECT 1 AS Id", "inline table-valued functions require AS before RETURN"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS @t TABLE (Id int) BEGIN RETURN; END", "multi-statement table-valued functions require AS before BEGIN"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS int AS BEGIN SELECT 1; END;", "scalar functions must end with RETURN <expression>"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS int AS BEGIN RETURN; END;", "scalar functions must not use bare RETURN"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS @t TABLE (Id int) AS BEGIN INSERT INTO @t VALUES (1); END;", "multi-statement table-valued functions must end with RETURN"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS @t TABLE (Id int) AS BEGIN RETURN 1; END;", "multi-statement table-valued functions must use bare RETURN")
            };

            foreach (var (script, reason) in invalidScripts)
            {
                var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);
                parseResult.IsSuccess.Should().BeFalse(reason);
            }
        }

        [Fact]
        public void ParseScript_ShouldParseProcParameter_WithAsKeyword()
        {
            const string script = """
                CREATE PROCEDURE [DataLoadSimulation].[GetFicticiousName]
                    @FirstName AS NVARCHAR(20) OUTPUT
                  , @LastName  AS NVARCHAR(20) OUTPUT
                  , @FullName  AS NVARCHAR(40) OUTPUT
                  , @Email     AS NVARCHAR(200) OUTPUT
                AS
                BEGIN
                    SELECT TOP 1
                           @FirstName = PreferredName
                         , @LastName  = LastName
                      FROM dbo.NamePool
                     ORDER BY NEWID();
                END;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }
    }
}