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
        [InlineData("IF (1=1) DELETE dbo.T ELSE TRUNCATE TABLE dbo.T")]
        [InlineData("IF @x < 500 SET @c = 'A'; ELSE IF @x < 1000 SET @c = 'B'; ELSE SET @c = 'C'")]
        [InlineData("IF 1 = 1 BEGIN END;")]
        [InlineData("IF 1 = 1 PRINT N'yes' ELSE PRINT N'no'")]
        public void ParseScript_Stage13_ShouldPreserveIfAndElseIfForms(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
            Stage13Nodes(parsed.ParseTree!)
                .Should().Contain(node => node.NonTerminal.Name == "IfStatement");
        }

        [Fact]
        public void ParseScript_Stage13_ShouldBindElseToNearestIfInChain()
        {
            const string sql = "IF @x < 500 SET @c = 'A'; ELSE IF @x < 1000 SET @c = 'B'; ELSE SET @c = 'C'";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var ifs = Stage13Nodes(parsed.ParseTree!)
                .Where(node => node.NonTerminal.Name == "IfStatement").ToArray();
            ifs.Should().HaveCount(2);

            Stage13ElseDepths(parsed.ParseTree!, 0).Should().Equal(1, 2);
        }

        [Theory]
        [InlineData("IF 1 = 1 ELSE PRINT 'x';")]
        [InlineData("IF 1 = 1 PRINT 'yes' ELSE")]
        [InlineData("IF 1 = 1 PRINT 'yes' ELSE ELSE PRINT 'no';")]
        [InlineData("IF 1 = 1 PRINT 'yes' ELSE IF 2 = 2")]
        public void ParseScript_Stage13_ShouldRejectMalformedElseBranches(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid SQL was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage13_ShouldRecordOriginalIfElseAcceptanceMatrix()
        {
            var contexts = new (string Name, string Prefix)[]
            {
                ("print", "IF 1 = 1 PRINT 'a'"),
                ("print-semicolon", "IF 1 = 1 PRINT 'a';"),
                ("set", "IF @x < 5 SET @y = 1"),
                ("set-semicolon", "IF @x < 5 SET @y = 1;"),
                ("block", "IF 1 = 1 BEGIN PRINT 'a'; END"),
                ("nested", "IF 1 = 1 IF 2 = 2 PRINT 'a'")
            };
            var suffixes = new (string Name, string Sql)[]
            {
                ("none", ""),
                ("terminator", ";"),
                ("else-print", " ELSE PRINT 'b'"),
                ("else-if", " ELSE IF 2 = 2 PRINT 'b'"),
                ("else-if-else", " ELSE IF 2 = 2 PRINT 'b' ELSE PRINT 'c'"),
                ("else-block", " ELSE BEGIN PRINT 'b'; END"),
                ("else-empty", " ELSE"),
                ("else-else", " ELSE ELSE PRINT 'b'"),
                ("next-select", "; SELECT 3 AS Tail;"),
                ("else-if-next", " ELSE IF 2 = 2 PRINT 'b'; SELECT 3 AS Tail;")
            };
            var report = new StringBuilder();
            report.AppendLine("# Stage 13 original IF/ELSE acceptance");
            report.AppendLine();
            report.AppendLine("| Context | Suffix | Accepted |");
            report.AppendLine("| --- | --- | --- |");
            foreach (var (name, prefix) in contexts)
                foreach (var (suffix, sql) in suffixes)
                {
                    var accepted = ModernMsSqlGrammarExample.ParseBatch(prefix + sql).IsSuccess;
                    report.AppendLine($"| {name} | {suffix} | {(accepted ? "yes" : "no")} |");
                }

            var path = Environment.GetEnvironmentVariable("DSLKIT_SQL_CONFLICT_REPORT");
            if (!string.IsNullOrWhiteSpace(path))
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "sql-if-else-matrix.md"), report.ToString());
            }
            report.ToString().Should().Contain("| print | else-if |");
        }

        private static IEnumerable<NonTerminalNode> Stage13Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var nested in Stage13Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<int> Stage13ElseDepths(ParseTreeNode node, int depth)
        {
            if (node is TerminalNode terminal)
            {
                if (string.Equals(terminal.Token.OriginalString, "ELSE", StringComparison.OrdinalIgnoreCase))
                    yield return depth;
                yield break;
            }
            if (node is NonTerminalNode nonTerminal &&
                nonTerminal.NonTerminal.Name == "IfStatement")
                depth++;
            foreach (var child in node.Children)
                foreach (var nestedDepth in Stage13ElseDepths(child, depth))
                    yield return nestedDepth;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage13_ShouldPreserveIfElseTokensAndStability(SqlStatementTerminationMode mode)
        {
            var cases = new[]
            {
                "IF (1=1) DELETE dbo.T ELSE TRUNCATE TABLE dbo.T",
                "IF @x < 500 SET @c = 'A'; ELSE IF @x < 1000 SET @c = 'B'; ELSE SET @c = 'C'",
                "IF 1 = 1 BEGIN END;"
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
                Stage13FormatterTokens(reparsed.ParseTree!).Should().Equal(
                    Stage13FormatterTokens(original.ParseTree!),
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));

                var reformatted = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                reformatted.IsSuccess.Should().BeTrue(reformatted.ErrorMessage);
                reformatted.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }

        private static IEnumerable<string> Stage13FormatterTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage13FormatterTokens(child))
                    yield return token;
        }
    }
}
