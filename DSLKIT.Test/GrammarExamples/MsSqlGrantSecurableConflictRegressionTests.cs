using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using DSLKIT.Parser;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        [Theory]
        [InlineData("GRANT SELECT ON dbo.T TO app_role;")]
        [InlineData("GRANT SELECT ON T TO app_role;")]
        [InlineData("GRANT SELECT ON OBJECT::dbo.T TO app_role;")]
        [InlineData("GRANT SELECT ON OBJECT::T TO app_role;")]
        [InlineData("GRANT SELECT ON SCHEMA::dbo TO app_role;")]
        [InlineData("GRANT SELECT ON DATABASE::[AdventureWorks] TO app_role;")]
        [InlineData("GRANT SELECT ON [dbo].[T] TO [app_role];")]
        [InlineData("GRANT SELECT ON \"dbo\".\"T\" TO \"app_role\";")]
        [InlineData("GRANT SELECT ON dbo.T TO role1, role2 WITH GRANT OPTION AS dbo.owner_role;")]
        public void ParseScript_Stage22_ShouldAcceptGrantSecurableNames(string sql)
        {
            var result = ModernMsSqlGrammarExample.ParseBatch(sql);
            result.IsSuccess.Should().BeTrue(
                $"'{sql}' failed at {result.Error?.ErrorPosition}: {result.Error?.Message}");
            result.ParseTree.Should().NotBeNull();
            Stage22Nodes(result.ParseTree!)
                .Count(node => node.NonTerminal.Name == "GrantSecurable").Should().Be(1);
        }

        [Theory]
        [InlineData("GRANT SELECT ON TO app_role;")]
        [InlineData("GRANT SELECT ON dbo. TO app_role;")]
        [InlineData("GRANT SELECT ON .T TO app_role;")]
        [InlineData("GRANT SELECT ON dbo.. TO app_role;")]
        [InlineData("GRANT SELECT ON dbo.T. TO app_role;")]
        [InlineData("GRANT SELECT ON dbo.T TO;")]
        [InlineData("GRANT SELECT ON dbo.T TO role1,;")]
        public void ParseScript_Stage22_ShouldRejectMalformedGrantSecurables(string sql)
        {
            ModernMsSqlGrammarExample.ParseBatch(sql).IsSuccess.Should().BeFalse(
                $"malformed GRANT securable was accepted: {sql}");
        }

        private static System.Collections.Generic.IEnumerable<NonTerminalNode> Stage22Nodes(ParseTreeNode node)
        {
            if (node is not NonTerminalNode nonTerminal)
                yield break;
            yield return nonTerminal;
            foreach (var child in nonTerminal.Children)
                foreach (var nested in Stage22Nodes(child))
                    yield return nested;
        }
    }
}
