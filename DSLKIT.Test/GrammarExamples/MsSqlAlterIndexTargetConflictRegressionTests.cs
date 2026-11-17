using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Theory]
        [InlineData("ALTER INDEX IX_T ON dbo.T REBUILD;")]
        [InlineData("ALTER INDEX [IX T] ON [dbo].[T] DISABLE;")]
        [InlineData("ALTER INDEX ALL ON dbo.T REORGANIZE;")]
        [InlineData("ALTER INDEX NAME ON dbo.T REBUILD;")]
        [InlineData("ALTER INDEX IX_T ON T SET (ALLOW_ROW_LOCKS = ON);")]
        public void ParseScript_Stage23_ShouldAcceptSinglePartAlterIndexTargets(string sql)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(sql);
            result.IsSuccess.Should().BeTrue(
                $"'{sql}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
        }

        [Theory]
        [InlineData("ALTER INDEX dbo.IX_T ON dbo.T REBUILD;")]
        [InlineData("ALTER INDEX db.dbo.IX_T ON dbo.T REBUILD;")]
        [InlineData("ALTER INDEX IX_T. ON dbo.T REBUILD;")]
        [InlineData("ALTER INDEX ON dbo.T REBUILD;")]
        public void ParseScript_Stage23_ShouldRejectQualifiedOrMissingAlterIndexTargets(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"invalid ALTER INDEX target was accepted: {sql}");
        }
    }
}
