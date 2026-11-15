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
        [InlineData("DELETE FROM dbo.Orders WHERE Id = 1;")]
        [InlineData("DELETE o FROM dbo.Orders AS o WHERE o.Id = 1;")]
        [InlineData("DELETE FROM o FROM dbo.Orders AS o WHERE o.Id = 1;")]
        [InlineData("DELETE FROM dbo.Orders WITH (ROWLOCK) WHERE Id = 1;")]
        [InlineData("DELETE TOP (10) FROM dbo.Orders WHERE Id = 1;")]
        [InlineData("DELETE FROM dbo.Orders OUTPUT deleted.Id;")]
        [InlineData("DELETE FROM dbo.Orders OUTPUT deleted.Id INTO @DeletedRows (Id);")]
        [InlineData("DELETE FROM dbo.Orders WHERE CURRENT OF CurDelete;")]
        [InlineData("DELETE FROM @Orders WHERE Id = 1;")]
        [InlineData("DELETE FROM OPENQUERY('Server', 'SELECT Id FROM dbo.Orders') WHERE Id = 1;")]
        [InlineData("DELETE FROM [dbo].[Orders] WHERE [Id] = 1;")]
        [InlineData("DELETE FROM \"dbo\".\"Orders\" WHERE \"Id\" = 1;")]
        [InlineData("DELETE FROM $(SchemaName).$(TableName) WHERE Id = 1;")]
        [InlineData("WITH cte AS (SELECT Id FROM dbo.Orders) DELETE FROM cte WHERE Id = 1;")]
        [InlineData("INSERT INTO dbo.Orders (Id, Name) VALUES (1, 'A');")]
        [InlineData("INSERT INTO dbo.Orders (Id) SELECT Id FROM dbo.Source;")]
        [InlineData("INSERT INTO dbo.Orders EXEC dbo.GetOrders;")]
        [InlineData("UPDATE o SET Id = 2 FROM dbo.Orders AS o WHERE o.Id = 1;")]
        [InlineData("MERGE dbo.Orders AS o USING dbo.Source AS s ON o.Id = s.Id WHEN MATCHED THEN DELETE;")]
        public void ParseScript_Stage6_ShouldPreserveSupportedDmlForms(string sql)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(sql);
            result.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
            result.ParseTree.Should().NotBeNull();
        }

        [Theory]
        [InlineData("DELETE FROM dbo.Orders WHERE Id = 1;", "Orders")]
        [InlineData("DELETE o FROM dbo.Orders AS o WHERE o.Id = 1;", "o")]
        [InlineData("DELETE FROM [dbo].[Orders] WITH (ROWLOCK) WHERE [Id] = 1;", "[Orders]")]
        [InlineData("DELETE FROM dbo.Orders OUTPUT deleted.Id INTO @DeletedRows (Id);", "Orders")]
        public void ParseScript_Stage6_ShouldKeepTargetAndClausesInDeleteTree(string sql, string targetToken)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var delete = Stage6Nodes(parsed.ParseTree!).Where(node => node.NonTerminal.Name == "DeleteStatement")
                .Should().ContainSingle().Subject;
            var target = Stage6Nodes(delete).Where(node => node.NonTerminal.Name == "DeleteTarget")
                .Should().ContainSingle().Subject;
            Stage6Tokens(target).Should().Contain(targetToken);
            var tokens = Stage6Tokens(delete).ToArray();
            if (sql.Contains(" WHERE ", StringComparison.Ordinal))
                tokens.Should().Contain("WHERE");
            if (sql.Contains(" OUTPUT ", StringComparison.Ordinal))
                tokens.Should().Contain("OUTPUT");
            if (sql.Contains(" WITH ", StringComparison.Ordinal))
                tokens.Should().Contain("WITH");
        }

        [Fact]
        public void ParseScript_Stage6_ShouldNotMoveDeleteTargetsBetweenStatements()
        {
            const string sql = "DELETE FROM dbo.FirstTable WHERE Id = 1; DELETE o FROM dbo.SecondTable AS o WHERE o.Id = 2; INSERT INTO dbo.ThirdTable (Id) VALUES (3);";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var deletes = Stage6Nodes(parsed.ParseTree!).Where(node => node.NonTerminal.Name == "DeleteStatement").ToArray();
            deletes.Should().HaveCount(2);
            var first = Stage6Tokens(deletes[0]).ToArray();
            var second = Stage6Tokens(deletes[1]).ToArray();
            first.Should().Contain("FirstTable").And.NotContain("SecondTable");
            second.Should().Contain("SecondTable").And.NotContain("FirstTable");
            Stage6Nodes(parsed.ParseTree!).Count(node => node.NonTerminal.Name == "InsertStatement").Should().Be(1);
        }

        [Theory]
        [InlineData("DELETE FROM;")]
        [InlineData("DELETE FROM dbo.;")]
        [InlineData("DELETE FROM dbo..;")]
        [InlineData("DELETE FROM dbo.Orders WITH ();")]
        [InlineData("DELETE FROM dbo.Orders WITH (WAITFOR);")]
        [InlineData("DELETE FROM dbo.Orders WHERE;")]
        [InlineData("INSERT INTO dbo.Orders (Id,) VALUES (1);")]
        [InlineData("INSERT INTO dbo.Orders () VALUES (1);")]
        [InlineData("DELETE FROM dbo.Orders OUTPUT deleted.Id INTO @DeletedRows (Id,);")]
        public void ParseScript_Stage6_ShouldRejectMalformedDml(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        // Record original acceptance of contextual keywords before changing the grammar.
        // The matrix is a CI artifact, not an assertion that every keyword is legal in every position.
        [Fact]
        public void ParseScript_Stage6_ShouldRecordOriginalContextualKeywordMatrix()
        {
            var cases = new (string Context, string Template)[]
            {
                ("delete-target", "DELETE FROM {0};"),
                ("delete-hinted-target", "DELETE FROM {0} WITH (ROWLOCK);"),
                ("delete-qualified-target", "DELETE FROM dbo.{0};"),
                ("delete-alias", "DELETE {0} FROM dbo.Orders AS {0};"),
                ("insert-target", "INSERT INTO {0} (Id) VALUES (1);"),
                ("insert-column", "INSERT INTO dbo.Orders ({0}) VALUES (1);"),
                ("output-target", "DELETE FROM dbo.Orders OUTPUT deleted.Id INTO {0};"),
                ("output-column", "DELETE FROM dbo.Orders OUTPUT deleted.Id INTO @out ({0});"),
                ("cursor", "DELETE FROM dbo.Orders WHERE CURRENT OF {0};")
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 6 original contextual keyword acceptance");
            report.AppendLine();
            report.AppendLine("| Context | Identifier | Bare keyword | Accepted |");
            report.AppendLine("| --- | --- | --- | --- |");
            foreach (var (context, template) in cases)
            {
                foreach (var keyword in new[] { "NAME", "SOURCE", "TARGET", "POLICY" })
                {
                    foreach (var form in new[] { keyword, "[" + keyword + "]", "\"" + keyword + "\"" })
                    {
                        var sql = string.Format(template, form);
                        var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
                        report.AppendLine($"| {context} | {form} | {(form == keyword ? "yes" : "no")} | {(parsed.IsSuccess ? "yes" : "no")} |");
                    }
                }
            }

            var reportPath = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var folder = Path.GetDirectoryName(Path.GetFullPath(reportPath))!;
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "sql-dml-keyword-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| delete-target |");
        }

        private static IEnumerable<NonTerminalNode> Stage6Nodes(ParseTreeNode root)
        {
            if (root is not NonTerminalNode node)
                yield break;
            yield return node;
            foreach (var child in node.Children)
                foreach (var item in Stage6Nodes(child))
                    yield return item;
        }

        private static IEnumerable<string> Stage6Tokens(ParseTreeNode root)
        {
            if (root is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in root.Children)
                foreach (var token in Stage6Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage6_ShouldPreserveDeleteTokensAndStability(SqlStatementTerminationMode termination)
        {
            var sqlCases = new[]
            {
                "DELETE FROM dbo.Orders WITH (ROWLOCK) WHERE Id = 1;",
                "DELETE o OUTPUT deleted.Id INTO @out (Id) FROM dbo.Orders AS o WHERE Id = 1;",
                "DELETE FROM [dbo].[Orders] WHERE [Id] = 1;",
                "DELETE FROM \"dbo\".\"Orders\" WHERE \"Id\" = 1;",
                "DELETE FROM dbo.Orders /* audit */ WHERE Id = 1;"
            };
            var options = new SqlFormattingOptions
            {
                Statement = new SqlStatementFormattingOptions { TerminateWithSemicolon = termination }
            };

            foreach (var sql in sqlCases)
            {
                var before = ModernMsSqlGrammarExample.ParseBatch(sql);
                before.IsSuccess.Should().BeTrue(before.Error?.Message);
                var formatted = ModernMsSqlFormatter.TryFormat(sql, options);
                formatted.IsSuccess.Should().BeTrue(formatted.ErrorMessage);
                formatted.FormattedSql.Should().NotBeNull();
                var after = ModernMsSqlGrammarExample.ParseBatch(formatted.FormattedSql!);
                after.IsSuccess.Should().BeTrue(after.Error?.Message);

                Stage6FormatTokens(after.ParseTree!).Should().Equal(
                    Stage6FormatTokens(before.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));
                if (sql.Contains("/* audit */", StringComparison.Ordinal))
                    formatted.FormattedSql.Should().Contain("/* audit */");
                if (sql.Contains("[Orders]", StringComparison.Ordinal))
                    formatted.FormattedSql.Should().Contain("[Orders]");
                if (sql.Contains("\"Orders\"", StringComparison.Ordinal))
                    formatted.FormattedSql.Should().Contain("\"Orders\"");

                var second = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                second.IsSuccess.Should().BeTrue(second.ErrorMessage);
                second.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage6FormatTokens(ParseTreeNode root)
        {
            if (root is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in root.Children)
                foreach (var token in Stage6FormatTokens(child))
                    yield return token;
        }
    }
}
