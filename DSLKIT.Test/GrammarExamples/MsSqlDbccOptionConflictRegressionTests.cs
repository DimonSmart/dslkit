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
        [InlineData("DBCC CHECKDB (0, NOINDEX) WITH NO_INFOMSGS, ALL_ERRORMSGS, MAXDOP = 2;")]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = 2;")]
        [InlineData("DBCC TRACESTATUS (0) WITH MAXDOP = 1;")]
        [InlineData("DBCC CHECKDB WITH MAXDOP = 2;")]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = ON;")]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = OFF;")]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = [option_value];")]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = '2';")]
        public void ParseScript_Stage10_ShouldPreserveDbccOptionForms(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            Stage10Nodes(parsed.ParseTree!)
                .Should().Contain(node => node.NonTerminal.Name == "DbccOptionValue");
        }

        [Fact]
        public void ParseScript_Stage10_ShouldKeepOptionValuesWithTheirDbccStatements()
        {
            const string sql =
                "DBCC CHECKDB (0) WITH NO_INFOMSGS, MAXDOP = 2; " +
                "DBCC TRACESTATUS (0) WITH MAXDOP = OFF;";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var statements = Stage10Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "DbccStatement").ToArray();
            statements.Should().HaveCount(2);
            var firstOptions = Stage10Nodes(statements[0])
                .Where(node => node.NonTerminal.Name == "DbccOptionValue").ToArray();
            var secondOptions = Stage10Nodes(statements[1])
                .Where(node => node.NonTerminal.Name == "DbccOptionValue").ToArray();
            firstOptions.Should().ContainSingle();
            secondOptions.Should().ContainSingle();
            Stage10Tokens(firstOptions[0]).Should().Equal("2");
            Stage10Tokens(secondOptions[0]).Should().Equal("OFF");
            Stage10Tokens(statements[0]).Should().Contain("CHECKDB").And.NotContain("TRACESTATUS");
            Stage10Tokens(statements[1]).Should().Contain("TRACESTATUS").And.NotContain("CHECKDB");
        }

        [Theory]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = ;")]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = , NO_INFOMSGS;")]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = ();")]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = 2,;")]
        [InlineData("DBCC CHECKDB (0) WITH GRAPH = NODE;")]
        public void ParseScript_Stage10_ShouldRejectMalformedDbccOptions(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage10_ShouldRecordOriginalDbccValueAcceptanceMatrix()
        {
            var contexts = new (string Name, string Template)[]
            {
                ("checkdb", "DBCC CHECKDB (0) WITH MAXDOP = {0};"),
                ("tracestatus", "DBCC TRACESTATUS (0) WITH MAXDOP = {0};"),
                ("unparenthesized", "DBCC CHECKDB WITH MAXDOP = {0};"),
                ("following-flag", "DBCC CHECKDB (0) WITH NO_INFOMSGS, MAXDOP = {0};"),
                ("preceding-flag", "DBCC CHECKDB (0) WITH MAXDOP = {0}, ALL_ERRORMSGS;")
            };
            var values = new[]
            {
                "1", "-2", "1 + 2", "option_value", "[option_value]", "\"option_value\"",
                "dbo.option_value", "'option_value'", "@value", "ON", "OFF", "DEFAULT", "NULL"
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 10 original DBCC option-value acceptance");
            report.AppendLine();
            report.AppendLine("| Context | Value | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (context, template) in contexts)
            {
                foreach (var value in values)
                {
                    var sql = string.Format(template, value);
                    report.AppendLine($"| {context} | {value} | {(ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess ? "yes" : "no")} |");
                }
            }

            var reportPath = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(reportPath))!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sql-dbcc-option-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| checkdb | 1 |");
        }

        private static IEnumerable<NonTerminalNode> Stage10Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var item in Stage10Nodes(child))
                    yield return item;
        }

        private static IEnumerable<string> Stage10Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage10Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage10_ShouldPreserveDbccOptionTokensAndStability(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "DBCC CHECKDB (0) WITH MAXDOP = 2;",
                "DBCC CHECKDB (0, NOINDEX) WITH NO_INFOMSGS, MAXDOP = 2;",
                "DBCC TRACESTATUS (0) WITH MAXDOP = 1;"
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
                Stage10FormatterTokens(reparsed.ParseTree!)
                    .Should().Equal(Stage10FormatterTokens(original.ParseTree!),
                        (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var reformatted = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                reformatted.IsSuccess.Should().BeTrue(reformatted.ErrorMessage);
                reformatted.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage10FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage10FormatterTokens(child))
                    yield return token;
        }
    }
}
