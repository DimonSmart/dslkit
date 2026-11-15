using System;
using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Theory]
        [InlineData("CREATE TABLE dbo.Example (A INT DEFAULT 0, B INT DEFAULT (0), C INT DEFAULT ((0)));")]
        [InlineData("CREATE TABLE dbo.Example (A INT CONSTRAINT DF_Example_A DEFAULT (0));")]
        [InlineData("CREATE TABLE dbo.Example (CreatedAt DATETIME DEFAULT (GETDATE()), Value INT DEFAULT (1 + 2));")]
        [InlineData("CREATE TABLE dbo.Example (A INT NULL DEFAULT (0), B INT NOT NULL DEFAULT ((1)), C INT DEFAULT 2 NOT NULL);")]
        [InlineData("CREATE TABLE dbo.Example (A INT NULL, B INT NOT NULL, C NVARCHAR(40));")]
        [InlineData("ALTER TABLE dbo.Example ADD A INT DEFAULT (0);")]
        [InlineData("ALTER TABLE dbo.Example ADD A INT CONSTRAINT DF_Example_A DEFAULT ((0));")]
        [InlineData("ALTER TABLE dbo.Example ADD A INT NOT NULL DEFAULT (1 + 2);")]
        [InlineData("ALTER TABLE dbo.Example ADD A INT NULL;")]
        public void ParseScript_ShouldAcceptSupportedColumnDefaultAndRelatedDdl(string script)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(script);

            result.IsSuccess.Should().BeTrue(
                $"script '{script}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
            result.ParseTree.Should().NotBeNull();
        }

        [Theory]
        [InlineData("CREATE TABLE dbo.Example (A INT DEFAULT ());")]
        [InlineData("CREATE TABLE dbo.Example (A INT DEFAULT (1);")]
        [InlineData("CREATE TABLE dbo.Example (A INT DEFAULT);")]
        [InlineData("CREATE TABLE dbo.Example (A INT CONSTRAINT DF_Example_A DEFAULT ());")]
        [InlineData("CREATE TABLE dbo.Example (A INT CONSTRAINT DF_Example_A DEFAULT);")]
        [InlineData("ALTER TABLE dbo.Example ADD A INT DEFAULT ();")]
        [InlineData("ALTER TABLE dbo.Example ADD A INT DEFAULT;")]
        public void ParseScript_ShouldRejectMalformedColumnDefaults(string script)
        {
            ModernMsSqlGrammarExample.ParseBatch(script).IsSuccess.Should().BeFalse(
                $"malformed DEFAULT must be rejected: {script}");
        }

        [Theory]
        [InlineData("CREATE TABLE dbo.Example (A INT DEFAULT (0));", "CreateTableColumnOption")]
        [InlineData("CREATE TABLE dbo.Example (A INT CONSTRAINT DF_Example_A DEFAULT (0));", "CreateTableColumnConstraintBody")]
        public void ParseScript_ShouldKeepDefaultTokensInTheirColumnConstraintContext(
            string script, string expectedContext)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(script);
            result.IsSuccess.Should().BeTrue(
                $"script '{script}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
            result.ParseTree.Should().NotBeNull();

            var tokens = GetTerminalNodes(result.ParseTree!);
            tokens.Count(node => node.Token.OriginalString == "DEFAULT").Should().Be(1);
            tokens.Count(node => node.Token.OriginalString == "(").Should().Be(2);
            tokens.Count(node => node.Token.OriginalString == ")").Should().Be(2);
            tokens.Count(node => node.Token.OriginalString == "0").Should().Be(1);
            tokens.Should().Contain(node => node.Token.OriginalString == "A");
            if (expectedContext == "CreateTableColumnConstraintBody")
            {
                tokens.Should().Contain(node => node.Token.OriginalString == "DF_Example_A");
            }

            FindTerminalPaths(result.ParseTree!, "DEFAULT").Single().Should().Contain(expectedContext);
            var valuePath = FindTerminalPaths(result.ParseTree!, "0").Single();
            valuePath.Should().Contain(expectedContext);
            valuePath.Should().Contain("Expression");
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData("CREATE TABLE dbo.Example (A INT DEFAULT 0, B INT DEFAULT (0), C INT DEFAULT ((0)));")]
        [InlineData("CREATE TABLE dbo.Example (A INT CONSTRAINT DF_Example_A DEFAULT (0));")]
        [InlineData("CREATE TABLE dbo.Example (CreatedAt DATETIME DEFAULT (GETDATE()), Value INT DEFAULT (1 + 2));")]
        [InlineData("CREATE TABLE dbo.Example (A INT NULL DEFAULT (0), B INT NOT NULL DEFAULT ((1)));")]
        [InlineData("CREATE TABLE dbo.Example (A INT NOT NULL, B NVARCHAR(40) NULL);")]
        [InlineData("ALTER TABLE dbo.Example ADD A INT DEFAULT ((0));")]
        [InlineData("ALTER TABLE dbo.Example ADD A INT CONSTRAINT DF_Example_A DEFAULT (0);")]
        public void TryFormat_ShouldRoundTripColumnDefaults_WithoutLosingSignificantTokens(string script)
        {
            var firstParse = ModernMsSqlGrammarExample.ParseBatch(script);
            firstParse.IsSuccess.Should().BeTrue(
                $"initial parse failed at {firstParse.Error?.ErrorPosition}: {firstParse.Error?.Message}");

            var firstFormat = ModernMsSqlFormatter.TryFormat(script);
            firstFormat.IsSuccess.Should().BeTrue(firstFormat.ErrorMessage);
            firstFormat.FormattedSql.Should().NotBeNullOrWhiteSpace();

            var formatted = firstFormat.FormattedSql!;
            NormalizeSql(formatted).Should().Be(NormalizeSql(script));

            var secondParse = ModernMsSqlGrammarExample.ParseBatch(formatted);
            secondParse.IsSuccess.Should().BeTrue(
                $"formatted parse failed at {secondParse.Error?.ErrorPosition}: {secondParse.Error?.Message}");

            var secondFormat = ModernMsSqlFormatter.TryFormat(formatted);
            secondFormat.IsSuccess.Should().BeTrue(secondFormat.ErrorMessage);
            secondFormat.FormattedSql.Should().Be(formatted);
        }
    }
}
