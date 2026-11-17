using System;
using System.Collections.Generic;
using System.Linq;
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
        [InlineData("DBCC CHECKDB;")]
        [InlineData("DBCC CHECKDB (AdventureWorks2022, NOINDEX);")]
        [InlineData("DBCC CHECKDB ('AdventureWorks2022') WITH NO_INFOMSGS;")]
        [InlineData("DBCC CHECKDB (0, REPAIR_REBUILD) WITH ALL_ERRORMSGS, MAXDOP = 2;")]
        [InlineData("DBCC DROPCLEANBUFFERS;")]
        [InlineData("DBCC DROPCLEANBUFFERS WITH NO_INFOMSGS;")]
        [InlineData("DBCC TRACESTATUS ();")]
        [InlineData("DBCC TRACESTATUS (3205, -1) WITH NO_INFOMSGS;")]
        [InlineData("DBCC TRACEON (3205);")]
        [InlineData("DBCC TRACEON (3205, 260, -1) WITH NO_INFOMSGS;")]
        [InlineData("DBCC FREEPROCCACHE;")]
        [InlineData("DBCC FREEPROCCACHE (0xABCDEF) WITH NO_INFOMSGS;")]
        [InlineData("DBCC SHRINKFILE (DataFile1, 7);")]
        [InlineData("DBCC SHRINKFILE (1, TRUNCATEONLY);")]
        [InlineData("DBCC SHRINKFILE (DataFile1, 7) WITH NO_INFOMSGS;")]
        public void ParseScript_Stage21_ShouldAcceptDocumentedDbccForms(string sql)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(sql);
            result.IsSuccess.Should().BeTrue(
                $"SQL '{sql}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
            result.ParseTree.Should().NotBeNull();
            Stage21Nodes(result.ParseTree!).Should().Contain(
                node => node.NonTerminal.Name == "DbccStatement");
        }

        [Theory]
        [InlineData("DBCC CHECKDB (0 + 1);")]
        [InlineData("DBCC CHECKDB (dbo.DatabaseName);")]
        [InlineData("DBCC CHECKDB (0, 1);")]
        [InlineData("DBCC CHECKDB (0, NOINDEX, REPAIR_REBUILD);")]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = OFF;")]
        [InlineData("DBCC CHECKDB (0) WITH MAXDOP = '2';")]
        [InlineData("DBCC CHECKDB (0) WITH NO_INFOMSGS = 1;")]
        [InlineData("DBCC DROPCLEANBUFFERS (1);")]
        [InlineData("DBCC TRACESTATUS (0) WITH MAXDOP = 1;")]
        [InlineData("DBCC TRACEON (FlagName);")]
        [InlineData("DBCC TRACEON ();")]
        [InlineData("DBCC FREEPROCCACHE (1, 2);")]
        [InlineData("DBCC SHRINKFILE (DataFile1, 7 + 1);")]
        [InlineData("DBCC SHRINKFILE (DataFile1, EMPTYFILE, 3);")]
        public void ParseScript_Stage21_ShouldRejectUndocumentedDbccForms(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid DBCC syntax was accepted: {sql}");
        }

        [Fact]
        public void ParseScript_Stage21_ShouldKeepDbccStatementsSeparate()
        {
            const string sql =
                "DBCC CHECKDB (AdventureWorks2022, NOINDEX); " +
                "DBCC TRACEON (3205, -1); " +
                "DBCC DROPCLEANBUFFERS;";
            var result = ModernMsSqlGrammarExample.ParseBatch(sql);
            result.IsSuccess.Should().BeTrue(result.Error?.Message);
            result.ParseTree.Should().NotBeNull();
            var statements = Stage21Nodes(result.ParseTree!)
                .Where(node => node.NonTerminal.Name == "DbccStatement").ToArray();
            statements.Should().HaveCount(3);
            Stage21Tokens(statements[0]).Should().Contain("CHECKDB").And.NotContain("TRACEON");
            Stage21Tokens(statements[1]).Should().Contain("TRACEON").And.NotContain("DROPCLEANBUFFERS");
            Stage21Tokens(statements[2]).Should().Contain("DROPCLEANBUFFERS").And.NotContain("CHECKDB");
        }

        private static IEnumerable<NonTerminalNode> Stage21Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var nested in Stage21Nodes(child))
                    yield return nested;
        }

        private static IEnumerable<string> Stage21Tokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                yield return terminal.Token.OriginalString;
                yield break;
            }
            foreach (var child in node.Children)
                foreach (var token in Stage21Tokens(child))
                    yield return token;
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_Stage21_ShouldKeepDbccTokensAndBeIdempotent(SqlStatementTerminationMode mode)
        {
            var sqlSamples = new[]
            {
                "DBCC CHECKDB (AdventureWorks2022, NOINDEX) WITH NO_INFOMSGS;",
                "DBCC TRACESTATUS (3205, -1);",
                "DBCC SHRINKFILE (DataFile1, 7);"
            };
            var options = new SqlFormattingOptions
            {
                Statement = new SqlStatementFormattingOptions { TerminateWithSemicolon = mode }
            };
            foreach (var sql in sqlSamples)
            {
                var formatted = ModernMsSqlFormatter.TryFormat(sql, options);
                formatted.IsSuccess.Should().BeTrue(formatted.ErrorMessage);
                var reparsed = ModernMsSqlGrammarExample.ParseBatch(formatted.FormattedSql!);
                reparsed.IsSuccess.Should().BeTrue(reparsed.Error?.Message);
                var again = ModernMsSqlFormatter.TryFormat(formatted.FormattedSql!, options);
                again.IsSuccess.Should().BeTrue(again.ErrorMessage);
                again.FormattedSql.Should().Be(formatted.FormattedSql);
            }
        }
    }
}
