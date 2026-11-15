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
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = ALL;")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REORGANIZE PARTITION = ALL;")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = 1;")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REORGANIZE PARTITION = 2;")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = ALL WITH (MAXDOP = 2);")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = @partition;")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = [ALL];")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REORGANIZE PARTITION = (1);")]
        public void ParseScript_Stage9_ShouldPreserveAlterIndexPartitionForms(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            var selector = Stage9Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "AlterIndexPartitionSelector")
                .Should().ContainSingle().Subject;
            Stage9Tokens(selector).Should().NotBeEmpty();
        }

        [Fact]
        public void ParseScript_Stage9_ShouldKeepPartitionSelectorsAttachedToTheirStatements()
        {
            const string sql =
                "ALTER INDEX IX_First ON dbo.T REBUILD PARTITION = ALL; " +
                "ALTER INDEX IX_Second ON dbo.T REORGANIZE PARTITION = 2;";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var statements = Stage9Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "AlterIndexStatement").ToArray();
            statements.Should().HaveCount(2);
            Stage9Tokens(statements[0]).Should().Contain("IX_First").And.NotContain("IX_Second");
            Stage9Tokens(statements[1]).Should().Contain("IX_Second").And.NotContain("IX_First");

            var first = Stage9Nodes(statements[0]).Where(node =>
                node.NonTerminal.Name == "AlterIndexPartitionSelector").Should().ContainSingle().Subject;
            var second = Stage9Nodes(statements[1]).Where(node =>
                node.NonTerminal.Name == "AlterIndexPartitionSelector").Should().ContainSingle().Subject;
            Stage9Tokens(first).Should().Equal("ALL");
            Stage9Tokens(second).Should().Equal("2");
        }

        [Theory]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = ;")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REORGANIZE PARTITION = ;")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = ALL WITH ();")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = ALL,;")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = (1;")]
        public void ParseScript_Stage9_ShouldRejectMalformedPartitionSelectors(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage9_ShouldRecordOriginalPartitionAcceptanceMatrix()
        {
            var contexts = new (string Name, string Template)[]
            {
                ("rebuild", "ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = {0};"),
                ("reorganize", "ALTER INDEX IX_T ON dbo.T REORGANIZE PARTITION = {0};"),
                ("rebuild-with", "ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = {0} WITH (MAXDOP = 2);"),
                ("reorganize-with", "ALTER INDEX IX_T ON dbo.T REORGANIZE PARTITION = {0} WITH (COMPRESS_ALL_ROW_GROUPS = ON);")
            };
            var values = new[]
            {
                "ALL", "all", "[ALL]", "\"ALL\"", "1", "(1)", "1 + 2",
                "@partition", "dbo.ALL", "NULL", "DEFAULT", "ON", "ALL.ALL"
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 9 original ALTER INDEX PARTITION selector acceptance");
            report.AppendLine();
            report.AppendLine("| Context | Selector | Accepted |");
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
                File.WriteAllText(Path.Combine(directory, "sql-alter-index-partition-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| rebuild | ALL |");
        }

        private static IEnumerable<NonTerminalNode> Stage9Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var item in Stage9Nodes(child))
                    yield return item;
        }

        private static IEnumerable<string> Stage9Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage9Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage9_ShouldPreserveAlterIndexPartitionAndStability(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = ALL;",
                "ALTER INDEX IX_T ON dbo.T REORGANIZE PARTITION = 2;",
                "ALTER INDEX IX_T ON dbo.T REBUILD PARTITION = ALL WITH (MAXDOP = 2);"
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
                Stage9FormatterTokens(reparsed.ParseTree!)
                    .Should().Equal(Stage9FormatterTokens(original.ParseTree!),
                        (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var reformatted = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                reformatted.IsSuccess.Should().BeTrue(reformatted.ErrorMessage);
                reformatted.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage9FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage9FormatterTokens(child))
                    yield return token;
        }
    }
}
