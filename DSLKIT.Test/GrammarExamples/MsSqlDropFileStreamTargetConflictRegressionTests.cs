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
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON DEFAULT);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON [DEFAULT]);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON [PRIMARY]);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT FILESTREAM_ON DEFAULT);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO [PRIMARY] FILESTREAM_ON DEFAULT);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON DEFAULT, MAXDOP = 2);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MAXDOP = 2, FILESTREAM_ON DEFAULT);")]
        [InlineData("DROP INDEX IF EXISTS IX_T ON dbo.T WITH (FILESTREAM_ON DEFAULT);")]
        public void ParseScript_Stage17_ShouldPreserveDropIndexFilestreamTargets(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            Stage17Nodes(parsed.ParseTree!).Should().Contain(
                node => node.NonTerminal.Name == "DropFileStreamTarget");
        }

        [Fact]
        public void ParseScript_Stage17_ShouldKeepFilestreamTargetsInsideTheirOwnStatements()
        {
            const string sql =
                "DROP INDEX IX_First ON dbo.T WITH (FILESTREAM_ON DEFAULT); " +
                "DROP INDEX IX_Second ON dbo.T WITH (MOVE TO [PRIMARY] FILESTREAM_ON [FSGroup]);";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var statements = Stage17Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "DropIndexStatement").ToArray();
            statements.Should().HaveCount(2);
            Stage17Nodes(statements[0]).Count(node => node.NonTerminal.Name == "DropFileStreamTarget")
                .Should().Be(1);
            Stage17Nodes(statements[1]).Count(node => node.NonTerminal.Name == "DropFileStreamTarget")
                .Should().Be(1);
            Stage17Tokens(statements[0]).Should().Contain("IX_First").And.NotContain("IX_Second");
            Stage17Tokens(statements[1]).Should().Contain("IX_Second").And.NotContain("IX_First");
        }

        [Theory]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON , MAXDOP = 2);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON DEFAULT,);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON DEFAULT DEFAULT);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON DEFAULT,, MAXDOP = 2);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT FILESTREAM_ON);")]
        [InlineData("DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON DEFAULT;")]
        public void ParseScript_Stage17_ShouldRejectMalformedFilestreamOptions(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage17_ShouldRecordOriginalFilestreamAcceptanceMatrix()
        {
            var contexts = new (string Name, string Prefix, string Suffix)[]
            {
                ("standalone", "DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON ", ");"),
                ("after-move-default", "DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT FILESTREAM_ON ", ");"),
                ("after-move-primary", "DROP INDEX IX_T ON dbo.T WITH (MOVE TO [PRIMARY] FILESTREAM_ON ", ");"),
                ("preceded-by-maxdop", "DROP INDEX IX_T ON dbo.T WITH (MAXDOP = 2, FILESTREAM_ON ", ");"),
                ("followed-by-maxdop", "DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON ", ", MAXDOP = 2);"),
                ("if-exists", "DROP INDEX IF EXISTS IX_T ON dbo.T WITH (FILESTREAM_ON ", ");")
            };
            var values = new[]
            {
                "DEFAULT", "default", "[DEFAULT]", "\"DEFAULT\"",
                "[PRIMARY]", "PRIMARY", "FSGroup", "dbo.FSGroup",
                "dbo.DEFAULT", "[dbo].[FSGroup]", "DEFAULT DEFAULT",
                "", "1", "@target", "DEFAULT,", "FSGroup()",
                "DEFAULT FILESTREAM_ON", "DEFAULT (Id)"
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 17 DROP INDEX FILESTREAM_ON target acceptance matrix");
            report.AppendLine();
            report.AppendLine("| Context | Target | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (name, prefix, suffix) in contexts)
                foreach (var value in values)
                {
                    var sql = prefix + value + suffix;
                    var accepted = ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess;
                    report.AppendLine($"| {name} | {value} | {(accepted ? "yes" : "no")} |");
                }

            var reportPath = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var folder = Path.GetDirectoryName(Path.GetFullPath(reportPath))!;
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "sql-drop-filestream-target-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| standalone | DEFAULT |");
        }

        private static IEnumerable<NonTerminalNode> Stage17Nodes(ParseTreeNode root)
        {
            if (root is not NonTerminalNode node)
                yield break;
            yield return node;
            foreach (var child in node.Children)
                foreach (var nested in Stage17Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<string> Stage17Tokens(ParseTreeNode root)
        {
            if (root is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in root.Children)
                foreach (var token in Stage17Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage17_ShouldPreserveFilestreamTargetsAndIdempotence(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON DEFAULT);",
                "DROP INDEX IX_T ON dbo.T WITH (MOVE TO DEFAULT FILESTREAM_ON DEFAULT);",
                "DROP INDEX IX_T ON dbo.T WITH (FILESTREAM_ON [PRIMARY], MAXDOP = 2);"
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
                Stage17FormatterTokens(reparsed.ParseTree!).Should().Equal(
                    Stage17FormatterTokens(original.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var again = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                again.IsSuccess.Should().BeTrue(again.ErrorMessage);
                again.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage17FormatterTokens(ParseTreeNode root)
        {
            if (root is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in root.Children)
                foreach (var token in Stage17FormatterTokens(child))
                    yield return token;
        }
    }
}
