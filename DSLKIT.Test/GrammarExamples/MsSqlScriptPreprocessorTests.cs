using System;
using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class MsSqlScriptPreprocessorTests
    {
        [Theory]
        [InlineData("/*\nGO\n*/\nSELECT 1;\n")]
        [InlineData("/* comment\nGO 3\n:setvar Owner sa\n*/\nSELECT 1;\n")]
        [InlineData("SELECT 'before\nGO\nafter' AS Value;\n")]
        [InlineData("SELECT N'before\n:setvar Owner sa\nafter' AS Value;\n")]
        [InlineData("SELECT 'before\nGO\nit''s after' AS Value;\n")]
        public void ParseDocument_ShouldNotSplitGoOrSqlcmdInsideMultilineSqlConstructs(string sql)
        {
            var result = ModernMsSqlGrammarExample.ParseDocument(sql);

            result.IsSuccess.Should().BeTrue(
                $"SQL should parse without splitting inside a comment or string: {result.Error?.Message}");
            var batch = result.Document!.Segments.Should().ContainSingle()
                .Which.Should().BeOfType<SqlBatchDocumentNode>().Subject;
            batch.Text.Should().Be(sql);
            batch.StartPosition.Should().Be(0);
        }

        [Fact]
        public void ParseDocument_ShouldRecognizeGoWithInlineCommentsAndRepeatCount()
        {
            const string firstBatch = "SELECT 1;\n";
            const string separator = "/* before */ GO 3 /* after */ -- repeat\n";
            const string secondBatch = "SELECT 2;\n";

            var result = ModernMsSqlGrammarExample.ParseDocument(firstBatch + separator + secondBatch);

            result.IsSuccess.Should().BeTrue($"SQL should parse: {result.Error?.Message}");
            result.Document!.Segments.Should().HaveCount(3);
            result.Document.Segments[0].Should().BeOfType<SqlBatchDocumentNode>()
                .Which.Text.Should().Be(firstBatch);
            var go = result.Document.Segments[1].Should()
                .BeOfType<SqlBatchSeparatorDocumentNode>().Subject;
            go.Text.Should().Be(separator.TrimEnd('\n'));
            go.StartPosition.Should().Be(firstBatch.Length);
            go.RepeatCount.Should().Be(3);
            result.Document.Segments[2].Should().BeOfType<SqlBatchDocumentNode>()
                .Which.Text.Should().Be(secondBatch);
        }

        [Fact]
        public void ParseDocument_ShouldKeepSqlcmdCommandsSeparateFromSqlBatches()
        {
            const string sql = ":setvar Owner 'unclosed quote\nSELECT 1;\nGO\n:r .\\setup.sql\n";

            var result = ModernMsSqlGrammarExample.ParseDocument(sql);

            result.IsSuccess.Should().BeTrue($"SQL should parse: {result.Error?.Message}");
            result.Document!.Segments.Should().HaveCount(4);
            result.Document.Segments[0].Should().BeOfType<SqlcmdCommandDocumentNode>();
            result.Document.Segments[1].Should().BeOfType<SqlBatchDocumentNode>();
            result.Document.Segments[2].Should().BeOfType<SqlBatchSeparatorDocumentNode>();
            result.Document.Segments[3].Should().BeOfType<SqlcmdCommandDocumentNode>();
        }

        [Fact]
        public void ParseDocument_ShouldNotThrowForOutOfRangeGoRepeatCount()
        {
            const string sql = "SELECT 1;\nGO 9999999999999999999999999999\n";

            Action parse = () => ModernMsSqlGrammarExample.ParseDocument(sql);

            parse.Should().NotThrow<OverflowException>();
        }
    }
}
