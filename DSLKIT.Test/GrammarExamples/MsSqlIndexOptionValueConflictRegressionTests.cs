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
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE);")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = COLUMNSTORE);")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS (1 TO 3));")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = COLUMNSTORE ON PARTITIONS (1 TO 3));")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD WITH (DATA_COMPRESSION = PAGE);")]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD WITH (DATA_COMPRESSION = COLUMNSTORE);")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE, FILLFACTOR = 90);")]
        public void ParseScript_Stage14_ShouldPreserveIndexCompressionValues(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            Stage14Nodes(parsed.ParseTree!).Should().Contain(
                node => node.NonTerminal.Name == "IndexOptionValue");
        }

        [Fact]
        public void ParseScript_Stage14_ShouldPreserveOptionAndStatementBoundaries()
        {
            const string sql =
                "CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE, FILLFACTOR = 90); " +
                "ALTER INDEX IX_T ON dbo.T REBUILD WITH (DATA_COMPRESSION = COLUMNSTORE);";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            Stage14Nodes(parsed.ParseTree!)
                .Count(node => node.NonTerminal.Name == "IndexOptionValue")
                .Should().Be(3);
            Stage14Tokens(parsed.ParseTree!).Should().Contain("PAGE").And.Contain("COLUMNSTORE");
        }

        [Theory]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = );")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = , FILLFACTOR = 90);")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE PAGE);")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = COLUMNSTORE COLUMNSTORE);")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE ON PARTITIONS ());")]
        public void ParseScript_Stage14_ShouldRejectMalformedCompressionOptions(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage14_ShouldRecordOriginalIndexOptionAcceptanceMatrix()
        {
            var contexts = new (string Name, string Prefix, string Suffix)[]
            {
                ("create", "CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = ", ");"),
                ("create-followed-by-fillfactor", "CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = ", ", FILLFACTOR = 90);"),
                ("create-partitions", "CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = ", " ON PARTITIONS (1 TO 3));"),
                ("rebuild", "ALTER INDEX IX_T ON dbo.T REBUILD WITH (DATA_COMPRESSION = ", ");"),
                ("reorganize", "ALTER INDEX IX_T ON dbo.T REORGANIZE WITH (DATA_COMPRESSION = ", ");")
            };
            var values = new[]
            {
                "PAGE", "COLUMNSTORE", "ROW", "COLUMNSTORE_ARCHIVE",
                "NONE", "SELF", "BLOCKERS", "ON", "OFF",
                "90", "@mode", "[PAGE]", "PAGE PAGE", ""
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 14 original index-option acceptance");
            report.AppendLine();
            report.AppendLine("| Context | Value | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (name, prefix, suffix) in contexts)
                foreach (var value in values)
                {
                    var accepted = ModernMsSqlGrammarExample.ParseBatch(prefix + value + suffix).IsSuccess;
                    report.AppendLine($"| {name} | {value} | {(accepted ? "yes" : "no")} |");
                }

            var path = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(path))
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sql-index-option-value-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| create | PAGE |");
        }

        private static IEnumerable<NonTerminalNode> Stage14Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var nested in Stage14Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<string> Stage14Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage14Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage14_ShouldPreserveIndexCompressionTokensAndStability(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = PAGE);",
                "CREATE INDEX IX_T ON dbo.T (Id) WITH (DATA_COMPRESSION = COLUMNSTORE, FILLFACTOR = 90);",
                "ALTER INDEX IX_T ON dbo.T REBUILD WITH (DATA_COMPRESSION = PAGE);"
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
                Stage14FormatterTokens(reparsed.ParseTree!).Should().Equal(
                    Stage14FormatterTokens(original.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));
                var reformatted = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                reformatted.IsSuccess.Should().BeTrue(reformatted.ErrorMessage);
                reformatted.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage14FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage14FormatterTokens(child))
                    yield return token;
        }
    }
}
