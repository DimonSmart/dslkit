using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Theory]
        [InlineData("SELECT (SELECT 1) AS Value;")]
        [InlineData("SELECT 1 WHERE 1 = (SELECT 1);")]
        [InlineData("SELECT 1 WHERE EXISTS (SELECT 1);")]
        [InlineData("SELECT 1 WHERE 1 IN (SELECT 1);")]
        [InlineData("SELECT 1 UNION (SELECT 2);")]
        [InlineData("(SELECT 1) UNION SELECT 2;")]
        [InlineData("SELECT (SELECT (SELECT 1)) AS Value;")]
        public void ParseScript_Stage24_ShouldAcceptParenthesizedQueryContexts(string sql)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(sql);
            result.IsSuccess.Should().BeTrue(
                $"'{sql}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
        }
    }
}
