using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
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

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData(
            """
            CREATE DATABASE Sales
            WITH TRUSTWORTHY = OFF
            COLLATE Latin1_General_100_CI_AS;
            """,
            "CREATE DATABASE clauses should not allow WITH before COLLATE.")]
        [InlineData(
            """
            CREATE DATABASE Sales
            WITH TRUSTWORTHY = OFF
            WITH LEDGER = OFF;
            """,
            "CREATE DATABASE clauses should not allow duplicate WITH sections.")]
        [InlineData(
            """
            CREATE DATABASE Sales
            COLLATE Latin1_General_100_CI_AS
            COLLATE SQL_Latin1_General_CP1_CI_AS;
            """,
            "CREATE DATABASE clauses should not allow duplicate COLLATE sections.")]
        public void ParseScript_ShouldRejectCreateDatabase_WithInvalidClauseOrderOrDuplicates(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Theory]
        [InlineData(
            "SELECT Name COLLATE PATH FROM dbo.Company;",
            "COLLATE should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE DATABASE Sales COLLATE PATH;",
            "CREATE DATABASE COLLATE should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "EXECUTE dbo.usp_DoWork WITH RESULT SETS ((Name NVARCHAR(100) COLLATE PATH));",
            "RESULT SETS column COLLATE should not accept contextual keywords through broad IdentifierTerm fallback.")]
        public void ParseScript_ShouldRejectCollate_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Theory]
        [InlineData(
            "USE PATH;",
            "USE should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE DATABASE PATH;",
            "CREATE DATABASE should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "DROP DATABASE PATH;",
            "DROP DATABASE should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "ALTER DATABASE PATH SET READ_ONLY;",
            "ALTER DATABASE database names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE ROLE PATH;",
            "CREATE ROLE should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE SCHEMA PATH;",
            "CREATE SCHEMA should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE LOGIN PATH WITH PASSWORD = N'P@ssw0rd!';",
            "CREATE LOGIN should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE USER PATH WITHOUT LOGIN;",
            "CREATE USER should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE STATISTICS PATH ON dbo.Customers (CustomerId);",
            "CREATE STATISTICS should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "DROP COLUMN ENCRYPTION KEY PATH;",
            "DROP COLUMN ENCRYPTION KEY should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "DROP EVENT SESSION PATH ON SERVER;",
            "DROP EVENT SESSION should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE TABLE dbo.T (ID int, CONSTRAINT PATH PRIMARY KEY (ID));",
            "Named table constraints should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE INDEX PATH ON dbo.T (ID);",
            "CREATE INDEX should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "DROP INDEX dbo.T.PATH;",
            "DROP INDEX should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "DROP STATISTICS dbo.T.PATH;",
            "DROP STATISTICS should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "ALTER INDEX GRAPH ON dbo.T DISABLE;",
            "ALTER INDEX should not accept contextual keywords through broad IdentifierTerm fallback.")]
        public void ParseScript_ShouldRejectObjectNames_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Theory]
        [InlineData(
            "CREATE TABLE dbo.T (WAITFOR int);",
            "CREATE TABLE column names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE TABLE dbo.T (Id int, WAITFOR AS 1);",
            "Computed column names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE TABLE dbo.T (WAITFOR XML COLUMN_SET FOR ALL_SPARSE_COLUMNS);",
            "COLUMN_SET names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE TABLE dbo.T (Id int, CONSTRAINT CK_T DEFAULT (1) FOR WAITFOR);",
            "DEFAULT ... FOR targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            CREATE TABLE dbo.T
            (
                ValidFrom DATETIME2 NOT NULL,
                ValidTo DATETIME2 NOT NULL
            )
            PERIOD FOR SYSTEM_TIME (WAITFOR, ValidTo);
            """,
            "PERIOD FOR SYSTEM_TIME column names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE TABLE dbo.T (Id int, CONSTRAINT PK_T PRIMARY KEY (WAITFOR));",
            "PRIMARY KEY column names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "ALTER TABLE dbo.T ALTER COLUMN WAITFOR INT;",
            "ALTER COLUMN targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "ALTER TABLE dbo.T DROP COLUMN WAITFOR;",
            "DROP COLUMN targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "ALTER TABLE dbo.T DROP CONSTRAINT WAITFOR;",
            "DROP CONSTRAINT targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "ALTER TABLE dbo.T CHECK CONSTRAINT WAITFOR;",
            "CHECK CONSTRAINT targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "ALTER TABLE dbo.T ENABLE TRIGGER WAITFOR;",
            "ENABLE TRIGGER targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        public void ParseScript_ShouldRejectTableDefinitionIdentifiers_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Theory]
        [InlineData(
            "CREATE TABLE dbo.T (Id int, ParentId int, CONSTRAINT FK_T FOREIGN KEY (WAITFOR) REFERENCES dbo.Parent (ParentId));",
            "FOREIGN KEY local column lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE TABLE dbo.T (Id int, ParentId int, CONSTRAINT FK_T FOREIGN KEY (ParentId) REFERENCES dbo.Parent (WAITFOR));",
            "REFERENCES column lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE TABLE dbo.T (ParentId int REFERENCES dbo.Parent (WAITFOR));",
            "Column REFERENCES lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE INDEX IX_T ON dbo.T (WAITFOR);",
            "CREATE INDEX key lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE INDEX IX_T ON dbo.T (Id) INCLUDE (WAITFOR);",
            "CREATE INDEX INCLUDE lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        public void ParseScript_ShouldRejectSchemaDdlIdentifierLists_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Theory]
        [InlineData(
            "CREATE STATISTICS Stat_T ON dbo.T (WAITFOR);",
            "CREATE STATISTICS column lists should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE INDEX IX_T ON dbo.T (Id) ON PartScheme(WAITFOR);",
            "Index storage targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE INDEX IX_T ON dbo.T (Id) FILESTREAM_ON PartScheme(WAITFOR);",
            "Index FILESTREAM storage targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "DROP INDEX IX_T ON dbo.T WITH (MOVE TO PartScheme(WAITFOR));",
            "DROP INDEX MOVE TO targets should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "ALTER DATABASE SCOPED CONFIGURATION CLEAR WAITFOR;",
            "ALTER DATABASE SCOPED CONFIGURATION names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "ALTER DATABASE SCOPED CONFIGURATION SET WAITFOR = 1;",
            "ALTER DATABASE SCOPED CONFIGURATION names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "CREATE DATABASE Sales WITH DEFAULT_LANGUAGE = WAITFOR;",
            "CREATE DATABASE option values should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            """
            CREATE DATABASE Sales
            ON
            (
                NAME = SalesData,
                FILENAME = 'C:\\data\\sales.mdf',
                SIZE = 64 WAITFOR
            );
            """,
            "CREATE DATABASE size specs should not accept arbitrary identifier units.")]
        [InlineData(
            """
            CREATE DATABASE Sales
            ON
            (
                NAME = SalesData,
                FILENAME = 'C:\\data\\sales.mdf',
                MAXSIZE = 128 WAITFOR
            );
            """,
            "CREATE DATABASE max size specs should not accept arbitrary identifier units.")]
        [InlineData(
            """
            CREATE DATABASE Sales
            ON
            (
                NAME = SalesData,
                FILENAME = 'C:\\data\\sales.mdf',
                FILEGROWTH = 10 WAITFOR
            );
            """,
            "CREATE DATABASE growth specs should not accept arbitrary identifier units.")]
        public void ParseScript_ShouldRejectDdlStorageAndOptionIdentifiers_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
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

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateRole_WithAuthorization()
        {
            const string script = "CREATE ROLE [Plains Sales] AUTHORIZATION [dbo];";

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectInlineGoBatchSeparator()
        {
            const string script = "USE [Clinic]; GO";

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldTreatGoAsIdentifier_InsideBatch()
        {
            const string script = "SELECT GO AS GoToken;";

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectGrantStatement_WithContextualKeywordColumnList()
        {
            const string script = "GRANT UPDATE (WAITFOR) ON OBJECT::dbo.Company TO [app_role];";

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(
                "GRANT column lists should not accept contextual keywords through broad IdentifierTerm fallback.");
        }

        [Fact]
        public void ParseScript_ShouldParseDbccStatement_Variants()
        {
            const string script = """
                DBCC CHECKDB (0, NOINDEX) WITH NO_INFOMSGS, ALL_ERRORMSGS, MAXDOP = 2;
                DBCC DROPCLEANBUFFERS;
                DBCC TRACESTATUS (0) WITH NO_INFOMSGS;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData(
            """
            CREATE INDEX IX_T ON dbo.T (ID)
            WITH (FILLFACTOR = 90)
            INCLUDE (Name);
            """,
            "CREATE INDEX tail clauses should not allow INCLUDE after WITH.")]
        [InlineData(
            """
            CREATE INDEX IX_T ON dbo.T (ID)
            WITH (FILLFACTOR = 90)
            WITH (ONLINE = ON);
            """,
            "CREATE INDEX tail clauses should not allow duplicate WITH sections.")]
        [InlineData(
            """
            CREATE INDEX IX_T ON dbo.T (ID)
            ON [PRIMARY]
            WHERE ID > 0;
            """,
            "CREATE INDEX tail clauses should not allow WHERE after ON.")]
        public void ParseScript_ShouldRejectCreateIndex_WithInvalidTailClauseOrderOrDuplicates(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Fact]
        public void ParseScript_ShouldParseRaiserrorAndWaitforStatements()
        {
            const string script = """
                RAISERROR (N'busy', 16, 1) WITH NOWAIT, SETERROR;
                WAITFOR DELAY '00:00:01';
                WAITFOR TIME '23:59:00';
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData("WAITFOR DELAY 1 + 1;")]
        [InlineData("WAITFOR TIME DATEADD(HOUR, 1, GETDATE());")]
        public void ParseScript_ShouldRejectWaitfor_WithArbitraryExpressions(string script)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("WAITFOR DELAY/TIME should not accept arbitrary scalar expressions.");
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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData(
            """
            CREATE TABLE dbo.T (ID INT)
            WITH (MEMORY_OPTIMIZED = ON)
            WITH (DURABILITY = SCHEMA_ONLY);
            """,
            "CREATE TABLE tail clauses should not allow duplicate WITH sections.")]
        [InlineData(
            """
            CREATE TABLE dbo.T (ID INT)
            TEXTIMAGE_ON [PRIMARY]
            WITH (MEMORY_OPTIMIZED = ON);
            """,
            "CREATE TABLE tail clauses should not allow TEXTIMAGE_ON before WITH.")]
        [InlineData(
            """
            CREATE TABLE dbo.T (ID INT)
            ON [PRIMARY]
            PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
            """,
            "CREATE TABLE tail clauses should not allow PERIOD after ON.")]
        public void ParseScript_ShouldRejectCreateTable_WithInvalidTailClauseOrderOrDuplicates(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse(reason);
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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseUpdateStatistics_WithKnownOptions()
        {
            const string script = """
                UPDATE STATISTICS dbo.Customers IX_Customers_Name
                WITH FULLSCAN, NORECOMPUTE, SAMPLE 20 PERCENT, MAXDOP = 2, AUTO_DROP = ON, ROWCOUNT = 10, PAGECOUNT = 2;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData("UPDATE STATISTICS dbo.Customers WITH GRAPH = ON;")]
        [InlineData("UPDATE STATISTICS dbo.Customers WITH Banana;")]
        [InlineData("UPDATE STATISTICS dbo.Customers WITH SAMPLE 20;")]
        public void ParseScript_ShouldRejectUpdateStatistics_WithUnknownOrMalformedOptions(string script)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("UPDATE STATISTICS WITH should only accept the known option forms.");
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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateTrigger_ForDmlAndServerEvents()
        {
            const string script = """
                CREATE TRIGGER dbo.trCustomerAudit
                ON dbo.Customers
                AFTER INSERT
                AS
                BEGIN
                    PRINT N'audit';
                END;

                CREATE TRIGGER dbo.trServerLogon
                ON ALL SERVER
                FOR LOGON
                AS
                BEGIN
                    PRINT N'logon';
                END;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectCreateTrigger_WithContextualKeywordEvent()
        {
            const string script = """
                CREATE TRIGGER dbo.trBad
                ON dbo.Customers
                AFTER PATH
                AS
                BEGIN
                    PRINT N'bad';
                END;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("trigger events should not accept unrelated contextual keywords.");
        }

        [Fact]
        public void ParseScript_ShouldParseSecurityPolicy_WithKnownOptions()
        {
            const string script = """
                CREATE SECURITY POLICY dbo.CustomerFilter
                    ADD FILTER PREDICATE dbo.fn_FilterPredicate(TenantId) ON dbo.Customers
                WITH (STATE = ON, SCHEMABINDING = OFF);
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectSecurityPolicy_WithUnknownOptionName()
        {
            const string script = """
                CREATE SECURITY POLICY dbo.CustomerFilter
                    ADD FILTER PREDICATE dbo.fn_FilterPredicate(TenantId) ON dbo.Customers
                WITH (GRAPH = ON);
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("security policy WITH options should only accept known names.");
        }

        [Fact]
        public void ParseScript_ShouldRejectMemoryOptimizedInlineIndexWithoutDedicatedContext()
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(
                "CREATE TABLE dbo.T ([RowID] bigint NOT NULL\nINDEX [IX] NONCLUSTERED HASH ([RowID]) WITH (BUCKET_COUNT=100000)) WITH (MEMORY_OPTIMIZED=ON)");
            parseResult.IsSuccess.Should().BeFalse("no-comma inline INDEX remains disabled until there is a dedicated memory-optimized CREATE TABLE branch.");
        }
    }
}