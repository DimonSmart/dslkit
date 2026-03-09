using System.Collections.Generic;
using DSLKIT.NonTerminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlSecurityAndAdminGrammar
    {
        public static void BuildPreQuery(
            MsSqlGrammarContext context,
            INonTerminal grantPermissionSet,
            INonTerminal grantPermissionList,
            INonTerminal grantPermissionItem,
            INonTerminal grantPermission,
            INonTerminal grantPermissionWord,
            INonTerminal grantOnClause,
            INonTerminal grantClassType,
            INonTerminal grantSecurable,
            INonTerminal grantPrincipalList,
            INonTerminal grantPrincipal,
            INonTerminal grantStatement,
            INonTerminal dbccCommand,
            INonTerminal dbccParamList,
            INonTerminal dbccParam,
            INonTerminal dbccOptionList,
            INonTerminal dbccOption,
            INonTerminal dbccOptionName,
            INonTerminal dbccOptionValue,
            INonTerminal dbccStatement,
            INonTerminal dropProcStatement,
            INonTerminal dropIfExistsClause,
            INonTerminal dropTableStatement,
            INonTerminal dropTableTargetList,
            INonTerminal dropViewStatement,
            INonTerminal dropViewTargetList,
            INonTerminal dropIndexStatement,
            INonTerminal dropIndexSpecList,
            INonTerminal dropIndexSpec,
            INonTerminal dropIndexOptionList,
            INonTerminal dropIndexOption,
            INonTerminal dropMoveToTarget,
            INonTerminal dropFileStreamTarget,
            INonTerminal dropStatisticsStatement,
            INonTerminal dropStatisticsTargetList,
            INonTerminal dropStatisticsTarget,
            INonTerminal dropDatabaseStatement,
            INonTerminal createTriggerHead,
            INonTerminal createTriggerFireClause,
            INonTerminal createTriggerEventList,
            INonTerminal createTriggerEvent,
            INonTerminal createTriggerWithOptionList,
            INonTerminal createTriggerWithOption,
            INonTerminal createTriggerStatement,
            INonTerminal createProcExecuteAsClause,
            INonTerminal createProcBodyBlock,
            INonTerminal dropTriggerStatement,
            INonTerminal createRoleStatement,
            INonTerminal createSchemaStatement,
            INonTerminal schemaNameClause)
        {
            var gb = context.Gb;
            var expression = context.Symbols.Expression;
            var identifierList = context.Symbols.IdentifierList;
            var identifierTerm = context.Symbols.IdentifierTerm;
            var qualifiedName = context.Symbols.QualifiedName;
            var strictIdentifierTerm = context.Symbols.StrictIdentifierTerm;
            var strictQualifiedName = context.Symbols.StrictQualifiedName;

            gb.Prod(grantPermissionSet).Is("ALL");
            gb.Prod(grantPermissionSet).Is("ALL", "PRIVILEGES");
            gb.Prod(grantPermissionSet).Is(grantPermissionList);
            gb.Prod(grantPermissionList).Is(grantPermissionItem);
            gb.Prod(grantPermissionList).Is(grantPermissionList, ",", grantPermissionItem);
            gb.Prod(grantPermissionItem).Is(grantPermission);
            gb.Prod(grantPermissionItem).Is(grantPermission, "(", identifierList, ")");

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

            gb.Prod(grantPrincipalList).Is(grantPrincipal);
            gb.Prod(grantPrincipalList).Is(grantPrincipalList, ",", grantPrincipal);
            gb.Prod(grantPrincipal).Is(strictIdentifierTerm);
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

            var dbccCommandNames = new List<string>
            {
                "CHECKDB",
                "DROPCLEANBUFFERS",
                "TRACESTATUS",
                "FREEPROCCACHE",
                "SHRINKFILE",
                "LOGINFO",
                "TRACEON",
                "PAGE",
                "WRITEPAGE"
            };
            if (context.HasFeature(MsSqlDialectFeatures.SynapseExtensions))
            {
                dbccCommandNames.Add("PDW_SHOWSPACEUSED");
            }

            gb.Rule(dbccCommand).Keywords([.. dbccCommandNames]);
            gb.Prod(dbccStatement).Is("DBCC", dbccCommand);
            gb.Prod(dbccStatement).Is("DBCC", dbccCommand, "(", dbccParamList, ")");
            gb.Prod(dbccStatement).Is("DBCC", dbccCommand, "WITH", dbccOptionList);
            gb.Prod(dbccStatement).Is("DBCC", dbccCommand, "(", dbccParamList, ")", "WITH", dbccOptionList);
            gb.Prod(dbccParamList).Is(dbccParam);
            gb.Prod(dbccParamList).Is(dbccParamList, ",", dbccParam);
            gb.Prod(dbccParam).Is(expression);
            gb.Prod(dbccParam).Is(strictIdentifierTerm);
            gb.Prod(dbccParam).Is(strictQualifiedName);
            gb.Rule(dbccOptionList).SeparatedBy(",", dbccOption);
            gb.Prod(dbccOption).Is(dbccOptionName);
            gb.Prod(dbccOption).Is(dbccOptionName, "=", dbccOptionValue);
            gb.Rule(dbccOptionName)
                .Keywords("NO_INFOMSGS", "ALL_ERRORMSGS", "MAXDOP", "TABLERESULTS");
            gb.Rule(dbccOptionValue)
                .CanBe(expression)
                .Or(strictIdentifierTerm)
                .OrKeywords("ON", "OFF");

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
            gb.Prod(dropTableTargetList).Is(qualifiedName);
            gb.Prod(dropTableTargetList).Is(dropTableTargetList, ",", qualifiedName);

            gb.Prod(dropViewStatement).Is("DROP", "VIEW", dropViewTargetList);
            gb.Prod(dropViewStatement).Is("DROP", "VIEW", dropIfExistsClause, dropViewTargetList);
            gb.Prod(dropViewTargetList).Is(qualifiedName);
            gb.Prod(dropViewTargetList).Is(dropViewTargetList, ",", qualifiedName);

            gb.Prod(dropIndexStatement).Is("DROP", "INDEX", dropIndexSpecList);
            gb.Prod(dropIndexStatement).Is("DROP", "INDEX", dropIfExistsClause, dropIndexSpecList);
            gb.Prod(dropIndexSpecList).Is(dropIndexSpec);
            gb.Prod(dropIndexSpecList).Is(dropIndexSpecList, ",", dropIndexSpec);
            gb.Prod(dropIndexSpec).Is(qualifiedName, "ON", qualifiedName);
            gb.Prod(dropIndexSpec).Is(qualifiedName, "ON", qualifiedName, "WITH", "(", dropIndexOptionList, ")");
            gb.Prod(dropIndexSpec).Is(qualifiedName, ".", strictIdentifierTerm);
            gb.Prod(dropIndexOptionList).Is(dropIndexOption);
            gb.Prod(dropIndexOptionList).Is(dropIndexOptionList, ",", dropIndexOption);
            gb.Rule(dropIndexOption)
                .CanBe("MAXDOP", "=", expression)
                .Or("ONLINE", "=", "ON")
                .Or("ONLINE", "=", "OFF")
                .Or("MOVE", "TO", dropMoveToTarget)
                .Or("MOVE", "TO", dropMoveToTarget, "FILESTREAM_ON", dropFileStreamTarget)
                .Or("FILESTREAM_ON", dropFileStreamTarget);
            gb.Rule(dropMoveToTarget)
                .CanBe(qualifiedName)
                .OrKeywords("DEFAULT")
                .Or(qualifiedName, "(", identifierTerm, ")");
            gb.Rule(dropFileStreamTarget)
                .CanBe(qualifiedName)
                .OrKeywords("DEFAULT");

            gb.Prod(dropStatisticsStatement).Is("DROP", "STATISTICS", dropStatisticsTargetList);
            gb.Prod(dropStatisticsTargetList).Is(dropStatisticsTarget);
            gb.Prod(dropStatisticsTargetList).Is(dropStatisticsTargetList, ",", dropStatisticsTarget);
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
            MsSqlGrammarContext context,
            INonTerminal truncateStatement,
            INonTerminal createTableAsSelectStatement,
            INonTerminal createTableOptionList,
            INonTerminal alterDatabaseStatement,
            INonTerminal alterDatabaseSetOption,
            INonTerminal alterDatabaseSetOnOffOption,
            INonTerminal alterDatabaseSetEqualsOnOffOption,
            INonTerminal alterDatabaseSetModeOption,
            INonTerminal alterDatabaseRecoveryModel,
            INonTerminal alterDatabasePageVerifyMode,
            INonTerminal alterDatabaseCursorDefaultMode,
            INonTerminal alterDatabaseParameterizationMode,
            INonTerminal alterDatabaseTargetRecoveryUnit,
            INonTerminal alterDatabaseDelayedDurabilityMode,
            INonTerminal alterDatabaseTerminationClause,
            INonTerminal alterDatabaseTerminationOpt,
            INonTerminal createLoginPasswordSpec,
            INonTerminal createLoginOptionList,
            INonTerminal createLoginOption,
            INonTerminal createLoginWindowsOptionList,
            INonTerminal createLoginWindowsOption,
            INonTerminal createLoginStatement,
            INonTerminal createUserStatement,
            INonTerminal createStatisticsStatement,
            INonTerminal dropTypeStatement,
            INonTerminal dropColumnEncryptionKeyStatement,
            INonTerminal revertStatement,
            INonTerminal dropEventSessionStatement,
            INonTerminal createTypeStatement,
            INonTerminal tableTypeDefinition,
            INonTerminal checkpointStatement)
        {
            var gb = context.Gb;
            var expression = context.Symbols.Expression;
            var identifierList = context.Symbols.IdentifierList;
            var identifierTerm = context.Symbols.IdentifierTerm;
            var strictIdentifierTerm = context.Symbols.StrictIdentifierTerm;
            var namedOptionValue = context.Symbols.NamedOptionValue;
            var qualifiedName = context.Symbols.QualifiedName;
            var typeSpec = context.Symbols.TypeSpec;
            var indexOptionList = context.Symbols.IndexOptionList;
            var indexOnOffValue = context.Symbols.IndexOnOffValue;
            var queryExpression = context.Symbols.QueryExpression;
            var stringLiteral = context.StringLiteralTerminal;
            var alterDatabaseTarget = gb.NT("AlterDatabaseTarget");

            gb.Prod(truncateStatement).Is("TRUNCATE", "TABLE", qualifiedName);

            gb.Prod(createTableAsSelectStatement).Is("CREATE", "TABLE", qualifiedName, "AS", queryExpression);
            gb.Prod(createTableAsSelectStatement).Is("CREATE", "TABLE", qualifiedName, "WITH", "(", createTableOptionList, ")", "AS", queryExpression);

            gb.Rule(alterDatabaseTarget)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("CURRENT");
            gb.Prod(alterDatabaseStatement).Is("ALTER", "DATABASE", alterDatabaseTarget, "SET", alterDatabaseSetOption);
            gb.Prod(alterDatabaseStatement).Is("ALTER", "DATABASE", "SCOPED", "CONFIGURATION", "CLEAR", identifierTerm);
            gb.Prod(alterDatabaseStatement).Is("ALTER", "DATABASE", "SCOPED", "CONFIGURATION", "SET", identifierTerm, "=", expression);
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

            gb.Prod(createStatisticsStatement).Is("CREATE", "STATISTICS", strictIdentifierTerm, "ON", qualifiedName, "(", identifierList, ")");
            gb.Prod(createStatisticsStatement).Is("CREATE", "STATISTICS", strictIdentifierTerm, "ON", qualifiedName, "(", identifierList, ")", "WITH", "(", indexOptionList, ")");

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
