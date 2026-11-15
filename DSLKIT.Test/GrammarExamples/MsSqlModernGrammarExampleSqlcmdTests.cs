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
        [InlineData("CREATE DATABASE $(DatabaseName);")]
        [InlineData("USE $(DatabaseName);")]
        [InlineData("DROP DATABASE $(DatabaseName);")]
        [InlineData("SELECT $(SQLCMDSERVER) AS ServerName;")]
        [InlineData("SELECT $(Value) + 1 AS Result;")]
        [InlineData("INSERT INTO dbo.Info (Value) VALUES ($(DefaultValue));")]
        [InlineData("SELECT CASE WHEN $(Flag) = 1 THEN $(Value) ELSE 0 END AS Result;")]
        [InlineData("SELECT t.Id FROM $(SchemaName).$(TableName) AS t;")]
        public void ParseScript_ShouldAcceptSqlcmdVariables_InSupportedContexts(string script)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(script);

            result.IsSuccess.Should().BeTrue(
                $"script '{script}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
            result.ParseTree.Should().NotBeNull();

            var placeholders = GetTerminalNodes(result.ParseTree!)
                .Where(node => node.Token.OriginalString.StartsWith("$(", StringComparison.Ordinal))
                .ToArray();
            placeholders.Should().NotBeEmpty();
            placeholders.Should().OnlyContain(node => node.Token.Terminal.Name == "SqlcmdVariable");
        }

        [Theory]
        [InlineData("SELECT $();")]
        [InlineData("SELECT $(9Invalid);")]
        [InlineData("SELECT $(Invalid-Name);")]
        public void ParseScript_ShouldRejectMalformedSqlcmdVariables(string script)
        {
            ModernMsSqlGrammarExample.ParseBatch(script).IsSuccess.Should().BeFalse();
        }

        [Theory]
        [InlineData("SELECT $(Value) + 1 AS Result;", "$(Value)", true)]
        [InlineData("SELECT $(SQLCMDSERVER) AS ServerName;", "$(SQLCMDSERVER)", true)]
        [InlineData("CREATE DATABASE $(DatabaseName);", "$(DatabaseName)", false)]
        [InlineData("SELECT t.Id FROM $(SchemaName).$(TableName) AS t;", "$(SchemaName)", false)]
        [InlineData("SELECT t.Id FROM $(SchemaName).$(TableName) AS t;", "$(TableName)", false)]
        public void ParseScript_ShouldKeepSqlcmdVariableInCorrectTreeContext(
            string script,
            string placeholder,
            bool isExpression)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(script);

            result.IsSuccess.Should().BeTrue(
                $"script '{script}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
            result.ParseTree.Should().NotBeNull();

            var token = GetTerminalNodes(result.ParseTree!)
                .Single(node => node.Token.OriginalString == placeholder);
            token.Token.Terminal.Name.Should().Be("SqlcmdVariable");
            token.Token.OriginalString.Should().Be(placeholder);

            var path = FindTerminalPaths(result.ParseTree!, placeholder).Single();
            path.Should().Contain("StrictIdentifierTerm");
            path.Contains("PrimaryExpression").Should().Be(isExpression);
        }
    }
}