using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.Parser;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class MsSqlModernGrammarExampleTests
    {
        [Theory]
        [MemberData(nameof(ValidSqlScripts))]
        public void ParseScript_ShouldParseModernMsSqlExamples(string scriptName, string scriptText)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseScript(scriptText);

            parseResult.IsSuccess.Should().BeTrue(
                $"script '{scriptName}' should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateDatabase_WithPopularOptions()
        {
            const string script = """
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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateDatabase_WithMultipleFilespecsInSingleList()
        {
            const string script = """
                CREATE DATABASE SalesArchive
                ON
                (
                    NAME = SalesArchiveData1,
                    FILENAME = 'C:\data\archive1.mdf'
                ),
                (
                    NAME = SalesArchiveData2,
                    FILENAME = 'C:\data\archive2.ndf'
                );
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateTable_AsFileTable()
        {
            const string script = """
                CREATE TABLE dbo.Documents AS FILETABLE
                WITH
                (
                    FILETABLE_DIRECTORY = 'Documents',
                    FILETABLE_COLLATE_FILENAME = database_default
                );
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateRole_WithAuthorization()
        {
            const string script = "CREATE ROLE [Plains Sales] AUTHORIZATION [dbo];";

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseUseStatement()
        {
            const string script = """
                USE [Clinic];
                GO
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateSchema_BasicAndAuthorization()
        {
            const string script = """
                CREATE SCHEMA ext;
                GO
                CREATE SCHEMA [sales] AUTHORIZATION [dbo];
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectInlineGoBatchSeparator()
        {
            const string script = "USE [Clinic]; GO";

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeFalse("GO must be recognized only as a dedicated-line batch separator.");
        }

        [Fact]
        public void ParseScript_ShouldParseGoBatchRepeat_OnDedicatedLine()
        {
            const string script = """
                SELECT 1;
                GO 5
                SELECT 2;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldTreatGoAsIdentifier_InsideBatch()
        {
            const string script = "SELECT GO AS GoToken;";

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData("SELECT 1 WHERE 1;")]
        [InlineData("SELECT 1 WHERE SomeColumn IN OtherColumn;")]
        [InlineData("SELECT 1 WHERE SomeColumn IS 5;")]
        [InlineData("SELECT 1 WHERE SomeColumn IS OtherColumn + 1;")]
        public void ParseScript_ShouldRejectScalarExpressionsAsSearchConditions(string script)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeFalse(
                $"script '{script}' should not parse because WHERE requires a predicate.");
        }

        [Theory]
        [InlineData("SELECT 1 WHERE SomeColumn IN (1, 2, 3);")]
        [InlineData("SELECT 1 WHERE SomeColumn IN (SELECT OtherColumn FROM dbo.OtherTable);")]
        [InlineData("SELECT 1 WHERE SomeColumn IS NULL;")]
        [InlineData("SELECT 1 WHERE SomeColumn IS NOT NULL;")]
        public void ParseScript_ShouldParseStructuredSearchPredicates(string script)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script '{script}' should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseGroupBy_WithRollupAndCube()
        {
            const string script = """
                SELECT a
                FROM dbo.T
                GROUP BY a WITH ROLLUP;

                SELECT a, b
                FROM dbo.T
                GROUP BY a, b WITH CUBE;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseBitwiseOrAndXorExpressions()
        {
            const string script = """
                SELECT a | b AS BitwiseOrResult
                FROM dbo.Flags;

                SELECT a ^ b AS BitwiseXorResult
                FROM dbo.Flags;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseDropStatements_TableViewIndexStatistics()
        {
            const string script = """
                DROP TABLE IF EXISTS dbo.TempA, [dbo].[TempB];
                DROP VIEW IF EXISTS dbo.V1, [dbo].[V2];
                DROP INDEX IF EXISTS IX_OrderDate ON dbo.Orders;
                DROP INDEX IF EXISTS dbo.Orders.IX_Old;
                DROP INDEX IF EXISTS IX_Clustered ON dbo.Orders WITH (MAXDOP = 2, ONLINE = ON, MOVE TO [PRIMARY], FILESTREAM_ON [PRIMARY]);
                DROP STATISTICS dbo.Orders.Stat_OrderDate, [dbo].[Orders].[_WA_Sys_00000001];
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseGrantStatement_Variants()
        {
            const string script = """
                GRANT SELECT, UPDATE (Email, Phone), VIEW DEFINITION ON OBJECT::dbo.Company TO [app_role], PUBLIC WITH GRANT OPTION AS dbo;
                GRANT ALL PRIVILEGES TO [report_user];
                GRANT CONNECT ON DATABASE::[Clinic] TO [reader];
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseDbccStatement_Variants()
        {
            const string script = """
                DBCC CHECKDB (0, NOINDEX) WITH NO_INFOMSGS, ALL_ERRORMSGS, MAXDOP = 2;
                DBCC DROPCLEANBUFFERS;
                DBCC TRACESTATUS (0) WITH NO_INFOMSGS;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseOptionClause_CommonHints()
        {
            const string script = """
                SELECT 1
                OPTION (
                    RECOMPILE,
                    MAXDOP 1,
                    QUERYTRACEON 9481,
                    MAXRECURSION 25,
                    MIN_GRANT_PERCENT = 20,
                    LABEL = 'unit-test',
                    USE HINT('DISALLOW_BATCH_MODE'),
                    LOOP JOIN,
                    IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX
                );
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseAlterDatabase_CommonSetForms()
        {
            const string script = """
                ALTER DATABASE CURRENT SET COMPATIBILITY_LEVEL = 130;
                ALTER DATABASE CURRENT SET MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT = ON;
                ALTER DATABASE CURRENT SET QUERY_STORE CLEAR ALL;
                ALTER DATABASE [AdventureWorks] SET AUTO_UPDATE_STATISTICS OFF WITH NO_WAIT;
                ALTER DATABASE [AdventureWorks] SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;
                ALTER DATABASE [AdventureWorks] SET CURSOR_DEFAULT GLOBAL;
                ALTER DATABASE [AdventureWorks] SET PAGE_VERIFY CHECKSUM;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateAlterTableAndIndex_Variants()
        {
            const string script = """
                CREATE TABLE dbo.Company
                (
                    CompanyId INT NOT NULL PRIMARY KEY,
                    Email NVARCHAR(320) NULL,
                    Phone NVARCHAR(32) NULL,
                    Price DECIMAL(10,2) NOT NULL DEFAULT (0),
                    ValidFrom DATETIME2(0) NOT NULL,
                    ValidTo DATETIME2(0) NOT NULL,
                    CONSTRAINT CK_Company_Price CHECK (Price >= 0),
                    INDEX IX_Company_Email NONCLUSTERED (Email) WITH (FILLFACTOR = 90)
                ) WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);

                ALTER TABLE Company
                ALTER COLUMN Email ADD MASKED WITH (FUNCTION = 'email()');

                ALTER TABLE Company
                ALTER COLUMN Phone ADD MASKED WITH (FUNCTION = 'partial(5,"XXXXXXX",2)');

                ALTER TABLE Company
                ALTER COLUMN Price ADD ENCRYPTED WITH (
                    COLUMN_ENCRYPTION_KEY = CEK_Company,
                    ENCRYPTION_TYPE = DETERMINISTIC,
                    ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256'
                );

                ALTER TABLE Company
                ADD CONSTRAINT CK_Company_Phone CHECK (Phone IS NOT NULL);

                CREATE UNIQUE NONCLUSTERED INDEX IX_Company_Email2
                ON dbo.Company (Email ASC, CompanyId DESC)
                INCLUDE (Price, ValidFrom)
                WHERE CompanyId > 0
                WITH (
                    PAD_INDEX = OFF,
                    FILLFACTOR = 90,
                    ONLINE = ON (WAIT_AT_LOW_PRIORITY (MAX_DURATION = 5 MINUTES, ABORT_AFTER_WAIT = BLOCKERS)),
                    DATA_COMPRESSION = PAGE ON PARTITIONS (1 TO 3),
                    XML_COMPRESSION = ON ON PARTITIONS (4)
                )
                ON [PRIMARY]
                FILESTREAM_ON [PRIMARY];

                ALTER INDEX ALL ON dbo.Company REBUILD;
                ALTER INDEX IX_Company_Email2 ON dbo.Company REORGANIZE WITH (COMPRESS_ALL_ROW_GROUPS = ON);
                ALTER INDEX IX_Company_Email2 ON dbo.Company SET (COMPRESSION_DELAY = 30, OPTIMIZE_FOR_SEQUENTIAL_KEY = ON);
                ALTER INDEX IX_Company_Email2 ON dbo.Company RESUME WITH (MAXDOP = 2, MAX_DURATION = 60 MINUTES);
                ALTER INDEX IX_Company_Email2 ON dbo.Company PAUSE;
                ALTER INDEX IX_Company_Email2 ON dbo.Company ABORT;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseRaiserrorAndWaitforStatements()
        {
            const string script = """
                RAISERROR (N'busy', 16, 1) WITH NOWAIT, SETERROR;
                WAITFOR DELAY '00:00:01';
                WAITFOR TIME '23:59:00';
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateTable_WithFilestreamAndGeneratedAlwaysColumns()
        {
            const string script = """
                CREATE TABLE dbo.DocumentTimeline
                (
                    RowGuid UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL UNIQUE,
                    Payload VARBINARY(MAX) FILESTREAM NULL,
                    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
                    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL
                )
                PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateLogin_Variants()
        {
            const string script = """
                CREATE LOGIN app_sql WITH PASSWORD = N'P@ssw0rd!', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = master, DEFAULT_LANGUAGE = us_english;
                CREATE LOGIN [domain\service-account] FROM WINDOWS WITH DEFAULT_DATABASE = master, DEFAULT_LANGUAGE = us_english;
                CREATE LOGIN entra_user FROM EXTERNAL PROVIDER WITH OBJECT_ID = '11111111-1111-1111-1111-111111111111';
                CREATE LOGIN cert_login FROM CERTIFICATE certAuth;
                CREATE LOGIN asym_login FROM ASYMMETRIC KEY asymAuth;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseOpenRowsetBulk_Variants()
        {
            const string script = """
                SELECT src.BulkColumn
                FROM OPENROWSET(
                    BULK 'C:\temp\data.csv',
                    FORMAT = 'CSV',
                    FIRSTROW = 2,
                    FIELDTERMINATOR = ',',
                    ROWTERMINATOR = '0x0a'
                ) AS src;

                SELECT bin.BulkColumn
                FROM OPENROWSET(BULK 'C:\temp\data.bin', SINGLE_BLOB) AS bin;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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
                ("CREATE FUNCTION dbo.f(@x int) RETURNS int AS BEGIN SELECT 1; END;", "scalar functions must end with RETURN <expression>"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS int AS BEGIN RETURN; END;", "scalar functions must not use bare RETURN"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS @t TABLE (Id int) AS BEGIN INSERT INTO @t VALUES (1); END;", "multi-statement table-valued functions must end with RETURN"),
                ("CREATE FUNCTION dbo.f(@x int) RETURNS @t TABLE (Id int) AS BEGIN RETURN 1; END;", "multi-statement table-valued functions must use bare RETURN")
            };

            foreach (var (script, reason) in invalidScripts)
            {
                var parseResult = ModernMsSqlGrammarExample.ParseScript(script);
                parseResult.IsSuccess.Should().BeFalse(reason);
            }
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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseSqlcmdVariables_AsIdentifiersAndExpressions()
        {
            const string script = """
                IF EXISTS (SELECT [name] FROM [master].[sys].[databases] WHERE [name] = N'$(DatabaseName)')
                    DROP DATABASE $(DatabaseName);
                CREATE DATABASE $(DatabaseName);
                USE $(DatabaseName);
                SELECT $(SQLCMDSERVER) AS ServerName, $(SQLCMDDBNAME) AS DbName;
                IF NOT EXISTS (SELECT 1 FROM dbo.Info) INSERT dbo.Info VALUES ($(DefaultDataPath));
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseSqlcmdPreprocessorCommands()
        {
            const string script = """
                :r .\setup.sql
                :setvar JobOwner sa
                :on error exit
                PRINT N'after sqlcmd preprocessor commands';
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectInlineSqlcmdPreprocessorCommand()
        {
            const string script = "PRINT N'before'; :setvar JobOwner sa";

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeFalse("SQLCMD commands must be recognized only as dedicated-line control commands.");
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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseSystemFunctionWithDoubleColonPrefix()
        {
            const string script = """
                IF EXISTS (SELECT * FROM ::fn_listextendedproperty('SnapshotFolder', 'user', 'dbo', 'table', 'UIProperties', null, null))
                    SELECT 1;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseSqlGraphShortestPathPatterns()
        {
            const string script = """
                SELECT *
                FROM PRODUCT P1, PRODUCT P2, ISPARTOF IPO
                WHERE MATCH(P1-(IPO)->P2);

                SELECT
                    STRING_AGG(P2.Name,'->') WITHIN GROUP (GRAPH PATH) AS [Assembly],
                    COUNT(P2.ProductID) WITHIN GROUP (GRAPH PATH) AS Levels
                FROM PRODUCT P1, PRODUCT FOR PATH P2, ISPARTOF FOR PATH IPO
                WHERE MATCH(SHORTEST_PATH(P1(-(IPO)->P2)+))
                  AND P1.ProductID = 2;

                SELECT
                    LAST_VALUE(P2.ProductID) WITHIN GROUP (GRAPH PATH) AS FinalProductID
                FROM PRODUCT P1, PRODUCT FOR PATH P2, ISPARTOF FOR PATH IPO
                WHERE MATCH(SHORTEST_PATH(P1(-(IPO)->P2){1,3}))
                  AND P1.ProductID = 2;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseTemporalTable_ForSystemTimeClauses()
        {
            const string script = """
                SELECT ProductID, Name, Price
                FROM Product FOR SYSTEM_TIME AS OF '2015-07-28 13:20:00';

                SELECT ProductID, Name, Price
                FROM Product FOR SYSTEM_TIME ALL
                WHERE ProductID = 17
                ORDER BY DateModified DESC;

                SELECT ProductID, Name, Price
                FROM Product FOR SYSTEM_TIME BETWEEN '2015-01-01' AND '2016-01-01';

                SELECT ProductID, Name, Price
                FROM Product FOR SYSTEM_TIME FROM '2015-01-01' TO '2016-01-01';

                SELECT ProductID, Name, Price
                FROM Product FOR SYSTEM_TIME AS OF @date AS p;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldKeepTriviaInsideSplitMultiKeywordConstructs()
        {
            const string script = """
                CREATE VIEW dbo.vTrivia AS
                SELECT 1 AS A
                WITH /*check-before*/ CHECK /*option-before*/ OPTION;

                SELECT ProductID
                FROM Product FOR /*system-time-before*/ SYSTEM_TIME AS OF '2015-07-28 13:20:00';

                SELECT 1
                FROM Product FOR /*path-before*/ PATH p;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var terminalNodes = GetTerminalNodes(parseResult.ParseTree!);

            terminalNodes.Single(node => string.Equals(node.Token.OriginalString, "CHECK", StringComparison.OrdinalIgnoreCase))
                .Token.Trivia.LeadingTrivia
                .Select(token => token.OriginalString)
                .Should()
                .Contain("/*check-before*/");

            terminalNodes.Single(node => string.Equals(node.Token.OriginalString, "OPTION", StringComparison.OrdinalIgnoreCase))
                .Token.Trivia.LeadingTrivia
                .Select(token => token.OriginalString)
                .Should()
                .Contain("/*option-before*/");

            terminalNodes.Single(node => string.Equals(node.Token.OriginalString, "SYSTEM_TIME", StringComparison.OrdinalIgnoreCase))
                .Token.Trivia.LeadingTrivia
                .Select(token => token.OriginalString)
                .Should()
                .Contain("/*system-time-before*/");

            terminalNodes.Single(node => string.Equals(node.Token.OriginalString, "PATH", StringComparison.OrdinalIgnoreCase))
                .Token.Trivia.LeadingTrivia
                .Select(token => token.OriginalString)
                .Should()
                .Contain("/*path-before*/");
        }

        [Fact]
        public void ParseScript_ShouldParseOpenJsonWithClause()
        {
            const string script = """
                SELECT j.RegionId, j.RegionMeta
                FROM OPENJSON(@json)
                WITH
                (
                    RegionId INT '$.id',
                    RegionMeta NVARCHAR(MAX) N'$.meta' AS JSON
                ) AS j;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectOpenJsonWithClause_OnNonOpenJsonFunction()
        {
            const string script = """
                SELECT *
                FROM dbo.SomeTvf(@x) WITH (a INT);
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeFalse("OPENJSON WITH (...) must not be accepted on arbitrary function calls.");
        }

        [Fact]
        public void ParseScript_ShouldRejectNonLiteralOpenJsonColumnPath()
        {
            const string script = """
                SELECT *
                FROM OPENJSON(@json)
                WITH
                (
                    RegionId INT @path
                );
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeFalse("OPENJSON column paths should be string literals in the strict grammar.");
        }

        [Fact]
        public void ParseScript_ShouldParseSetOperators_WithIntersectMix()
        {
            const string script = """
                SELECT 1 AS X
                UNION
                SELECT 2 AS X
                INTERSECT
                SELECT 2 AS X;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldAttachSetOperatorOrderBy_ToQueryExpression()
        {
            const string script = """
                SELECT 1 AS X
                UNION
                SELECT 2 AS X
                ORDER BY 1;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var orderByPath = FindNonTerminalPath(parseResult.ParseTree!, "OrderByClause");

            orderByPath.Should().NotBeNull();
            var orderByPathText = string.Join(" > ", orderByPath!);
            orderByPathText.Should().Contain("QueryExpression > QueryExpressionTail > QueryExpressionOrderByAndOffsetOpt > OrderByClause");
            orderByPathText.Should().NotContain("QueryPrimary");
        }

        [Fact]
        public void ParseScript_ShouldAttachSetOperatorForClause_ToQueryExpression()
        {
            const string script = """
                SELECT 1 AS X
                UNION
                SELECT 2 AS X
                FOR XML AUTO;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var forClausePath = FindNonTerminalPath(parseResult.ParseTree!, "ForClause");

            forClausePath.Should().NotBeNull();
            var forClausePathText = string.Join(" > ", forClausePath!);
            forClausePathText.Should().Contain("QueryExpression > QueryExpressionTail > QueryExpressionForOpt > ForClause");
            forClausePathText.Should().NotContain("QueryPrimary");
        }

        [Fact]
        public void ParseScript_ShouldAttachSetOperatorOption_ToQueryExpression()
        {
            const string script = """
                SELECT 1 AS X
                UNION
                SELECT 2 AS X
                OPTION (RECOMPILE);
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var optionClausePath = FindNonTerminalPath(parseResult.ParseTree!, "OptionClause");

            optionClausePath.Should().NotBeNull();
            var optionClausePathText = string.Join(" > ", optionClausePath!);
            optionClausePathText.Should().Contain("QueryExpression > QueryExpressionTail > QueryExpressionOptionOpt > OptionClause");
            optionClausePathText.Should().NotContain("QueryPrimary");
        }

        [Fact]
        public void ParseScript_ShouldParseWhere_WithLogicalAndArithmeticPrecedenceMix()
        {
            const string script = """
                SELECT *
                FROM dbo.T
                WHERE A = 1 OR B = 2 AND C + 3 * D > 10;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseIfWithElse_InsideProcBody()
        {
            // Simple IF DELETE ELSE TRUNCATE (was failing before epsilon fix, should now work)
            var r0 = ModernMsSqlGrammarExample.ParseScript(
                "IF (1=1) DELETE dbo.T ELSE TRUNCATE TABLE dbo.T");
            r0.IsSuccess.Should().BeTrue($"DELETE ELSE TRUNCATE: {r0.Error?.ErrorPosition}: {r0.Error?.Message}");

            // Pattern from 4401.sql: IF cond SET val; ELSE IF cond SET val; ELSE SET val
            var r1 = ModernMsSqlGrammarExample.ParseScript(
                "IF @x < 500 SET @c = 'A'; ELSE IF @x < 1000 SET @c = 'B'; ELSE SET @c = 'C'");
            r1.IsSuccess.Should().BeTrue($"IF SET; ELSE IF: {r1.Error?.ErrorPosition}: {r1.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseAggregateFunction_WithDistinctModifier()
        {
            const string script = """
                SELECT
                    COUNT(DISTINCT ProductKey)         AS DistinctProducts,
                    COUNT(DISTINCT CustomerKey)        AS DistinctCustomers,
                    COUNT(DISTINCT query_hash)         AS DistinctQueries,
                    AVG(DISTINCT CONVERT(BIGINT, qty)) AS AvgDistinct,
                    SUM(ALL Price)                     AS TotalPrice
                FROM dbo.Sales;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseSetOptions_WithContextualKeywords()
        {
            const string script = """
                SET LANGUAGE us_english;
                SET ROWCOUNT 10;
                SET STATISTICS IO ON;
                SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        public static IEnumerable<object[]> ValidSqlScripts()
        {
            var scriptsRoot = ResolveScriptsRoot();
            foreach (var filePath in Directory.EnumerateFiles(scriptsRoot, "*.sql", SearchOption.AllDirectories).OrderBy(i => i))
            {
                var scriptName = Path.GetRelativePath(scriptsRoot, filePath);
                var scriptText = File.ReadAllText(filePath);
                yield return new object[] { scriptName, scriptText };
            }
        }

        [Fact]
        public void ParseScript_ShouldRejectMemoryOptimizedInlineIndexWithoutDedicatedContext()
        {
            var parseResult = ModernMsSqlGrammarExample.ParseScript(
                "CREATE TABLE dbo.T ([RowID] bigint NOT NULL\nINDEX [IX] NONCLUSTERED HASH ([RowID]) WITH (BUCKET_COUNT=100000)) WITH (MEMORY_OPTIMIZED=ON)");
            parseResult.IsSuccess.Should().BeFalse("no-comma inline INDEX remains disabled until there is a dedicated memory-optimized CREATE TABLE branch.");
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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseXmlMethods_OnColumnReferences()
        {
            const string script = """
                SELECT XmlCol.value('(/x)[1]', 'int')
                FROM dbo.T;

                SELECT t.XmlCol.query('/root')
                FROM dbo.T AS t;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseDerivedTableUnpivot()
        {
            const string script = """
                SELECT src.EmployeeId, src.QuarterName, src.SalesAmount
                FROM
                (
                    SELECT EmployeeId, Q1, Q2, Q3, Q4
                    FROM dbo.SalesByQuarter
                ) AS d
                UNPIVOT
                (
                    SalesAmount FOR QuarterName IN (Q1, Q2, Q3, Q4)
                ) AS src;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseBaseTablePivot()
        {
            const string script = """
                SELECT p.[2024]
                FROM dbo.Sales AS s
                PIVOT
                (
                    SUM(Amount) FOR Yr IN ([2024])
                ) AS p;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectOverAcceptedRescueForms()
        {
            var invalidScripts = new (string Script, string Reason)[]
            {
                ("CREATE PROCEDURE dbo.p(@x int) AS SELECT 1", "CREATE PROCEDURE must not accept outer parentheses around the full parameter list"),
                ("CREATE PROCEDURE dbo.p AS EXTERNAL Foo A.B.C", "CLR procedures require EXTERNAL NAME"),
                ("CREATE INDEX IX_T ON dbo.T", "rowstore CREATE INDEX requires a key list"),
                ("CREATE TABLE dbo.T (ID int, PRIMARY KEY)", "table-level PRIMARY KEY requires a column list"),
                ("CREATE TABLE dbo.T (ID int, CONSTRAINT FK_X FOREIGN KEY REFERENCES dbo.S (SID))", "table-level FOREIGN KEY requires a local column list"),
                ("CREATE TABLE dbo.T (ID int INDEX IX_T (ID))", "inline INDEX without a comma must not be accepted in the generic CREATE TABLE path"),
                ("CREATE TABLE dbo.T (ID int GRAPH NODE)", "CREATE TABLE column options must not accept arbitrary keyword soup"),
                ("CREATE TABLE dbo.T (ID int) WITH (GRAPH = NODE)", "CREATE TABLE WITH options must not accept arbitrary keyword soup"),
                ("CREATE TABLE dbo.T (ID int Foo Bar)", "CREATE TABLE column options must not accept arbitrary identifier pairs"),
                ("CREATE TABLE dbo.T (ID int Foo(1))", "CREATE TABLE column options must not accept arbitrary identifier-call patterns"),
                ("CREATE TABLE dbo.T (ID int WITH (FILLFACTOR = 90))", "column definitions must not accept generic WITH(index options)"),
                ("CREATE TABLE dbo.T (ID int) WITH (Foo = Bar)", "CREATE TABLE WITH options must not accept arbitrary identifiers"),
                ("ALTER TABLE dbo.T ALTER COLUMN Email ADD MASKED WITH (ONLINE = ON)", "MASKED WITH must not accept index options"),
                ("ALTER TABLE dbo.T ALTER COLUMN Email ADD ENCRYPTED WITH (ONLINE = ON)", "ENCRYPTED WITH must not accept index options"),
                ("SET GRAPH NODE ON", "SET should not accept arbitrary keyword soup"),
                ("ALTER DATABASE Sales SET GRAPH NODE", "ALTER DATABASE SET should not accept arbitrary keyword soup"),
                ("ALTER DATABASE Sales SET Foo Bar", "ALTER DATABASE SET should not accept arbitrary option names"),
                ("GRANT GRAPH NODE TO [app_role]", "GRANT should not accept arbitrary keyword soup"),
                ("DBCC CHECKDB (0) WITH GRAPH = NODE", "DBCC options should not accept arbitrary keyword soup"),
                ("DBCC Banana (0)", "DBCC should not accept arbitrary command names"),
                ("SELECT 1 OPTION (GRAPH NODE)", "OPTION() should not accept arbitrary keyword soup"),
                ("SELECT 1 OPTION (Banana 1)", "OPTION() should not accept arbitrary hint names"),
                ("CREATE INDEX IX_T ON dbo.T (ID) WITH (Banana = 1)", "index WITH options must not accept arbitrary names"),
                ("CREATE LOGIN app_login WITH GRAPH = ON", "CREATE LOGIN WITH must not accept arbitrary option names"),
                ("CREATE LOGIN app_login FROM WINDOWS WITH CHECK_POLICY = ON", "Windows CREATE LOGIN must not accept SQL-only option names"),
                ("RAISERROR (N'oops', 16, 1) WITH GRAPH", "RAISERROR WITH must not accept arbitrary identifiers"),
                ("WAITFOR GRAPH 1", "WAITFOR must not accept arbitrary command names"),
                ("PRINT N'before'\n(SELECT 1);", "implicit statement boundaries must not treat parenthesized queries as keyword-led statements"),
                ("SELECT * FROM OPENROWSET(BULK 'x', GRAPH = 1) AS src", "OPENROWSET(BULK) must not accept arbitrary option names"),
                ("SELECT * FROM OPENROWSET(BULK 'x', GRAPH) AS src", "OPENROWSET(BULK) must not accept arbitrary standalone identifiers"),
                ("BULK INSERT dbo.T FROM 'x.csv' WITH (INDEX = 1, ONLINE = ON)", "BULK INSERT must not accept index options"),
                ("CREATE EXTERNAL TABLE dbo.ExtT (ID int) WITH (INDEX = 1)", "CREATE EXTERNAL TABLE must not accept table hints"),
                ("CREATE EXTERNAL DATA SOURCE MyStorage WITH (ONLINE = ON)", "CREATE EXTERNAL DATA SOURCE must not accept index options"),
                ("CREATE TABLE dbo.Documents AS FILETABLE (DocumentName NVARCHAR(260))", "FILETABLE must not accept user-defined column lists"),
                ("CREATE DATABASE Sales ON (foo = SalesData, bar = 'C:\\data\\sales.mdf')", "database filespecs require NAME and FILENAME keywords"),
                ("DECLARE c CURSOR GRAPH NODE FOR SELECT 1", "DECLARE CURSOR must not accept arbitrary identifier soup as cursor options")
            };

            foreach (var (script, reason) in invalidScripts)
            {
                var parseResult = ModernMsSqlGrammarExample.ParseScript(script);
                parseResult.IsSuccess.Should().BeFalse(reason);
            }
        }

        [Fact]
        public void ParseScript_ShouldParse1575_DiagnosticFile()
        {
            if (!TryReadSqlDatasetFile("1575.sql", out var sql1575))
            {
                return;
            }

            var parseResult = ModernMsSqlGrammarExample.ParseScript(sql1575);
            parseResult.IsSuccess.Should().BeTrue($"1575.sql failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseExternalDataSourceAnd608_DiagnosticFiles()
        {
            // 5080: CREATE EXTERNAL DATA SOURCE with WITH clause
            var r5080a = ModernMsSqlGrammarExample.ParseScript(
                "CREATE EXTERNAL DATA SOURCE MyStorage WITH (TYPE = BLOB_STORAGE, LOCATION = 'https://example.com/path')");
            r5080a.IsSuccess.Should().BeTrue($"5080a failed at {r5080a.Error?.ErrorPosition}: {r5080a.Error?.Message}");

            // 608: FREETEXTTABLE with * and LANGUAGE param
            var r608a = ModernMsSqlGrammarExample.ParseScript(
                "SELECT * FROM t INNER JOIN FREETEXTTABLE(dbo.T, *, @s, LANGUAGE @lang) AS k ON t.id = k.[KEY]");
            r608a.IsSuccess.Should().BeTrue($"608a failed at {r608a.Error?.ErrorPosition}: {r608a.Error?.Message}");

            if (!TryReadSqlDatasetFile("608.sql", out var sql608))
            {
                return;
            }

            var r608 = ModernMsSqlGrammarExample.ParseScript(sql608);
            r608.IsSuccess.Should().BeTrue($"608 failed at {r608.Error?.ErrorPosition}: {r608.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseParenthesizedScalarExpressions()
        {
            const string script = """
                SELECT (1) AS SingleValue;
                SELECT ((1)) AS NestedValue;
                SELECT CAST((1) AS INT) AS CastValue;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        private static string ResolveScriptsRoot()
        {
            var outputPath = Path.Combine(AppContext.BaseDirectory, "GrammarExamples", "TestData", "MsSql", "Valid");
            if (Directory.Exists(outputPath))
            {
                return outputPath;
            }

            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), "GrammarExamples", "TestData", "MsSql", "Valid");
            if (Directory.Exists(projectPath))
            {
                return projectPath;
            }

            throw new DirectoryNotFoundException("Could not find SQL test data folder.");
        }

        private static bool TryReadSqlDatasetFile(string fileName, out string scriptText)
        {
            scriptText = string.Empty;
            if (!TryResolveRepositoryRoot(out var repositoryRoot))
            {
                return false;
            }

            var datasetFilePath = Path.Combine(repositoryRoot, "sql-dataset", fileName);
            if (!File.Exists(datasetFilePath))
            {
                return false;
            }

            scriptText = File.ReadAllText(datasetFilePath);
            return true;
        }

        private static bool TryResolveRepositoryRoot(out string repositoryRoot)
        {
            var startingPoints = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
            foreach (var startingPoint in startingPoints)
            {
                var current = new DirectoryInfo(startingPoint);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "DSLKIT.sln")))
                    {
                        repositoryRoot = current.FullName;
                        return true;
                    }

                    current = current.Parent;
                }
            }

            repositoryRoot = string.Empty;
            return false;
        }

        private static IReadOnlyList<TerminalNode> GetTerminalNodes(ParseTreeNode rootNode)
        {
            var terminalNodes = new List<TerminalNode>();
            CollectTerminalNodes(rootNode, terminalNodes);
            return terminalNodes;
        }

        private static void CollectTerminalNodes(ParseTreeNode node, List<TerminalNode> output)
        {
            if (node is TerminalNode terminalNode)
            {
                output.Add(terminalNode);
                return;
            }

            foreach (var childNode in node.Children)
            {
                CollectTerminalNodes(childNode, output);
            }
        }

        private static IReadOnlyList<string> FindNonTerminalPath(ParseTreeNode rootNode, string nonTerminalName)
        {
            if (rootNode is not NonTerminalNode rootNonTerminalNode)
            {
                return null;
            }

            if (string.Equals(rootNonTerminalNode.NonTerminal.Name, nonTerminalName, StringComparison.Ordinal))
            {
                return [rootNonTerminalNode.NonTerminal.Name];
            }

            foreach (var childNode in rootNonTerminalNode.Children)
            {
                var childPath = FindNonTerminalPath(childNode, nonTerminalName);
                if (childPath == null)
                {
                    continue;
                }

                return [rootNonTerminalNode.NonTerminal.Name, .. childPath];
            }

            return null;
        }
    }
}
