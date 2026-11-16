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
        [InlineData("DECLARE @payload VARBINARY(MAX);")]
        [InlineData("DECLARE @text NVARCHAR(MAX);")]
        [InlineData("DECLARE @name VARCHAR(MAX);")]
        [InlineData("DECLARE @bytes VARBINARY(128);")]
        [InlineData("DECLARE @amount DECIMAL(18, 4);")]
        [InlineData("CREATE TABLE dbo.T (Payload NVARCHAR(MAX));")]
        [InlineData("DECLARE @text NVARCHAR(MAX) = N'hello';")]
        [InlineData("CREATE FUNCTION dbo.ufn_Echo(@value NVARCHAR(MAX)) RETURNS NVARCHAR(MAX) AS BEGIN RETURN @value; END;")]
        public void ParseScript_Stage19_ShouldPreserveMaxAndNumericTypeArguments(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            Stage19Nodes(parsed.ParseTree!).Should().Contain(
                node => node.NonTerminal.Name == "TypeArgument");
        }

        [Fact]
        public void ParseScript_Stage19_ShouldKeepTypeArgumentsInTheirOwnDeclareStatements()
        {
            const string sql =
                "DECLARE @first NVARCHAR(MAX); " +
                "DECLARE @second DECIMAL(18, 4);";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var declares = Stage19Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "DeclareStatement").ToArray();
            declares.Should().HaveCount(2);
            Stage19Nodes(declares[0]).Count(node => node.NonTerminal.Name == "TypeArgument")
                .Should().Be(1);
            Stage19Nodes(declares[1]).Count(node => node.NonTerminal.Name == "TypeArgument")
                .Should().Be(2);
            Stage19Tokens(declares[0]).Should().Contain("@first").And.NotContain("@second");
            Stage19Tokens(declares[1]).Should().Contain("@second").And.NotContain("@first");
        }

        [Theory]
        [InlineData("DECLARE @v NVARCHAR();")]
        [InlineData("DECLARE @v NVARCHAR(,);")]
        [InlineData("DECLARE @v NVARCHAR(MAX,);")]
        [InlineData("DECLARE @v NVARCHAR(,MAX);")]
        [InlineData("DECLARE @v NVARCHAR(MAX MAX);")]
        [InlineData("DECLARE @v NVARCHAR(MAX,MAX,MAX);")]
        [InlineData("DECLARE @v NVARCHAR(MAX;")]
        public void ParseScript_Stage19_ShouldRejectMalformedTypeArgumentLists(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage19_ShouldRecordOriginalTypeArgumentAcceptanceMatrix()
        {
            var contexts = new (string Name, string Prefix, string Suffix)[]
            {
                ("declare-varchar", "DECLARE @v VARCHAR(", ");"),
                ("declare-nvarchar", "DECLARE @v NVARCHAR(", ");"),
                ("declare-varbinary", "DECLARE @v VARBINARY(", ");"),
                ("declare-decimal", "DECLARE @v DECIMAL(", ");"),
                ("create-table", "CREATE TABLE dbo.T (Payload NVARCHAR(", "));"),
                ("function-parameter", "CREATE FUNCTION dbo.ufn_X(@value NVARCHAR(", ")) RETURNS INT AS BEGIN RETURN 1; END;")
            };
            var values = new[]
            {
                "MAX", "max", "[MAX]", "\"MAX\"",
                "10", "18, 4", "MAX, 2", "2, MAX", "MAX, MAX",
                "1 + 1", "(MAX)", "@size", "dbo.MAX",
                "MAX + 1", "NULL", "DEFAULT", "",
                "MAX,", ", MAX", "MAX MAX", "10, 2, 3"
            };

            var report = new StringBuilder();
            report.AppendLine("# Stage 19 TypeArgument MAX acceptance and rejection matrix");
            report.AppendLine();
            report.AppendLine("| Context | Arguments | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (name, prefix, suffix) in contexts)
                foreach (var value in values)
                {
                    var accepted = ModernMsSqlGrammarExample.ParseBatch(prefix + value + suffix).IsSuccess;
                    report.AppendLine($"| {name} | {value} | {(accepted ? "yes" : "no")} |");
                }

            var reportPath = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(reportPath))!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sql-type-argument-max-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| declare-nvarchar | MAX |");
        }

        private static IEnumerable<NonTerminalNode> Stage19Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var nested in Stage19Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<string> Stage19Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage19Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage19_ShouldPreserveTypeArgumentsAndIdempotence(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "DECLARE @text NVARCHAR(MAX);",
                "DECLARE @amount DECIMAL(18, 4);",
                "DECLARE @payload VARBINARY(MAX);"
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
                Stage19FormatterTokens(reparsed.ParseTree!).Should().Equal(
                    Stage19FormatterTokens(original.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var again = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                again.IsSuccess.Should().BeTrue(again.ErrorMessage);
                again.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage19FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage19FormatterTokens(child))
                    yield return token;
        }
    }
}
