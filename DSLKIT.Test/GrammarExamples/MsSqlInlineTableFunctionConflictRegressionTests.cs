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
        [InlineData("CREATE FUNCTION dbo.ufn_Ids() RETURNS TABLE AS RETURN SELECT 1 AS Id;")]
        [InlineData("CREATE FUNCTION dbo.ufn_Ids() RETURNS TABLE AS RETURN (SELECT 1 AS Id);")]
        [InlineData("CREATE OR ALTER FUNCTION dbo.ufn_Ids(@id INT) RETURNS TABLE AS RETURN (SELECT @id AS Id);")]
        [InlineData("ALTER FUNCTION dbo.ufn_Ids(@id INT) RETURNS TABLE AS RETURN (SELECT @id AS Id);")]
        [InlineData("CREATE FUNCTION dbo.ufn_Ids() RETURNS TABLE AS RETURN (SELECT 1 AS Id UNION ALL SELECT 2 AS Id);")]
        [InlineData("CREATE FUNCTION dbo.ufn_Ids() RETURNS TABLE AS RETURN ((SELECT 1 AS Id));")]
        [InlineData("CREATE FUNCTION dbo.ufn_Ids() RETURNS TABLE AS RETURN (WITH cte AS (SELECT 1 AS Id) SELECT Id FROM cte);")]
        public void ParseScript_Stage11_ShouldPreserveInlineTableFunctionBodyForms(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();

            var function = Stage11Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "CreateFunctionStatement")
                .Should().ContainSingle().Subject;
            var body = Stage11Nodes(function)
                .Where(node => node.NonTerminal.Name == "CreateFunctionInlineTableBody")
                .Should().ContainSingle().Subject;
            Stage11Tokens(body).Should().Contain("RETURN");
            Stage11Nodes(body).Should().Contain(node => node.NonTerminal.Name == "QueryExpression");
        }

        [Fact]
        public void ParseScript_Stage11_ShouldKeepParenthesizedFunctionBodyBoundaries()
        {
            const string sql =
                "CREATE FUNCTION dbo.ufn_First() RETURNS TABLE AS RETURN (SELECT 1 AS Id); " +
                "SELECT 2 AS NextValue;";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();
            var function = Stage11Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "CreateFunctionStatement")
                .Should().ContainSingle().Subject;
            var functionTokens = Stage11Tokens(function).ToArray();
            functionTokens.Should().Contain("ufn_First").And.NotContain("NextValue");
            Stage11Tokens(parsed.ParseTree!).Should().Contain("NextValue");
        }

        [Theory]
        [InlineData("CREATE FUNCTION dbo.ufn_Bad() RETURNS TABLE AS RETURN ();")]
        [InlineData("CREATE FUNCTION dbo.ufn_Bad() RETURNS TABLE AS RETURN (SELECT);")]
        [InlineData("CREATE FUNCTION dbo.ufn_Bad() RETURNS TABLE AS RETURN (SELECT 1 AS Id;")]
        [InlineData("CREATE FUNCTION dbo.ufn_Bad() RETURNS TABLE AS RETURN SELECT 1 AS Id);")]
        [InlineData("CREATE FUNCTION dbo.ufn_Bad() RETURNS INT AS RETURN (SELECT 1 AS Id);")]
        [InlineData("CREATE FUNCTION dbo.ufn_Bad() RETURNS TABLE AS BEGIN RETURN; END;")]
        public void ParseScript_Stage11_ShouldRejectMalformedFunctionBodies(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage11_ShouldRecordOriginalInlineFunctionAcceptanceMatrix()
        {
            var contexts = new (string Name, string Prefix)[]
            {
                ("create", "CREATE FUNCTION dbo.ufn_Matrix() RETURNS TABLE AS RETURN "),
                ("create-or-alter", "CREATE OR ALTER FUNCTION dbo.ufn_Matrix() RETURNS TABLE AS RETURN "),
                ("alter", "ALTER FUNCTION dbo.ufn_Matrix() RETURNS TABLE AS RETURN "),
                ("with-option", "CREATE FUNCTION dbo.ufn_Matrix() RETURNS TABLE WITH SCHEMABINDING AS RETURN ")
            };
            var queries = new (string Name, string Sql)[]
            {
                ("bare", "SELECT 1 AS Id"),
                ("parenthesized", "(SELECT 1 AS Id)"),
                ("double-parenthesized", "((SELECT 1 AS Id))"),
                ("union", "(SELECT 1 AS Id UNION ALL SELECT 2 AS Id)"),
                ("cte", "(WITH cte AS (SELECT 1 AS Id) SELECT Id FROM cte)"),
                ("missing-expression", "(SELECT)"),
                ("empty-parentheses", "()"),
                ("missing-closing-parenthesis", "(SELECT 1 AS Id"),
                ("scalar-return", "1")
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 11 original inline table-valued function acceptance");
            report.AppendLine();
            report.AppendLine("| Context | Body | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (name, prefix) in contexts)
            {
                foreach (var (body, sql) in queries)
                {
                    var parsed = ModernMsSqlGrammarExample.ParseBatch(prefix + sql + ";");
                    report.AppendLine($"| {name} | {body} | {(parsed.IsSuccess ? "yes" : "no")} |");
                }
            }
            var reportPath = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var dir = Path.GetDirectoryName(Path.GetFullPath(reportPath))!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "sql-inline-function-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| create | parenthesized |");
        }

        private static IEnumerable<NonTerminalNode> Stage11Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in node.Children)
                foreach (var nested in Stage11Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<string> Stage11Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage11Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage11_ShouldPreserveInlineFunctionTokensAndStability(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "CREATE FUNCTION dbo.ufn_Ids() RETURNS TABLE AS RETURN (SELECT 1 AS Id);",
                "CREATE FUNCTION dbo.ufn_Ids() RETURNS TABLE AS RETURN SELECT 1 AS Id;",
                "CREATE OR ALTER FUNCTION dbo.ufn_Ids(@id INT) RETURNS TABLE AS RETURN (SELECT @id AS Id);"
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
                Stage11FormatterTokens(reparsed.ParseTree!).Should().Equal(
                    Stage11FormatterTokens(original.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var reformatted = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                reformatted.IsSuccess.Should().BeTrue(reformatted.ErrorMessage);
                reformatted.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage11FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage11FormatterTokens(child))
                    yield return token;
        }
    }
}
