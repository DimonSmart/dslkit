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
        [InlineData("CREATE VIEW dbo.vStage12 AS SELECT 1 AS Id;")]
        [InlineData("CREATE OR ALTER VIEW dbo.vStage12 AS SELECT 1 AS Id;")]
        [InlineData("ALTER VIEW dbo.vStage12 AS SELECT 1 AS Id;")]
        [InlineData("CREATE VIEW dbo.vStage12 AS SELECT 1 AS Id WITH CHECK OPTION;")]
        [InlineData("CREATE OR ALTER VIEW dbo.vStage12 AS SELECT 1 AS Id WITH CHECK OPTION;")]
        [InlineData("CREATE VIEW dbo.vStage12 (Id) WITH SCHEMABINDING AS SELECT 1 AS Id WITH CHECK OPTION;")]
        [InlineData("CREATE VIEW dbo.vStage12 (Id) WITH ENCRYPTION, SCHEMABINDING AS WITH src AS (SELECT 1 AS Id) SELECT Id FROM src WITH CHECK OPTION;")]
        [InlineData("CREATE VIEW dbo.vStage12 AS WITH src AS (SELECT 1 AS Id) SELECT Id FROM src;")]
        public void ParseScript_Stage12_ShouldPreserveCreateViewBodyForms(string sql)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(sql);
            result.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
            result.ParseTree.Should().NotBeNull();
            var view = Stage12Nodes(result.ParseTree!)
                .Where(node => node.NonTerminal.Name == "CreateViewStatement")
                .Should().ContainSingle().Subject;
            var body = Stage12Nodes(view)
                .Where(node => node.NonTerminal.Name == "CreateViewBody")
                .Should().ContainSingle().Subject;
            Stage12Nodes(body).Should().Contain(node => node.NonTerminal.Name == "CreateViewQuery");
            if (sql.Contains("WITH CHECK OPTION", StringComparison.Ordinal))
                Stage12Tokens(body).Should().ContainInOrder("WITH", "CHECK", "OPTION");
        }

        [Fact]
        public void ParseScript_Stage12_ShouldKeepViewBoundariesAcrossMultipleStatements()
        {
            const string sql =
                "CREATE VIEW dbo.vFirst AS SELECT 1 AS FirstId WITH CHECK OPTION; " +
                "SELECT 2 AS NextValue;";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();
            var view = Stage12Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "CreateViewStatement")
                .Should().ContainSingle().Subject;
            Stage12Tokens(view).Should().Contain("FirstId").And.NotContain("NextValue");
            Stage12Tokens(parsed.ParseTree!).Should().Contain("NextValue");
        }

        [Theory]
        [InlineData("CREATE VIEW dbo.vBad AS ;")]
        [InlineData("CREATE VIEW dbo.vBad AS SELECT 1 AS Id WITH CHECK;")]
        [InlineData("CREATE VIEW dbo.vBad AS SELECT 1 AS Id WITH CHECK OPTION WITH CHECK OPTION;")]
        [InlineData("CREATE VIEW dbo.vBad AS SELECT 1 AS Id CHECK OPTION;")]
        [InlineData("CREATE VIEW dbo.vBad AS WITH src AS (SELECT 1 AS Id) WITH CHECK OPTION;")]
        public void ParseScript_Stage12_ShouldRejectMalformedViewBodies(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage12_ShouldRecordOriginalCreateViewAcceptanceMatrix()
        {
            var contexts = new (string Name, string Prefix)[]
            {
                ("create", "CREATE VIEW dbo.vMatrix AS "),
                ("create-or-alter", "CREATE OR ALTER VIEW dbo.vMatrix AS "),
                ("alter", "ALTER VIEW dbo.vMatrix AS "),
                ("columns", "CREATE VIEW dbo.vMatrix (Id) AS "),
                ("options", "CREATE VIEW dbo.vMatrix WITH SCHEMABINDING AS "),
                ("columns-options", "CREATE VIEW dbo.vMatrix (Id) WITH ENCRYPTION, SCHEMABINDING AS ")
            };
            var bodies = new (string Name, string Body)[]
            {
                ("select", "SELECT 1 AS Id"),
                ("select-check", "SELECT 1 AS Id WITH CHECK OPTION"),
                ("cte", "WITH src AS (SELECT 1 AS Id) SELECT Id FROM src"),
                ("cte-check", "WITH src AS (SELECT 1 AS Id) SELECT Id FROM src WITH CHECK OPTION"),
                ("union", "SELECT 1 AS Id UNION ALL SELECT 2 AS Id"),
                ("union-check", "SELECT 1 AS Id UNION ALL SELECT 2 AS Id WITH CHECK OPTION"),
                ("missing-query", ""),
                ("missing-option", "SELECT 1 AS Id WITH CHECK"),
                ("duplicate-check", "SELECT 1 AS Id WITH CHECK OPTION WITH CHECK OPTION"),
                ("bare-check", "SELECT 1 AS Id CHECK OPTION")
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 12 original CREATE VIEW body acceptance");
            report.AppendLine();
            report.AppendLine("| Context | Body | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (name, prefix) in contexts)
                foreach (var (bodyName, body) in bodies)
                {
                    var accepted = ModernMsSqlGrammarExample.ParseBatch(prefix + body + ";").IsSuccess;
                    report.AppendLine($"| {name} | {bodyName} | {(accepted ? "yes" : "no")} |");
                }
            var path = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(path))
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sql-create-view-check-option-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| create | select-check |");
        }

        private static IEnumerable<NonTerminalNode> Stage12Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var nested in Stage12Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<string> Stage12Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage12Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage12_ShouldPreserveViewTokensAndStability(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "CREATE VIEW dbo.vStage12 AS SELECT 1 AS Id;",
                "CREATE VIEW dbo.vStage12 AS SELECT 1 AS Id WITH CHECK OPTION;",
                "CREATE VIEW dbo.vStage12 (Id) WITH SCHEMABINDING AS WITH src AS (SELECT 1 AS Id) SELECT Id FROM src WITH CHECK OPTION;"
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
                Stage12FormatterTokens(reparsed.ParseTree!).Should().Equal(
                    Stage12FormatterTokens(original.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var reformatted = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                reformatted.IsSuccess.Should().BeTrue(reformatted.ErrorMessage);
                reformatted.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage12FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage12FormatterTokens(child))
                    yield return token;
        }
    }
}
