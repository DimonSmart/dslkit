using System.Collections.Generic;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlExtensionsGrammar
    {
        public static void BuildTriggerGrammar(
            GrammarBuilder gb,
            INonTerminal createTriggerHead,
            INonTerminal createTriggerFireClause,
            INonTerminal createTriggerEventList,
            INonTerminal createTriggerEvent,
            INonTerminal createTriggerWithOptionList,
            INonTerminal createTriggerWithOption,
            INonTerminal createTriggerStatement,
            INonTerminal createProcExecuteAsClause,
            INonTerminal createProcBodyBlock,
            INonTerminal strictIdentifierTerm,
            INonTerminal qualifiedName,
            INonTerminal dropTriggerStatement,
            INonTerminal dropIfExistsClause)
        {
            gb.Prod(createTriggerHead).Is("CREATE", "TRIGGER");
            gb.Prod(createTriggerHead).Is("CREATE", "OR", "ALTER", "TRIGGER");
            gb.Prod(createTriggerHead).Is("ALTER", "TRIGGER");

            gb.Prod(createTriggerFireClause).Is("FOR", createTriggerEventList);
            gb.Prod(createTriggerFireClause).Is("AFTER", createTriggerEventList);
            gb.Prod(createTriggerFireClause).Is("INSTEAD", "OF", createTriggerEventList);

            gb.Rule(createTriggerEventList).SeparatedBy(",", createTriggerEvent);
            gb.Rule(createTriggerEvent)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("INSERT", "UPDATE", "DELETE");

            gb.Rule(createTriggerWithOptionList).SeparatedBy(",", createTriggerWithOption);
            gb.Rule(createTriggerWithOption)
                .CanBe(createProcExecuteAsClause)
                .OrKeywords("ENCRYPTION", "SCHEMABINDING", "NATIVE_COMPILATION");

            gb.Prod(createTriggerStatement).Is(createTriggerHead, qualifiedName, "ON", qualifiedName, createTriggerFireClause, "AS", createProcBodyBlock);
            gb.Prod(createTriggerStatement).Is(createTriggerHead, qualifiedName, "ON", qualifiedName, "WITH", createTriggerWithOptionList, createTriggerFireClause, "AS", createProcBodyBlock);
            gb.Prod(createTriggerStatement).Is(createTriggerHead, qualifiedName, "ON", qualifiedName, createTriggerFireClause, "NOT", "FOR", "REPLICATION", "AS", createProcBodyBlock);
            gb.Prod(createTriggerStatement).Is(createTriggerHead, qualifiedName, "ON", qualifiedName, "WITH", createTriggerWithOptionList, createTriggerFireClause, "NOT", "FOR", "REPLICATION", "AS", createProcBodyBlock);
            gb.Prod(createTriggerStatement).Is(createTriggerHead, qualifiedName, "ON", "ALL", "SERVER", createTriggerFireClause, "AS", createProcBodyBlock);
            gb.Prod(createTriggerStatement).Is(createTriggerHead, qualifiedName, "ON", "DATABASE", createTriggerFireClause, "AS", createProcBodyBlock);
            gb.Prod(createTriggerStatement).Is(createTriggerHead, qualifiedName, "ON", "ALL", "SERVER", "WITH", createTriggerWithOptionList, createTriggerFireClause, "AS", createProcBodyBlock);
            gb.Prod(createTriggerStatement).Is(createTriggerHead, qualifiedName, "ON", "DATABASE", "WITH", createTriggerWithOptionList, createTriggerFireClause, "AS", createProcBodyBlock);

            gb.Prod(dropTriggerStatement).Is("DROP", "TRIGGER", qualifiedName);
            gb.Prod(dropTriggerStatement).Is("DROP", "TRIGGER", dropIfExistsClause, qualifiedName);
            gb.Prod(dropTriggerStatement).Is("DROP", "TRIGGER", qualifiedName, "ON", "DATABASE");
            gb.Prod(dropTriggerStatement).Is("DROP", "TRIGGER", dropIfExistsClause, qualifiedName, "ON", "DATABASE");
        }

        public static void BuildGraphGrammar(
            GrammarBuilder gb,
            INonTerminal matchGraphPattern,
            INonTerminal matchGraphPath,
            INonTerminal matchGraphStep,
            INonTerminal matchGraphStepChain,
            INonTerminal matchGraphShortestPath,
            INonTerminal matchGraphShortestPathBody,
            INonTerminal strictIdentifierTerm,
            ITerminal number)
        {
            var graphIdentifierTerm = gb.NT("GraphIdentifierTerm");

            gb.Rule(graphIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("NAME", "SOURCE", "TARGET");
            gb.Prod(matchGraphPattern).Is(matchGraphPath);
            gb.Prod(matchGraphPattern).Is("SHORTEST_PATH", "(", matchGraphShortestPath, ")");
            gb.Prod(matchGraphPattern).Is(matchGraphPattern, "AND", matchGraphPath);
            gb.Prod(matchGraphPattern).Is(matchGraphPattern, "AND", "SHORTEST_PATH", "(", matchGraphShortestPath, ")");
            gb.Prod(matchGraphShortestPath).Is(matchGraphShortestPathBody);
            gb.Prod(matchGraphShortestPath).Is(matchGraphShortestPathBody, "+");
            gb.Prod(matchGraphShortestPath).Is(matchGraphShortestPathBody, "{", number, ",", number, "}");
            gb.Prod(matchGraphPath).Is(graphIdentifierTerm);
            gb.Prod(matchGraphPath).Is(graphIdentifierTerm, matchGraphStepChain);
            gb.Prod(matchGraphShortestPathBody).Is(graphIdentifierTerm, "(", matchGraphStepChain, ")");
            gb.Rule(matchGraphStep)
                .CanBe("-", "(", graphIdentifierTerm, ")", "-", ">", graphIdentifierTerm)
                .Or("<", "-", "(", graphIdentifierTerm, ")", "-", graphIdentifierTerm)
                .Or("-", "(", graphIdentifierTerm, ")", "-", graphIdentifierTerm);
            gb.Rule(matchGraphStepChain).Plus(matchGraphStep);
        }

        public static void BuildMergeGrammar(GrammarBuilder gb)
        {
            var mergeTargetTable = gb.NT("MergeTargetTable");
            var mergeSourceTable = gb.NT("MergeSourceTable");
            var mergeOutputClauseOpt = gb.NT("MergeOutputClauseOpt");
            var mergeOptionClauseOpt = gb.NT("MergeOptionClauseOpt");
            var mergeWhenList = gb.NT("MergeWhenList");
            var mergeWhen = gb.NT("MergeWhen");
            var mergeMatchedAction = gb.NT("MergeMatchedAction");
            var mergeNotMatchedAction = gb.NT("MergeNotMatchedAction");
            var qualifiedName = gb.NT("QualifiedName");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var tableHintLimitedList = gb.NT("TableHintLimitedList");
            var tableSource = gb.NT("TableSource");
            var dmlOutputClause = gb.NT("DeleteOutputClause");
            var optionClause = gb.NT("OptionClause");
            var topValue = gb.NT("TopValue");
            var searchCondition = gb.NT("SearchCondition");
            var updateSetList = gb.NT("UpdateSetList");
            var insertColumnList = gb.NT("InsertColumnList");
            var insertValueList = gb.NT("InsertValueList");
            var mergeAliasTerm = gb.NT("MergeAliasTerm");

            gb.Rule(mergeAliasTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("NAME", "SOURCE", "TARGET");
            gb.Prod(mergeTargetTable).Is(qualifiedName);
            gb.Prod(mergeTargetTable).Is(qualifiedName, "AS", mergeAliasTerm);
            gb.Prod(mergeTargetTable).Is(qualifiedName, mergeAliasTerm);
            gb.Prod(mergeTargetTable).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(mergeTargetTable).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")", "AS", mergeAliasTerm);
            gb.Prod(mergeTargetTable).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")", mergeAliasTerm);
            gb.Prod(mergeSourceTable).Is(tableSource);
            gb.Opt(mergeOutputClauseOpt, dmlOutputClause);
            gb.Opt(mergeOptionClauseOpt, optionClause);
            gb.Rule("MergeStatement").OneOf(
                gb.Seq("MERGE", mergeTargetTable, "USING", mergeSourceTable, "ON", searchCondition, mergeWhenList, mergeOutputClauseOpt, mergeOptionClauseOpt),
                gb.Seq("MERGE", "INTO", mergeTargetTable, "USING", mergeSourceTable, "ON", searchCondition, mergeWhenList, mergeOutputClauseOpt, mergeOptionClauseOpt),
                gb.Seq("MERGE", "TOP", topValue, mergeTargetTable, "USING", mergeSourceTable, "ON", searchCondition, mergeWhenList, mergeOutputClauseOpt, mergeOptionClauseOpt),
                gb.Seq("MERGE", "TOP", topValue, "PERCENT", mergeTargetTable, "USING", mergeSourceTable, "ON", searchCondition, mergeWhenList, mergeOutputClauseOpt, mergeOptionClauseOpt),
                gb.Seq("MERGE", "TOP", topValue, "INTO", mergeTargetTable, "USING", mergeSourceTable, "ON", searchCondition, mergeWhenList, mergeOutputClauseOpt, mergeOptionClauseOpt),
                gb.Seq("MERGE", "TOP", topValue, "PERCENT", "INTO", mergeTargetTable, "USING", mergeSourceTable, "ON", searchCondition, mergeWhenList, mergeOutputClauseOpt, mergeOptionClauseOpt));
            gb.Rule("MergeWhenList").Plus(mergeWhen);
            gb.Rule("MergeWhen").OneOf(
                gb.Seq("WHEN", "MATCHED", "THEN", mergeMatchedAction),
                gb.Seq("WHEN", "MATCHED", "AND", searchCondition, "THEN", mergeMatchedAction),
                gb.Seq("WHEN", "NOT", "MATCHED", "THEN", mergeNotMatchedAction),
                gb.Seq("WHEN", "NOT", "MATCHED", "AND", searchCondition, "THEN", mergeNotMatchedAction),
                gb.Seq("WHEN", "NOT", "MATCHED", "BY", "TARGET", "THEN", mergeNotMatchedAction),
                gb.Seq("WHEN", "NOT", "MATCHED", "BY", "TARGET", "AND", searchCondition, "THEN", mergeNotMatchedAction),
                gb.Seq("WHEN", "NOT", "MATCHED", "BY", "SOURCE", "THEN", mergeMatchedAction),
                gb.Seq("WHEN", "NOT", "MATCHED", "BY", "SOURCE", "AND", searchCondition, "THEN", mergeMatchedAction));
            gb.Rule("MergeMatchedAction").OneOf(
                gb.Seq("UPDATE", "SET", updateSetList),
                "DELETE");
            gb.Rule("MergeNotMatchedAction").OneOf(
                gb.Seq("INSERT", "(", insertColumnList, ")", "VALUES", "(", insertValueList, ")"),
                gb.Seq("INSERT", "VALUES", "(", insertValueList, ")"),
                gb.Seq("INSERT", "(", insertColumnList, ")", "DEFAULT", "VALUES"),
                gb.Seq("INSERT", "DEFAULT", "VALUES"));
        }

        public static void BuildSecurityPolicyGrammar(
            GrammarBuilder gb,
            INonTerminal createSecurityPolicyStatement,
            INonTerminal alterSecurityPolicyStatement,
            INonTerminal securityPolicyClauseList,
            INonTerminal securityPolicyClause,
            INonTerminal securityPolicyOptionList,
            INonTerminal securityPolicyOption,
            INonTerminal securityPolicyOptionName,
            INonTerminal functionCall,
            INonTerminal qualifiedName)
        {
            gb.Prod(createSecurityPolicyStatement).Is("CREATE", "SECURITY", "POLICY", qualifiedName, securityPolicyClauseList);
            gb.Prod(createSecurityPolicyStatement).Is("CREATE", "SECURITY", "POLICY", qualifiedName, securityPolicyClauseList, "WITH", "(", securityPolicyOptionList, ")");
            gb.Prod(alterSecurityPolicyStatement).Is("ALTER", "SECURITY", "POLICY", qualifiedName, securityPolicyClauseList);
            gb.Prod(alterSecurityPolicyStatement).Is("ALTER", "SECURITY", "POLICY", qualifiedName, securityPolicyClauseList, "WITH", "(", securityPolicyOptionList, ")");
            gb.Rule(securityPolicyClauseList).SeparatedBy(",", securityPolicyClause);
            gb.Rule(securityPolicyClause)
                .CanBe("ADD", "FILTER", "PREDICATE", functionCall, "ON", qualifiedName)
                .Or("ADD", "BLOCK", "PREDICATE", functionCall, "ON", qualifiedName)
                .Or("DROP", "FILTER", "PREDICATE", "ON", qualifiedName)
                .Or("DROP", "BLOCK", "PREDICATE", "ON", qualifiedName);
            gb.Rule(securityPolicyOptionName).Keywords("STATE", "SCHEMABINDING");
            gb.Rule(securityPolicyOptionList).SeparatedBy(",", securityPolicyOption);
            gb.Prod(securityPolicyOption).Is(securityPolicyOptionName, "=", "ON");
            gb.Prod(securityPolicyOption).Is(securityPolicyOptionName, "=", "OFF");
        }

        public static void BuildExternalObjectGrammar(
            GrammarBuilder gb,
            INonTerminal createExternalTableStatement,
            INonTerminal externalTableOptionList,
            INonTerminal createExternalDataSourceStatement,
            INonTerminal externalDataSourceOptionList,
            INonTerminal qualifiedName,
            INonTerminal strictIdentifierTerm,
            INonTerminal createTableElementList,
            INonTerminal namedOptionValue)
        {
            var externalObjectIdentifierTerm = gb.NT("ExternalObjectIdentifierTerm");
            var externalObjectQualifiedName = gb.NT("ExternalObjectQualifiedName");

            gb.Rule(externalObjectIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("NAME");
            gb.Rule(externalObjectQualifiedName).SeparatedBy(".", externalObjectIdentifierTerm);
            gb.Prod(externalObjectQualifiedName).Is(externalObjectQualifiedName, ".", ".", externalObjectIdentifierTerm);
            gb.Prod(createExternalTableStatement).Is("CREATE", "EXTERNAL", "TABLE", qualifiedName, "(", createTableElementList, ")");
            gb.Prod(createExternalTableStatement).Is("CREATE", "EXTERNAL", "TABLE", qualifiedName, "(", createTableElementList, ")", "WITH", "(", externalTableOptionList, ")");
            DefineNamedOptionList(
                gb,
                externalTableOptionList,
                namedOptionValue,
                "LOCATION",
                "DATA_SOURCE",
                "FILE_FORMAT",
                "REJECT_TYPE",
                "REJECT_VALUE",
                "REJECT_SAMPLE_VALUE",
                "DISTRIBUTION",
                "SCHEMA_NAME",
                "OBJECT_NAME");

            gb.Prod(createExternalDataSourceStatement).Is("CREATE", "EXTERNAL", "DATA", "SOURCE", externalObjectQualifiedName, "WITH", "(", externalDataSourceOptionList, ")");
            DefineNamedOptionList(
                gb,
                externalDataSourceOptionList,
                namedOptionValue,
                "TYPE",
                "LOCATION",
                "RESOURCE_MANAGER_LOCATION",
                "DATABASE_NAME",
                "SHARD_MAP_NAME",
                "CREDENTIAL",
                "CONNECTION_OPTIONS",
                "PUSHDOWN");
        }

        public static void BuildBulkInsertGrammar(
            GrammarBuilder gb,
            INonTerminal bulkInsertOptionList,
            INonTerminal qualifiedName,
            INonTerminal expression,
            INonTerminal namedOptionValue,
            INonTerminal createTableKeyColumnList)
        {
            gb.Prod("BulkInsertStatement").Is("BULK", "INSERT", qualifiedName, "FROM", expression);
            gb.Prod("BulkInsertStatement").Is("BULK", "INSERT", qualifiedName, "FROM", expression, "WITH", "(", bulkInsertOptionList, ")");
            DefineKeywordAndNamedOptionList(
                gb,
                bulkInsertOptionList,
                namedOptionValue,
                ["CHECK_CONSTRAINTS", "KEEPIDENTITY", "KEEPNULLS", "TABLOCK", "FIRE_TRIGGERS"],
                [
                    "CODEPAGE",
                    "DATAFILETYPE",
                    "DATA_SOURCE",
                    "ERRORFILE",
                    "ERRORFILE_DATA_SOURCE",
                    "FIRSTROW",
                    "FORMAT",
                    "FIELDQUOTE",
                    "FORMATFILE",
                    "FORMATFILE_DATA_SOURCE",
                    "KILOBYTES_PER_BATCH",
                    "LASTROW",
                    "MAXERRORS",
                    "ROWS_PER_BATCH",
                    "ROWTERMINATOR",
                    "FIELDTERMINATOR",
                    "BATCHSIZE"
                ]);
            gb.Prod(bulkInsertOptionList).Is("ORDER", "(", createTableKeyColumnList, ")");
            gb.Prod(bulkInsertOptionList).Is(bulkInsertOptionList, ",", "ORDER", "(", createTableKeyColumnList, ")");
        }

        public static void BuildSynapseGrammar(
            GrammarBuilder gb,
            INonTerminal functionCall,
            INonTerminal predictArgList,
            INonTerminal predictArg,
            INonTerminal strictIdentifierTerm,
            INonTerminal expression)
        {
            var predictIdentifierTerm = gb.NT("PredictIdentifierTerm");

            gb.Rule(predictIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("NAME", "MODEL", "DATA");
            gb.Prod(functionCall).Is("PREDICT", "(", predictArgList, ")");
            gb.Rule(predictArgList).SeparatedBy(",", predictArg);
            gb.Prod(predictArg).Is(predictIdentifierTerm, "=", expression);
            gb.Prod(predictArg).Is(predictIdentifierTerm, "=", expression, "AS", predictIdentifierTerm);
        }

        private static void DefineNamedOptionList(
            GrammarBuilder gb,
            INonTerminal list,
            INonTerminal namedOptionValue,
            params string[] optionNames)
        {
            foreach (var optionName in optionNames)
            {
                gb.Prod(list).Is(optionName, "=", namedOptionValue);
            }

            foreach (var optionName in optionNames)
            {
                gb.Prod(list).Is(list, ",", optionName, "=", namedOptionValue);
            }
        }

        private static void DefineKeywordAndNamedOptionList(
            GrammarBuilder gb,
            INonTerminal list,
            INonTerminal namedOptionValue,
            IEnumerable<string> keywordOptionNames,
            IEnumerable<string> namedOptionNames)
        {
            foreach (var optionName in keywordOptionNames)
            {
                gb.Prod(list).Is(optionName);
            }

            foreach (var optionName in namedOptionNames)
            {
                gb.Prod(list).Is(optionName, "=", namedOptionValue);
            }

            foreach (var optionName in keywordOptionNames)
            {
                gb.Prod(list).Is(list, ",", optionName);
            }

            foreach (var optionName in namedOptionNames)
            {
                gb.Prod(list).Is(list, ",", optionName, "=", namedOptionValue);
            }
        }
    }
}
