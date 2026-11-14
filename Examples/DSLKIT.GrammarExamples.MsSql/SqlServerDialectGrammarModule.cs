using System.Collections.Generic;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal sealed class SqlServerDialectGrammarModule : SqlDialectGrammarModule
    {
        public override SqlDialect Dialect => SqlDialect.SqlServer;

        public override MsSqlDialectFeatures DefaultFeatures => MsSqlDialectFeatures.All;

        public override MsSqlDialectFeatures NormalizeFeatures(MsSqlDialectFeatures dialectFeatures)
        {
            return dialectFeatures & (
                MsSqlDialectFeatures.ExternalObjects |
                MsSqlDialectFeatures.SynapseExtensions |
                MsSqlDialectFeatures.GraphExtensions);
        }

        public override IReadOnlyCollection<object> CreateLeadingWithStatementAlternatives(SqlDialectGrammarModuleContext context)
        {
            var extensionPoints = context.ExtensionPoints;
            var gb = context.GrammarContext.Gb;
            return [gb.Seq(extensionPoints.WithClause, extensionPoints.MergeStatement)];
        }

        public override void RegisterStatements(MsSqlStatementRegistry statementRegistry, SqlDialectGrammarModuleContext context)
        {
            var grammarContext = context.GrammarContext;
            var extensionPoints = context.ExtensionPoints;

            statementRegistry.Add(extensionPoints.BulkInsertStatement);
            statementRegistry.Add(extensionPoints.CreateSecurityPolicyStatement);
            statementRegistry.Add(extensionPoints.AlterSecurityPolicyStatement);
            statementRegistry.Add(extensionPoints.MergeStatement);

            if (grammarContext.HasFeature(MsSqlDialectFeatures.SynapseExtensions))
            {
                statementRegistry.Add(extensionPoints.CreateTableAsSelectStatement);
            }

            if (grammarContext.HasFeature(MsSqlDialectFeatures.ExternalObjects))
            {
                statementRegistry.Add(extensionPoints.CreateExternalTableStatement);
                statementRegistry.Add(extensionPoints.CreateExternalDataSourceStatement);
            }
        }

        public override void Apply(SqlDialectGrammarModuleContext context)
        {
            var grammarContext = context.GrammarContext;
            var gb = grammarContext.Gb;
            var extensionPoints = context.ExtensionPoints;

            gb.Prod(extensionPoints.CreateViewHead).Is("CREATE", "VIEW");
            gb.Prod(extensionPoints.CreateViewHead).Is("CREATE", "OR", "ALTER", "VIEW");
            gb.Prod(extensionPoints.CreateViewHead).Is("ALTER", "VIEW");

            gb.Prod(gb.NT("FunctionCall")).Is("::", gb.NT("QualifiedName"), "(", ")");
            gb.Prod(gb.NT("FunctionCall")).Is("::", gb.NT("QualifiedName"), "(", gb.NT("FunctionArgumentList"), ")");

            MsSqlExtensionsGrammar.BuildBulkInsertGrammar(
                gb,
                extensionPoints.BulkInsertOptionList,
                gb.NT("QualifiedName"),
                gb.NT("Expression"),
                gb.NT("NamedOptionValue"),
                gb.NT("CreateTableKeyColumnList"));

            if (grammarContext.HasFeature(MsSqlDialectFeatures.GraphExtensions))
            {
                MsSqlExtensionsGrammar.BuildGraphGrammar(
                    gb,
                    gb.NT("MatchGraphPattern"),
                    extensionPoints.MatchGraphPath,
                    extensionPoints.MatchGraphStep,
                    extensionPoints.MatchGraphStepChain,
                    extensionPoints.MatchGraphShortestPath,
                    extensionPoints.MatchGraphShortestPathBody,
                    gb.NT("StrictIdentifierTerm"),
                    grammarContext.NumberTerminal);
            }

            if (grammarContext.HasFeature(MsSqlDialectFeatures.SynapseExtensions))
            {
                MsSqlExtensionsGrammar.BuildSynapseGrammar(
                    gb,
                    gb.NT("FunctionCall"),
                    extensionPoints.PredictArgList,
                    extensionPoints.PredictArg,
                    gb.NT("StrictIdentifierTerm"),
                    gb.NT("Expression"));
            }

            MsSqlExtensionsGrammar.BuildSecurityPolicyGrammar(
                gb,
                extensionPoints.CreateSecurityPolicyStatement,
                extensionPoints.AlterSecurityPolicyStatement,
                extensionPoints.SecurityPolicyClauseList,
                extensionPoints.SecurityPolicyClause,
                extensionPoints.SecurityPolicyOptionList,
                extensionPoints.SecurityPolicyOption,
                extensionPoints.SecurityPolicyOptionName,
                gb.NT("FunctionCall"),
                gb.NT("QualifiedName"));

            if (grammarContext.HasFeature(MsSqlDialectFeatures.ExternalObjects))
            {
                MsSqlExtensionsGrammar.BuildExternalObjectGrammar(
                    gb,
                    extensionPoints.CreateExternalTableStatement,
                    extensionPoints.ExternalTableOptionList,
                    extensionPoints.CreateExternalDataSourceStatement,
                    extensionPoints.ExternalDataSourceOptionList,
                    gb.NT("QualifiedName"),
                    gb.NT("StrictIdentifierTerm"),
                    extensionPoints.CreateTableElementList,
                    gb.NT("NamedOptionValue"));
            }
        }
    }
}
