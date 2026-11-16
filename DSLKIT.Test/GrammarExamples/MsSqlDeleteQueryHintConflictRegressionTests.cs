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
        [InlineData("DELETE FROM dbo.Orders OPTION (RECOMPILE);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX);")]
        [InlineData("DELETE FROM dbo.Orders WHERE Id = 1 OPTION (MAXDOP 2);")]
        [InlineData("DELETE FROM dbo.Orders WHERE Id = 1 OPTION (MAXRECURSION 25);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (RECOMPILE, MAXDOP 2);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (MAXDOP 2, RECOMPILE);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (RECOMPILE, MAXDOP 2, HASH JOIN);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (USE HINT('DISALLOW_BATCH_MODE'));")]
        [InlineData("DELETE TOP (10) FROM dbo.Orders OPTION (RECOMPILE);")]
        public void ParseScript_Stage15_ShouldPreserveDeleteQueryHints(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            Stage15Nodes(parsed.ParseTree!).Should().Contain(node =>
                node.NonTerminal.Name == "DeleteQueryHintList");
        }

        [Fact]
        public void ParseScript_Stage15_ShouldPreserveHintListTreeAndStatementBoundaries()
        {
            const string sql =
                "DELETE FROM dbo.Orders OPTION (RECOMPILE, MAXDOP 2); " +
                "DELETE FROM dbo.OtherOrders WHERE Id = 3 OPTION (RECOMPILE);";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var deletes = Stage15Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "DeleteStatement").ToArray();
            deletes.Should().HaveCount(2);
            Stage15Nodes(deletes[0]).Count(node => node.NonTerminal.Name == "DeleteQueryHintList")
                .Should().Be(2);
            Stage15Tokens(deletes[0]).Should().Contain("Orders").And.NotContain("OtherOrders");
            Stage15Tokens(deletes[1]).Should().Contain("OtherOrders").And.NotContain("Orders");
        }

        [Theory]
        [InlineData("DELETE FROM dbo.Orders OPTION ();")]
        [InlineData("DELETE FROM dbo.Orders OPTION (RECOMPILE,);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (, RECOMPILE);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (RECOMPILE,, MAXDOP 2);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (RECOMPILE RECOMPILE);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (MAXDOP);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (MAXDOP =);")]
        [InlineData("DELETE FROM dbo.Orders OPTION (RECOMPILE;")]
        public void ParseScript_Stage15_ShouldRejectMalformedDeleteHints(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage15_ShouldRecordDeleteQueryHintAcceptanceMatrix()
        {
            var contexts = new (string Name, string Prefix)[]
            {
                ("simple", "DELETE FROM dbo.Orders"),
                ("where", "DELETE FROM dbo.Orders WHERE Id = 1"),
                ("top", "DELETE TOP (10) FROM dbo.Orders"),
                ("table-hint", "DELETE FROM dbo.Orders WITH (ROWLOCK)"),
                ("output", "DELETE FROM dbo.Orders OUTPUT deleted.Id"),
                ("alias", "DELETE o FROM dbo.Orders AS o WHERE o.Id = 1")
            };
            var hints = new[]
            {
                "RECOMPILE",
                "IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX",
                "MAXDOP 2",
                "MAXDOP = 2",
                "MAXRECURSION 25",
                "QUERYTRACEON 9481",
                "MIN_GRANT_PERCENT = 20",
                "USE HINT('DISALLOW_BATCH_MODE')",
                "HASH JOIN",
                "RECOMPILE, MAXDOP 2",
                "MAXDOP 2, RECOMPILE",
                "RECOMPILE, MAXDOP 2, HASH JOIN",
                "",
                "RECOMPILE,",
                ", RECOMPILE",
                "RECOMPILE,, MAXDOP 2",
                "RECOMPILE RECOMPILE",
                "MAXDOP",
                "MAXDOP ="
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 15 DELETE query-hint acceptance matrix");
            report.AppendLine();
            report.AppendLine("| Context | Hint list | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (name, prefix) in contexts)
                foreach (var hint in hints)
                {
                    var sql = prefix + " OPTION (" + hint + ");";
                    var accepted = ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess;
                    report.AppendLine($"| {name} | {hint} | {(accepted ? "yes" : "no")} |");
                }
            var reportPath = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var folder = Path.GetDirectoryName(Path.GetFullPath(reportPath))!;
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "sql-delete-query-hint-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| simple | RECOMPILE |");
        }

        private static IEnumerable<NonTerminalNode> Stage15Nodes(ParseTreeNode root)
        {
            if (root is not NonTerminalNode node)
                yield break;
            yield return node;
            foreach (var child in node.Children)
                foreach (var nested in Stage15Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<string> Stage15Tokens(ParseTreeNode root)
        {
            if (root is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in root.Children)
                foreach (var token in Stage15Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage15_ShouldPreserveDeleteHintsAndIdempotence(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "DELETE FROM dbo.Orders OPTION (RECOMPILE);",
                "DELETE FROM dbo.Orders WHERE Id = 1 OPTION (RECOMPILE, MAXDOP 2);",
                "DELETE TOP (10) FROM dbo.Orders OPTION (MAXDOP 2, RECOMPILE);"
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
                Stage15FormatterTokens(reparsed.ParseTree!).Should().Equal(
                    Stage15FormatterTokens(original.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));
                var again = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                again.IsSuccess.Should().BeTrue(again.ErrorMessage);
                again.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage15FormatterTokens(ParseTreeNode root)
        {
            if (root is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in root.Children)
                foreach (var token in Stage15FormatterTokens(child))
                    yield return token;
        }
    }
}
