using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.GrammarExamples.MsSql;
using FluentAssertions;
using Xunit;

namespace DSLKIT.Test.GrammarExamples
{
    public partial class MsSqlModernGrammarExampleTests
    {
        // Characterized in the successful pre-change Stage 6 CI (run 43711441350).
        // Bare keyword acceptance is context-specific; brackets and quoted identifiers
        // are accepted in all nine characterized positions.
        public static IEnumerable<object[]> Stage6KeywordAcceptanceCases()
        {
            var contexts = new (string Sql, string AllowedBare)[]
            {
                ("DELETE FROM {0};", "NAME,SOURCE,TARGET,POLICY"),
                ("DELETE FROM {0} WITH (ROWLOCK);", "NAME,SOURCE,TARGET,POLICY"),
                ("DELETE FROM dbo.{0};", "POLICY"),
                ("DELETE {0} FROM dbo.Orders AS {0};", "NAME,SOURCE,TARGET"),
                ("INSERT INTO {0} (Id) VALUES (1);", "POLICY"),
                ("INSERT INTO dbo.Orders ({0}) VALUES (1);", "NAME,SOURCE,TARGET"),
                ("DELETE FROM dbo.Orders OUTPUT deleted.Id INTO {0};", "POLICY"),
                ("DELETE FROM dbo.Orders OUTPUT deleted.Id INTO @out ({0});", "NAME,SOURCE,TARGET"),
                ("DELETE FROM dbo.Orders WHERE CURRENT OF {0};", "NAME,SOURCE,TARGET")
            };

            foreach (var (template, acceptedBare) in contexts)
            {
                var allowed = acceptedBare.Split(',');
                foreach (var keyword in new[] { "NAME", "SOURCE", "TARGET", "POLICY" })
                {
                    yield return new object[] { string.Format(template, keyword), allowed.Contains(keyword) };
                    yield return new object[] { string.Format(template, "[" + keyword + "]"), true };
                    yield return new object[] { string.Format(template, "\"" + keyword + "\""), true };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Stage6KeywordAcceptanceCases))]
        public void ParseScript_Stage6_ShouldKeepCharacterizedKeywordAcceptance(string sql, bool accepted)
        {
            var parsed = ModernMsSqlGrammarExample.ParseBatch(sql);
            parsed.IsSuccess.Should().Be(accepted,
                $"the pre-change grammar {(accepted ? "accepted" : "rejected")} '{sql}'");
        }
    }
}
