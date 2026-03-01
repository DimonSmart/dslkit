using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
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
            const string script = "USE [Clinic]; GO";

            var parseResult = ModernMsSqlGrammarExample.ParseScript(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateSchema_BasicAndAuthorization()
        {
            const string script = "CREATE SCHEMA ext; GO CREATE SCHEMA [sales] AUTHORIZATION [dbo];";

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
    }
}
