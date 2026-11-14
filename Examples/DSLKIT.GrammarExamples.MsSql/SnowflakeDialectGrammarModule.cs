using System.Collections.Generic;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal sealed class SnowflakeDialectGrammarModule : SqlDialectGrammarModule
    {
        public override SqlDialect Dialect => SqlDialect.Snowflake;

        public override MsSqlDialectFeatures DefaultFeatures => MsSqlDialectFeatures.SnowflakeCompat;

        public override MsSqlDialectFeatures NormalizeFeatures(MsSqlDialectFeatures dialectFeatures)
        {
            return MsSqlDialectFeatures.SnowflakeCompat;
        }

        public override IReadOnlyCollection<object> CreateLeadingWithStatementAlternatives(SqlDialectGrammarModuleContext context)
        {
            return [];
        }

        public override void RegisterStatements(MsSqlStatementRegistry statementRegistry, SqlDialectGrammarModuleContext context)
        {
        }

        public override void Apply(SqlDialectGrammarModuleContext context)
        {
            var gb = context.GrammarContext.Gb;
            var extensionPoints = context.ExtensionPoints;

            gb.Prod(extensionPoints.CreateViewHead).Is("CREATE", "OR", "REPLACE", "VIEW");
            gb.Prod(gb.NT("QuerySpecificationQualifyOpt")).Is("QUALIFY", gb.NT("SearchCondition"));
            gb.Prod(gb.NT("CollateExpression")).Is(gb.NT("CollateExpression"), "::", gb.NT("TypeSpec"));

            gb.Rule(gb.NT("QuerySpecification")).OneOf(
                gb.Seq(gb.NT("SelectCore"), gb.NT("QuerySpecificationQualifyOpt")),
                gb.Seq(gb.NT("SelectCore"), gb.NT("QuerySpecificationWhereClause"), gb.NT("QuerySpecificationQualifyOpt")),
                gb.Seq(gb.NT("SelectCore"), gb.NT("QuerySpecificationGroupByClause"), gb.NT("QuerySpecificationQualifyOpt")),
                gb.Seq(
                    gb.NT("SelectCore"),
                    gb.NT("QuerySpecificationWhereClause"),
                    gb.NT("QuerySpecificationGroupByClause"),
                    gb.NT("QuerySpecificationQualifyOpt")));
        }
    }
}
