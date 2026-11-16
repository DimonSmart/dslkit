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
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO [DEFAULT]);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO [PRIMARY]);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT, MAXDOP = 2);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO [PRIMARY], ONLINE = ON);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT FILESTREAM_ON [PRIMARY]);")]
        [InlineData("DROP INDEX IF EXISTS IX_T ON dbo.T WITH (MOVE TO DEFAULT);")]
        public void ParseScript_Stage16_ShouldPreserveDropIndexMoveToTargets(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            Stage16Nodes(parsed.ParseTree!).Should().Contain(node =>
                node.NonTerminal.Name == "DropMoveToTarget");
        }

        [Fact]
        public void ParseScript_Stage16_ShouldKeepMoveTargetInsideItsOwnDropStatement()
        {
            const string sql =
                "DROP INDEX IX_A ON dbo.T WITH (MOVE TO DEFAULT, MAXDOP = 2); " +
                "DROP INDEX IX_B ON dbo.T WITH (MOVE TO [PRIMARY]);";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var drops = Stage16Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "DropIndexStatement").ToArray();
            drops.Should().HaveCount(2);
            Stage16Tokens(drops[0]).Should().Contain("IX_A").And.NotContain("IX_B");
            Stage16Tokens(drops[1]).Should().Contain("IX_B").And.NotContain("IX_A");
            Stage16Nodes(drops[0]).Count(node => node.NonTerminal.Name == "DropMoveToTarget")
                .Should().Be(1);
            Stage16Nodes(drops[1]).Count(node => node.NonTerminal.Name == "DropMoveToTarget")
                .Should().Be(1);
        }

        [Theory]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO , MAXDOP = 2);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT,);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT DEFAULT);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT FILESTREAM_ON);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT FILESTREAM_ON , MAXDOP = 2);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT ());")]
        public void ParseScript_Stage16_ShouldRejectMalformedMoveOptions(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage16_ShouldRecordOriginalMoveTargetAcceptanceMatrix()
        {
            var contexts = new (string Name, string Prefix, string Suffix)[]
            {
                ("simple", "DROP INDEX IX_T ON dbo.T WITH (MOVE TO ", ");"),
                ("followed-by-maxdop", "DROP INDEX IX_T ON dbo.T WITH (MOVE TO ", ", MAXDOP = 2);"),
                ("filestream", "DROP INDEX IX_T ON dbo.T WITH (MOVE TO ", " FILESTREAM_ON [PRIMARY]);"),
                ("filestream-default", "DROP INDEX IX_T ON dbo.T WITH (MOVE TO ", " FILESTREAM_ON DEFAULT);"),
                ("if-exists", "DROP INDEX IF EXISTS IX_T ON dbo.T WITH (MOVE TO ", ");"),
                ("preceded-by-online", "DROP INDEX IX_T ON dbo.T WITH (ONLINE = ON, MOVE TO ", ");")
            };
            var values = new[]
            {
                "DEFAULT", "default", "[DEFAULT]", "\"DEFAULT\"",
                "[PRIMARY]", "PRIMARY", "dbo.StorageGroup", "PartScheme(Id)",
                "PartScheme([Id])", "DEFAULT (Id)", "dbo.DEFAULT",
                "DEFAULT DEFAULT", "", "1", "@target", "DEFAULT,",
                "PartScheme()", "DEFAULT FILESTREAM_ON"
            };

            var report = new StringBuilder();
            report.AppendLine("# Stage 16 DROP INDEX MOVE TO target acceptance matrix");
            report.AppendLine();
            report.AppendLine("| Context | Target | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (name, prefix, suffix) in contexts)
                foreach (var value in values)
                {
                    var sql = prefix + value + suffix;
                    report.AppendLine($"| {name} | {value} | {(ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess ? "yes" : "no")} |");
                }

            var reportPath = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(reportPath))!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sql-drop-move-target-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| simple | DEFAULT |");
        }

        private static IEnumerable<NonTerminalNode> Stage16Nodes(ParseTreeNode root)
        {
            if (root is not NonTerminalNode node)
                yield break;
            yield return node;
            foreach (var child in node.Children)
                foreach (var nested in Stage16Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<string> Stage16Tokens(ParseTreeNode root)
        {
            if (root is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in root.Children)
                foreach (var token in Stage16Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage16_ShouldPreserveMoveTargetAndIdempotence(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT);",
                "DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT, MAXDOP = 2);",
                "DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT FILESTREAM_ON [PRIMARY]);"
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
                Stage16FormatterTokens(reparsed.ParseTree!).Should().Equal(
                    Stage16FormatterTokens(original.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));
                var again = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                again.IsSuccess.Should().BeTrue(again.ErrorMessage);
                again.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage16FormatterTokens(ParseTreeNode root)
        {
            if (root is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in root.Children)
                foreach (var token in Stage16FormatterTokens(child))
                    yield return token;
        }
    }
}
