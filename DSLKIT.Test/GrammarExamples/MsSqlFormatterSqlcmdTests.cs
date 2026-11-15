using System;
using System.Linq;
using System.Text.RegularExpressions;
using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData("CREATE DATABASE $(DatabaseName);")]
        [InlineData("USE $(DatabaseName);")]
        [InlineData("DROP DATABASE $(DatabaseName);")]
        [InlineData("SELECT $(SQLCMDSERVER) AS ServerName;")]
        [InlineData("SELECT $(Value) + 1 AS Result;")]
        [InlineData("INSERT INTO dbo.Info (Value) VALUES ($(DefaultValue));")]
        [InlineData("SELECT CASE WHEN $(Flag) = 1 THEN $(Value) ELSE 0 END AS Result;")]
        [InlineData("SELECT t.Id FROM $(SchemaName).$(TableName) AS t;")]
        [InlineData("SELECT $(Value) AS Result;\nGO\n:setvar DatabaseName Demo\nUSE $(DatabaseName);")]
        public void TryFormat_ShouldRoundTripSqlcmdVariables_WithoutChangingTokens(string script)
        {
            var firstParse = ModernMsSqlGrammarExample.ParseDocument(script);
            firstParse.IsSuccess.Should().BeTrue(
                $"initial parse failed at {firstParse.Error?.ErrorPosition}: {firstParse.Error?.Message}");

            var firstFormat = ModernMsSqlFormatter.TryFormat(script);
            firstFormat.IsSuccess.Should().BeTrue(firstFormat.ErrorMessage);
            firstFormat.FormattedSql.Should().NotBeNullOrWhiteSpace();

            var formattedSql = firstFormat.FormattedSql!;
            var secondParse = ModernMsSqlGrammarExample.ParseDocument(formattedSql);
            secondParse.IsSuccess.Should().BeTrue(
                $"formatted parse failed at {secondParse.Error?.ErrorPosition}: {secondParse.Error?.Message}");
            secondParse.Document!.Segments.Count.Should().Be(firstParse.Document!.Segments.Count);

            NormalizeSql(formattedSql).Should().Be(NormalizeSql(script));
            Placeholders(formattedSql).Should().Equal(Placeholders(script));

            var secondFormat = ModernMsSqlFormatter.TryFormat(formattedSql);
            secondFormat.IsSuccess.Should().BeTrue(secondFormat.ErrorMessage);
            secondFormat.FormattedSql.Should().Be(formattedSql);
        }

        private static string[] Placeholders(string sql) =>
            Regex.Matches(sql, @"\$\([a-zA-Z_][a-zA-Z0-9_]*\)")
                .Select(match => match.Value)
                .ToArray();
    }
}