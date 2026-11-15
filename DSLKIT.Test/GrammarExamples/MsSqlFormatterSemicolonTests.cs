using System;
using System.Linq;
using System.Text.RegularExpressions;
using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.GrammarExamples.MsSql.Formatting;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public class MsSqlFormatterSemicolonTests
    {
        [Fact]
        public void Never_ShouldPreserveSemicolonBeforeLeadingCte()
        {
            const string source = "SELECT 1; WITH seed AS (SELECT 2 AS x) SELECT x FROM seed;";

            var formatted = Format(source, SqlStatementTerminationMode.Never);

            Regex.IsMatch(formatted, @";\s*WITH seed", RegexOptions.IgnoreCase).Should().BeTrue();
            formatted.TrimEnd().Should().NotEndWith(";");
            ModernMsSqlGrammarExample.ParseBatch(formatted).IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Never_ShouldPreserveMandatoryMergeTerminator()
        {
            const string source = """
                MERGE dbo.Target AS tgt
                USING dbo.Source AS src
                ON tgt.Id = src.Id
                WHEN MATCHED THEN DELETE;
                """;

            var formatted = Format(source, SqlStatementTerminationMode.Never);

            formatted.TrimEnd().Should().EndWith(";");
            ModernMsSqlGrammarExample.ParseBatch(formatted).IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Never_ShouldRemoveOptionalSeparatorsWithoutChangingStatementCount()
        {
            const string source = "SELECT 1; SELECT 2;";

            var formatted = Format(source, SqlStatementTerminationMode.Never);

            formatted.Should().NotContain(";");
            ModernMsSqlGrammarExample.ParseBatch(formatted).IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Always_ShouldTerminateImplicitStatementsIndividually()
        {
            const string source = """
                DECLARE @policy_id INT
                EXEC msdb.dbo.sp_syspolicy_add_policy @name=N'Policy', @enabled=True, @policy_id=@policy_id OUTPUT
                SELECT @policy_id;
                """;

            var formatted = Format(source, SqlStatementTerminationMode.Always);

            formatted.Should().Contain("DECLARE @policy_id INT;");
            formatted.Should().Contain("OUTPUT;");
            formatted.TrimEnd().Should().EndWith(";");
            formatted.Count(c => c == ';').Should().Be(3);
            ModernMsSqlGrammarExample.ParseBatch(formatted).IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Always_ShouldNotDuplicateExistingSeparators()
        {
            var formatted = Format("SELECT 1; SELECT 2;", SqlStatementTerminationMode.Always);

            formatted.Count(c => c == ';').Should().Be(2);
            ModernMsSqlGrammarExample.ParseBatch(formatted).IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Always_ShouldNotTerminateNestedCteQuery()
        {
            const string source = "SELECT 1; WITH seed AS (SELECT 2 AS x) SELECT x FROM seed";

            var formatted = Format(source, SqlStatementTerminationMode.Always);

            formatted.Count(c => c == ';').Should().Be(2);
            Regex.IsMatch(formatted, @";\s*WITH seed", RegexOptions.IgnoreCase).Should().BeTrue();
            ModernMsSqlGrammarExample.ParseBatch(formatted).IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Always_ShouldKeepIfElseAssociation()
        {
            const string source = "IF 1 = 1 SELECT 1 ELSE SELECT 2";

            var formatted = Format(source, SqlStatementTerminationMode.Always);

            formatted.Should().Contain("ELSE");
            ModernMsSqlGrammarExample.ParseBatch(formatted).IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        [InlineData(SqlStatementTerminationMode.Always)]
        public void Formatting_ShouldRemainStableAcrossRepeatedFormatting(SqlStatementTerminationMode mode)
        {
            const string source = "SELECT 1; WITH seed AS (SELECT 2 AS x) SELECT x FROM seed;";

            var first = Format(source, mode);
            var second = Format(first, mode);

            second.Should().Be(first);
        }

        [Fact]
        public void ExistingOnly_ShouldPreserveOriginalSeparatorCount()
        {
            var formatted = Format("SELECT 1; SELECT 2", SqlStatementTerminationMode.ExistingOnly);

            formatted.Count(c => c == ';').Should().Be(1);
            ModernMsSqlGrammarExample.ParseBatch(formatted).IsSuccess.Should().BeTrue();
        }

        private static string Format(string sql, SqlStatementTerminationMode mode)
        {
            var result = ModernMsSqlFormatter.TryFormat(sql, new SqlFormattingOptions
            {
                Statement = new SqlStatementFormattingOptions
                {
                    TerminateWithSemicolon = mode
                }
            });

            result.IsSuccess.Should().BeTrue(result.ErrorMessage);
            return result.FormattedSql!;
        }
    }
}
