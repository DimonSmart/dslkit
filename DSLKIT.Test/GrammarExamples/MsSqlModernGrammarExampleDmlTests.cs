using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Fact]
        public void ParseScript_ShouldParseDeclareTableVariable_WithAndWithoutAs()
        {
            const string script = """
                DECLARE @OrdersToProcess TABLE
                (
                    OrderId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    CustomerId INT NOT NULL,
                    Payload NVARCHAR(100) NULL,
                    CreatedAt DATETIME2(0) NOT NULL DEFAULT (SYSUTCDATETIME())
                );

                DECLARE @Processed AS TABLE
                (
                    ProcessedId INT NOT NULL,
                    Status NVARCHAR(20) NULL,
                    INDEX IX_Processed_Status NONCLUSTERED (Status)
                );

                INSERT INTO @OrdersToProcess (CustomerId, Payload, CreatedAt)
                VALUES (101, N'pending', SYSUTCDATETIME());

                INSERT @Processed (ProcessedId, Status)
                SELECT OrderId, N'done'
                FROM @OrdersToProcess;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCtePrefixedInsertAndUpdate()
        {
            const string script = """
                WITH SourceRows AS
                (
                    SELECT 1 AS ItemId, N'Alpha' AS ItemName
                )
                INSERT INTO dbo.TargetTable (ItemId, ItemName)
                SELECT ItemId, ItemName
                FROM SourceRows;

                WITH RowsToUpdate AS
                (
                    SELECT 1 AS ItemId, 10 AS Qty
                )
                UPDATE RowsToUpdate
                    SET Qty = Qty + 1;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRequireTerminatorBeforeCte()
        {
            const string script = """
                SELECT 1
                WITH cte AS
                (
                    SELECT 1 AS Value
                )
                SELECT Value
                FROM cte;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("CTE after another statement requires ';' before WITH in SQL Server.");
            parseResult.Error?.Message.Should().Contain("WITH");
        }

        [Fact]
        public void ParseScript_ShouldParseCteAfterSemicolon()
        {
            const string script = """
                SELECT 1;
                WITH cte AS
                (
                    SELECT 1 AS Value
                )
                SELECT Value
                FROM cte;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseUpdateWithFromAndWhere()
        {
            const string script = """
                UPDATE c
                   SET c.ValidTo = rtco.ValidFrom
                FROM Dimension.City AS c
                INNER JOIN Integration.City_Staging AS rtco
                        ON c.CityId = rtco.CityId
                WHERE c.ValidTo = @EndOfTime;

                WITH RowsToCloseOff AS
                (
                    SELECT c.CityId, MIN(c.ValidFrom) AS ValidFrom
                    FROM Integration.City_Staging AS c
                    GROUP BY c.CityId
                )
                UPDATE c
                   SET c.ValidTo = r.ValidFrom
                FROM Dimension.City AS c
                INNER JOIN RowsToCloseOff AS r
                        ON c.CityId = r.CityId
                WHERE c.ValidTo = @EndOfTime;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseDeleteWithFromAndWhere()
        {
            const string script = """
                DELETE c
                FROM Dimension.City AS c
                INNER JOIN Integration.City_Staging AS s
                        ON c.CityId = s.CityId
                WHERE s.IsDeleted = 1;

                WITH RowsToDelete AS
                (
                    SELECT s.CityId
                    FROM Integration.City_Staging AS s
                    WHERE s.IsDeleted = 1
                )
                DELETE FROM c
                FROM Dimension.City AS c
                INNER JOIN RowsToDelete AS d
                        ON c.CityId = d.CityId
                WHERE c.IsActive = 0;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseDelete_WithTopOutputOptionAndRowsetHints()
        {
            const string script = """
                DECLARE @DeletedRows TABLE
                (
                    CityId INT NOT NULL
                );

                DELETE TOP (10) PERCENT FROM c
                OUTPUT deleted.CityId INTO @DeletedRows (CityId)
                FROM Dimension.City AS c
                INNER JOIN Integration.City_Staging AS s
                        ON c.CityId = s.CityId
                WHERE s.IsDeleted = 1
                OPTION (RECOMPILE, MAXDOP 1);

                DELETE FROM OPENROWSET(
                    'SQLOLEDB',
                    'Server=(local);Trusted_Connection=yes;',
                    'SELECT CityId FROM dbo.City')
                WITH (HOLDLOCK, INDEX(1))
                WHERE CURRENT OF GLOBAL CurDelete;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectDeleteStatement_WithUnknownTableHintName()
        {
            const string script = "DELETE FROM dbo.Company WITH (WAITFOR) WHERE IsDeleted = 1;";

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(
                "DML table hints should not accept contextual keywords through a generic identifier fallback.");
        }

        [Fact]
        public void ParseScript_ShouldParseMerge_WithOutputAndOption()
        {
            const string script = """
                MERGE TOP (10) PERCENT INTO dbo.Target AS tgt
                USING (SELECT 1 AS Id) AS src
                ON tgt.Id = src.Id
                WHEN MATCHED THEN UPDATE SET tgt.Id = src.Id
                WHEN NOT MATCHED THEN INSERT (Id) VALUES (src.Id)
                OUTPUT inserted.Id
                OPTION (RECOMPILE);
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectMerge_WithNonTableTarget()
        {
            const string script = """
                MERGE (SELECT 1 AS Id) AS tgt
                USING dbo.Source AS src
                ON tgt.Id = src.Id
                WHEN MATCHED THEN DELETE;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("MERGE target must be a target table, not a derived table.");
        }

        [Fact]
        public void ParseScript_ShouldParseExecuteStatement_Variants()
        {
            const string script = """
                DECLARE @policy_id INT
                EXEC msdb.dbo.sp_syspolicy_add_policy @name=N'Policy', @enabled=True, @policy_id=@policy_id OUTPUT
                SELECT @policy_id;

                EXECUTE @return_code = dbo.usp_DoWork @arg1 = DEFAULT, @arg2 = @policy_id OUT WITH RECOMPILE;
                EXECUTE ('SELECT 1' + N' AS Value') AS USER = 'dbo';
                EXECUTE (N'SELECT * FROM dbo.T WHERE Id = ?', @policy_id OUTPUT) AT DATA_SOURCE [RemoteSource];
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseInsertExec_Variants()
        {
            const string script = """
                INSERT INTO dbo.TargetTable EXEC dbo.usp_FillTarget;
                INSERT dbo.TargetTable (A, B) EXECUTE dbo.usp_FillTargetByParams @a = 1, @b = DEFAULT;
                INSERT INTO [dbo].[models]
                EXEC sp_execute_external_script
                    @language = N'R',
                    @script = N'SELECT 1';
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData(
            "INSERT INTO dbo.T (WAITFOR) VALUES (1);",
            "INSERT column lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "DELETE WAITFOR FROM dbo.T AS t;",
            "DELETE targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "DELETE FROM dbo.T OUTPUT deleted.Id INTO @DeletedRows (WAITFOR);",
            "OUTPUT INTO column lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "DELETE FROM OPENROWSET('SQLOLEDB', 'Server=(local);Trusted_Connection=yes;', 'SELECT CityId FROM dbo.City') WHERE CURRENT OF WAITFOR;",
            "CURRENT OF cursor names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "DELETE FROM OPENROWSET('SQLOLEDB', 'Server=(local);Trusted_Connection=yes;', 'SELECT CityId FROM dbo.City') WHERE CURRENT OF GLOBAL WAITFOR;",
            "GLOBAL CURRENT OF cursor names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        public void ParseScript_ShouldRejectDmlIdentifiers_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Fact]
        public void ParseScript_ShouldParseSetIdentityInsert_WithSchemaQualifiedTableName()
        {
            const string script = """
                SET IDENTITY_INSERT dbo.MyTable ON;
                INSERT INTO dbo.MyTable (Id, Name) VALUES (1, N'Alpha');
                SET IDENTITY_INSERT dbo.MyTable OFF;

                SET IDENTITY_INSERT Nodes.StockItems ON;
                INSERT INTO Nodes.StockItems (StockItemID, StockItemName) SELECT StockItemID, StockItemName FROM Warehouse.StockItems;
                SET IDENTITY_INSERT Nodes.StockItems OFF;

                SET IDENTITY_INSERT SimpleTable ON;
                INSERT INTO SimpleTable (Id) VALUES (42);
                SET IDENTITY_INSERT SimpleTable OFF;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseMultiRowValues_InInsertAndTableConstructor()
        {
            const string script = """
                INSERT INTO dbo.Colors (Name, Hex) VALUES
                    (N'Red',   N'#FF0000'),
                    (N'Green', N'#00FF00'),
                    (N'Blue',  N'#0000FF');

                INSERT dbo.Numbers VALUES (1), (2), (3), (4), (5);

                SELECT TOP(1) Quantity
                FROM (VALUES (0), (0), (0), (1)) AS q(Quantity)
                ORDER BY NEWID();

                WITH a AS (SELECT * FROM (VALUES (1),(2),(3)) AS a(a))
                SELECT a.a FROM a;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseSelectIntoTempTable_WithoutFromClause()
        {
            const string script = """
                SELECT 1 INTO #t;
                SELECT (1) INTO #u WHERE 1 = 1;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseStatementSpecificWithClauses()
        {
            const string script = """
                BULK INSERT dbo.T
                FROM 'x.csv'
                WITH (
                    FORMAT = 'CSV',
                    FIRSTROW = 2,
                    FIELDTERMINATOR = ',',
                    ROWTERMINATOR = '0x0a',
                    TABLOCK
                );

                CREATE EXTERNAL TABLE dbo.ExtCompany
                (
                    Id INT,
                    Name NVARCHAR(100)
                )
                WITH (
                    LOCATION = '/company/',
                    DATA_SOURCE = CompanyDataSource,
                    FILE_FORMAT = CompanyFileFormat
                );
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }
    }
}