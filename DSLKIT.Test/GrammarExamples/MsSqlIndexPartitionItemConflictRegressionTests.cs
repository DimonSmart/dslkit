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
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (ALL));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS ([ALL]));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (1 TO 3));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (1, 2, 3));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (ALL), FILLFACTOR = 90);")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (ALL));")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REORGANIZE WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (ALL));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (XML_COMPRESSION = ON ON PARTITIONS (ALL));")]
        public void ParseScript_Stage18_ShouldPreserveIndexPartitionItems(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            Stage18Nodes(parsed.ParseTree!).Should().Contain(
                node => node.NonTerminal.Name == "IndexPartitionItem");
        }

        [Fact]
        public void ParseScript_Stage18_ShouldKeepPartitionItemsInsideTheirOwnOptionsAndStatements()
        {
            const string sql =
                "CREATE INDEX IX_A ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (ALL, 3), FILLFACTOR = 90); " +
                "ALTER INDEX IX_B ON dbo.T REBUILD WITH (DATA_COMPRESSION = ROW ON PARTITIONS (1 TO 3));";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var create = Stage18Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "CreateIndexStatement")
                .Should().ContainSingle().Subject;
            var alter = Stage18Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "AlterIndexStatement")
                .Should().ContainSingle().Subject;

            Stage18Nodes(create).Count(node => node.NonTerminal.Name == "IndexPartitionItem")
                .Should().Be(2);
            Stage18Nodes(alter).Count(node => node.NonTerminal.Name == "IndexPartitionItem")
                .Should().Be(1);
            Stage18Tokens(create).Should().Contain("IX_A").And.NotContain("IX_B");
            Stage18Tokens(alter).Should().Contain("IX_B").And.NotContain("IX_A");
        }

        [Theory]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS ());")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (ALL,));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (,ALL));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (ALL ALL));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (1 TO));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (TO 3));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS ALL);")]
        public void ParseScript_Stage18_ShouldRejectMalformedPartitionLists(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage18_ShouldRecordOriginalIndexPartitionItemAcceptanceMatrix()
        {
            var contexts = new (string Name, string Prefix, string Suffix)[]
            {
                ("create", "CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (", "));"),
                ("create-followed-by-option", "CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (", "), FILLFACTOR = 90);"),
                ("create-xml-compression", "CREATE INDEX IX_T ON dbo.T (Id) WITH (XML_COMPRESSION = ON ON PARTITIONS (", "));"),
                ("rebuild", "ALTER INDEX IX_T ON dbo.T REBUILD WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (", "));"),
                ("reorganize", "ALTER INDEX IX_T ON dbo.T REORGANIZE WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (", "));"),
                ("rebuild-after-option", "ALTER INDEX IX_T ON dbo.T REBUILD WITH (MAXDOP = 2, DATA_COMPRESSION = PAGE ON PARTITIONS (", "));")
            };
            var values = new[]
            {
                "ALL", "all", "[ALL]", "\"ALL\"",
                "1", "1 TO 3", "1, 3", "ALL, 3",
                "1 TO ALL", "ALL TO 3", "dbo.ALL", "@partition",
                "NULL", "DEFAULT", "", "ALL,",
                ", ALL", "ALL ALL", "1 TO", "(1)", "ALL (1)"
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 18 index-option ON PARTITIONS item acceptance matrix");
            report.AppendLine();
            report.AppendLine("| Context | Partition list | Accepted |");
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
                var directory = Path.GetDirectoryName(Path.GetFullPath(reportPath))!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sql-index-partition-item-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| create | ALL |");
        }

        private static IEnumerable<NonTerminalNode> Stage18Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var nested in Stage18Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<string> Stage18Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage18Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage18_ShouldPreservePartitionTokensAndIdempotence(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (ALL));",
                "ALTER INDEX IX_T ON dbo.T REBUILD WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (ALL, 3));",
                "CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (1 TO 3), FILLFACTOR = 90);"
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
                Stage18FormatterTokens(reparsed.ParseTree!).Should().Equal(
                    Stage18FormatterTokens(original.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var again = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                again.IsSuccess.Should().BeTrue(again.ErrorMessage);
                again.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage18FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage18FormatterTokens(child))
                    yield return token;
        }
    }
}
