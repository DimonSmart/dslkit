using System.Collections.Generic;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlSecurityAndAdminGrammar
    {
        public static void BuildPreQuery(
            MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var grantPermissionSet = gb.NT("GrantPermissionSet");
            var grantPermissionList = gb.NT("GrantPermissionList");
            var grantPermissionItem = gb.NT("GrantPermissionItem");
            var grantPermission = gb.NT("GrantPermission");
            var grantPermissionWord = gb.NT("GrantPermissionWord");
            var grantOnClause = gb.NT("GrantOnClause");
            var grantClassType = gb.NT("GrantClassType");
            var grantSecurable = gb.NT("GrantSecurable");
            var grantPrincipalList = gb.NT("GrantPrincipalList");
            var grantPrincipal = gb.NT("GrantPrincipal");
            var grantStatement = gb.NT("GrantStatement");
            var dbccParamList = gb.NT("DbccParamList");
            var dbccParam = gb.NT("DbccParam");
            var dbccOptionList = gb.NT("DbccOptionList");
            var dbccOption = gb.NT("DbccOption");
            var dbccOptionName = gb.NT("DbccOptionName");
            var dbccOptionValue = gb.NT("DbccOptionValue");
            var dbccStatement = gb.NT("DbccStatement");
            var dropProcStatement = gb.NT("DropProcStatement");
            var dropIfExistsClause = gb.NT("DropIfExistsClause");
            var dropTableStatement = gb.NT("DropTableStatement");
            var dropTableTargetList = gb.NT("DropTableTargetList");
            var dropViewStatement = gb.NT("DropViewStatement");
            var dropViewTargetList = gb.NT("DropViewTargetList");
            var dropIndexStatement = gb.NT("DropIndexStatement");
            var dropIndexSpecList = gb.NT("DropIndexSpecList");
            var dropIndexSpec = gb.NT("DropIndexSpec");
            var dropIndexOptionList = gb.NT("DropIndexOptionList");
            var dropIndexOption = gb.NT("DropIndexOption");
            var dropMoveToTarget = gb.NT("DropMoveToTarget");
            var dropFileStreamTarget = gb.NT("DropFileStreamTarget");
            var dropStatisticsStatement = gb.NT("DropStatisticsStatement");
            var dropStatisticsTargetList = gb.NT("DropStatisticsTargetList");
            var dropStatisticsTarget = gb.NT("DropStatisticsTarget");
            var dropDatabaseStatement = gb.NT("DropDatabaseStatement");
            var createTriggerHead = gb.NT("CreateTriggerHead");
            var createTriggerFireClause = gb.NT("CreateTriggerFireClause");
            var createTriggerEventList = gb.NT("CreateTriggerEventList");
            var createTriggerEvent = gb.NT("CreateTriggerEvent");
            var createTriggerWithOptionList = gb.NT("CreateTriggerWithOptionList");
            var createTriggerWithOption = gb.NT("CreateTriggerWithOption");
            var createTriggerStatement = gb.NT("CreateTriggerStatement");
            var createProcExecuteAsClause = gb.NT("CreateProcExecuteAsClause");
            var createProcBodyBlock = gb.NT("CreateProcBodyBlock");
            var dropTriggerStatement = gb.NT("DropTriggerStatement");
            var createRoleStatement = gb.NT("CreateRoleStatement");
            var createSchemaStatement = gb.NT("CreateSchemaStatement");
            var schemaNameClause = gb.NT("SchemaNameClause");
            var expression = gb.NT("Expression");
            var qualifiedName = gb.NT("QualifiedName");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var strictQualifiedName = gb.NT("StrictQualifiedName");
            var adminIdentifierTerm = gb.NT("AdminIdentifierTerm");
            var adminIdentifierList = gb.NT("AdminIdentifierList");

            gb.Rule(adminIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("NAME");
            gb.Rule(adminIdentifierList).SeparatedBy(",", adminIdentifierTerm);

            gb.Prod(grantPermissionSet).Is("ALL");
            gb.Prod(grantPermissionSet).Is("ALL", "PRIVILEGES");
            gb.Prod(grantPermissionSet).Is(grantPermissionList);
            gb.Rule(grantPermissionList).SeparatedBy(",", grantPermissionItem);
            gb.Prod(grantPermissionItem).Is(grantPermission);
            gb.Prod(grantPermissionItem).Is(grantPermission, "(", adminIdentifierList, ")");

            gb.Prod(grantPermission).Is(grantPermissionWord);
            gb.Prod(grantPermission).Is("VIEW", "DEFINITION");
            gb.Prod(grantPermission).Is("TAKE", "OWNERSHIP");
            gb.Prod(grantPermission).Is("CREATE", "ANY", "SCHEMA");
            gb.Prod(grantPermission).Is("VIEW", "ANY", "COLUMN", "MASTER", "KEY", "DEFINITION");
            gb.Prod(grantPermission).Is("VIEW", "ANY", "COLUMN", "ENCRYPTION", "KEY", "DEFINITION");

            gb.Rule(grantPermissionWord)
                .Keywords(
                    "SELECT",
                    "INSERT",
                    "UPDATE",
                    "DELETE",
                    "EXECUTE",
                    "REFERENCES",
                    "CONNECT",
                    "ALTER",
                    "CONTROL",
                    "IMPERSONATE",
                    "RECEIVE",
                    "SEND");

            gb.Prod(grantOnClause).Is("ON", grantSecurable);
            gb.Prod(grantOnClause).Is("ON", grantClassType, "::", grantSecurable);
            gb.Rule(grantClassType).Keywords("LOGIN", "DATABASE", "OBJECT", "ROLE", "SCHEMA", "USER");
            gb.Prod(grantSecurable).Is(strictQualifiedName);
            gb.Prod(grantSecurable).Is(strictIdentifierTerm);

            gb.Rule(grantPrincipalList).SeparatedBy(",", grantPrincipal);
            gb.Prod(grantPrincipal).Is(strictQualifiedName);
            gb.Prod(grantPrincipal).Is("PUBLIC");

            gb.Prod(grantStatement).Is("GRANT", grantPermissionSet, "TO", grantPrincipalList);
            gb.Prod(grantStatement).Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList);
            gb.Prod(grantStatement).Is("GRANT", grantPermissionSet, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION");
            gb.Prod(grantStatement).Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION");
            gb.Prod(grantStatement).Is("GRANT", grantPermissionSet, "TO", grantPrincipalList, "AS", grantPrincipal);
            gb.Prod(grantStatement).Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList, "AS", grantPrincipal);
            gb.Prod(grantStatement).Is("GRANT", grantPermissionSet, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION", "AS", grantPrincipal);
            gb.Prod(grantStatement).Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION", "AS", grantPrincipal);

            var dbccDatabaseTarget = gb.NT("DbccDatabaseTarget");
            var dbccCheckdbArguments = gb.NT("DbccCheckdbArguments");
            var dbccCheckdbOption = gb.NT("DbccCheckdbOption");
            var dbccCheckdbOptionList = gb.NT("DbccCheckdbOptionList");
            var dbccTraceNumbers = gb.NT("DbccTraceNumbers");
            var dbccTraceStatusArguments = gb.NT("DbccTraceStatusArguments");
            var dbccFreeProcCacheTarget = gb.NT("DbccFreeProcCacheTarget");
            var dbccShrinkFileTarget = gb.NT("DbccShrinkFileTarget");
            var dbccShrinkFileArguments = gb.NT("DbccShrinkFileArguments");
            var dbccShrinkWaitOption = gb.NT("DbccShrinkWaitOption");
            var dbccLegacyCommand = gb.NT("DbccLegacyCommand");
            var number = context.NumberTerminal;
            var stringLiteral = context.StringLiteralTerminal;
            var unicodeStringLiteral = gb.NT("UnicodeStringLiteral");

            gb.Prod(dbccDatabaseTarget).Is(strictIdentifierTerm);
            gb.Prod(dbccDatabaseTarget).Is(stringLiteral);
            gb.Prod(dbccDatabaseTarget).Is(unicodeStringLiteral);
            gb.Prod(dbccDatabaseTarget).Is(number);
            gb.Prod(dbccCheckdbArguments).Is(dbccDatabaseTarget);
            gb.Rule(dbccCheckdbArguments).Or(dbccDatabaseTarget, ",", "NOINDEX")
                .Or(dbccDatabaseTarget, ",", "REPAIR_ALLOW_DATA_LOSS")
                .Or(dbccDatabaseTarget, ",", "REPAIR_FAST")
                .Or(dbccDatabaseTarget, ",", "REPAIR_REBUILD");

            gb.Rule(dbccCheckdbOption)
                .Keywords("ALL_ERRORMSGS", "EXTENDED_LOGICAL_CHECKS", "NO_INFOMSGS",
                    "TABLOCK", "ESTIMATEONLY", "PHYSICAL_ONLY", "DATA_PURITY");
            gb.Prod(dbccOptionValue).Is(number);
            gb.Prod(dbccCheckdbOption).Is("MAXDOP", "=", dbccOptionValue);
            gb.Rule(dbccCheckdbOptionList).SeparatedBy(",", dbccCheckdbOption);

            gb.Prod(dbccStatement).Is("DBCC", "CHECKDB");
            gb.Prod(dbccStatement).Is("DBCC", "CHECKDB", "(", dbccCheckdbArguments, ")");
            gb.Prod(dbccStatement).Is("DBCC", "CHECKDB", "WITH", dbccCheckdbOptionList);
            gb.Prod(dbccStatement).Is("DBCC", "CHECKDB", "(", dbccCheckdbArguments, ")", "WITH", dbccCheckdbOptionList);

            gb.Prod(dbccStatement).Is("DBCC", "DROPCLEANBUFFERS");
            gb.Prod(dbccStatement).Is("DBCC", "DROPCLEANBUFFERS", "WITH", "NO_INFOMSGS");

            gb.Rule(dbccTraceNumbers).SeparatedBy(",", number);
            gb.Prod(dbccTraceStatusArguments).Is("(", ")");
            gb.Prod(dbccTraceStatusArguments).Is("(", dbccTraceNumbers, ")");
            gb.Prod(dbccTraceStatusArguments).Is("(", "-", number, ")");
            gb.Prod(dbccTraceStatusArguments).Is("(", dbccTraceNumbers, ",", "-", number, ")");
            gb.Prod(dbccStatement).Is("DBCC", "TRACESTATUS", dbccTraceStatusArguments);
            gb.Prod(dbccStatement).Is("DBCC", "TRACESTATUS", dbccTraceStatusArguments, "WITH", "NO_INFOMSGS");

            gb.Prod(dbccStatement).Is("DBCC", "TRACEON", "(", dbccTraceNumbers, ")");
            gb.Prod(dbccStatement).Is("DBCC", "TRACEON", "(", dbccTraceNumbers, ",", "-", number, ")");
            gb.Prod(dbccStatement).Is("DBCC", "TRACEON", "(", dbccTraceNumbers, ")", "WITH", "NO_INFOMSGS");
            gb.Prod(dbccStatement).Is("DBCC", "TRACEON", "(", dbccTraceNumbers, ",", "-", number, ")", "WITH", "NO_INFOMSGS");

            gb.Prod(dbccFreeProcCacheTarget).Is(strictIdentifierTerm);
            gb.Prod(dbccFreeProcCacheTarget).Is(stringLiteral);
            gb.Prod(dbccFreeProcCacheTarget).Is(unicodeStringLiteral);
            gb.Prod(dbccFreeProcCacheTarget).Is(number);
            gb.Prod(dbccFreeProcCacheTarget).Is(gb.NT("VariableReference"));
            gb.Prod(dbccStatement).Is("DBCC", "FREEPROCCACHE");
            gb.Prod(dbccStatement).Is("DBCC", "FREEPROCCACHE", "(", dbccFreeProcCacheTarget, ")");
            gb.Prod(dbccStatement).Is("DBCC", "FREEPROCCACHE", "WITH", "NO_INFOMSGS");
            gb.Prod(dbccStatement).Is("DBCC", "FREEPROCCACHE", "(", dbccFreeProcCacheTarget, ")", "WITH", "NO_INFOMSGS");

            gb.Prod(dbccShrinkFileTarget).Is(strictIdentifierTerm);
            gb.Prod(dbccShrinkFileTarget).Is(stringLiteral);
            gb.Prod(dbccShrinkFileTarget).Is(unicodeStringLiteral);
            gb.Prod(dbccShrinkFileTarget).Is(number);
            gb.Prod(dbccShrinkFileArguments).Is(dbccShrinkFileTarget);
            gb.Prod(dbccShrinkFileArguments).Is(dbccShrinkFileTarget, ",", number);
            gb.Prod(dbccShrinkFileArguments).Is(dbccShrinkFileTarget, ",", "EMPTYFILE");
            gb.Prod(dbccShrinkFileArguments).Is(dbccShrinkFileTarget, ",", "NOTRUNCATE");
            gb.Prod(dbccShrinkFileArguments).Is(dbccShrinkFileTarget, ",", "TRUNCATEONLY");
            gb.Prod(dbccShrinkFileArguments).Is(dbccShrinkFileTarget, ",", number, ",", "NOTRUNCATE");
            gb.Prod(dbccShrinkFileArguments).Is(dbccShrinkFileTarget, ",", number, ",", "TRUNCATEONLY");
            gb.Prod(dbccShrinkWaitOption).Is("WAIT_AT_LOW_PRIORITY");
            gb.Prod(dbccShrinkWaitOption).Is("WAIT_AT_LOW_PRIORITY", "(",
                "ABORT_AFTER_WAIT", "=", "SELF", ")");
            gb.Prod(dbccShrinkWaitOption).Is("WAIT_AT_LOW_PRIORITY", "(",
                "ABORT_AFTER_WAIT", "=", "BLOCKERS", ")");
            gb.Prod(dbccStatement).Is("DBCC", "SHRINKFILE", "(", dbccShrinkFileArguments, ")");
            gb.Prod(dbccStatement).Is("DBCC", "SHRINKFILE", "(", dbccShrinkFileArguments, ")", "WITH", "NO_INFOMSGS");
            gb.Prod(dbccStatement).Is("DBCC", "SHRINKFILE", "(", dbccShrinkFileArguments, ")", "WITH", dbccShrinkWaitOption);
            gb.Prod(dbccStatement).Is("DBCC", "SHRINKFILE", "(", dbccShrinkFileArguments, ")", "WITH", dbccShrinkWaitOption, ",", "NO_INFOMSGS");

            // These internal/undocumented commands are intentionally not treated as documented SQL Server syntax.
            gb.Rule(dbccLegacyCommand).Keywords("LOGINFO", "PAGE", "WRITEPAGE");
            if (context.HasFeature(MsSqlDialectFeatures.SynapseExtensions))
            {
                gb.Prod(dbccLegacyCommand).Is("PDW_SHOWSPACEUSED");
            }

            gb.Prod(dbccStatement).Is("DBCC", dbccLegacyCommand);
            gb.Prod(dbccStatement).Is("DBCC", dbccLegacyCommand, "(", dbccParamList, ")");
            gb.Prod(dbccStatement).Is("DBCC", dbccLegacyCommand, "WITH", dbccOptionList);
            gb.Prod(dbccStatement).Is("DBCC", dbccLegacyCommand, "(", dbccParamList, ")", "WITH", dbccOptionList);
            gb.Rule(dbccParamList).SeparatedBy(",", dbccParam);
            gb.Prod(dbccParam).Is(expression);
            gb.Rule(dbccOptionList).SeparatedBy(",", dbccOption);
            gb.Prod(dbccOption).Is(dbccOptionName);
            gb.Prod(dbccOption).Is(dbccOptionName, "=", dbccOptionValue);
            gb.Rule(dbccOptionName)
                .Keywords("NO_INFOMSGS", "ALL_ERRORMSGS", "MAXDOP", "TABLERESULTS");

            gb.Rule(dropProcStatement)
                .CanBe("DROP", "PROC", qualifiedName)
                .Or("DROP", "PROCEDURE", qualifiedName)
                .Or("DROP", "PROC", dropIfExistsClause, qualifiedName)
                .Or("DROP", "PROCEDURE", dropIfExistsClause, qualifiedName)
                .Or("DROP", "FUNCTION", qualifiedName)
                .Or("DROP", "FUNCTION", dropIfExistsClause, qualifiedName);
            gb.Prod(dropIfExistsClause).Is("IF", "EXISTS");

            gb.Prod(dropTableStatement).Is("DROP", "TABLE", dropTableTargetList);
            gb.Prod(dropTableStatement).Is("DROP", "TABLE", dropIfExistsClause, dropTableTargetList);
            gb.Rule(dropTableTargetList).SeparatedBy(",", qualifiedName);

            gb.Prod(dropViewStatement).Is("DROP", "VIEW", dropViewTargetList);
            gb.Prod(dropViewStatement).Is("DROP", "VIEW", dropIfExistsClause, dropViewTargetList);
            gb.Rule(dropViewTargetList).SeparatedBy(",", qualifiedName);

            gb.Prod(dropIndexStatement).Is("DROP", "INDEX", dropIndexSpecList);
            gb.Prod(dropIndexStatement).Is("DROP", "INDEX", dropIfExistsClause, dropIndexSpecList);
            gb.Rule(dropIndexSpecList).SeparatedBy(",", dropIndexSpec);
            gb.Prod(dropIndexSpec).Is(qualifiedName, "ON", qualifiedName);
            gb.Prod(dropIndexSpec).Is(qualifiedName, "ON", qualifiedName, "WITH", "(", dropIndexOptionList, ")");
            gb.Prod(dropIndexSpec).Is(qualifiedName, ".", strictIdentifierTerm);
            gb.Rule(dropIndexOptionList).SeparatedBy(",", dropIndexOption);
            gb.Rule(dropIndexOption)
                .CanBe("MAXDOP", "=", expression)
                .Or("ONLINE", "=", "ON")
                .Or("ONLINE", "=", "OFF")
                .Or("MOVE", "TO", dropMoveToTarget)
                .Or("MOVE", "TO", dropMoveToTarget, "FILESTREAM_ON", dropFileStreamTarget)
                .Or("FILESTREAM_ON", dropFileStreamTarget);
            gb.Rule(dropMoveToTarget)
                .CanBe(qualifiedName)
                .Or(qualifiedName, "(", adminIdentifierTerm, ")");
            gb.Rule(dropFileStreamTarget)
                .CanBe(qualifiedName);

            gb.Prod(dropStatisticsStatement).Is("DROP", "STATISTICS", dropStatisticsTargetList);
            gb.Rule(dropStatisticsTargetList).SeparatedBy(",", dropStatisticsTarget);
            gb.Prod(dropStatisticsTarget).Is(qualifiedName, ".", strictIdentifierTerm);

            gb.Prod(dropDatabaseStatement).Is("DROP", "DATABASE", strictIdentifierTerm);
            gb.Prod(dropDatabaseStatement).Is("DROP", "DATABASE", dropIfExistsClause, strictIdentifierTerm);

            MsSqlExtensionsGrammar.BuildTriggerGrammar(
                gb,
                createTriggerHead,
                createTriggerFireClause,
                createTriggerEventList,
                createTriggerEvent,
                createTriggerWithOptionList,
                createTriggerWithOption,
                createTriggerStatement,
                createProcExecuteAsClause,
                createProcBodyBlock,
                strictIdentifierTerm,
                qualifiedName,
                dropTriggerStatement,
                dropIfExistsClause);
            gb.Prod(dropTriggerStatement).Is("DROP", "TRIGGER", qualifiedName, "ON", "ALL", "SERVER");
            gb.Prod(dropTriggerStatement).Is("DROP", "TRIGGER", dropIfExistsClause, qualifiedName, "ON", "ALL", "SERVER");

            gb.Prod(createRoleStatement).Is("CREATE", "ROLE", strictIdentifierTerm);
            gb.Prod(createRoleStatement).Is("CREATE", "ROLE", strictIdentifierTerm, "AUTHORIZATION", strictIdentifierTerm);

            gb.Prod(createSchemaStatement).Is("CREATE", "SCHEMA", schemaNameClause);
            gb.Prod(schemaNameClause).Is(strictIdentifierTerm);
            gb.Prod(schemaNameClause).Is("AUTHORIZATION", strictIdentifierTerm);
            gb.Prod(schemaNameClause).Is(strictIdentifierTerm, "AUTHORIZATION", strictIdentifierTerm);
        }

        public static void BuildPostQuery(
            MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var truncateStatement = gb.NT("TruncateStatement");
            var createTableAsSelectStatement = gb.NT("CreateTableAsSelectStatement");
            var createTableOptionList = gb.NT("CreateTableOptionList");
            var alterDatabaseStatement = gb.NT("AlterDatabaseStatement");
            var alterDatabaseSetOption = gb.NT("AlterDatabaseSetOption");
            var alterDatabaseSetOnOffOption = gb.NT("AlterDatabaseSetOnOffOption");
            var alterDatabaseSetEqualsOnOffOption = gb.NT("AlterDatabaseSetEqualsOnOffOption");
            var alterDatabaseSetModeOption = gb.NT("AlterDatabaseSetModeOption");
            var alterDatabaseRecoveryModel = gb.NT("AlterDatabaseRecoveryModel");
            var alterDatabasePageVerifyMode = gb.NT("AlterDatabasePageVerifyMode");
            var alterDatabaseCursorDefaultMode = gb.NT("AlterDatabaseCursorDefaultMode");
            var alterDatabaseParameterizationMode = gb.NT("AlterDatabaseParameterizationMode");
            var alterDatabaseTargetRecoveryUnit = gb.NT("AlterDatabaseTargetRecoveryUnit");
            var alterDatabaseDelayedDurabilityMode = gb.NT("AlterDatabaseDelayedDurabilityMode");
            var alterDatabaseTerminationClause = gb.NT("AlterDatabaseTerminationClause");
            var alterDatabaseTerminationOpt = gb.NT("AlterDatabaseTerminationOpt");
            var createLoginPasswordSpec = gb.NT("CreateLoginPasswordSpec");
            var createLoginOptionList = gb.NT("CreateLoginOptionList");
            var createLoginOption = gb.NT("CreateLoginOption");
            var createLoginWindowsOptionList = gb.NT("CreateLoginWindowsOptionList");
            var createLoginWindowsOption = gb.NT("CreateLoginWindowsOption");
            var createLoginStatement = gb.NT("CreateLoginStatement");
            var createUserStatement = gb.NT("CreateUserStatement");
            var createStatisticsStatement = gb.NT("CreateStatisticsStatement");
            var dropTypeStatement = gb.NT("DropTypeStatement");
            var dropColumnEncryptionKeyStatement = gb.NT("DropColumnEncryptionKeyStatement");
            var revertStatement = gb.NT("RevertStatement");
            var dropEventSessionStatement = gb.NT("DropEventSessionStatement");
            var createTypeStatement = gb.NT("CreateTypeStatement");
            var tableTypeDefinition = gb.NT("TableTypeDefinition");
            var checkpointStatement = gb.NT("CheckpointStatement");
            var expression = gb.NT("Expression");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var namedOptionValue = gb.NT("NamedOptionValue");
            var qualifiedName = gb.NT("QualifiedName");
            var typeSpec = gb.NT("TypeSpec");
            var indexOptionList = gb.NT("IndexOptionList");
            var indexOnOffValue = gb.NT("IndexOnOffValue");
            var queryExpression = gb.NT("QueryExpression");
            var stringLiteral = context.StringLiteralTerminal;
            var alterDatabaseTarget = gb.NT("AlterDatabaseTarget");
            var statisticsIdentifierTerm = gb.NT("StatisticsIdentifierTerm");
            var statisticsIdentifierList = gb.NT("StatisticsIdentifierList");

            gb.Rule(statisticsIdentifierTerm)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("NAME");
            gb.Rule(statisticsIdentifierList).SeparatedBy(",", statisticsIdentifierTerm);

            gb.Prod(truncateStatement).Is("TRUNCATE", "TABLE", qualifiedName);

            gb.Prod(createTableAsSelectStatement).Is("CREATE", "TABLE", qualifiedName, "AS", queryExpression);
            gb.Prod(createTableAsSelectStatement).Is("CREATE", "TABLE", qualifiedName, "WITH", "(", createTableOptionList, ")", "AS", queryExpression);

            gb.Rule(alterDatabaseTarget)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("CURRENT");
            gb.Prod(alterDatabaseStatement).Is("ALTER", "DATABASE", alterDatabaseTarget, "SET", alterDatabaseSetOption);
            gb.Prod(alterDatabaseStatement).Is("ALTER", "DATABASE", "SCOPED", "CONFIGURATION", "CLEAR", strictIdentifierTerm);
            gb.Prod(alterDatabaseStatement).Is("ALTER", "DATABASE", "SCOPED", "CONFIGURATION", "SET", strictIdentifierTerm, "=", expression);
            gb.Rule(alterDatabaseSetOnOffOption).Keywords(
                "ALLOW_SNAPSHOT_ISOLATION",
                "AUTO_CREATE_STATISTICS",
                "AUTO_UPDATE_STATISTICS",
                "AUTO_UPDATE_STATISTICS_ASYNC",
                "ANSI_NULL_DEFAULT",
                "ANSI_NULLS",
                "ANSI_PADDING",
                "ANSI_WARNINGS",
                "ARITHABORT",
                "AUTO_CLOSE",
                "AUTO_SHRINK",
                "CONCAT_NULL_YIELDS_NULL",
                "CURSOR_CLOSE_ON_COMMIT",
                "DATE_CORRELATION_OPTIMIZATION",
                "DB_CHAINING",
                "HONOR_BROKER_PRIORITY",
                "QUOTED_IDENTIFIER",
                "NUMERIC_ROUNDABORT",
                "READ_COMMITTED_SNAPSHOT",
                "RECURSIVE_TRIGGERS",
                "TRUSTWORTHY");
            gb.Rule(alterDatabaseSetEqualsOnOffOption).Keywords(
                "MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT");
            gb.Rule(alterDatabaseSetModeOption).Keywords(
                "READ_ONLY",
                "READ_WRITE",
                "SINGLE_USER",
                "RESTRICTED_USER",
                "MULTI_USER",
                "ENABLE_BROKER",
                "DISABLE_BROKER",
                "NEW_BROKER",
                "ERROR_BROKER_CONVERSATIONS");
            gb.Rule(alterDatabaseRecoveryModel).Keywords("FULL", "SIMPLE", "BULK_LOGGED");
            gb.Rule(alterDatabasePageVerifyMode).Keywords("CHECKSUM", "NONE", "TORN_PAGE_DETECTION");
            gb.Rule(alterDatabaseCursorDefaultMode).Keywords("LOCAL", "GLOBAL");
            gb.Rule(alterDatabaseParameterizationMode).Keywords("SIMPLE", "FORCED");
            gb.Rule(alterDatabaseTargetRecoveryUnit).Keywords("SECONDS", "MINUTES");
            gb.Rule(alterDatabaseDelayedDurabilityMode).Keywords("DISABLED", "ALLOWED", "FORCED");
            gb.Rule(alterDatabaseTerminationClause).OneOf(
                gb.Seq("WITH", "NO_WAIT"),
                gb.Seq("WITH", "ROLLBACK", "IMMEDIATE"),
                gb.Seq("WITH", "ROLLBACK", "AFTER", expression));
            gb.Opt(alterDatabaseTerminationOpt, alterDatabaseTerminationClause);

            gb.Prod(alterDatabaseSetOption).Is(alterDatabaseSetModeOption);
            gb.Prod(alterDatabaseSetOption).Is(alterDatabaseSetOnOffOption, "ON", alterDatabaseTerminationOpt);
            gb.Prod(alterDatabaseSetOption).Is(alterDatabaseSetOnOffOption, "OFF", alterDatabaseTerminationOpt);
            gb.Prod(alterDatabaseSetOption).Is(alterDatabaseSetEqualsOnOffOption, "=", "ON");
            gb.Prod(alterDatabaseSetOption).Is(alterDatabaseSetEqualsOnOffOption, "=", "OFF");
            gb.Prod(alterDatabaseSetOption).Is("COMPATIBILITY_LEVEL", "=", expression);
            gb.Prod(alterDatabaseSetOption).Is("RECOVERY", alterDatabaseRecoveryModel);
            gb.Prod(alterDatabaseSetOption).Is("PAGE_VERIFY", alterDatabasePageVerifyMode);
            gb.Prod(alterDatabaseSetOption).Is("CURSOR_DEFAULT", alterDatabaseCursorDefaultMode);
            gb.Prod(alterDatabaseSetOption).Is("PARAMETERIZATION", alterDatabaseParameterizationMode);
            gb.Prod(alterDatabaseSetOption).Is("TARGET_RECOVERY_TIME", "=", expression, alterDatabaseTargetRecoveryUnit);
            gb.Prod(alterDatabaseSetOption).Is("DELAYED_DURABILITY", "=", alterDatabaseDelayedDurabilityMode);
            gb.Prod(alterDatabaseSetOption).Is("QUERY_STORE", "CLEAR");
            gb.Prod(alterDatabaseSetOption).Is("QUERY_STORE", "CLEAR", "ALL");
            gb.Prod(alterDatabaseSetOption).Is("QUERY_STORE", "=", "ON");
            gb.Prod(alterDatabaseSetOption).Is("QUERY_STORE", "=", "OFF");
            gb.Prod(alterDatabaseSetOption).Is("QUERY_STORE", "(", indexOptionList, ")");
            gb.Prod(alterDatabaseSetOption).Is("QUERY_STORE", "=", "ON", "(", indexOptionList, ")");
            gb.Prod(alterDatabaseSetOption).Is("QUERY_STORE", "=", "OFF", "(", indexOptionList, ")");
            gb.Prod(alterDatabaseSetOption).Is("AUTOMATIC_TUNING", "(", indexOptionList, ")");
            gb.Prod(alterDatabaseSetOption).Is("FILESTREAM", "(", indexOptionList, ")");

            gb.Prod(createLoginStatement).Is("CREATE", "LOGIN", strictIdentifierTerm, "WITH", createLoginPasswordSpec);
            gb.Prod(createLoginStatement).Is("CREATE", "LOGIN", strictIdentifierTerm, "WITH", createLoginPasswordSpec, ",", createLoginOptionList);
            gb.Prod(createLoginStatement).Is("CREATE", "LOGIN", strictIdentifierTerm, "FROM", "WINDOWS");
            gb.Prod(createLoginStatement).Is("CREATE", "LOGIN", strictIdentifierTerm, "FROM", "WINDOWS", "WITH", createLoginWindowsOptionList);
            gb.Prod(createLoginStatement).Is("CREATE", "LOGIN", strictIdentifierTerm, "FROM", "EXTERNAL", "PROVIDER");
            gb.Prod(createLoginStatement).Is("CREATE", "LOGIN", strictIdentifierTerm, "FROM", "EXTERNAL", "PROVIDER", "WITH", "OBJECT_ID", "=", stringLiteral);
            gb.Prod(createLoginStatement).Is("CREATE", "LOGIN", strictIdentifierTerm, "FROM", "CERTIFICATE", strictIdentifierTerm);
            gb.Prod(createLoginStatement).Is("CREATE", "LOGIN", strictIdentifierTerm, "FROM", "ASYMMETRIC", "KEY", strictIdentifierTerm);
            gb.Prod(createLoginPasswordSpec).Is("PASSWORD", "=", expression);
            gb.Prod(createLoginPasswordSpec).Is("PASSWORD", "=", expression, "HASHED");
            gb.Prod(createLoginPasswordSpec).Is("PASSWORD", "=", expression, "MUST_CHANGE");
            gb.Prod(createLoginPasswordSpec).Is("PASSWORD", "=", expression, "HASHED", "MUST_CHANGE");
            gb.Rule(createLoginOptionList).SeparatedBy(",", createLoginOption);
            gb.Prod(createLoginOption).Is("SID", "=", expression);
            gb.Prod(createLoginOption).Is("DEFAULT_DATABASE", "=", namedOptionValue);
            gb.Prod(createLoginOption).Is("DEFAULT_LANGUAGE", "=", namedOptionValue);
            gb.Prod(createLoginOption).Is("CHECK_EXPIRATION", "=", indexOnOffValue);
            gb.Prod(createLoginOption).Is("CHECK_POLICY", "=", indexOnOffValue);
            gb.Prod(createLoginOption).Is("CREDENTIAL", "=", namedOptionValue);
            gb.Rule(createLoginWindowsOptionList).SeparatedBy(",", createLoginWindowsOption);
            gb.Prod(createLoginWindowsOption).Is("DEFAULT_DATABASE", "=", namedOptionValue);
            gb.Prod(createLoginWindowsOption).Is("DEFAULT_LANGUAGE", "=", namedOptionValue);

            gb.Prod(createUserStatement).Is("CREATE", "USER", strictIdentifierTerm, "FOR", "LOGIN", strictIdentifierTerm);
            gb.Prod(createUserStatement).Is("CREATE", "USER", strictIdentifierTerm, "WITHOUT", "LOGIN");

            gb.Prod(createStatisticsStatement).Is("CREATE", "STATISTICS", strictIdentifierTerm, "ON", qualifiedName, "(", statisticsIdentifierList, ")");
            gb.Prod(createStatisticsStatement).Is("CREATE", "STATISTICS", strictIdentifierTerm, "ON", qualifiedName, "(", statisticsIdentifierList, ")", "WITH", "(", indexOptionList, ")");

            gb.Prod(dropTypeStatement).Is("DROP", "TYPE", qualifiedName);
            gb.Prod(dropTypeStatement).Is("DROP", "TYPE", "IF", "EXISTS", qualifiedName);

            gb.Prod(dropColumnEncryptionKeyStatement).Is("DROP", "COLUMN", "ENCRYPTION", "KEY", strictIdentifierTerm);
            gb.Prod(dropColumnEncryptionKeyStatement).Is("DROP", "COLUMN", "MASTER", "KEY", strictIdentifierTerm);

            gb.Rule(revertStatement).OneOf(
                "REVERT",
                gb.Seq("REVERT", "WITH", "COOKIE", "=", expression));
            gb.Rule(dropEventSessionStatement).OneOf(
                gb.Seq("DROP", "EVENT", "SESSION", strictIdentifierTerm, "ON", "DATABASE"),
                gb.Seq("DROP", "EVENT", "SESSION", strictIdentifierTerm, "ON", "SERVER"));

            gb.Prod(createTypeStatement).Is("CREATE", "TYPE", qualifiedName, "AS", tableTypeDefinition);
            gb.Prod(createTypeStatement).Is("CREATE", "TYPE", qualifiedName, "FROM", typeSpec);

            gb.Prod(checkpointStatement).Is("CHECKPOINT");
        }
    }
}
