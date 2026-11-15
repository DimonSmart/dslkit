using System;
using System.Linq;
using System.Collections.Generic;
using DSLKIT.Parser;
using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.GrammarExamples.MsSql.Formatting;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Theory]
        [InlineData("CREATE PROC dbo.p AS SELECT 101")]
        [InlineData("CREATE PROCEDURE dbo.p AS SELECT 101; SELECT 202")]
        [InlineData("ALTER PROCEDURE dbo.p AS BEGIN SELECT 101; SELECT 202; END;")]
        [InlineData("CREATE OR ALTER PROCEDURE dbo.p AS SELECT 101; SELECT 202;")]
        [InlineData("CREATE PROC dbo.p AS ; SELECT 101;; SELECT 202;;")]
        [InlineData("CREATE PROC dbo.p AS DECLARE @x INT SET @x = 1 SELECT @x")]
        [InlineData("CREATE PROC dbo.p AS BEGIN DECLARE @x INT; SET @x = 1; SELECT @x; END;")]
        [InlineData("CREATE PROC dbo.p AS BEGIN IF 1 = 1 SELECT 101 ELSE SELECT 202 END;")]
        [InlineData("CREATE PROC dbo.p AS BEGIN BEGIN SELECT 101 END; SELECT 202 END;")]
        [InlineData("CREATE PROCEDURE dbo.p AS EXTERNAL NAME MyAssembly.MyNamespace.MyType.MyMethod;")]
        [InlineData("CREATE TRIGGER dbo.t ON dbo.T AFTER INSERT AS BEGIN SELECT 101; SELECT 202 END;")]
        public void ParseScript_ShouldKeepSupportedProcedureAndTriggerBodies(string sql)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(sql);
            result.IsSuccess.Should().BeTrue(
                $"input '{sql}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
            result.ParseTree.Should().NotBeNull();
        }

        [Fact]
        public void ParseScript_ShouldKeepThreeUnwrappedStatementsInsideProcedure()
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(
                "CREATE PROC dbo.p AS SELECT 101; SELECT 202; SELECT 303;");
            result.IsSuccess.Should().BeTrue(result.Error?.Message);
            result.ParseTree.Should().NotBeNull();

            foreach (var value in new[] { "101", "202", "303" })
            {
                var path = FindTerminalPaths(result.ParseTree!, value).Single();
                path.Should().Contain("CreateProcBodyBlock");
                path.Should().Contain("StatementList");
            }

            FindTerminalPaths(result.ParseTree!, "CREATE").Should().HaveCount(1);
        }

        [Fact]
        public void ParseDocument_ShouldKeepGoBatchOutsideProcedure()
        {
            const string sql = "CREATE PROC dbo.p AS SELECT 101; SELECT 202;\nGO\nSELECT 303;";
            var result = ModernMsSqlGrammarExample.ParseDocument(sql);
            result.IsSuccess.Should().BeTrue(result.Error?.Message);
            result.Document.Should().NotBeNull();
        }

        [Theory]
        [InlineData("CREATE PROC dbo.p AS BEGIN SELECT 1")]
        [InlineData("CREATE PROC dbo.p AS SELECT")]
        [InlineData("CREATE PROC dbo.p AS ;;;")]
        [InlineData("CREATE TRIGGER dbo.t ON dbo.T AFTER INSERT AS BEGIN SELECT 1")]
        public void ParseScript_ShouldRejectMalformedProcedureAndTriggerBodies(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse();
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always, SqlCreateProcLayout.Expanded)]
        [InlineData(SqlStatementTerminationMode.Never, SqlCreateProcLayout.Expanded)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly, SqlCreateProcLayout.Expanded)]
        [InlineData(SqlStatementTerminationMode.Always, SqlCreateProcLayout.Compact)]
        [InlineData(SqlStatementTerminationMode.Never, SqlCreateProcLayout.Compact)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly, SqlCreateProcLayout.Compact)]
        public void TryFormat_ShouldRoundTripProcedureBodyAndComments(
            SqlStatementTerminationMode termination, SqlCreateProcLayout layout)
        {
            const string sql = """
                CREATE PROCEDURE dbo.p AS
                BEGIN
                    -- audit comment
                    SELECT 101;
                    SELECT 202;
                END;
                """;

            var options = new SqlFormattingOptions
            {
                Statement = new SqlStatementFormattingOptions { TerminateWithSemicolon = termination },
                Ddl = new SqlDdlFormattingOptions { CreateProcLayout = layout }
            };

            var before = ModernMsSqlGrammarExample.ParseBatch(sql);
            before.IsSuccess.Should().BeTrue(before.Error?.Message);

            var first = ModernMsSqlFormatter.TryFormat(sql, options);
            first.IsSuccess.Should().BeTrue(first.ErrorMessage);
            first.FormattedSql.Should().Contain("-- audit comment");

            var reparsed = ModernMsSqlGrammarExample.ParseBatch(first.FormattedSql!);
            reparsed.IsSuccess.Should().BeTrue(reparsed.Error?.Message);
            reparsed.ParseTree.Should().NotBeNull();

            var bodyTokens = ProcBodyTokens(reparsed.ParseTree!).ToArray();
            bodyTokens.Should().Contain("101").And.Contain("202");

            var tokensBefore = AllProcTokens(before.ParseTree!).Where(value => value != ";");
            var tokensAfter = AllProcTokens(reparsed.ParseTree!).Where(value => value != ";");
            tokensAfter.Should().Equal(tokensBefore);

            var second = ModernMsSqlFormatter.TryFormat(first.FormattedSql!, options);
            second.IsSuccess.Should().BeTrue(second.ErrorMessage);
            second.FormattedSql.Should().Be(first.FormattedSql);
        }

        private static IEnumerable<string> ProcBodyTokens(ParseTreeNode node)
        {
            if (node is NonTerminalNode nonTerminal &&
                nonTerminal.NonTerminal.Name == "CreateProcBodyBlock")
            {
                return AllProcTokens(node);
            }

            return node.Children.SelectMany(ProcBodyTokens);
        }

        private static IEnumerable<string> AllProcTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                return [terminal.Token.OriginalString];
            }

            return node.Children.SelectMany(AllProcTokens);
        }
    }
}
