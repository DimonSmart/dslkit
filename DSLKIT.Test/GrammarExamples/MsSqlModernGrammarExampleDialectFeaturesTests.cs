using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.Parser;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Fact]
        public void ParseScript_ShouldParseSqlcmdVariables_AsIdentifiersAndExpressions()
        {
            const string script = """
                IF EXISTS (SELECT [name] FROM [master].[sys].[databases] WHERE [name] = N'$(DatabaseName)')
                    DROP DATABASE $(DatabaseName);
                CREATE DATABASE $(DatabaseName);
                USE $(DatabaseName);
                SELECT $(SQLCMDSERVER) AS ServerName, $(SQLCMDDBNAME) AS DbName;
                IF NOT EXISTS (SELECT 1 FROM dbo.Info) INSERT dbo.Info VALUES ($(DefaultDataPath));
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParseSqlcmdPreprocessorCommands()
        {
            const string script = """
                :r .\setup.sql
                :setvar JobOwner sa
                :on error exit
                PRINT N'after sqlcmd preprocessor commands';
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData(MsSqlDialectFeatures.SqlServerCore, false)]
        [InlineData(MsSqlDialectFeatures.SqlServerCore | MsSqlDialectFeatures.GraphExtensions, false)]
        [InlineData(MsSqlDialectFeatures.SqlServerCore | MsSqlDialectFeatures.SqlCmdPreprocessing, true)]
        [InlineData(MsSqlDialectFeatures.All, true)]
        public void ParseDocument_ShouldRespectSqlcmdFeature_AcrossDialectMatrix(
            MsSqlDialectFeatures dialectFeatures,
            bool shouldSucceed)
        {
            const string script = """
                :setvar JobOwner sa
                PRINT N'after sqlcmd preprocessor commands';
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script, dialectFeatures);

            parseResult.IsSuccess.Should().Be(
                shouldSucceed,
                $"sqlcmd preprocessing support should be {shouldSucceed} for {dialectFeatures}.");
        }

        [Fact]
        public void BuildGrammar_ShouldIgnoreSqlcmdPreprocessing_InGrammarCache()
        {
            var grammarWithSqlcmd = ModernMsSqlGrammarExample.BuildGrammar(MsSqlDialectFeatures.All);
            var grammarWithoutSqlcmd = ModernMsSqlGrammarExample.BuildGrammar(
                MsSqlDialectFeatures.SqlServerCore |
                MsSqlDialectFeatures.ExternalObjects |
                MsSqlDialectFeatures.SynapseExtensions |
                MsSqlDialectFeatures.GraphExtensions);

            grammarWithSqlcmd.Should().BeSameAs(
                grammarWithoutSqlcmd,
                "SqlCmdPreprocessing changes document splitting, but not the batch grammar.");
        }

        [Fact]
        public void BuildGrammar_ShouldBuildConflictInventoryFromParserDiagnostics()
        {
            var grammar = ModernMsSqlGrammarExample.BuildGrammar();
            var inventory = BuildConflictInventory(grammar.ActionAndGotoTable.Conflicts);

            inventory.Should().ContainSingle(item =>
                item.Kind == ParserConflictKind.ShiftReduce &&
                item.TerminalName == "ELSE" &&
                item.Resolution == DSLKIT.Terminals.Resolve.Shift);
            inventory.Should().Contain(item =>
                item.Kind == ParserConflictKind.ShiftReduce &&
                item.Resolution == null);
            inventory.Should().Contain(item => item.Kind == ParserConflictKind.ReduceReduce);
        }

        [Fact]
        public void ParseScript_ShouldRejectSqlcmdPreprocessorCommands_WhenFeatureDisabled()
        {
            const string script = """
                :setvar JobOwner sa
                PRINT N'after sqlcmd preprocessor commands';
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(
                script,
                MsSqlDialectFeatures.SqlServerCore |
                MsSqlDialectFeatures.ExternalObjects |
                MsSqlDialectFeatures.SynapseExtensions |
                MsSqlDialectFeatures.GraphExtensions);

            parseResult.IsSuccess.Should().BeFalse("sqlcmd preprocessing should be disabled when SqlCmdPreprocessing is not enabled.");
        }

        [Fact]
        public void ParseScript_ShouldRejectInlineSqlcmdPreprocessorCommand()
        {
            const string script = "PRINT N'before'; :setvar JobOwner sa";

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeFalse("SQLCMD commands must be recognized only as dedicated-line control commands.");
        }

        [Fact]
        public void ParseDocument_ShouldSplitBatchesAndControlLinesIntoTypedSegments()
        {
            const string script = """
                SELECT 1;
                GO 5
                :setvar JobOwner sa
                PRINT N'after';
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseDocument(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
            parseResult.Document.Should().NotBeNull();
            parseResult.Document!.Segments.Should().HaveCount(4);
            parseResult.Document.Segments[0].Should().BeOfType<SqlBatchDocumentNode>();
            parseResult.Document.Segments[1].Should().BeOfType<SqlBatchSeparatorDocumentNode>();
            parseResult.Document.Segments[2].Should().BeOfType<SqlcmdCommandDocumentNode>();
            parseResult.Document.Segments[3].Should().BeOfType<SqlBatchDocumentNode>();
            ((SqlBatchSeparatorDocumentNode)parseResult.Document.Segments[1]).RepeatCount.Should().Be(5);
        }

        [Fact]
        public void ParseScript_ShouldParseSqlGraphShortestPathPatterns()
        {
            const string script = """
                SELECT *
                FROM PRODUCT P1, PRODUCT P2, ISPARTOF IPO
                WHERE MATCH(P1-(IPO)->P2);

                SELECT
                    STRING_AGG(P2.Name,'->') WITHIN GROUP (GRAPH PATH) AS [Assembly],
                    COUNT(P2.ProductID) WITHIN GROUP (GRAPH PATH) AS Levels
                FROM PRODUCT P1, PRODUCT FOR PATH P2, ISPARTOF FOR PATH IPO
                WHERE MATCH(SHORTEST_PATH(P1(-(IPO)->P2)+))
                  AND P1.ProductID = 2;

                SELECT
                    LAST_VALUE(P2.ProductID) WITHIN GROUP (GRAPH PATH) AS FinalProductID
                FROM PRODUCT P1, PRODUCT FOR PATH P2, ISPARTOF FOR PATH IPO
                WHERE MATCH(SHORTEST_PATH(P1(-(IPO)->P2){1,3}))
                  AND P1.ProductID = 2;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData(MsSqlDialectFeatures.SqlServerCore, false)]
        [InlineData(MsSqlDialectFeatures.SqlServerCore | MsSqlDialectFeatures.GraphExtensions, true)]
        [InlineData(MsSqlDialectFeatures.SqlServerCore | MsSqlDialectFeatures.SqlCmdPreprocessing, false)]
        [InlineData(MsSqlDialectFeatures.All, true)]
        public void ParseScript_ShouldRespectGraphFeature_AcrossDialectMatrix(
            MsSqlDialectFeatures dialectFeatures,
            bool shouldSucceed)
        {
            const string script = """
                SELECT *
                FROM PRODUCT P1, PRODUCT FOR PATH P2, ISPARTOF FOR PATH IPO
                WHERE MATCH(SHORTEST_PATH(P1(-(IPO)->P2)+));
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, dialectFeatures);

            parseResult.IsSuccess.Should().Be(
                shouldSucceed,
                $"graph syntax support should be {shouldSucceed} for {dialectFeatures}.");
        }

        [Fact]
        public void ParseScript_ShouldRejectSqlGraphSyntax_WhenFeatureDisabled()
        {
            const string script = """
                SELECT *
                FROM PRODUCT P1, PRODUCT FOR PATH P2, ISPARTOF FOR PATH IPO
                WHERE MATCH(SHORTEST_PATH(P1(-(IPO)->P2)+));
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(
                script,
                MsSqlDialectFeatures.SqlServerCore |
                MsSqlDialectFeatures.ExternalObjects |
                MsSqlDialectFeatures.SynapseExtensions |
                MsSqlDialectFeatures.SqlCmdPreprocessing);

            parseResult.IsSuccess.Should().BeFalse("graph syntax should be gated behind GraphExtensions.");
        }

        [Theory]
        [InlineData(MsSqlDialectFeatures.SqlServerCore)]
        [InlineData(MsSqlDialectFeatures.SqlServerCore | MsSqlDialectFeatures.GraphExtensions)]
        [InlineData(MsSqlDialectFeatures.SqlServerCore | MsSqlDialectFeatures.SqlCmdPreprocessing)]
        [InlineData(MsSqlDialectFeatures.All)]
        public void ParseScript_ShouldParseCoreQuery_AcrossDialectMatrix(MsSqlDialectFeatures dialectFeatures)
        {
            const string script = """
                SELECT TOP (1) 1 AS Id
                FROM dbo.T
                WHERE 1 = 1
                ORDER BY Id;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, dialectFeatures);

            parseResult.IsSuccess.Should().BeTrue(
                $"core query grammar should remain available for {dialectFeatures}, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldRejectExternalObjects_WhenFeatureDisabled()
        {
            const string script = """
                CREATE EXTERNAL DATA SOURCE MyStorage
                WITH (TYPE = BLOB_STORAGE, LOCATION = 'https://example.com/path');
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, MsSqlDialectFeatures.SqlServerCore);

            parseResult.IsSuccess.Should().BeFalse("external objects should be gated behind ExternalObjects feature.");
        }

        [Fact]
        public void ParseScript_ShouldParseCreateTableAsSelect_WhenSynapseFeaturesEnabled()
        {
            const string script = """
                CREATE TABLE dbo.Stage
                AS
                SELECT 1 AS Id;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, MsSqlDialectFeatures.All);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Fact]
        public void ParseScript_ShouldParsePredictFunction_WhenSynapseFeaturesEnabled()
        {
            const string script = """
                DECLARE @model VARBINARY(MAX);
                DECLARE @input INT = 1;
                SELECT PREDICT(Model = @model, Data = @input AS Score);
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, MsSqlDialectFeatures.All);

            parseResult.IsSuccess.Should().BeTrue(
                $"script should parse, but failed at {parseResult.Error?.ErrorPosition}: {parseResult.Error?.Message}");
        }

        [Theory]
        [InlineData(
            "SELECT PREDICT(WAITFOR = 1);",
            "PREDICT argument names should not accept contextual keywords through broad IdentifierTerm fallback.")]
        [InlineData(
            "SELECT PREDICT(Model = 1 AS WAITFOR);",
            "PREDICT result aliases should not accept contextual keywords through broad IdentifierTerm fallback.")]
        public void ParseScript_ShouldRejectPredictIdentifiers_WithContextualKeywordFallback(string script, string reason)
        {
            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, MsSqlDialectFeatures.All);

            parseResult.IsSuccess.Should().BeFalse(reason);
        }

        [Fact]
        public void ParseScript_ShouldRejectSynapseExtensions_WhenFeatureDisabled()
        {
            const string script = """
                CREATE TABLE dbo.Stage
                AS
                SELECT 1 AS Id;
                """;

            var parseResult = ModernMsSqlGrammarExample.ParseBatch(script, MsSqlDialectFeatures.SqlServerCore);

            parseResult.IsSuccess.Should().BeFalse("CTAS should be gated behind SynapseExtensions feature.");
        }
    }
}