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
            var symbols = context.GrammarContext.Symbols;
            var extensionPoints = context.ExtensionPoints;

            gb.Prod(extensionPoints.CreateViewHead).Is("CREATE", "OR", "REPLACE", "VIEW");
            gb.Prod(symbols.QuerySpecificationQualifyOpt).Is("QUALIFY", symbols.SearchCondition);
            gb.Prod(symbols.CollateExpression).Is(symbols.CollateExpression, "::", symbols.TypeSpec);

            gb.Rule(symbols.QuerySpecification).OneOf(
                gb.Seq(symbols.SelectCore, symbols.QuerySpecificationQualifyOpt),
                gb.Seq(symbols.SelectCore, symbols.QuerySpecificationWhereClause, symbols.QuerySpecificationQualifyOpt),
                gb.Seq(symbols.SelectCore, symbols.QuerySpecificationGroupByClause, symbols.QuerySpecificationQualifyOpt),
                gb.Seq(
                    symbols.SelectCore,
                    symbols.QuerySpecificationWhereClause,
                    symbols.QuerySpecificationGroupByClause,
                    symbols.QuerySpecificationQualifyOpt));
        }
    }
}
