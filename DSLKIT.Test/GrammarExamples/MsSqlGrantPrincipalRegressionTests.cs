using System;
using System.Collections.Generic;
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
        [InlineData("GRANT SELECT TO app_role;", "app_role")]
        [InlineData("GRANT SELECT TO [app_role];", "[app_role]")]
        [InlineData("GRANT SELECT TO \"app_role\";", "\"app_role\"")]
        [InlineData("GRANT SELECT TO dbo.app_role;", "dbo.app_role")]
        [InlineData("GRANT SELECT TO PUBLIC;", "PUBLIC")]
        [InlineData("GRANT SELECT TO role1, role2, PUBLIC;", "role1|role2|PUBLIC")]
        [InlineData("GRANT SELECT ON OBJECT::dbo.TableName TO role1;", "role1")]
        [InlineData("GRANT SELECT ON DATABASE::[DatabaseName] TO role1;", "role1")]
        [InlineData("GRANT SELECT TO role1 WITH GRANT OPTION;", "role1")]
        [InlineData("GRANT SELECT TO role1 AS owner_role;", "role1")]
        [InlineData("GRANT SELECT, INSERT ON OBJECT::dbo.TableName TO role1, role2 WITH GRANT OPTION AS dbo.owner_role;", "role1|role2")]
        [InlineData("GRANT SELECT TO $(RoleName);", "$(RoleName)")]
        public void ParseScript_ShouldPreserveGrantPrincipalLists(string sql, string expectedList)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(
                $"script '{sql}' failed at {parsed.Error?.ErrorPosition}: {parsed.Error?.Message}");
            parsed.ParseTree.Should().NotBeNull();

            var statement = GrantStatements(parsed.ParseTree!).Should().ContainSingle().Subject;
            var names = GrantRecipients(statement);
            names.Should().Equal(expectedList.Split('|'));
            var allTokens = GetTerminalNodes(parsed.ParseTree!).Select(node => node.Token.OriginalString).ToArray();
            allTokens.Should().ContainInOrder(expectedList.Split('|').SelectMany(PrincipalTokens));
            if (expectedList.Split('|').Contains("PUBLIC"))
            {
                GetTerminalNodes(statement).Where(node => node.Token.OriginalString == "PUBLIC")
                    .Should().OnlyContain(node => node.Token.Terminal.Name == "PUBLIC");
            }
        }

        [Fact]
        public void ParseScript_ShouldNotMoveGrantPrincipalsBetweenStatements()
        {
            const string sql = "GRANT SELECT TO first_role, PUBLIC; GRANT INSERT ON OBJECT::dbo.T TO dbo.second_role AS owner_role; GRANT UPDATE TO [third_role];";
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().BeTrue(parsed.Error?.Message);
            parsed.ParseTree.Should().NotBeNull();

            var statements = GrantStatements(parsed.ParseTree!).ToArray();
            statements.Should().HaveCount(3);
            GrantRecipients(statements[0]).Should().Equal("first_role", "PUBLIC");
            GrantRecipients(statements[1]).Should().Equal("dbo.second_role");
            GrantRecipients(statements[2]).Should().Equal("[third_role]");
            statements.Select(statement => GetTerminalNodes(statement).First().Token.OriginalString)
                .Should().OnlyContain(token => token == "GRANT");
            GetTerminalNodes(statements[0]).Select(node => node.Token.OriginalString)
                .Should().NotContain("second_role");
            GetTerminalNodes(statements[1]).Select(node => node.Token.OriginalString)
                .Should().NotContain("third_role");
        }

        [Theory]
        [InlineData("GRANT SELECT TO")]
        [InlineData("GRANT SELECT TO ,")]
        [InlineData("GRANT SELECT TO role1,")]
        [InlineData("GRANT SELECT TO role1..")]
        [InlineData("GRANT SELECT TO WAITFOR;")]
        [InlineData("GRANT SELECT TO role1, , role2;")]
        [InlineData("GRANT SELECT ON OBJECT::dbo.T TO;")]
        [InlineData("GRANT SELECT TO role1 WITH GRANT;")]
        public void ParseScript_ShouldRejectMalformedGrantPrincipalLists(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"malformed GRANT was unexpectedly accepted: {sql}");
        }

        private static IEnumerable<NonTerminalNode> GrantStatements(ParseTreeNode root) =>
            DescendantNonTerminals(root).Where(node => node.NonTerminal.Name == "GrantStatement");

        private static string[] GrantRecipients(NonTerminalNode statement)
        {
            var list = DescendantNonTerminals(statement)
                .First(node => node.NonTerminal.Name == "GrantPrincipalList");
            return DescendantNonTerminals(list)
                .Where(node => node.NonTerminal.Name == "GrantPrincipal")
                .Select(node => string.Concat(GetTerminalNodes(node).Select(token => token.Token.OriginalString)))
                .ToArray();
        }

        private static IEnumerable<string> PrincipalTokens(string name)
        {
            if (name.Contains('.') && !name.StartsWith("\"", StringComparison.Ordinal))
            {
                var parts = name.Split('.');
                for (var i = 0; i < parts.Length; i++)
                {
                    if (i > 0) yield return ".";
                    yield return parts[i];
                }
            }
            else yield return name;
        }

        private static IEnumerable<NonTerminalNode> DescendantNonTerminals(ParseTreeNode root)
        {
            if (root is NonTerminalNode node)
            {
                yield return node;
                foreach (var child in node.Children)
                    foreach (var descendant in DescendantNonTerminals(child))
                        yield return descendant;
            }
        }
    }

    public partial class MsSqlFormatterTests
    {
        [Theory]
        [InlineData(SqlStatementTerminationMode.Always)]
        [InlineData(SqlStatementTerminationMode.Never)]
        [InlineData(SqlStatementTerminationMode.ExistingOnly)]
        public void TryFormat_ShouldRoundTripGrantPrincipals(SqlStatementTerminationMode terminationMode)
        {
            var sqlCases = new[]
            {
                "GRANT SELECT TO app_role;",
                "GRANT SELECT TO [app_role], \"quoted_role\", PUBLIC;",
                "GRANT SELECT, INSERT ON OBJECT::dbo.TableName TO dbo.app_role, $(RoleName) WITH GRANT OPTION AS owner_role;",
                "GRANT SELECT ON DATABASE::[DatabaseName] TO /* grant audit */ role1;"
            };

            foreach (var sql in sqlCases)
            {
                var options = new SqlFormattingOptions
                {
                    Statement = new SqlStatementFormattingOptions { TerminateWithSemicolon = terminationMode }
                };
                var original = ModernMsSqlGrammarExample.ParseBatch(sql);
                original.IsSuccess.Should().BeTrue(original.Error?.Message);

                var first = ModernMsSqlFormatter.TryFormat(sql, options);
                first.IsSuccess.Should().BeTrue(first.ErrorMessage);
                first.FormattedSql.Should().NotBeNull();
                var formattedSql = first.FormattedSql!;

                var reparsed = ModernMsSqlGrammarExample.ParseBatch(formattedSql);
                reparsed.IsSuccess.Should().BeTrue(reparsed.Error?.Message);
                reparsed.ParseTree.Should().NotBeNull();

                var initialTokens = GetGrantTokens(original.ParseTree!);
                GetGrantTokens(reparsed.ParseTree!).Should().Equal(initialTokens,
                    (expected, actual) => string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase));
                if (sql.Contains("/* grant audit */", StringComparison.Ordinal))
                    formattedSql.Should().Contain("/* grant audit */");
                if (sql.Contains("PUBLIC", StringComparison.Ordinal))
                    formattedSql.Should().Contain("PUBLIC");
                if (sql.Contains("[app_role]", StringComparison.Ordinal))
                    formattedSql.Should().Contain("[app_role]");
                if (sql.Contains("\"quoted_role\"", StringComparison.Ordinal))
                    formattedSql.Should().Contain("\"quoted_role\"");
                if (sql.Contains("dbo.app_role", StringComparison.Ordinal))
                    formattedSql.Should().Contain("dbo.app_role");

                var second = ModernMsSqlFormatter.TryFormat(formattedSql, options);
                second.IsSuccess.Should().BeTrue(second.ErrorMessage);
                second.FormattedSql.Should().Be(formattedSql);
            }
        }

        private static string[] GetGrantTokens(ParseTreeNode root) =>
            GrantSignificantTokens(root).ToArray();

        private static IEnumerable<string> GrantSignificantTokens(ParseTreeNode node)
        {
            if (node is TerminalNode terminal)
            {
                if (terminal.Token.OriginalString != ";")
                    yield return terminal.Token.OriginalString;
                yield break;
            }

            foreach (var child in node.Children)
                foreach (var token in GrantSignificantTokens(child))
                    yield return token;
        }
    }
}
