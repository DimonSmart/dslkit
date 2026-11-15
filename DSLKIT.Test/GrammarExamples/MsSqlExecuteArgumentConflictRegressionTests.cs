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
        [InlineData("EXEC dbo.usp_DoWork DEFAULT;")]
        [InlineData("EXEC dbo.usp_DoWork @value = DEFAULT;")]
        [InlineData("EXECUTE dbo.usp_DoWork @first = DEFAULT, @second = 123;")]
        [InlineData("EXECUTE @result = dbo.usp_DoWork @value = DEFAULT, @out = @result OUTPUT WITH RECOMPILE;")]
        [InlineData("EXEC dbo.usp_DoWork @value = NULL;")]
        [InlineData("EXEC dbo.usp_DoWork @value = N'text', @number = -2;")]
        [InlineData("EXEC dbo.usp_DoWork @value = [DEFAULT];")]
        [InlineData("EXEC dbo.usp_DoWork @value = \"DEFAULT\";")]
        [InlineData("INSERT INTO dbo.TargetTable EXEC dbo.usp_DoWork @value = DEFAULT;")]
        public void ParseScript_Stage7_ShouldPreserveExecuteArgumentForms(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
        }

        [Theory]
        [InlineData("EXEC dbo.usp_DoWork @value = ;")]
        [InlineData("EXEC dbo.usp_DoWork @value = DEFAULT,;")]
        [InlineData("EXEC dbo.usp_DoWork @value = DEFAULT DEFAULT;")]
        public void ParseScript_Stage7_ShouldRejectMalformedExecuteArguments(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage7_ShouldKeepDefaultArgumentsAttachedToTheirCalls()
        {
            const string sql =
                "EXEC dbo.FirstProc @a = DEFAULT, @b = 1; " +
                "EXEC dbo.SecondProc @x = DEFAULT, @y = DEFAULT;";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            var calls = Stage7Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "ExecuteModuleCallCore").ToArray();
            calls.Should().HaveCount(2);
            Stage7Tokens(calls[0]).Should().Contain("FirstProc").And.NotContain("SecondProc");
            Stage7Tokens(calls[1]).Should().Contain("SecondProc").And.NotContain("FirstProc");
            Stage7Nodes(calls[0]).Count(node => node.NonTerminal.Name == "ExecuteArgValue")
                .Should().Be(2);
            Stage7Nodes(calls[1]).Count(node => node.NonTerminal.Name == "ExecuteArgValue")
                .Should().Be(2);
        }

        [Fact]
        public void ParseScript_Stage7_ShouldRecordOriginalDefaultArgumentAcceptance()
        {
            var templates = new (string Context, string Sql)[]
            {
                ("positional", "EXEC dbo.usp_DoWork {0};"),
                ("named", "EXEC dbo.usp_DoWork @value = {0};"),
                ("named-list", "EXEC dbo.usp_DoWork @other = 1, @value = {0};"),
                ("insert-exec", "INSERT INTO dbo.TargetTable EXEC dbo.usp_DoWork @value = {0};")
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 7 original EXEC DEFAULT acceptance");
            report.AppendLine();
            report.AppendLine("| Context | Argument | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (context, template) in templates)
            {
                foreach (var value in new[] { "DEFAULT", "[DEFAULT]", "\"DEFAULT\"", "NULL", "@value", "1" })
                {
                    var sql = string.Format(template, value);
                    report.AppendLine($"| {context} | {value} | {(ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess ? "yes" : "no")} |");
                }
            }

            var reportPath = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(reportPath))!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sql-execute-argument-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| positional | DEFAULT |");
        }

        private static IEnumerable<NonTerminalNode> Stage7Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var item in Stage7Nodes(child))
                    yield return item;
        }

        private static IEnumerable<string> Stage7Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage7Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage7_ShouldPreserveExecuteArgumentsAndStability(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "EXEC dbo.usp_DoWork @value = DEFAULT, @count = 1;",
                "EXEC dbo.usp_DoWork @value = [DEFAULT];",
                "EXECUTE @result = dbo.usp_DoWork @arg = DEFAULT, @out = @result OUTPUT WITH RECOMPILE;",
                "INSERT INTO dbo.TargetTable EXEC dbo.usp_DoWork @arg = DEFAULT;"
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
                Stage7FormatterTokens(reparsed.ParseTree!)
                    .Should().Equal(Stage7FormatterTokens(original.ParseTree!),
                        (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var reformatted = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                reformatted.IsSuccess.Should().BeTrue(reformatted.ErrorMessage);
                reformatted.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage7FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage7FormatterTokens(child))
                    yield return token;
        }
    }
}
