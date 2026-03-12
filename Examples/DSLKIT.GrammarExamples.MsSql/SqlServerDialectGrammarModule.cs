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
            var symbols = grammarContext.Symbols;
            var extensionPoints = context.ExtensionPoints;

            gb.Prod(extensionPoints.CreateViewHead).Is("CREATE", "VIEW");
            gb.Prod(extensionPoints.CreateViewHead).Is("CREATE", "OR", "ALTER", "VIEW");
            gb.Prod(extensionPoints.CreateViewHead).Is("ALTER", "VIEW");

            gb.Prod(symbols.FunctionCall).Is("::", symbols.QualifiedName, "(", ")");
            gb.Prod(symbols.FunctionCall).Is("::", symbols.QualifiedName, "(", symbols.FunctionArgumentList, ")");

            MsSqlExtensionsGrammar.BuildBulkInsertGrammar(
                gb,
                extensionPoints.BulkInsertOptionList,
                symbols.QualifiedName,
                symbols.Expression,
                symbols.NamedOptionValue,
                symbols.CreateTableKeyColumnList);

            if (grammarContext.HasFeature(MsSqlDialectFeatures.GraphExtensions))
            {
                MsSqlExtensionsGrammar.BuildGraphGrammar(
                    gb,
                    symbols.MatchGraphPattern,
                    extensionPoints.MatchGraphPath,
                    extensionPoints.MatchGraphStep,
                    extensionPoints.MatchGraphStepChain,
                    extensionPoints.MatchGraphShortestPath,
                    extensionPoints.MatchGraphShortestPathBody,
                    symbols.StrictIdentifierTerm,
                    grammarContext.NumberTerminal);
            }

            if (grammarContext.HasFeature(MsSqlDialectFeatures.SynapseExtensions))
            {
                MsSqlExtensionsGrammar.BuildSynapseGrammar(
                    gb,
                    symbols.FunctionCall,
                    extensionPoints.PredictArgList,
                    extensionPoints.PredictArg,
                    symbols.StrictIdentifierTerm,
                    symbols.Expression);
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
                symbols.FunctionCall,
                symbols.QualifiedName);

            if (grammarContext.HasFeature(MsSqlDialectFeatures.ExternalObjects))
            {
                MsSqlExtensionsGrammar.BuildExternalObjectGrammar(
                    gb,
                    extensionPoints.CreateExternalTableStatement,
                    extensionPoints.ExternalTableOptionList,
                    extensionPoints.CreateExternalDataSourceStatement,
                    extensionPoints.ExternalDataSourceOptionList,
                    symbols.QualifiedName,
                    symbols.StrictIdentifierTerm,
                    extensionPoints.CreateTableElementList,
                    symbols.NamedOptionValue);
            }
        }
    }
}
