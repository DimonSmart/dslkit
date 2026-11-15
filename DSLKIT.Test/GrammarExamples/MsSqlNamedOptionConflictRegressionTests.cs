using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.GrammarExamples.MsSql.Formatting;
using DSLKIT.Parser;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Theory]
        [InlineData("CREATE LOGIN app_sql WITH PASSWORD = N'P@ssw0rd!', DEFAULT_DATABASE = master, DEFAULT_LANGUAGE = us_english;")]
        [InlineData("CREATE LOGIN app_sql WITH PASSWORD = N'P@ssw0rd!', CHECK_POLICY = ON, CHECK_EXPIRATION = OFF, DEFAULT_DATABASE = master;")]
        [InlineData("CREATE TABLE dbo.Documents AS FILETABLE WITH (FILETABLE_DIRECTORY = 'Documents', FILETABLE_COLLATE_FILENAME = database_default);")]
        [InlineData("ALTER TABLE Company ALTER COLUMN Price ADD ENCRYPTED WITH (COLUMN_ENCRYPTION_KEY = CEK_Company, ENCRYPTION_TYPE = DETERMINISTIC, ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256');")]
        [InlineData("BULK INSERT dbo.T FROM 'x.csv' WITH (FORMAT = 'CSV', FIRSTROW = 2, FIELDTERMINATOR = ',', ROWTERMINATOR = '0x0a', TABLOCK);")]
        [InlineData("CREATE EXTERNAL TABLE dbo.ExtCompany (Id INT, Name NVARCHAR(100)) WITH (LOCATION = '/company/', DATA_SOURCE = CompanyDataSource, FILE_FORMAT = CompanyFileFormat);")]
        [InlineData("SELECT src.BulkColumn FROM OPENROWSET(BULK 'x', FIRSTROW = 2) AS src;")]
        public void ParseScript_Stage8_ShouldPreserveNamedOptionContexts(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            Stage8Nodes(parsed.ParseTree!)
                .Should().Contain(node => node.NonTerminal.Name == "NamedOptionValue");
        }

        [Fact]
        public void ParseScript_Stage8_ShouldKeepSequentialNamedOptionsAttached()
        {
            const string sql =
                "CREATE LOGIN app_sql WITH PASSWORD = N'P@ssw0rd!', DEFAULT_DATABASE = master, DEFAULT_LANGUAGE = us_english; " +
                "BULK INSERT dbo.T FROM 'x.csv' WITH (FIRSTROW = 2, FIELDTERMINATOR = ',');";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            var options = Stage8Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "NamedOptionValue").ToArray();
            options.Should().HaveCount(4);
            var texts = options.Select(node => string.Concat(Stage8Tokens(node))).ToArray();
            texts.Should().ContainInOrder("master", "us_english", "2", "','");
        }

        [Theory]
        [InlineData("CREATE LOGIN app_sql WITH PASSWORD = N'x', DEFAULT_DATABASE = ;")]
        [InlineData("BULK INSERT dbo.T FROM 'x.csv' WITH (FIRSTROW = );")]
        [InlineData("CREATE EXTERNAL TABLE dbo.ExtT (Id INT) WITH (LOCATION = '/t/', DATA_SOURCE = , FILE_FORMAT = FileFormat);")]
        public void ParseScript_Stage8_ShouldRejectMissingNamedOptionValues(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage8_ShouldRecordOriginalNamedOptionAcceptanceMatrix()
        {
            var contexts = new (string Name, string Template)[]
            {
                ("login-default-database", "CREATE LOGIN app_sql WITH PASSWORD = N'x', DEFAULT_DATABASE = {0};"),
                ("login-default-language", "CREATE LOGIN app_sql WITH PASSWORD = N'x', DEFAULT_LANGUAGE = {0};"),
                ("filetable-collation", "CREATE TABLE dbo.Documents AS FILETABLE WITH (FILETABLE_DIRECTORY = 'Documents', FILETABLE_COLLATE_FILENAME = {0});"),
                ("bulk-firstrow", "BULK INSERT dbo.T FROM 'x.csv' WITH (FIRSTROW = {0});"),
                ("bulk-codepage", "BULK INSERT dbo.T FROM 'x.csv' WITH (CODEPAGE = {0});"),
                ("external-data-source", "CREATE EXTERNAL TABLE dbo.ExtT (Id INT) WITH (LOCATION = '/t/', DATA_SOURCE = {0}, FILE_FORMAT = FileFormat);"),
                ("encryption-key", "ALTER TABLE Company ALTER COLUMN Price ADD ENCRYPTED WITH (COLUMN_ENCRYPTION_KEY = {0}, ENCRYPTION_TYPE = DETERMINISTIC, ALGORITHM = 'AEAD_AES_256_CBC_HMAC_SHA_256');"),
                ("openrowset-firstrow", "SELECT src.BulkColumn FROM OPENROWSET(BULK 'x', FIRSTROW = {0}) AS src;")
            };
            var values = new[]
            {
                "master", "dbo.master", "[master]", "\"master\"", "'master'",
                "1", "1 + 2", "ON", "OFF", "DEFAULT"
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 8 original named-option acceptance");
            report.AppendLine();
            report.AppendLine("| Context | Value | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (name, template) in contexts)
            {
                foreach (var value in values)
                {
                    var sql = string.Format(template, value);
                    var accepted = ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess;
                    report.AppendLine($"| {name} | {value} | {(accepted ? "yes" : "no")} |");
                }
            }

            var path = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(path))
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sql-named-option-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| login-default-database | master |");
        }

        private static IEnumerable<NonTerminalNode> Stage8Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var item in Stage8Nodes(child))
                    yield return item;
        }

        private static IEnumerable<string> Stage8Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage8Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage8_ShouldPreserveNamedOptionsAndStability(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "SELECT src.BulkColumn FROM OPENROWSET(BULK 'x', FIRSTROW = 2) AS src;",
                "BULK INSERT dbo.T FROM 'x.csv' WITH (FORMAT = 'CSV', FIRSTROW = 2);"
            };
            var options = new SqlFormattingOptions
            {
                Statement = new SqlStatementFormattingOptions { TerminateWithSemicolon = mode }
            };
            foreach (var sql in cases)
            {
                var original = ModernMsSqlGrammarExample.ParseBatch(sql);
                original.IsSuccess.Should().BeTrue(original.Error?.Message);

                var formatted = ModernMsSqlFormatter.TryFormat(sql, options);
                formatted.IsSuccess.Should().BeTrue(formatted.ErrorMessage);
                formatted.FormattedSql.Should().NotBeNullOrWhiteSpace();

                var reparsed = ModernMsSqlGrammarExample.ParseBatch(formatted.FormattedSql!);
                reparsed.IsSuccess.Should().BeTrue(reparsed.Error?.Message);
                Stage8FormatterTokens(reparsed.ParseTree!)
                    .Should().Equal(Stage8FormatterTokens(original.ParseTree!),
                        (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var reformatted = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                reformatted.IsSuccess.Should().BeTrue(reformatted.ErrorMessage);
                reformatted.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage8FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage8FormatterTokens(child))
                    yield return token;
        }
    }
}
