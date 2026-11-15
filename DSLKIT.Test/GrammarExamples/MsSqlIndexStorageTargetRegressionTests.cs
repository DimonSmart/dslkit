using System;
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
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON [PRIMARY];")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON [default];")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON \"default\";")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON DEFAULT;")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON FG_Archive;")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON dbo.FG_Archive;")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON [FG Archive];")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON \"FG Archive\";")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON PartScheme(Id);")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON dbo.PartScheme(Id);")]
        [InlineData("CREATE TABLE dbo.T (Id INT) ON [PRIMARY];")]
        [InlineData("CREATE TABLE dbo.T (Id INT) ON [default];")]
        [InlineData("CREATE TABLE dbo.T (Id INT) ON \"default\";")]
        [InlineData("CREATE TABLE dbo.T (Id INT) ON DEFAULT;")]
        [InlineData("CREATE TABLE dbo.T (Id INT, Data NVARCHAR(MAX)) ON [PRIMARY] TEXTIMAGE_ON [default];")]
        [InlineData("CREATE TABLE dbo.T (Id INT, CONSTRAINT PK_T PRIMARY KEY CLUSTERED (Id) ON [PRIMARY]);")]
        [InlineData("CREATE TABLE dbo.T (Id INT, CONSTRAINT UQ_T UNIQUE NONCLUSTERED (Id) WITH (FILLFACTOR = 90) ON [default]);")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (FILLFACTOR = 90) ON [default] FILESTREAM_ON [PRIMARY];")]
        [InlineData("CREATE TABLE dbo.T (Id INT) ON [PRIMARY] FILESTREAM_ON [default];")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) FILESTREAM_ON dbo.PartScheme(Id);")]
        public void ParseScript_ShouldKeepSupportedIndexStorageTargets(string sql)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"script '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();
        }

        [Theory]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON [PRIMARY];", "[PRIMARY]", "CreateIndexStorageClause")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON [default];", "[default]", "CreateIndexStorageClause")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON \"default\";", "\"default\"", "CreateIndexStorageClause")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON DEFAULT;", "DEFAULT", "CreateIndexStorageClause")]
        [InlineData("CREATE TABLE dbo.T (Id INT) ON [default];", "[default]", "CreateTableOnClause")]
        [InlineData("CREATE TABLE dbo.T (Id INT, Data NVARCHAR(MAX)) ON [PRIMARY] TEXTIMAGE_ON [default];", "[default]", "CreateTableTextImageClause")]
        public void ParseScript_ShouldPlaceStorageTargetInsideCorrectClause(string sql, string token, string clause)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var paths = FindTerminalPaths(parsed.ParseTree!, token);
            paths.Should().ContainSingle();
            paths.Single().Should().Contain("IndexStorageTarget");
            paths.Single().Should().Contain(clause);
        }

        [Fact]
        public void ParseScript_ShouldKeepStorageTargetsInsideTheirOwnStatements()
        {
            const string sql = "CREATE INDEX IX_T ON dbo.T (Id) ON [default]; CREATE TABLE dbo.U (Id INT) ON [PRIMARY]; CREATE INDEX IX_U ON dbo.U (Id) ON PartScheme(Id);";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            FindTerminalPaths(parsed.ParseTree!, "CREATE").Should().HaveCount(3);
            FindTerminalPaths(parsed.ParseTree!, "[default]").Single().Should().Contain("CreateIndexStatement");
            FindTerminalPaths(parsed.ParseTree!, "[PRIMARY]").Single().Should().Contain("CreateTableStatement");
            FindTerminalPaths(parsed.ParseTree!, "PartScheme").Single().Should().Contain("CreateIndexStatement");
        }

        [Theory]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON;")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON PartScheme();")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON PartScheme(Id, Name);")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON PartScheme(Id;")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) WITH (FILLFACTOR = 90) ON [PRIMARY] WITH (ONLINE = ON);")]
        [InlineData("CREATE INDEX IX_T ON dbo.T (Id) ON [PRIMARY] ON [default];")]
        [InlineData("CREATE TABLE dbo.T (Id INT) ON;")]
        [InlineData("CREATE TABLE dbo.T (Id INT) TEXTIMAGE_ON;")]
        [InlineData("CREATE TABLE dbo.T (Id INT) TEXTIMAGE_ON [default] ON [PRIMARY];")]
        [InlineData("CREATE TABLE dbo.T (Id INT) ON [PRIMARY] TEXTIMAGE_ON [default] ON [PRIMARY];")]
        public void ParseScript_ShouldRejectMalformedIndexStorageTargets(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"malformed storage clause unexpectedly accepted: {sql}");
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_ShouldRoundTripStorageClausesWithAllTerminationModes(SqlStatementTerminationMode termination)
        {
            const string sql = "CREATE INDEX IX_T ON dbo.T (Id) WITH (FILLFACTOR = 90) ON [default] FILESTREAM_ON [PRIMARY]; CREATE TABLE dbo.U (Id INT, Data NVARCHAR(MAX)) ON [PRIMARY] TEXTIMAGE_ON \"default\";";
            var options = new SqlFormattingOptions
            {
                Statement = new SqlStatementFormattingOptions { TerminateWithSemicolon = termination }
            };
            var original = ModernMsSqlGrammarExample.ParseBatch(sql);
            original.IsSuccess.Should().BeTrue(original.Error?.Message);

            var first = ModernMsSqlFormatter.TryFormat(sql, options);
            first.IsSuccess.Should().BeTrue(first.ErrorMessage);
            first.FormattedSql.Should().Contain("[default]").And.Contain("\"default\"");

            var parsed = ModernMsSqlGrammarExample.ParseBatch(first.FormattedSql!);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();
            var originalTokens = StorageSignificantTokens(original.ParseTree!).ToArray();
            var formattedTokens = StorageSignificantTokens(parsed.ParseTree!).ToArray();
            formattedTokens.Select(s => s.ToUpperInvariant())
                .Should().Equal(originalTokens.Select(s => s.ToUpperInvariant()));

            var second = ModernMsSqlFormatter.TryFormat(first.FormattedSql!, options);
            second.IsSuccess.Should().BeTrue(second.ErrorMessage);
            second.FormattedSql.Should().Be(first.FormattedSql);
        }

        private static System.Collections.Generic.IEnumerable<string> StorageSignificantTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }

            foreach (var child in node.Children)
                foreach (var token in StorageSignificantTokens(child))
                    yield return token;
        }
    }
}
