using DSLKIT.NonTerminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal sealed record SqlDialectGrammarModuleContext(
        MsSqlGrammarContext GrammarContext,
        SqlDialectGrammarExtensionPoints ExtensionPoints);

    internal sealed record SqlDialectGrammarExtensionPoints(
        INonTerminal WithClause,
        INonTerminal CreateViewHead,
        INonTerminal BulkInsertStatement,
        INonTerminal BulkInsertOptionList,
        INonTerminal MatchGraphPath,
        INonTerminal MatchGraphStep,
        INonTerminal MatchGraphStepChain,
        INonTerminal MatchGraphShortestPath,
        INonTerminal MatchGraphShortestPathBody,
        INonTerminal CreateTableAsSelectStatement,
        INonTerminal PredictArgList,
        INonTerminal PredictArg,
        INonTerminal CreateSecurityPolicyStatement,
        INonTerminal AlterSecurityPolicyStatement,
        INonTerminal SecurityPolicyClauseList,
        INonTerminal SecurityPolicyClause,
        INonTerminal SecurityPolicyOptionList,
        INonTerminal SecurityPolicyOption,
        INonTerminal SecurityPolicyOptionName,
        INonTerminal CreateExternalTableStatement,
        INonTerminal ExternalTableOptionList,
        INonTerminal CreateExternalDataSourceStatement,
        INonTerminal ExternalDataSourceOptionList,
        INonTerminal MergeStatement,
        INonTerminal CreateTableElementList);
}
