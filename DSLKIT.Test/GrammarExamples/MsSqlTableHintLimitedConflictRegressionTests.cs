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
        [InlineData("SELECT * FROM dbo.T WITH (NOLOCK);")]
        [InlineData("SELECT * FROM dbo.T WITH (ROWLOCK, HOLDLOCK);")]
        [InlineData("DELETE FROM dbo.T WITH (NOLOCK);")]
        [InlineData("DELETE FROM dbo.T WITH (HOLDLOCK, INDEX(1));")]
        [InlineData("INSERT INTO dbo.T WITH (TABLOCK) (Id) VALUES (1);")]
        [InlineData("UPDATE dbo.T WITH (UPDLOCK) SET Id = 1;")]
        public void ParseScript_Stage20_ShouldPreserveLimitedTableHintsAcrossDml(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            Stage20Nodes(parsed.ParseTree!).Should().Contain(
                node => node.NonTerminal.Name == "TableHintLimited");
        }

        [Fact]
        public void ParseScript_Stage20_ShouldKeepHintsAttachedToCorrectDeleteStatement()
        {
            const string sql =
                "DELETE FROM dbo.FirstTable WITH (NOLOCK); " +
                "DELETE FROM dbo.SecondTable WITH (HOLDLOCK, INDEX(1));";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var statements = Stage20Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "DeleteStatement").ToArray();
            statements.Should().HaveCount(2);
            Stage20Nodes(statements[0]).Count(node => node.NonTerminal.Name == "TableHintLimited")
                .Should().Be(1);
            Stage20Nodes(statements[1]).Count(node => node.NonTerminal.Name == "TableHintLimited")
                .Should().Be(2);
            Stage20Tokens(statements[0]).Should().Contain("FirstTable").And.NotContain("SecondTable");
            Stage20Tokens(statements[1]).Should().Contain("SecondTable").And.NotContain("FirstTable");
        }

        [Theory]
        [InlineData("SELECT * FROM dbo.T WITH ();")]
        [InlineData("SELECT * FROM dbo.T WITH (WAITFOR);")]
        [InlineData("DELETE FROM dbo.T WITH (GRAPH);")]
        [InlineData("DELETE FROM dbo.T WITH (NOLOCK,);")]
        [InlineData("DELETE FROM dbo.T WITH (,NOLOCK);")]
        [InlineData("DELETE FROM dbo.T WITH (NOLOCK,,ROWLOCK);")]
        [InlineData("DELETE FROM dbo.T WITH (INDEX =);")]
        [InlineData("DELETE FROM dbo.T WITH (NOLOCK NOLOCK);")]
        public void ParseScript_Stage20_ShouldRejectMalformedAndUnknownTableHints(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage20_ShouldRecordOriginalTableHintAcceptanceMatrix()
        {
            var contexts = new (string Name, string Prefix, string Suffix)[]
            {
                ("select", "SELECT * FROM dbo.T WITH (", ");"),
                ("delete", "DELETE FROM dbo.T WITH (", ");"),
                ("insert", "INSERT INTO dbo.T WITH (", ") (Id) VALUES (1);"),
                ("update", "UPDATE dbo.T WITH (", ") SET Id = 1;"),
                ("select-with-next-hint", "SELECT * FROM dbo.T WITH (", ", ROWLOCK);"),
                ("delete-with-next-hint", "DELETE FROM dbo.T WITH (", ", ROWLOCK);")
            };
            var hints = new[]
            {
                "NOLOCK", "HOLDLOCK", "ROWLOCK", "TABLOCK", "INDEX(1)",
                "INDEX = 1", "INDEX([IX_T])", "FORCESEEK", "FORCESEEK(1)",
                "dbo.NOLOCK", "dbo.INDEX(1)", "dbo.NOLOCK = 1",
                "NOLOCK = 1", "NOLOCK(1)", "NOLOCK, TABLOCK",
                "[NOLOCK]", "NOLOCK.NOLOCK", "NOLOCK..ROWLOCK",
                "WAITFOR", "", ", NOLOCK", "NOLOCK,", "NOLOCK NOLOCK",
                "INDEX()", "INDEX =", "TABLOCKX"
            };

            var report = new StringBuilder();
            report.AppendLine("# Stage 20 DML limited table hint acceptance matrix");
            report.AppendLine();
            report.AppendLine("| Context | Table hints | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (name, prefix, suffix) in contexts)
                foreach (var value in hints)
                {
                    var accepted = ModernMsSqlGrammarExample.ParseBatch(prefix + value + suffix).IsSuccess;
                    report.AppendLine($"| {name} | {value} | {(accepted ? "yes" : "no")} |");
                }

            var reportPath = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(reportPath))!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sql-table-hint-limited-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| delete | NOLOCK |");
        }

        private static IEnumerable<NonTerminalNode> Stage20Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var nested in Stage20Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<string> Stage20Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage20Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage20_ShouldPreserveHintTokensAndIdempotence(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "DELETE FROM dbo.T WITH (NOLOCK);",
                "DELETE FROM dbo.T WITH (HOLDLOCK, INDEX(1));",
                "SELECT * FROM dbo.T WITH (UPDLOCK);"
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
                Stage20FormatterTokens(reparsed.ParseTree!).Should().Equal(
                    Stage20FormatterTokens(original.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var again = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                again.IsSuccess.Should().BeTrue(again.ErrorMessage);
                again.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage20FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage20FormatterTokens(child))
                    yield return token;
        }
    }
}
