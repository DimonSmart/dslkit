using System;
using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

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
        public void ParseScript_ShouldAttachTrailingCommentBeforeEof_ToLastSignificantToken()
        {
            const string script = "SELECT 1 -- tail";

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.ParseTree.Should().NotBeNull();

            var terminalNodes = GetTerminalNodes(parseResult.ParseTree!);
            var lastTerminal = terminalNodes.Last();

            lastTerminal.Token.OriginalString.Should().Be("1");
            lastTerminal.Token.Trivia.TrailingTrivia
                .Select(token => token.OriginalString)
                .Should()
                .Contain("-- tail");
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
                var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);
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

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(sql1575);
            parseResult.IsSuccess.Should().BeTrue($"1575.sql failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseExternalDataSourceAnd608_DiagnosticFiles()
        {
            var r5080a = ModernMsSqlGrammarExample.ParseBatch(
                "CREATE EXTERNAL DATA SOURCE MyStorage WITH (TYPE = BLOB_STORAGE, LOCATION = 'https://example.com/path')");
            r5080a.IsSuccess.Should().BeTrue($"5080a failed at {r5080a.Error?.ErrorPosition}: {r5080a.Error?.Message}");

            var r608a = ModernMsSqlGrammarExample.ParseBatch(
                "SELECT * FROM t INNER JOIN FREETEXTTABLE(dbo.T, *, @s, LANGUAGE @lang) AS k ON t.id = k.[KEY]");
            r608a.IsSuccess.Should().BeTrue($"608a failed at {r608a.Error?.ErrorPosition}: {r608a.Error?.Message}");

            if (!TryReadSqlDatasetFile("608.sql", out var sql608))
            {
                return;
            }

            var r608 = ModernMsSqlGrammarExample.ParseBatch(sql608);
            r608.IsSuccess.Should().BeTrue($"608 failed at {r608.Error?.ErrorPosition}: {r608.Error?.Message}");
        }
    }
}