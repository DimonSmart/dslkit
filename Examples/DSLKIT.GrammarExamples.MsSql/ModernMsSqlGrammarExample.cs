using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Formatting;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using DSLKIT.Tokens;

namespace DSLKIT.GrammarExamples.MsSql
{
    /// <summary>
    /// SQL Server 2022 / Azure SQL query-language grammar subset.
    /// Focus: SELECT, CTE, joins, set operators, window functions and CASE.
    /// </summary>
    public static class ModernMsSqlGrammarExample
    {
        private static readonly Lazy<IGrammar> GrammarCache = new(BuildGrammarCore);

        public static IGrammar BuildGrammar()
        {
            return GrammarCache.Value;
        }

        public static ParseResult ParseScript(string source)
        {
            var grammar = BuildGrammar();
            var lexer = new Lexer.Lexer(CreateLexerSettings(grammar));
            var parser = new SyntaxParser(grammar);

            var rawTokens = lexer.GetTokens(new StringSourceStream(source)).ToList();
            var lexerError = rawTokens.OfType<ErrorToken>().FirstOrDefault();
            if (lexerError != null)
            {
                return new ParseResult
                {
                    Error = new ParseErrorDescription(
                        $"Lexer error: {lexerError.ErrorMessage}",
                        lexerError.Position)
                };
            }

            var tokens = BuildSignificantTokensWithTrivia(rawTokens);

            // An empty significant-token list (e.g. file contains only whitespace/comments)
            // counts as a valid, empty script — build an empty Script parse tree.
            // Note: the lexer always appends an EofToken (TermFlags.None) which is NOT
            // filtered as trivia, so a comment-only file yields tokens=[EofToken] (count=1).
            if (tokens.Count == 0 || (tokens.Count == 1 && tokens[0].Terminal == grammar.Eof))
            {
                var scriptNonTerminal = grammar.NonTerminals.First(nt => nt.Name == "Script");
                var emptyProduction = grammar.Productions
                    .First(p => p.LeftNonTerminal == scriptNonTerminal
                        && p.ProductionDefinition.Count == 1
                        && p.ProductionDefinition[0] is DSLKIT.SpecialTerms.EmptyTerm);
                var scriptNode = new NonTerminalNode(scriptNonTerminal, emptyProduction, []);
                var startNonTerminal = grammar.Root;
                var startProduction = grammar.Productions.First(p => p.LeftNonTerminal == startNonTerminal);
                var startNode = new NonTerminalNode(startNonTerminal, startProduction, [scriptNode]);
                return new ParseResult
                {
                    ParseTree = startNode,
                    Productions = [grammar.Productions.ToList().IndexOf(startProduction),
                                   grammar.Productions.ToList().IndexOf(emptyProduction)]
                };
            }

            return parser.Parse(tokens);
        }

        public static void ParseScriptOrThrow(string source)
        {
            var parseResult = ParseScript(source);
            if (!parseResult.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Parse failed. Position: {parseResult.Error?.ErrorPosition}. Message: {parseResult.Error?.Message}");
            }
        }

        public static LexerSettings CreateLexerSettings(IGrammar grammar)
        {
            var settings = new LexerSettings();
            foreach (var terminal in grammar.Terminals)
            {
                settings.Add(terminal);
            }

            return settings;
        }

        private static IReadOnlyList<IToken> BuildSignificantTokensWithTrivia(IReadOnlyList<IToken> rawTokens)
        {
            var significantTokens = new List<IToken>(rawTokens.Count);
            var pendingTrivia = new List<IToken>();

            foreach (var token in rawTokens)
            {
                var terminalFlags = token.Terminal?.Flags;
                var isTriviaToken = terminalFlags == TermFlags.Space ||
                    terminalFlags == TermFlags.Comment;
                if (isTriviaToken)
                {
                    pendingTrivia.Add(token);
                    continue;
                }

                if (pendingTrivia.Count == 0)
                {
                    significantTokens.Add(token);
                    continue;
                }

                var tokenWithLeadingTrivia = token.WithTrivia(new FormattingTrivia(pendingTrivia.ToList(), []));
                significantTokens.Add(tokenWithLeadingTrivia);
                pendingTrivia.Clear();
            }

            if (pendingTrivia.Count == 0 || significantTokens.Count == 0)
            {
                return significantTokens;
            }

            var lastToken = significantTokens[^1];
            var trivia = lastToken.Trivia;
            var tokenWithTrailingTrivia = lastToken.WithTrivia(new FormattingTrivia(trivia.LeadingTrivia, pendingTrivia.ToList()));
            significantTokens[^1] = tokenWithTrailingTrivia;
            return significantTokens;
        }

        private static IGrammar BuildGrammarCore()
        {
            var identifier = new IdentifierTerminal(allowDot: false);

            var bracketIdentifier = new RegExpTerminal(
                "BracketIdentifier",
                @"\G\[(?:[^\]\r\n]|]])+\]",
                previewChar: '[',
                flags: TermFlags.Identifier);

            var quotedIdentifier = new RegExpTerminal(
                "QuotedIdentifier",
                "\\G\"(?:[^\"]|\"\")+\"",
                previewChar: '"',
                flags: TermFlags.Identifier);

            var variable = new RegExpTerminal(
                "Variable",
                @"\G(?i)@@?[a-z_][a-z0-9_@$#]*",
                previewChar: '@',
                flags: TermFlags.Identifier);

            var tempIdentifier = new RegExpTerminal(
                "TempIdentifier",
                @"\G##?[a-z_][a-z0-9_$#]*",
                previewChar: '#',
                flags: TermFlags.Identifier);

            // SQLCMD/SSDT substitution variables: $(DatabaseName), $(SQLCMDSERVER), etc.
            // Treated as preprocessor placeholders — recognised as identifiers so the
            // formatter can round-trip scripts that contain them unchanged.
            var sqlcmdVariable = new RegExpTerminal(
                "SqlcmdVariable",
                @"\G\$\([a-zA-Z_][a-zA-Z0-9_]*\)",
                previewChar: '$',
                flags: TermFlags.Identifier);

            // SQLCMD preprocessor directives: :r, :setvar, :on error exit, etc.
            // Matched as a single token per line to keep parser logic simple.
            var sqlcmdPreprocessorCommand = new RegExpTerminal(
                "SqlcmdPreprocessorCommand",
                @"\G:[a-z_][a-z0-9_]*(?:[ \t][^\r\n]*)?",
                previewChar: ':',
                flags: TermFlags.None);

            // Combined terminal for "FOR SYSTEM_TIME" to reduce ambiguity with FOR JSON/XML/BROWSE.
            var forSystemTime = new RegExpTerminal(
                "FOR_SYSTEM_TIME",
                @"\G(?i)FOR\s+SYSTEM_TIME(?!\w)",
                previewChar: null,
                flags: TermFlags.None);

            // Combined terminal for SQL Graph table source syntax: "FOR PATH".
            // This avoids ambiguity with query FOR JSON / FOR XML clauses.
            var forPath = new RegExpTerminal(
                "FOR_PATH",
                @"\G(?i)FOR\s+PATH(?!\w)",
                previewChar: null,
                flags: TermFlags.None);

            // SQL Graph pseudo-column references: $node_id, $from_id, $to_id, etc.
            var graphColumnRef = new RegExpTerminal(
                "GraphColumnRef",
                @"\G\$[a-zA-Z_][a-zA-Z0-9_]*",
                previewChar: '$',
                flags: TermFlags.Identifier);

            var number = new NumberTerminal("Number", NumberStyle.SqlNumber, new NumberOptions { AllowHex = true });
            var stringLiteral = new QuotedStringTerminal("String", StringStyle.SqlSingleQuoted);

            var gb = new GrammarBuilder()
                .WithGrammarName("mssql-2022-query")
                .AddTerminal(new SpaceTerminal())
                .AddTerminal(new SingleLineCommentTerminal("--"))
                .AddTerminal(new MultiLineCommentTerminal("/*", "*/"))
                .AddTerminal(identifier)
                .AddTerminal(bracketIdentifier)
                .AddTerminal(quotedIdentifier)
                .AddTerminal(variable)
                .AddTerminal(tempIdentifier)
                .AddTerminal(number)
                .AddTerminal(stringLiteral)
                .AddTerminal(sqlcmdVariable)
                .AddTerminal(sqlcmdPreprocessorCommand)
                .AddTerminal(forSystemTime)
                .AddTerminal(forPath)
                .AddTerminal(graphColumnRef);
            var keywordCache = new Dictionary<string, KeywordTerminal>(StringComparer.OrdinalIgnoreCase);

            KeywordTerminal kw(string keyword)
            {
                if (keywordCache.TryGetValue(keyword, out var cachedKeyword))
                {
                    return cachedKeyword;
                }

                var newKeyword = new KeywordTerminal(keyword, wholeWord: true, ignoreCase: true);
                keywordCache[keyword] = newKeyword;
                return newKeyword;
            }

            // Resolve known LALR(1) ambiguities explicitly.
            gb.OnShiftReduce("IfStatement", "ELSE", Resolve.Shift); // dangling ELSE binds to nearest IF
            gb.OnShiftReduce("QueryUnionExpression", "UNION", Resolve.Reduce);
            gb.OnShiftReduce("QueryUnionExpression", "EXCEPT", Resolve.Reduce);
            gb.OnShiftReduce("QueryIntersectExpression", "INTERSECT", Resolve.Reduce);

            var script = gb.NT("Script");
            var statementList = gb.NT("StatementList");
            var statementSeparator = gb.NT("StatementSeparator");
            var statementSeparatorList = gb.NT("StatementSeparatorList");
            var statement = gb.NT("Statement");
            var queryStatement = gb.NT("QueryStatement");
            var updateStatement = gb.NT("UpdateStatement");
            var updateSetList = gb.NT("UpdateSetList");
            var updateSetItem = gb.NT("UpdateSetItem");
            var compoundAssignOp = gb.NT("CompoundAssignOp");
            var insertStatement = gb.NT("InsertStatement");
            var insertTarget = gb.NT("InsertTarget");
            var insertColumnList = gb.NT("InsertColumnList");
            var insertValueList = gb.NT("InsertValueList");
            var rowValue = gb.NT("RowValue");
            var rowValueList = gb.NT("RowValueList");
            var deleteStatement = gb.NT("DeleteStatement");
            var deleteTopClause = gb.NT("DeleteTopClause");
            var deleteTarget = gb.NT("DeleteTarget");
            var deleteTargetSimple = gb.NT("DeleteTargetSimple");
            var deleteTargetRowset = gb.NT("DeleteTargetRowset");
            var rowsetFunctionLimited = gb.NT("RowsetFunctionLimited");
            var tableHintLimitedList = gb.NT("TableHintLimitedList");
            var tableHintLimited = gb.NT("TableHintLimited");
            var tableHintLimitedName = gb.NT("TableHintLimitedName");
            var deleteStatementTail = gb.NT("DeleteStatementTail");
            var deleteOutputClause = gb.NT("DeleteOutputClause");
            var deleteOutputTarget = gb.NT("DeleteOutputTarget");
            var deleteOutputIntoColumnListOpt = gb.NT("DeleteOutputIntoColumnListOpt");
            var deleteSourceFromClause = gb.NT("DeleteSourceFromClause");
            var deleteWhereClause = gb.NT("DeleteWhereClause");
            var deleteOptionClause = gb.NT("DeleteOptionClause");
            var deleteQueryHintList = gb.NT("DeleteQueryHintList");
            var deleteQueryHint = gb.NT("DeleteQueryHint");
            var deleteQueryHintName = gb.NT("DeleteQueryHintName");
            var optionClause = gb.NT("OptionClause");
            var ifStatement = gb.NT("IfStatement");
            var ifBranchStatement = gb.NT("IfBranchStatement");
            var beginEndStatement = gb.NT("BeginEndStatement");
            var setStatement = gb.NT("SetStatement");
            var printStatement = gb.NT("PrintStatement");
            var declareStatement = gb.NT("DeclareStatement");
            var declareItemList = gb.NT("DeclareItemList");
            var declareItem = gb.NT("DeclareItem");
            var declareTableVariable = gb.NT("DeclareTableVariable");
            var tableTypeDefinition = gb.NT("TableTypeDefinition");
            var typeSpec = gb.NT("TypeSpec");
            var executeStatement = gb.NT("ExecuteStatement");
            var executeModuleCall = gb.NT("ExecuteModuleCall");
            var executeModuleCallCore = gb.NT("ExecuteModuleCallCore");
            var executeReturnAssignment = gb.NT("ExecuteReturnAssignment");
            var executeModuleTarget = gb.NT("ExecuteModuleTarget");
            var executeArgList = gb.NT("ExecuteArgList");
            var executeArg = gb.NT("ExecuteArg");
            var executeArgNamePrefix = gb.NT("ExecuteArgNamePrefix");
            var executeArgValue = gb.NT("ExecuteArgValue");
            var executeWithOptions = gb.NT("ExecuteWithOptions");
            var executeOptionList = gb.NT("ExecuteOptionList");
            var executeOption = gb.NT("ExecuteOption");
            var executeResultSetsDefList = gb.NT("ExecuteResultSetsDefList");
            var executeResultSetsDef = gb.NT("ExecuteResultSetsDef");
            var executeColumnDefList = gb.NT("ExecuteColumnDefList");
            var executeColumnDef = gb.NT("ExecuteColumnDef");
            var executeNullability = gb.NT("ExecuteNullability");
            var executeDynamicCall = gb.NT("ExecuteDynamicCall");
            var executeLinkedArgList = gb.NT("ExecuteLinkedArgList");
            var executeLinkedArg = gb.NT("ExecuteLinkedArg");
            var executeAsContext = gb.NT("ExecuteAsContext");
            var executeAtClause = gb.NT("ExecuteAtClause");
            var useStatement = gb.NT("UseStatement");
            var createProcStatement = gb.NT("CreateProcStatement");
            var createFunctionStatement = gb.NT("CreateFunctionStatement");
            var createFunctionHead = gb.NT("CreateFunctionHead");
            var createFunctionName = gb.NT("CreateFunctionName");
            var createFunctionSignature = gb.NT("CreateFunctionSignature");
            var createFunctionParameterList = gb.NT("CreateFunctionParameterList");
            var createFunctionParameter = gb.NT("CreateFunctionParameter");
            var createFunctionParameterOptionList = gb.NT("CreateFunctionParameterOptionList");
            var createFunctionParameterOption = gb.NT("CreateFunctionParameterOption");
            var createFunctionReturnsClause = gb.NT("CreateFunctionReturnsClause");
            var createFunctionWithClause = gb.NT("CreateFunctionWithClause");
            var createFunctionOptionList = gb.NT("CreateFunctionOptionList");
            var createFunctionOption = gb.NT("CreateFunctionOption");
            var createFunctionBody = gb.NT("CreateFunctionBody");
            var createFunctionTableReturnDefinition = gb.NT("CreateFunctionTableReturnDefinition");
            var createFunctionTableReturnItemList = gb.NT("CreateFunctionTableReturnItemList");
            var createFunctionTableReturnItem = gb.NT("CreateFunctionTableReturnItem");
            var grantStatement = gb.NT("GrantStatement");
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
            var dbccStatement = gb.NT("DbccStatement");
            var dbccCommand = gb.NT("DbccCommand");
            var dbccParamList = gb.NT("DbccParamList");
            var dbccParam = gb.NT("DbccParam");
            var dbccOptionList = gb.NT("DbccOptionList");
            var dbccOption = gb.NT("DbccOption");
            var dbccOptionName = gb.NT("DbccOptionName");
            var dbccOptionValue = gb.NT("DbccOptionValue");
            var dropProcStatement = gb.NT("DropProcStatement");
            var dropTableStatement = gb.NT("DropTableStatement");
            var dropViewStatement = gb.NT("DropViewStatement");
            var dropIndexStatement = gb.NT("DropIndexStatement");
            var dropStatisticsStatement = gb.NT("DropStatisticsStatement");
            var dropIfExistsClause = gb.NT("DropIfExistsClause");
            var dropTableTargetList = gb.NT("DropTableTargetList");
            var dropViewTargetList = gb.NT("DropViewTargetList");
            var dropIndexSpecList = gb.NT("DropIndexSpecList");
            var dropIndexSpec = gb.NT("DropIndexSpec");
            var dropIndexOptionList = gb.NT("DropIndexOptionList");
            var dropIndexOption = gb.NT("DropIndexOption");
            var dropMoveToTarget = gb.NT("DropMoveToTarget");
            var dropFileStreamTarget = gb.NT("DropFileStreamTarget");
            var dropStatisticsTargetList = gb.NT("DropStatisticsTargetList");
            var dropStatisticsTarget = gb.NT("DropStatisticsTarget");
            var createTriggerStatement = gb.NT("CreateTriggerStatement");
            var createTriggerHead = gb.NT("CreateTriggerHead");
            var createTriggerFireClause = gb.NT("CreateTriggerFireClause");
            var createTriggerEventList = gb.NT("CreateTriggerEventList");
            var createTriggerEvent = gb.NT("CreateTriggerEvent");
            var createTriggerWithOptionList = gb.NT("CreateTriggerWithOptionList");
            var createTriggerWithOption = gb.NT("CreateTriggerWithOption");
            var dropTriggerStatement = gb.NT("DropTriggerStatement");
            var createProcHead = gb.NT("CreateProcHead");
            var createProcKeyword = gb.NT("CreateProcKeyword");
            var createProcName = gb.NT("CreateProcName");
            var createProcSignature = gb.NT("CreateProcSignature");
            var createProcParameterList = gb.NT("CreateProcParameterList");
            var createProcParameter = gb.NT("CreateProcParameter");
            var createProcParameterOptionList = gb.NT("CreateProcParameterOptionList");
            var createProcParameterOption = gb.NT("CreateProcParameterOption");
            var createProcWithClause = gb.NT("CreateProcWithClause");
            var createProcOptionList = gb.NT("CreateProcOptionList");
            var createProcOption = gb.NT("CreateProcOption");
            var createProcExecuteAsClause = gb.NT("CreateProcExecuteAsClause");
            var createProcForReplicationClause = gb.NT("CreateProcForReplicationClause");
            var createProcBody = gb.NT("CreateProcBody");
            var createProcBodyBlock = gb.NT("CreateProcBodyBlock");
            var createProcExternalName = gb.NT("CreateProcExternalName");
            var createProcNativeWithClause = gb.NT("CreateProcNativeWithClause");
            var createProcNativeAtomicOptionList = gb.NT("CreateProcNativeAtomicOptionList");
            var createProcNativeAtomicOption = gb.NT("CreateProcNativeAtomicOption");
            var whileStatement = gb.NT("WhileStatement");
            var returnStatement = gb.NT("ReturnStatement");
            var transactionStatement = gb.NT("TransactionStatement");
            var raiserrorStatement = gb.NT("RaiserrorStatement");
            var raiserrorArgList = gb.NT("RaiserrorArgList");
            var raiserrorWithOptionList = gb.NT("RaiserrorWithOptionList");
            var throwStatement = gb.NT("ThrowStatement");
            var loopControlStatement = gb.NT("LoopControlStatement");
            var gotoStatement = gb.NT("GotoStatement");
            var labelStatement = gb.NT("LabelStatement");
            var sqlcmdPreprocessorStatement = gb.NT("SqlcmdPreprocessorStatement");
            var createRoleStatement = gb.NT("CreateRoleStatement");
            var createSchemaStatement = gb.NT("CreateSchemaStatement");
            var schemaNameClause = gb.NT("SchemaNameClause");
            var createViewHead = gb.NT("CreateViewHead");
            var createViewStatement = gb.NT("CreateViewStatement");
            var createTableStatement = gb.NT("CreateTableStatement");
            var createTableFileTableClause = gb.NT("CreateTableFileTableClause");
            var createTableElementList = gb.NT("CreateTableElementList");
            var createTableElement = gb.NT("CreateTableElement");
            var createTableColumnDefinition = gb.NT("CreateTableColumnDefinition");
            var createTableColumnOptionList = gb.NT("CreateTableColumnOptionList");
            var createTableColumnOption = gb.NT("CreateTableColumnOption");
            var createTableComputedColumn = gb.NT("CreateTableComputedColumn");
            var createTableColumnSet = gb.NT("CreateTableColumnSet");
            var createTableConstraint = gb.NT("CreateTableConstraint");
            var createTableConstraintBody = gb.NT("CreateTableConstraintBody");
            var createTableClusterType = gb.NT("CreateTableClusterType");
            var createTableKeyColumnList = gb.NT("CreateTableKeyColumnList");
            var createTableKeyColumn = gb.NT("CreateTableKeyColumn");
            var createTableTableIndex = gb.NT("CreateTableTableIndex");
            var createTablePeriodClause = gb.NT("CreateTablePeriodClause");
            var createTableTailClauseList = gb.NT("CreateTableTailClauseList");
            var createTableTailClause = gb.NT("CreateTableTailClause");
            var createTableOptions = gb.NT("CreateTableOptions");
            var createTableOptionList = gb.NT("CreateTableOptionList");
            var createTableOption = gb.NT("CreateTableOption");
            var createTableOnClause = gb.NT("CreateTableOnClause");
            var createTableTextImageClause = gb.NT("CreateTableTextImageClause");
            var alterTableStatement = gb.NT("AlterTableStatement");
            var alterTableAction = gb.NT("AlterTableAction");
            var alterTableAddItemList = gb.NT("AlterTableAddItemList");
            var alterTableAddItem = gb.NT("AlterTableAddItem");
            var alterTableAlterColumnAction = gb.NT("AlterTableAlterColumnAction");
            var alterTableColumnOptionList = gb.NT("AlterTableColumnOptionList");
            var alterTableColumnOption = gb.NT("AlterTableColumnOption");
            var alterTableDropItemList = gb.NT("AlterTableDropItemList");
            var alterTableDropItem = gb.NT("AlterTableDropItem");
            var alterTableCheckMode = gb.NT("AlterTableCheckMode");
            var alterTableConstraintTarget = gb.NT("AlterTableConstraintTarget");
            var alterTableTriggerTarget = gb.NT("AlterTableTriggerTarget");
            var createIndexStatement = gb.NT("CreateIndexStatement");
            var createIndexHead = gb.NT("CreateIndexHead");
            var createIndexKeyList = gb.NT("CreateIndexKeyList");
            var createIndexKeyItem = gb.NT("CreateIndexKeyItem");
            var createIndexTailClauseList = gb.NT("CreateIndexTailClauseList");
            var createIndexTailClause = gb.NT("CreateIndexTailClause");
            var createIndexIncludeClause = gb.NT("CreateIndexIncludeClause");
            var createIndexIncludeList = gb.NT("CreateIndexIncludeList");
            var createIndexWhereClause = gb.NT("CreateIndexWhereClause");
            var createIndexWithClause = gb.NT("CreateIndexWithClause");
            var createIndexStorageClause = gb.NT("CreateIndexStorageClause");
            var createIndexFileStreamClause = gb.NT("CreateIndexFileStreamClause");
            var indexOptionList = gb.NT("IndexOptionList");
            var indexOption = gb.NT("IndexOption");
            var indexOptionName = gb.NT("IndexOptionName");
            var indexOptionValue = gb.NT("IndexOptionValue");
            var indexOnOffValue = gb.NT("IndexOnOffValue");
            var indexPartitionList = gb.NT("IndexPartitionList");
            var indexPartitionItem = gb.NT("IndexPartitionItem");
            var indexStorageTarget = gb.NT("IndexStorageTarget");
            var indexFileStreamTarget = gb.NT("IndexFileStreamTarget");
            var alterIndexStatement = gb.NT("AlterIndexStatement");
            var alterIndexTarget = gb.NT("AlterIndexTarget");
            var alterIndexAction = gb.NT("AlterIndexAction");
            var alterIndexRebuildSpec = gb.NT("AlterIndexRebuildSpec");
            var alterIndexReorganizeSpec = gb.NT("AlterIndexReorganizeSpec");
            var alterIndexResumeSpec = gb.NT("AlterIndexResumeSpec");
            var alterIndexPartitionSelector = gb.NT("AlterIndexPartitionSelector");
            var createDatabaseStatement = gb.NT("CreateDatabaseStatement");
            var dropDatabaseStatement = gb.NT("DropDatabaseStatement");
            var createDatabaseClauseList = gb.NT("CreateDatabaseClauseList");
            var createDatabaseClause = gb.NT("CreateDatabaseClause");
            var createDatabaseContainmentClause = gb.NT("CreateDatabaseContainmentClause");
            var createDatabaseOnClause = gb.NT("CreateDatabaseOnClause");
            var createDatabaseOnItemList = gb.NT("CreateDatabaseOnItemList");
            var createDatabaseOnItem = gb.NT("CreateDatabaseOnItem");
            var createDatabaseFilespecList = gb.NT("CreateDatabaseFilespecList");
            var createDatabaseFilegroup = gb.NT("CreateDatabaseFilegroup");
            var createDatabaseCollateClause = gb.NT("CreateDatabaseCollateClause");
            var createDatabaseWithClause = gb.NT("CreateDatabaseWithClause");
            var createDatabaseOptionList = gb.NT("CreateDatabaseOptionList");
            var createDatabaseOption = gb.NT("CreateDatabaseOption");
            var createDatabaseOptionValue = gb.NT("CreateDatabaseOptionValue");
            var createDatabaseOnOffValue = gb.NT("CreateDatabaseOnOffValue");
            var createDatabaseFilespec = gb.NT("CreateDatabaseFilespec");
            var createDatabaseFileName = gb.NT("CreateDatabaseFileName");
            var createDatabaseFilespecOptionList = gb.NT("CreateDatabaseFilespecOptionList");
            var createDatabaseFilespecOption = gb.NT("CreateDatabaseFilespecOption");
            var createDatabaseSizeSpec = gb.NT("CreateDatabaseSizeSpec");
            var createDatabaseMaxSizeSpec = gb.NT("CreateDatabaseMaxSizeSpec");
            var createDatabaseGrowthSpec = gb.NT("CreateDatabaseGrowthSpec");
            var createDatabaseSizeUnit = gb.NT("CreateDatabaseSizeUnit");
            var createDatabaseGrowthUnit = gb.NT("CreateDatabaseGrowthUnit");
            var createDatabaseFilestreamOptionList = gb.NT("CreateDatabaseFilestreamOptionList");
            var createDatabaseFilestreamOption = gb.NT("CreateDatabaseFilestreamOption");
            var createDatabaseNonTransactedAccessValue = gb.NT("CreateDatabaseNonTransactedAccessValue");
            var procStatementList = gb.NT("ProcStatementList");
            var tryCatchStatement = gb.NT("TryCatchStatement");
            var truncateStatement = gb.NT("TruncateStatement");
            var createTableAsSelectStatement = gb.NT("CreateTableAsSelectStatement");
            var alterDatabaseStatement = gb.NT("AlterDatabaseStatement");
            var alterDatabaseSetOption = gb.NT("AlterDatabaseSetOption");
            var declareCursorStatement = gb.NT("DeclareCursorStatement");
            var cursorOptionList = gb.NT("CursorOptionList");
            var cursorOperationStatement = gb.NT("CursorOperationStatement");
            var fetchStatement = gb.NT("FetchStatement");
            var fetchDirection = gb.NT("FetchDirection");
            var fetchTargetList = gb.NT("FetchTargetList");
            var waitforStatement = gb.NT("WaitforStatement");
            var createLoginStatement = gb.NT("CreateLoginStatement");
            var createLoginOptionList = gb.NT("CreateLoginOptionList");
            var createLoginOption = gb.NT("CreateLoginOption");
            var bulkInsertStatement = gb.NT("BulkInsertStatement");
            var checkpointStatement = gb.NT("CheckpointStatement");
            var createUserStatement = gb.NT("CreateUserStatement");
            var createStatisticsStatement = gb.NT("CreateStatisticsStatement");
            var updateStatisticsStatement = gb.NT("UpdateStatisticsStatement");
            var updateStatisticsOptionList = gb.NT("UpdateStatisticsOptionList");
            var updateStatisticsOption = gb.NT("UpdateStatisticsOption");
            var dropTypeStatement = gb.NT("DropTypeStatement");
            var dropColumnEncryptionKeyStatement = gb.NT("DropColumnEncryptionKeyStatement");
            var matchGraphPattern = gb.NT("MatchGraphPattern");
            var matchGraphPath = gb.NT("MatchGraphPath");
            var matchGraphShortestPath = gb.NT("MatchGraphShortestPath");
            var graphWithinGroupClause = gb.NT("GraphWithinGroupClause");
            var predictArgList = gb.NT("PredictArgList");
            var predictArg = gb.NT("PredictArg");
            var openRowsetBulk = gb.NT("OpenRowsetBulk");
            var openRowsetBulkOptionList = gb.NT("OpenRowsetBulkOptionList");
            var openRowsetBulkOption = gb.NT("OpenRowsetBulkOption");
            var revertStatement = gb.NT("RevertStatement");
            var dropEventSessionStatement = gb.NT("DropEventSessionStatement");
            var createTypeStatement = gb.NT("CreateTypeStatement");
            var createSecurityPolicyStatement = gb.NT("CreateSecurityPolicyStatement");
            var alterSecurityPolicyStatement = gb.NT("AlterSecurityPolicyStatement");
            var securityPolicyClauseList = gb.NT("SecurityPolicyClauseList");
            var securityPolicyClause = gb.NT("SecurityPolicyClause");
            var securityPolicyOptionList = gb.NT("SecurityPolicyOptionList");
            var securityPolicyOption = gb.NT("SecurityPolicyOption");
            var createExternalTableStatement = gb.NT("CreateExternalTableStatement");
            var createExternalDataSourceStatement = gb.NT("CreateExternalDataSourceStatement");
            var mergeStatement = gb.NT("MergeStatement");
            var mergeWhenList = gb.NT("MergeWhenList");
            var mergeWhen = gb.NT("MergeWhen");
            var mergeMatchedAction = gb.NT("MergeMatchedAction");
            var mergeNotMatchedAction = gb.NT("MergeNotMatchedAction");
            var withClause = gb.NT("WithClause");
            var withXmlNamespacesClause = gb.NT("WithXmlNamespacesClause");
            var xmlNamespaceItemList = gb.NT("XmlNamespaceItemList");
            var xmlNamespaceItem = gb.NT("XmlNamespaceItem");
            var cteDefinitionList = gb.NT("CteDefinitionList");
            var cteDefinition = gb.NT("CteDefinition");
            var queryExpression = gb.NT("QueryExpression");
            var queryUnionExpression = gb.NT("QueryUnionExpression");
            var queryIntersectExpression = gb.NT("QueryIntersectExpression");
            var setOperator = gb.NT("SetOperator");
            var queryPrimary = gb.NT("QueryPrimary");
            var querySpecification = gb.NT("QuerySpecification");
            var selectCore = gb.NT("SelectCore");
            var setQuantifier = gb.NT("SetQuantifier");
            var topClause = gb.NT("TopClause");
            var topValue = gb.NT("TopValue");
            var selectList = gb.NT("SelectList");
            var selectItemList = gb.NT("SelectItemList");
            var selectItem = gb.NT("SelectItem");
            var tableSourceList = gb.NT("TableSourceList");
            var tableSource = gb.NT("TableSource");
            var tableFactor = gb.NT("TableFactor");
            var temporalClause = gb.NT("TemporalClause");
            var pivotClause = gb.NT("PivotClause");
            var pivotValueList = gb.NT("PivotValueList");
            var openJsonWithClause = gb.NT("OpenJsonWithClause");
            var openJsonColumnList = gb.NT("OpenJsonColumnList");
            var openJsonColumnDef = gb.NT("OpenJsonColumnDef");
            var joinPart = gb.NT("JoinPart");
            var joinType = gb.NT("JoinType");
            var orderByClause = gb.NT("OrderByClause");
            var orderItemList = gb.NT("OrderItemList");
            var orderItem = gb.NT("OrderItem");
            var offsetFetchClause = gb.NT("OffsetFetchClause");
            var searchCondition = gb.NT("SearchCondition");
            var expression = gb.NT("Expression");
            var logicalOrExpression = gb.NT("LogicalOrExpression");
            var logicalAndExpression = gb.NT("LogicalAndExpression");
            var logicalNotExpression = gb.NT("LogicalNotExpression");
            var comparisonExpression = gb.NT("ComparisonExpression");
            var comparisonOperator = gb.NT("ComparisonOperator");
            var likeOperator = gb.NT("LikeOperator");
            var inOperator = gb.NT("InOperator");
            var isOperator = gb.NT("IsOperator");
            var betweenOperator = gb.NT("BetweenOperator");
            var additiveExpression = gb.NT("AdditiveExpression");
            var multiplicativeExpression = gb.NT("MultiplicativeExpression");
            var unaryExpression = gb.NT("UnaryExpression");
            var collateExpression = gb.NT("CollateExpression");
            var primaryExpression = gb.NT("PrimaryExpression");
            var overClause = gb.NT("OverClause");
            var overSpec = gb.NT("OverSpec");
            var frameClause = gb.NT("FrameClause");
            var frameBoundary = gb.NT("FrameBoundary");
            var functionCall = gb.NT("FunctionCall");
            var functionArgumentList = gb.NT("FunctionArgumentList");
            var literal = gb.NT("Literal");
            var unicodeStringLiteral = gb.NT("UnicodeStringLiteral");
            var caseExpression = gb.NT("CaseExpression");
            var caseWhenList = gb.NT("CaseWhenList");
            var caseWhen = gb.NT("CaseWhen");
            var expressionList = gb.NT("ExpressionList");
            var identifierList = gb.NT("IdentifierList");
            var identifierTerm = gb.NT("IdentifierTerm");
            var qualifiedName = gb.NT("QualifiedName");
            var variableReference = gb.NT("VariableReference");
            var forClause = gb.NT("ForClause");
            var forJsonMode = gb.NT("ForJsonMode");
            var forJsonOptionList = gb.NT("ForJsonOptionList");
            var forJsonOption = gb.NT("ForJsonOption");
            var forXmlMode = gb.NT("ForXmlMode");
            var forXmlOptionList = gb.NT("ForXmlOptionList");
            var forXmlOption = gb.NT("ForXmlOption");

            gb.Prod("Start").Is(script);
            gb.Prod("Script").Is(statementList);
            gb.Prod("Script").Is(statementList, statementSeparatorList);
            gb.Prod("Script").Is(statementSeparatorList, statementList);
            gb.Prod("Script").Is(statementSeparatorList, statementList, statementSeparatorList);
            gb.Prod("Script").Is(statementSeparatorList);
            gb.Prod("Script").Is(EmptyTerm.Empty);
            gb.Prod("StatementList").Is(statement);
            gb.Prod("StatementList").Is(statementList, statementSeparatorList, statement);
            gb.Prod("StatementList").Is(statementList, statement);
            gb.Prod("StatementSeparatorList").Is(statementSeparator);
            gb.Prod("StatementSeparatorList").Is(statementSeparatorList, statementSeparator);
            gb.Prod("StatementSeparator").Is(";");
            gb.Prod("StatementSeparator").Is(kw("GO"));
            gb.Prod("StatementSeparator").Is(kw("GO"), number); // GO N (batch repeat)
            gb.Prod("Statement").Is(queryStatement);
            gb.Prod("Statement").Is(updateStatement);
            gb.Prod("Statement").Is(insertStatement);
            gb.Prod("Statement").Is(deleteStatement);
            gb.Prod("Statement").Is(ifStatement);
            gb.Prod("Statement").Is(beginEndStatement);
            gb.Prod("Statement").Is(whileStatement);
            gb.Prod("Statement").Is(setStatement);
            gb.Prod("Statement").Is(printStatement);
            gb.Prod("Statement").Is(declareStatement);
            gb.Prod("Statement").Is(returnStatement);
            gb.Prod("Statement").Is(transactionStatement);
            gb.Prod("Statement").Is(raiserrorStatement);
            gb.Prod("Statement").Is(throwStatement);
            gb.Prod("Statement").Is(loopControlStatement);
            gb.Prod("Statement").Is(gotoStatement);
            gb.Prod("Statement").Is(labelStatement);
            gb.Prod("Statement").Is(executeStatement);
            gb.Prod("Statement").Is(sqlcmdPreprocessorStatement);
            gb.Prod("Statement").Is(useStatement);
            gb.Prod("Statement").Is(createProcStatement);
            gb.Prod("Statement").Is(createFunctionStatement);
            gb.Prod("Statement").Is(grantStatement);
            gb.Prod("Statement").Is(dbccStatement);
            gb.Prod("Statement").Is(dropProcStatement);
            gb.Prod("Statement").Is(dropTableStatement);
            gb.Prod("Statement").Is(dropViewStatement);
            gb.Prod("Statement").Is(dropIndexStatement);
            gb.Prod("Statement").Is(dropStatisticsStatement);
            gb.Prod("Statement").Is(dropDatabaseStatement);
            gb.Prod("Statement").Is(createRoleStatement);
            gb.Prod("Statement").Is(createSchemaStatement);
            gb.Prod("Statement").Is(createViewStatement);
            gb.Prod("Statement").Is(createTableStatement);
            gb.Prod("Statement").Is(alterTableStatement);
            gb.Prod("Statement").Is(createIndexStatement);
            gb.Prod("Statement").Is(alterIndexStatement);
            gb.Prod("Statement").Is(createDatabaseStatement);
            gb.Prod("Statement").Is(createTriggerStatement);
            gb.Prod("Statement").Is(dropTriggerStatement);
            gb.Prod("Statement").Is(tryCatchStatement);
            gb.Prod("Statement").Is(truncateStatement);
            gb.Prod("Statement").Is(createTableAsSelectStatement);
            gb.Prod("Statement").Is(alterDatabaseStatement);
            gb.Prod("Statement").Is(withClause, updateStatement);
            gb.Prod("Statement").Is(withClause, insertStatement);
            gb.Prod("Statement").Is(withClause, deleteStatement);
            gb.Prod("Statement").Is(withXmlNamespacesClause, updateStatement);
            gb.Prod("Statement").Is(withXmlNamespacesClause, insertStatement);
            gb.Prod("Statement").Is(withXmlNamespacesClause, deleteStatement);
            gb.Prod("Statement").Is(withXmlNamespacesClause, queryStatement);
            gb.Prod("Statement").Is(declareCursorStatement);
            gb.Prod("Statement").Is(cursorOperationStatement);
            gb.Prod("Statement").Is(waitforStatement);
            gb.Prod("Statement").Is(createLoginStatement);
            gb.Prod("Statement").Is(bulkInsertStatement);
            gb.Prod("Statement").Is(checkpointStatement);
            gb.Prod("Statement").Is(createUserStatement);
            gb.Prod("Statement").Is(createStatisticsStatement);
            gb.Prod("Statement").Is(updateStatisticsStatement);
            gb.Prod("Statement").Is(dropTypeStatement);
            gb.Prod("Statement").Is(dropColumnEncryptionKeyStatement);
            gb.Prod("Statement").Is(revertStatement);
            gb.Prod("Statement").Is(dropEventSessionStatement);
            gb.Prod("Statement").Is(createTypeStatement);
            gb.Prod("Statement").Is(createSecurityPolicyStatement);
            gb.Prod("Statement").Is(alterSecurityPolicyStatement);
            gb.Prod("Statement").Is(createExternalTableStatement);
            gb.Prod("Statement").Is(createExternalDataSourceStatement);
            gb.Prod("Statement").Is(mergeStatement);
            gb.Prod("Statement").Is(withClause, mergeStatement);

            gb.Prod("QueryStatement").Is(queryExpression);
            gb.Prod("QueryStatement").Is(withClause, queryExpression);
            gb.Prod("UpdateStatement").Is(kw("UPDATE"), tableFactor, kw("SET"), updateSetList);
            gb.Prod("UpdateStatement").Is(kw("UPDATE"), tableFactor, kw("SET"), updateSetList, kw("WHERE"), searchCondition);
            gb.Prod("UpdateStatement").Is(kw("UPDATE"), tableFactor, kw("SET"), updateSetList, kw("FROM"), tableSourceList);
            gb.Prod("UpdateStatement").Is(kw("UPDATE"), tableFactor, kw("SET"), updateSetList, kw("FROM"), tableSourceList, kw("WHERE"), searchCondition);
            gb.Prod("UpdateSetList").Is(updateSetItem);
            gb.Prod("UpdateSetList").Is(updateSetList, ",", updateSetItem);
            gb.Prod("UpdateSetItem").Is(qualifiedName, "=", expression);
            gb.Prod("UpdateSetItem").Is(qualifiedName, compoundAssignOp, expression);
            gb.Prod("UpdateSetItem").Is(variableReference, "=", expression);
            gb.Prod("UpdateSetItem").Is(variableReference, compoundAssignOp, expression);
            gb.Prod("UpdateSetItem").Is(functionCall); // XML modify: col.modify(...)

            gb.Prod("CompoundAssignOp").Is("+=");
            gb.Prod("CompoundAssignOp").Is("-=");
            gb.Prod("CompoundAssignOp").Is("*=");
            gb.Prod("CompoundAssignOp").Is("/=");
            gb.Prod("CompoundAssignOp").Is("%=");
            gb.Prod("CompoundAssignOp").Is("&=");
            gb.Prod("CompoundAssignOp").Is("|=");
            gb.Prod("CompoundAssignOp").Is("^=");

            gb.Prod("InsertStatement").Is(kw("INSERT"), insertTarget, kw("VALUES"), rowValueList);
            gb.Prod("InsertStatement").Is(kw("INSERT"), insertTarget, executeStatement);
            gb.Prod("InsertStatement").Is(kw("INSERT"), insertTarget, queryExpression);
            gb.Prod("InsertStatement").Is(kw("INSERT"), insertTarget, deleteOutputClause, kw("VALUES"), rowValueList);
            gb.Prod("InsertStatement").Is(kw("INSERT"), insertTarget, deleteOutputClause, queryExpression);
            gb.Prod("InsertTarget").Is(kw("INTO"), qualifiedName);
            gb.Prod("InsertTarget").Is(qualifiedName);
            gb.Prod("InsertTarget").Is(kw("INTO"), qualifiedName, "(", insertColumnList, ")");
            gb.Prod("InsertTarget").Is(qualifiedName, "(", insertColumnList, ")");
            gb.Prod("InsertTarget").Is(kw("INTO"), variableReference);
            gb.Prod("InsertTarget").Is(variableReference);
            gb.Prod("InsertTarget").Is(kw("INTO"), variableReference, "(", insertColumnList, ")");
            gb.Prod("InsertTarget").Is(variableReference, "(", insertColumnList, ")");
            gb.Prod("InsertTarget").Is(kw("INTO"), qualifiedName, kw("WITH"), "(", tableHintLimitedList, ")");
            gb.Prod("InsertTarget").Is(qualifiedName, kw("WITH"), "(", tableHintLimitedList, ")");
            gb.Prod("InsertTarget").Is(kw("INTO"), qualifiedName, "(", insertColumnList, ")", kw("WITH"), "(", tableHintLimitedList, ")");
            gb.Prod("InsertTarget").Is(kw("INTO"), qualifiedName, kw("WITH"), "(", tableHintLimitedList, ")", "(", insertColumnList, ")");
            gb.Prod("InsertTarget").Is(qualifiedName, kw("WITH"), "(", tableHintLimitedList, ")", "(", insertColumnList, ")");
            gb.Prod("InsertColumnList").Is(identifierTerm);
            gb.Prod("InsertColumnList").Is(insertColumnList, ",", identifierTerm);
            gb.Prod("InsertColumnList").Is(graphColumnRef);
            gb.Prod("InsertColumnList").Is(insertColumnList, ",", graphColumnRef);
            gb.Prod("InsertValueList").Is(expression);
            gb.Prod("InsertValueList").Is(insertValueList, ",", expression);
            gb.Prod("RowValue").Is("(", insertValueList, ")");
            gb.Prod("RowValueList").Is(rowValue);
            gb.Prod("RowValueList").Is(rowValueList, ",", rowValue);

            gb.Prod("DeleteStatement").Is(
                kw("DELETE"),
                deleteTarget,
                deleteStatementTail);
            gb.Prod("DeleteStatement").Is(
                kw("DELETE"),
                kw("FROM"),
                deleteTarget,
                deleteStatementTail);
            gb.Prod("DeleteStatement").Is(
                kw("DELETE"),
                deleteTopClause,
                deleteTarget,
                deleteStatementTail);
            gb.Prod("DeleteStatement").Is(
                kw("DELETE"),
                deleteTopClause,
                kw("FROM"),
                deleteTarget,
                deleteStatementTail);

            gb.Prod("DeleteTopClause").Is(kw("TOP"), "(", expression, ")");
            gb.Prod("DeleteTopClause").Is(kw("TOP"), "(", expression, ")", kw("PERCENT"));

            gb.Prod("DeleteTarget").Is(deleteTargetSimple);
            gb.Prod("DeleteTarget").Is(deleteTargetRowset);

            gb.Prod("DeleteTargetSimple").Is(identifierTerm);
            gb.Prod("DeleteTargetSimple").Is(qualifiedName);
            gb.Prod("DeleteTargetSimple").Is(variableReference);
            gb.Prod("DeleteTargetSimple").Is(identifierTerm, kw("WITH"), "(", tableHintLimitedList, ")");
            gb.Prod("DeleteTargetSimple").Is(qualifiedName, kw("WITH"), "(", tableHintLimitedList, ")");

            gb.Prod("DeleteTargetRowset").Is(rowsetFunctionLimited);
            gb.Prod("DeleteTargetRowset").Is(rowsetFunctionLimited, kw("WITH"), "(", tableHintLimitedList, ")");
            gb.Prod("RowsetFunctionLimited").Is(kw("OPENQUERY"), "(", expressionList, ")");
            gb.Prod("RowsetFunctionLimited").Is(kw("OPENROWSET"), "(", expressionList, ")");

            gb.Prod("TableHintLimitedList").Is(tableHintLimited);
            gb.Prod("TableHintLimitedList").Is(tableHintLimitedList, ",", tableHintLimited);
            gb.Prod("TableHintLimited").Is(tableHintLimitedName);
            gb.Prod("TableHintLimited").Is(tableHintLimitedName, "=", expression);
            gb.Prod("TableHintLimited").Is(tableHintLimitedName, "(", expressionList, ")");
            gb.Prod("TableHintLimited").Is(qualifiedName);
            gb.Prod("TableHintLimited").Is(qualifiedName, "=", expression);
            gb.Prod("TableHintLimited").Is(qualifiedName, "(", expressionList, ")");
            gb.Prod("TableHintLimitedName").Is(identifierTerm);
            gb.Prod("TableHintLimitedName").Is(kw("INDEX"));

            gb.Prod("DeleteStatementTail").Is(EmptyTerm.Empty);
            gb.Prod("DeleteStatementTail").Is(deleteOutputClause);
            gb.Prod("DeleteStatementTail").Is(deleteSourceFromClause);
            gb.Prod("DeleteStatementTail").Is(deleteWhereClause);
            gb.Prod("DeleteStatementTail").Is(deleteOptionClause);
            gb.Prod("DeleteStatementTail").Is(deleteOutputClause, deleteSourceFromClause);
            gb.Prod("DeleteStatementTail").Is(deleteOutputClause, deleteWhereClause);
            gb.Prod("DeleteStatementTail").Is(deleteOutputClause, deleteOptionClause);
            gb.Prod("DeleteStatementTail").Is(deleteSourceFromClause, deleteWhereClause);
            gb.Prod("DeleteStatementTail").Is(deleteSourceFromClause, deleteOptionClause);
            gb.Prod("DeleteStatementTail").Is(deleteWhereClause, deleteOptionClause);
            gb.Prod("DeleteStatementTail").Is(deleteOutputClause, deleteSourceFromClause, deleteWhereClause);
            gb.Prod("DeleteStatementTail").Is(deleteOutputClause, deleteSourceFromClause, deleteOptionClause);
            gb.Prod("DeleteStatementTail").Is(deleteOutputClause, deleteWhereClause, deleteOptionClause);
            gb.Prod("DeleteStatementTail").Is(deleteSourceFromClause, deleteWhereClause, deleteOptionClause);
            gb.Prod("DeleteStatementTail").Is(deleteOutputClause, deleteSourceFromClause, deleteWhereClause, deleteOptionClause);

            gb.Prod("DeleteOutputClause").Is(kw("OUTPUT"), selectItemList);
            gb.Prod("DeleteOutputClause").Is(kw("OUTPUT"), selectItemList, kw("INTO"), deleteOutputTarget, deleteOutputIntoColumnListOpt);
            gb.Prod("DeleteOutputTarget").Is(qualifiedName);
            gb.Prod("DeleteOutputTarget").Is(variableReference);
            gb.Prod("DeleteOutputIntoColumnListOpt").Is("(", identifierList, ")");
            gb.Prod("DeleteOutputIntoColumnListOpt").Is(EmptyTerm.Empty);

            gb.Prod("DeleteSourceFromClause").Is(kw("FROM"), tableSourceList);

            gb.Prod("DeleteWhereClause").Is(kw("WHERE"), searchCondition);
            gb.Prod("DeleteWhereClause").Is(kw("WHERE"), kw("CURRENT"), kw("OF"), identifierTerm);
            gb.Prod("DeleteWhereClause").Is(kw("WHERE"), kw("CURRENT"), kw("OF"), variableReference);
            gb.Prod("DeleteWhereClause").Is(kw("WHERE"), kw("CURRENT"), kw("OF"), kw("GLOBAL"), identifierTerm);
            gb.Prod("DeleteWhereClause").Is(kw("WHERE"), kw("CURRENT"), kw("OF"), kw("GLOBAL"), variableReference);

            gb.Prod("DeleteOptionClause").Is(kw("OPTION"), "(", deleteQueryHintList, ")");
            gb.Prod("DeleteQueryHintList").Is(deleteQueryHint);
            gb.Prod("DeleteQueryHintList").Is(deleteQueryHintList, ",", deleteQueryHint);
            gb.Prod("DeleteQueryHint").Is(deleteQueryHintName);
            gb.Prod("DeleteQueryHint").Is(deleteQueryHintName, expression);
            gb.Prod("DeleteQueryHint").Is(deleteQueryHintName, "=", expression);
            gb.Prod("DeleteQueryHint").Is(deleteQueryHintName, "(", expressionList, ")");
            gb.Prod("DeleteQueryHint").Is(deleteQueryHintName, deleteQueryHintName);
            gb.Prod("DeleteQueryHint").Is(deleteQueryHintName, deleteQueryHintName, "(", expressionList, ")");
            gb.Prod("DeleteQueryHint").Is(qualifiedName);
            gb.Prod("DeleteQueryHint").Is(qualifiedName, "(", expressionList, ")");
            gb.Prod("DeleteQueryHintName").Is(identifierTerm);
            gb.Prod("DeleteQueryHintName").Is(kw("RECOMPILE"));
            gb.Prod("DeleteQueryHintName").Is(kw("MAXDOP"));
            gb.Prod("DeleteQueryHintName").Is(kw("USE"));
            gb.Prod("DeleteQueryHintName").Is(kw("JOIN"));
            gb.Prod("DeleteQueryHintName").Is(kw("ORDER"));

            gb.Prod("OptionClause").Is(kw("OPTION"), "(", deleteQueryHintList, ")");

            gb.Prod("IfBranchStatement").Is(statement);
            gb.Prod("IfBranchStatement").Is(statement, ";");
            gb.Prod("IfStatement").Is(kw("IF"), searchCondition, ifBranchStatement);
            gb.Prod("IfStatement").Is(kw("IF"), searchCondition, ifBranchStatement, kw("ELSE"), ifBranchStatement);
            gb.Prod("IfStatement").Is(kw("IF"), searchCondition, ifBranchStatement, kw("ELSE"), ifStatement);
            gb.Prod("BeginEndStatement").Is(kw("BEGIN"), statementList, kw("END"));
            gb.Prod("BeginEndStatement").Is(kw("BEGIN"), statementList, statementSeparatorList, kw("END"));
            gb.Prod("WhileStatement").Is(kw("WHILE"), searchCondition, statement);

            gb.Prod("SetStatement").Is(kw("SET"), variableReference, "=", expression);
            gb.Prod("SetStatement").Is(kw("SET"), variableReference, compoundAssignOp, expression);
            gb.Prod("SetStatement").Is(kw("SET"), identifierTerm, kw("ON"));
            gb.Prod("SetStatement").Is(kw("SET"), identifierTerm, kw("OFF"));
            gb.Prod("SetStatement").Is(kw("SET"), identifierTerm, "=", expression);
            gb.Prod("SetStatement").Is(kw("SET"), identifierTerm, expression);
            gb.Prod("SetStatement").Is(kw("SET"), identifierTerm, identifierTerm);
            gb.Prod("SetStatement").Is(kw("SET"), identifierTerm, identifierTerm, kw("ON"));
            gb.Prod("SetStatement").Is(kw("SET"), identifierTerm, identifierTerm, kw("OFF"));
            gb.Prod("SetStatement").Is(kw("SET"), kw("IDENTITY_INSERT"), qualifiedName, kw("ON"));
            gb.Prod("SetStatement").Is(kw("SET"), kw("IDENTITY_INSERT"), qualifiedName, kw("OFF"));

            gb.Prod("PrintStatement").Is(kw("PRINT"), expression);

            gb.Prod("ReturnStatement").Is(kw("RETURN"));
            gb.Prod("ReturnStatement").Is(kw("RETURN"), expression);

            gb.Prod("TransactionStatement").Is(kw("BEGIN"), kw("TRAN"));
            gb.Prod("TransactionStatement").Is(kw("BEGIN"), kw("TRANSACTION"));
            gb.Prod("TransactionStatement").Is(kw("BEGIN"), kw("TRAN"), identifierTerm);
            gb.Prod("TransactionStatement").Is(kw("BEGIN"), kw("TRANSACTION"), identifierTerm);
            gb.Prod("TransactionStatement").Is(kw("COMMIT"));
            gb.Prod("TransactionStatement").Is(kw("COMMIT"), kw("TRAN"));
            gb.Prod("TransactionStatement").Is(kw("COMMIT"), kw("TRANSACTION"));
            gb.Prod("TransactionStatement").Is(kw("ROLLBACK"));
            gb.Prod("TransactionStatement").Is(kw("ROLLBACK"), kw("TRAN"));
            gb.Prod("TransactionStatement").Is(kw("ROLLBACK"), kw("TRANSACTION"));

            gb.Prod("RaiserrorStatement").Is(kw("RAISERROR"), "(", raiserrorArgList, ")");
            gb.Prod("RaiserrorStatement").Is(kw("RAISERROR"), "(", raiserrorArgList, ")", kw("WITH"), raiserrorWithOptionList);
            gb.Prod("RaiserrorArgList").Is(expression);
            gb.Prod("RaiserrorArgList").Is(raiserrorArgList, ",", expression);
            gb.Prod("RaiserrorWithOptionList").Is(identifierTerm);
            gb.Prod("RaiserrorWithOptionList").Is(raiserrorWithOptionList, ",", identifierTerm);

            gb.Prod("ThrowStatement").Is(kw("THROW"));
            gb.Prod("ThrowStatement").Is(kw("THROW"), expression, ",", expression, ",", expression);
            gb.Prod("LoopControlStatement").Is(kw("BREAK"));
            gb.Prod("LoopControlStatement").Is(kw("CONTINUE"));
            gb.Prod("GotoStatement").Is(kw("GOTO"), identifierTerm);
            gb.Prod("LabelStatement").Is(identifierTerm, ":");
            gb.Prod("LabelStatement").Is(identifierTerm, ":", statement);
            gb.Prod("SqlcmdPreprocessorStatement").Is(sqlcmdPreprocessorCommand);

            gb.Prod("DeclareStatement").Is(kw("DECLARE"), declareItemList);
            gb.Prod("DeclareStatement").Is(kw("DECLARE"), declareTableVariable);
            gb.Prod("DeclareItemList").Is(declareItem);
            gb.Prod("DeclareItemList").Is(declareItemList, ",", declareItem);
            gb.Prod("DeclareItem").Is(variableReference, typeSpec);
            gb.Prod("DeclareItem").Is(variableReference, typeSpec, "=", expression);
            gb.Prod("DeclareItem").Is(variableReference, typeSpec, kw("NOT"), kw("NULL"));
            gb.Prod("DeclareItem").Is(variableReference, typeSpec, kw("NOT"), kw("NULL"), "=", expression);
            gb.Prod("DeclareItem").Is(variableReference, typeSpec, kw("NULL"));
            gb.Prod("DeclareItem").Is(variableReference, typeSpec, kw("NULL"), "=", expression);
            gb.Prod("DeclareItem").Is(variableReference, kw("AS"), typeSpec);
            gb.Prod("DeclareItem").Is(variableReference, kw("AS"), typeSpec, "=", expression);
            gb.Prod("DeclareItem").Is(variableReference, kw("AS"), typeSpec, kw("NOT"), kw("NULL"));
            gb.Prod("DeclareItem").Is(variableReference, kw("AS"), typeSpec, kw("NOT"), kw("NULL"), "=", expression);
            gb.Prod("DeclareTableVariable").Is(variableReference, tableTypeDefinition);
            gb.Prod("DeclareTableVariable").Is(variableReference, kw("AS"), tableTypeDefinition);
            gb.Prod("TableTypeDefinition").Is(kw("TABLE"), "(", createTableElementList, ")");
            gb.Prod("TableTypeDefinition").Is(kw("TABLE"), "(", createTableElementList, ")", createTableOptions);
            gb.Prod("TypeSpec").Is(qualifiedName);
            gb.Prod("TypeSpec").Is(qualifiedName, "(", expression, ")");
            gb.Prod("TypeSpec").Is(qualifiedName, "(", expression, ",", expression, ")");

            gb.Prod("ExecuteStatement").Is(kw("EXEC"), executeModuleCall);
            gb.Prod("ExecuteStatement").Is(kw("EXECUTE"), executeModuleCall);
            gb.Prod("ExecuteStatement").Is(kw("EXEC"), executeDynamicCall);
            gb.Prod("ExecuteStatement").Is(kw("EXECUTE"), executeDynamicCall);
            gb.Prod("ExecuteStatement").Is(kw("EXEC"), executeAsContext);
            gb.Prod("ExecuteStatement").Is(kw("EXECUTE"), executeAsContext);

            gb.Prod("ExecuteModuleCall").Is(executeModuleCallCore);
            gb.Prod("ExecuteModuleCall").Is(executeModuleCallCore, executeWithOptions);
            gb.Prod("ExecuteModuleCallCore").Is(executeModuleTarget);
            gb.Prod("ExecuteModuleCallCore").Is(executeReturnAssignment, executeModuleTarget);
            gb.Prod("ExecuteModuleCallCore").Is(executeModuleTarget, executeArgList);
            gb.Prod("ExecuteModuleCallCore").Is(executeReturnAssignment, executeModuleTarget, executeArgList);
            gb.Prod("ExecuteReturnAssignment").Is(variableReference, "=");
            gb.Prod("ExecuteModuleTarget").Is(qualifiedName);
            gb.Prod("ExecuteModuleTarget").Is(variableReference);

            gb.Prod("ExecuteArgList").Is(executeArg);
            gb.Prod("ExecuteArgList").Is(executeArgList, ",", executeArg);
            gb.Prod("ExecuteArg").Is(executeArgValue);
            gb.Prod("ExecuteArg").Is(executeArgNamePrefix, executeArgValue);
            gb.Prod("ExecuteArgNamePrefix").Is(variableReference, "=");
            gb.Prod("ExecuteArgValue").Is(expression);
            gb.Prod("ExecuteArgValue").Is(variableReference, kw("OUTPUT"));
            gb.Prod("ExecuteArgValue").Is(variableReference, kw("OUT"));
            gb.Prod("ExecuteArgValue").Is(kw("DEFAULT"));

            gb.Prod("ExecuteWithOptions").Is(kw("WITH"), executeOptionList);
            gb.Prod("ExecuteOptionList").Is(executeOption);
            gb.Prod("ExecuteOptionList").Is(executeOptionList, ",", executeOption);
            gb.Prod("ExecuteOption").Is(kw("RECOMPILE"));
            gb.Prod("ExecuteOption").Is(kw("RESULT"), kw("SETS"), kw("UNDEFINED"));
            gb.Prod("ExecuteOption").Is(kw("RESULT"), kw("SETS"), kw("NONE"));
            gb.Prod("ExecuteOption").Is(kw("RESULT"), kw("SETS"), "(", executeResultSetsDefList, ")");
            gb.Prod("ExecuteResultSetsDefList").Is(executeResultSetsDef);
            gb.Prod("ExecuteResultSetsDefList").Is(executeResultSetsDefList, ",", executeResultSetsDef);
            gb.Prod("ExecuteResultSetsDef").Is("(", executeColumnDefList, ")");
            gb.Prod("ExecuteResultSetsDef").Is(kw("AS"), kw("OBJECT"), qualifiedName);
            gb.Prod("ExecuteResultSetsDef").Is(kw("AS"), kw("TYPE"), qualifiedName);
            gb.Prod("ExecuteResultSetsDef").Is(kw("AS"), kw("FOR"), kw("XML"));
            gb.Prod("ExecuteColumnDefList").Is(executeColumnDef);
            gb.Prod("ExecuteColumnDefList").Is(executeColumnDefList, ",", executeColumnDef);
            gb.Prod("ExecuteColumnDef").Is(identifierTerm, typeSpec);
            gb.Prod("ExecuteColumnDef").Is(identifierTerm, typeSpec, kw("COLLATE"), identifierTerm);
            gb.Prod("ExecuteColumnDef").Is(identifierTerm, typeSpec, executeNullability);
            gb.Prod("ExecuteColumnDef").Is(identifierTerm, typeSpec, kw("COLLATE"), identifierTerm, executeNullability);
            gb.Prod("ExecuteNullability").Is(kw("NULL"));
            gb.Prod("ExecuteNullability").Is(kw("NOT"), kw("NULL"));

            gb.Prod("ExecuteDynamicCall").Is("(", expression, ")");
            gb.Prod("ExecuteDynamicCall").Is("(", expression, ")", executeAsContext);
            gb.Prod("ExecuteDynamicCall").Is("(", expression, ")", executeAtClause);
            gb.Prod("ExecuteDynamicCall").Is("(", expression, ")", executeAsContext, executeAtClause);
            gb.Prod("ExecuteDynamicCall").Is("(", expression, executeLinkedArgList, ")");
            gb.Prod("ExecuteDynamicCall").Is("(", expression, executeLinkedArgList, ")", executeAsContext);
            gb.Prod("ExecuteDynamicCall").Is("(", expression, executeLinkedArgList, ")", executeAtClause);
            gb.Prod("ExecuteDynamicCall").Is("(", expression, executeLinkedArgList, ")", executeAsContext, executeAtClause);
            gb.Prod("ExecuteLinkedArgList").Is(",", executeLinkedArg);
            gb.Prod("ExecuteLinkedArgList").Is(executeLinkedArgList, ",", executeLinkedArg);
            gb.Prod("ExecuteLinkedArg").Is(expression);
            gb.Prod("ExecuteLinkedArg").Is(variableReference, kw("OUTPUT"));
            gb.Prod("ExecuteLinkedArg").Is(variableReference, kw("OUT"));
            gb.Prod("ExecuteAsContext").Is(kw("AS"), kw("LOGIN"), "=", stringLiteral);
            gb.Prod("ExecuteAsContext").Is(kw("AS"), kw("USER"), "=", stringLiteral);
            gb.Prod("ExecuteAsContext").Is(kw("AS"), kw("LOGIN"), "=", unicodeStringLiteral);
            gb.Prod("ExecuteAsContext").Is(kw("AS"), kw("USER"), "=", unicodeStringLiteral);
            gb.Prod("ExecuteAsContext").Is(kw("AS"), kw("LOGIN"), "=", identifierTerm);
            gb.Prod("ExecuteAsContext").Is(kw("AS"), kw("USER"), "=", identifierTerm);
            gb.Prod("ExecuteAtClause").Is(kw("AT"), identifierTerm);
            gb.Prod("ExecuteAtClause").Is(kw("AT"), kw("DATA_SOURCE"), identifierTerm);

            gb.Prod("UseStatement").Is(kw("USE"), identifierTerm);

            gb.Prod("CreateProcKeyword").Is(kw("PROC"));
            gb.Prod("CreateProcKeyword").Is(kw("PROCEDURE"));

            gb.Prod("CreateProcHead").Is(kw("CREATE"), createProcKeyword);
            gb.Prod("CreateProcHead").Is(kw("CREATE"), kw("OR"), kw("ALTER"), createProcKeyword);
            gb.Prod("CreateProcHead").Is(kw("ALTER"), createProcKeyword);

            gb.Prod("CreateProcName").Is(qualifiedName);
            gb.Prod("CreateProcName").Is(qualifiedName, ";", number);

            gb.Prod("CreateProcSignature").Is(createProcName);
            gb.Prod("CreateProcSignature").Is(createProcName, createProcParameterList);
            gb.Prod("CreateProcSignature").Is(createProcName, createProcWithClause);
            gb.Prod("CreateProcSignature").Is(createProcName, createProcForReplicationClause);
            gb.Prod("CreateProcSignature").Is(createProcName, createProcParameterList, createProcWithClause);
            gb.Prod("CreateProcSignature").Is(createProcName, createProcParameterList, createProcForReplicationClause);
            gb.Prod("CreateProcSignature").Is(createProcName, createProcWithClause, createProcForReplicationClause);
            gb.Prod("CreateProcSignature").Is(createProcName, createProcParameterList, createProcWithClause, createProcForReplicationClause);
            gb.Prod("CreateProcSignature").Is(createProcName, "(", ")");
            gb.Prod("CreateProcSignature").Is(createProcName, "(", createProcParameterList, ")");
            gb.Prod("CreateProcSignature").Is(createProcName, "(", ")", createProcWithClause);
            gb.Prod("CreateProcSignature").Is(createProcName, "(", createProcParameterList, ")", createProcWithClause);
            gb.Prod("CreateProcSignature").Is(createProcName, "(", ")", createProcForReplicationClause);
            gb.Prod("CreateProcSignature").Is(createProcName, "(", createProcParameterList, ")", createProcForReplicationClause);
            gb.Prod("CreateProcSignature").Is(createProcName, "(", ")", createProcWithClause, createProcForReplicationClause);
            gb.Prod("CreateProcSignature").Is(createProcName, "(", createProcParameterList, ")", createProcWithClause, createProcForReplicationClause);

            gb.Prod("CreateProcParameterList").Is(createProcParameter);
            gb.Prod("CreateProcParameterList").Is(createProcParameterList, ",", createProcParameter);
            gb.Prod("CreateProcParameter").Is(variableReference, typeSpec);
            gb.Prod("CreateProcParameter").Is(variableReference, typeSpec, createProcParameterOptionList);
            gb.Prod("CreateProcParameter").Is(variableReference, kw("AS"), typeSpec);
            gb.Prod("CreateProcParameter").Is(variableReference, kw("AS"), typeSpec, createProcParameterOptionList);
            gb.Prod("CreateProcParameterOptionList").Is(createProcParameterOption);
            gb.Prod("CreateProcParameterOptionList").Is(createProcParameterOptionList, createProcParameterOption);
            gb.Prod("CreateProcParameterOption").Is(kw("VARYING"));
            gb.Prod("CreateProcParameterOption").Is(kw("NULL"));
            gb.Prod("CreateProcParameterOption").Is(kw("NOT"), kw("NULL"));
            gb.Prod("CreateProcParameterOption").Is("=", expression);
            gb.Prod("CreateProcParameterOption").Is(kw("OUT"));
            gb.Prod("CreateProcParameterOption").Is(kw("OUTPUT"));
            gb.Prod("CreateProcParameterOption").Is(kw("READONLY"));

            gb.Prod("CreateProcWithClause").Is(kw("WITH"), createProcOptionList);
            gb.Prod("CreateProcOptionList").Is(createProcOption);
            gb.Prod("CreateProcOptionList").Is(createProcOptionList, ",", createProcOption);
            gb.Prod("CreateProcOption").Is(kw("ENCRYPTION"));
            gb.Prod("CreateProcOption").Is(kw("RECOMPILE"));
            gb.Prod("CreateProcOption").Is(kw("NATIVE_COMPILATION"));
            gb.Prod("CreateProcOption").Is(kw("SCHEMABINDING"));
            gb.Prod("CreateProcOption").Is(createProcExecuteAsClause);

            gb.Prod("CreateProcExecuteAsClause").Is(kw("EXECUTE"), kw("AS"), kw("CALLER"));
            gb.Prod("CreateProcExecuteAsClause").Is(kw("EXECUTE"), kw("AS"), kw("SELF"));
            gb.Prod("CreateProcExecuteAsClause").Is(kw("EXECUTE"), kw("AS"), kw("OWNER"));
            gb.Prod("CreateProcExecuteAsClause").Is(kw("EXECUTE"), kw("AS"), stringLiteral);
            gb.Prod("CreateProcExecuteAsClause").Is(kw("EXECUTE"), kw("AS"), unicodeStringLiteral);
            gb.Prod("CreateProcExecuteAsClause").Is(kw("EXECUTE"), kw("AS"), identifierTerm);

            gb.Prod("CreateProcForReplicationClause").Is(kw("FOR"), kw("REPLICATION"));

            gb.Prod("CreateProcBody").Is(kw("AS"), createProcBodyBlock);
            gb.Prod("CreateProcBody").Is(kw("AS"), kw("EXTERNAL"), identifierTerm, createProcExternalName);
            gb.Prod("CreateProcBody").Is(createProcNativeWithClause, kw("AS"), kw("BEGIN"), kw("ATOMIC"), kw("WITH"), "(", createProcNativeAtomicOptionList, ")", statementList, kw("END"));
            gb.Prod("CreateProcBody").Is(createProcNativeWithClause, kw("AS"), kw("BEGIN"), kw("ATOMIC"), kw("WITH"), "(", createProcNativeAtomicOptionList, ")", statementList, statementSeparatorList, kw("END"));

            gb.Prod("CreateProcNativeWithClause").Is(kw("WITH"), kw("NATIVE_COMPILATION"), ",", kw("SCHEMABINDING"));
            gb.Prod("CreateProcNativeWithClause").Is(kw("WITH"), kw("NATIVE_COMPILATION"), ",", kw("SCHEMABINDING"), ",", createProcExecuteAsClause);

            gb.Prod("CreateProcNativeAtomicOptionList").Is(createProcNativeAtomicOption);
            gb.Prod("CreateProcNativeAtomicOptionList").Is(createProcNativeAtomicOptionList, ",", createProcNativeAtomicOption);
            gb.Prod("CreateProcNativeAtomicOption").Is(identifierTerm, "=", expression);
            gb.Prod("CreateProcNativeAtomicOption").Is(qualifiedName, "=", expression);
            gb.Prod("CreateProcNativeAtomicOption").Is(identifierTerm, identifierTerm, "=", expression);
            gb.Prod("CreateProcNativeAtomicOption").Is(identifierTerm, identifierTerm, identifierTerm, "=", expression);
            gb.Prod("CreateProcNativeAtomicOption").Is(kw("TRANSACTION"), identifierTerm, identifierTerm, "=", expression);

            gb.Prod("CreateProcExternalName").Is(qualifiedName);

            gb.Prod("CreateProcBodyBlock").Is(statementList);
            gb.Prod("CreateProcBodyBlock").Is(statementList, statementSeparatorList);
            gb.Prod("CreateProcBodyBlock").Is(statementSeparatorList, statementList);
            gb.Prod("CreateProcBodyBlock").Is(statementSeparatorList, statementList, statementSeparatorList);
            gb.Prod("CreateProcBodyBlock").Is(kw("BEGIN"), statementList, kw("END"));
            gb.Prod("CreateProcBodyBlock").Is(kw("BEGIN"), statementList, statementSeparatorList, kw("END"));
            gb.Prod("CreateProcBodyBlock").Is(kw("BEGIN"), kw("ATOMIC"), kw("WITH"), "(", createProcNativeAtomicOptionList, ")", statementList, kw("END"));
            gb.Prod("CreateProcBodyBlock").Is(kw("BEGIN"), kw("ATOMIC"), kw("WITH"), "(", createProcNativeAtomicOptionList, ")", statementList, statementSeparatorList, kw("END"));

            gb.Prod("CreateProcStatement").Is(createProcHead, createProcSignature, createProcBody);

            gb.Prod("CreateFunctionHead").Is(kw("CREATE"), kw("FUNCTION"));
            gb.Prod("CreateFunctionHead").Is(kw("CREATE"), kw("OR"), kw("ALTER"), kw("FUNCTION"));
            gb.Prod("CreateFunctionHead").Is(kw("ALTER"), kw("FUNCTION"));
            gb.Prod("CreateFunctionName").Is(qualifiedName);

            gb.Prod("CreateFunctionSignature").Is(createFunctionName, "(", ")", createFunctionReturnsClause);
            gb.Prod("CreateFunctionSignature").Is(createFunctionName, "(", createFunctionParameterList, ")", createFunctionReturnsClause);
            gb.Prod("CreateFunctionSignature").Is(createFunctionName, "(", ")", createFunctionReturnsClause, createFunctionWithClause);
            gb.Prod("CreateFunctionSignature").Is(createFunctionName, "(", createFunctionParameterList, ")", createFunctionReturnsClause, createFunctionWithClause);

            gb.Prod("CreateFunctionParameterList").Is(createFunctionParameter);
            gb.Prod("CreateFunctionParameterList").Is(createFunctionParameterList, ",", createFunctionParameter);
            gb.Prod("CreateFunctionParameter").Is(variableReference, typeSpec);
            gb.Prod("CreateFunctionParameter").Is(variableReference, kw("AS"), typeSpec);
            gb.Prod("CreateFunctionParameter").Is(variableReference, typeSpec, createFunctionParameterOptionList);
            gb.Prod("CreateFunctionParameter").Is(variableReference, kw("AS"), typeSpec, createFunctionParameterOptionList);
            gb.Prod("CreateFunctionParameterOptionList").Is(createFunctionParameterOption);
            gb.Prod("CreateFunctionParameterOptionList").Is(createFunctionParameterOptionList, createFunctionParameterOption);
            gb.Prod("CreateFunctionParameterOption").Is(kw("NULL"));
            gb.Prod("CreateFunctionParameterOption").Is(kw("NOT"), kw("NULL"));
            gb.Prod("CreateFunctionParameterOption").Is("=", expression);
            gb.Prod("CreateFunctionParameterOption").Is(kw("READONLY"));

            gb.Prod("CreateFunctionReturnsClause").Is(kw("RETURNS"), typeSpec);
            gb.Prod("CreateFunctionReturnsClause").Is(kw("RETURNS"), kw("TABLE"));
            gb.Prod("CreateFunctionReturnsClause").Is(kw("RETURNS"), createFunctionTableReturnDefinition);
            gb.Prod("CreateFunctionTableReturnDefinition").Is(variableReference, kw("TABLE"), "(", createFunctionTableReturnItemList, ")");
            gb.Prod("CreateFunctionTableReturnItemList").Is(createFunctionTableReturnItem);
            gb.Prod("CreateFunctionTableReturnItemList").Is(createFunctionTableReturnItemList, ",", createFunctionTableReturnItem);
            gb.Prod("CreateFunctionTableReturnItem").Is(createTableColumnDefinition);
            gb.Prod("CreateFunctionTableReturnItem").Is(createTableComputedColumn);
            gb.Prod("CreateFunctionTableReturnItem").Is(createTableConstraint);
            gb.Prod("CreateFunctionTableReturnItem").Is(createTableTableIndex);

            gb.Prod("CreateFunctionWithClause").Is(kw("WITH"), createFunctionOptionList);
            gb.Prod("CreateFunctionOptionList").Is(createFunctionOption);
            gb.Prod("CreateFunctionOptionList").Is(createFunctionOptionList, ",", createFunctionOption);
            gb.Prod("CreateFunctionOption").Is(kw("ENCRYPTION"));
            gb.Prod("CreateFunctionOption").Is(kw("SCHEMABINDING"));
            gb.Prod("CreateFunctionOption").Is(kw("RETURNS"), kw("NULL"), kw("ON"), kw("NULL"), kw("INPUT"));
            gb.Prod("CreateFunctionOption").Is(kw("CALLED"), kw("ON"), kw("NULL"), kw("INPUT"));
            gb.Prod("CreateFunctionOption").Is(createProcExecuteAsClause);
            gb.Prod("CreateFunctionOption").Is(kw("INLINE"), "=", kw("ON"));
            gb.Prod("CreateFunctionOption").Is(kw("INLINE"), "=", kw("OFF"));

            gb.Prod("CreateFunctionBody").Is(kw("AS"), kw("RETURN"), queryExpression);
            gb.Prod("CreateFunctionBody").Is(kw("AS"), kw("RETURN"), "(", queryExpression, ")");
            gb.Prod("CreateFunctionBody").Is(kw("AS"), kw("RETURN"), "(", withClause, queryExpression, ")");
            gb.Prod("CreateFunctionBody").Is(kw("RETURN"), queryExpression);
            gb.Prod("CreateFunctionBody").Is(kw("RETURN"), "(", queryExpression, ")");
            gb.Prod("CreateFunctionBody").Is(kw("RETURN"), "(", withClause, queryExpression, ")");
            gb.Prod("CreateFunctionBody").Is(kw("AS"), kw("BEGIN"), statementList, kw("END"));
            gb.Prod("CreateFunctionBody").Is(kw("AS"), kw("BEGIN"), statementList, statementSeparatorList, kw("END"));
            gb.Prod("CreateFunctionBody").Is(kw("BEGIN"), statementList, kw("END"));
            gb.Prod("CreateFunctionBody").Is(kw("BEGIN"), statementList, statementSeparatorList, kw("END"));

            gb.Prod("CreateFunctionStatement").Is(createFunctionHead, createFunctionSignature, createFunctionBody);

            gb.Prod("GrantPermissionSet").Is(kw("ALL"));
            gb.Prod("GrantPermissionSet").Is(kw("ALL"), kw("PRIVILEGES"));
            gb.Prod("GrantPermissionSet").Is(grantPermissionList);
            gb.Prod("GrantPermissionList").Is(grantPermissionItem);
            gb.Prod("GrantPermissionList").Is(grantPermissionList, ",", grantPermissionItem);
            gb.Prod("GrantPermissionItem").Is(grantPermission);
            gb.Prod("GrantPermissionItem").Is(grantPermission, "(", identifierList, ")");

            gb.Prod("GrantPermission").Is(grantPermissionWord);
            gb.Prod("GrantPermission").Is(grantPermissionWord, grantPermissionWord);
            gb.Prod("GrantPermission").Is(grantPermissionWord, grantPermissionWord, grantPermissionWord);
            gb.Prod("GrantPermission").Is(grantPermissionWord, grantPermissionWord, grantPermissionWord, grantPermissionWord);

            gb.Prod("GrantPermissionWord").Is(identifierTerm);
            gb.Prod("GrantPermissionWord").Is(kw("SELECT"));
            gb.Prod("GrantPermissionWord").Is(kw("INSERT"));
            gb.Prod("GrantPermissionWord").Is(kw("UPDATE"));
            gb.Prod("GrantPermissionWord").Is(kw("DELETE"));
            gb.Prod("GrantPermissionWord").Is(kw("EXECUTE"));
            gb.Prod("GrantPermissionWord").Is(kw("REFERENCES"));
            gb.Prod("GrantPermissionWord").Is(kw("CONNECT"));
            gb.Prod("GrantPermissionWord").Is(kw("ALTER"));
            gb.Prod("GrantPermissionWord").Is(kw("CONTROL"));
            gb.Prod("GrantPermissionWord").Is(kw("VIEW"));
            gb.Prod("GrantPermissionWord").Is(kw("DEFINITION"));
            gb.Prod("GrantPermissionWord").Is(kw("TAKE"));
            gb.Prod("GrantPermissionWord").Is(kw("OWNERSHIP"));
            gb.Prod("GrantPermissionWord").Is(kw("IMPERSONATE"));
            gb.Prod("GrantPermissionWord").Is(kw("RECEIVE"));
            gb.Prod("GrantPermissionWord").Is(kw("SEND"));
            gb.Prod("GrantPermissionWord").Is(kw("CREATE"));
            gb.Prod("GrantPermissionWord").Is(kw("ANY"));
            gb.Prod("GrantPermissionWord").Is(kw("SCHEMA"));
            gb.Prod("GrantPermissionWord").Is(kw("DATABASE"));
            gb.Prod("GrantPermissionWord").Is(kw("OBJECT"));
            gb.Prod("GrantPermissionWord").Is(kw("ROLE"));
            gb.Prod("GrantPermissionWord").Is(kw("LOGIN"));
            gb.Prod("GrantPermissionWord").Is(kw("USER"));

            gb.Prod("GrantOnClause").Is(kw("ON"), grantSecurable);
            gb.Prod("GrantOnClause").Is(kw("ON"), grantClassType, "::", grantSecurable);
            gb.Prod("GrantClassType").Is(kw("LOGIN"));
            gb.Prod("GrantClassType").Is(kw("DATABASE"));
            gb.Prod("GrantClassType").Is(kw("OBJECT"));
            gb.Prod("GrantClassType").Is(kw("ROLE"));
            gb.Prod("GrantClassType").Is(kw("SCHEMA"));
            gb.Prod("GrantClassType").Is(kw("USER"));
            gb.Prod("GrantSecurable").Is(qualifiedName);
            gb.Prod("GrantSecurable").Is(identifierTerm);

            gb.Prod("GrantPrincipalList").Is(grantPrincipal);
            gb.Prod("GrantPrincipalList").Is(grantPrincipalList, ",", grantPrincipal);
            gb.Prod("GrantPrincipal").Is(identifierTerm);
            gb.Prod("GrantPrincipal").Is(qualifiedName);
            gb.Prod("GrantPrincipal").Is(kw("PUBLIC"));

            gb.Prod("GrantStatement").Is(kw("GRANT"), grantPermissionSet, kw("TO"), grantPrincipalList);
            gb.Prod("GrantStatement").Is(kw("GRANT"), grantPermissionSet, grantOnClause, kw("TO"), grantPrincipalList);
            gb.Prod("GrantStatement").Is(kw("GRANT"), grantPermissionSet, kw("TO"), grantPrincipalList, kw("WITH"), kw("GRANT"), kw("OPTION"));
            gb.Prod("GrantStatement").Is(kw("GRANT"), grantPermissionSet, grantOnClause, kw("TO"), grantPrincipalList, kw("WITH"), kw("GRANT"), kw("OPTION"));
            gb.Prod("GrantStatement").Is(kw("GRANT"), grantPermissionSet, kw("TO"), grantPrincipalList, kw("AS"), grantPrincipal);
            gb.Prod("GrantStatement").Is(kw("GRANT"), grantPermissionSet, grantOnClause, kw("TO"), grantPrincipalList, kw("AS"), grantPrincipal);
            gb.Prod("GrantStatement").Is(kw("GRANT"), grantPermissionSet, kw("TO"), grantPrincipalList, kw("WITH"), kw("GRANT"), kw("OPTION"), kw("AS"), grantPrincipal);
            gb.Prod("GrantStatement").Is(kw("GRANT"), grantPermissionSet, grantOnClause, kw("TO"), grantPrincipalList, kw("WITH"), kw("GRANT"), kw("OPTION"), kw("AS"), grantPrincipal);

            gb.Prod("DbccCommand").Is(identifierTerm);
            gb.Prod("DbccCommand").Is(qualifiedName);
            gb.Prod("DbccStatement").Is(kw("DBCC"), dbccCommand);
            gb.Prod("DbccStatement").Is(kw("DBCC"), dbccCommand, "(", dbccParamList, ")");
            gb.Prod("DbccStatement").Is(kw("DBCC"), dbccCommand, kw("WITH"), dbccOptionList);
            gb.Prod("DbccStatement").Is(kw("DBCC"), dbccCommand, "(", dbccParamList, ")", kw("WITH"), dbccOptionList);
            gb.Prod("DbccParamList").Is(dbccParam);
            gb.Prod("DbccParamList").Is(dbccParamList, ",", dbccParam);
            gb.Prod("DbccParam").Is(expression);
            gb.Prod("DbccParam").Is(identifierTerm);
            gb.Prod("DbccParam").Is(qualifiedName);
            gb.Prod("DbccOptionList").Is(dbccOption);
            gb.Prod("DbccOptionList").Is(dbccOptionList, ",", dbccOption);
            gb.Prod("DbccOption").Is(dbccOptionName);
            gb.Prod("DbccOption").Is(dbccOptionName, "=", dbccOptionValue);
            gb.Prod("DbccOptionName").Is(identifierTerm);
            gb.Prod("DbccOptionName").Is(qualifiedName);
            gb.Prod("DbccOptionName").Is(kw("MAXDOP"));
            gb.Prod("DbccOptionValue").Is(expression);
            gb.Prod("DbccOptionValue").Is(identifierTerm);
            gb.Prod("DbccOptionValue").Is(kw("ON"));
            gb.Prod("DbccOptionValue").Is(kw("OFF"));

            gb.Prod("DropProcStatement").Is(kw("DROP"), kw("PROC"), qualifiedName);
            gb.Prod("DropProcStatement").Is(kw("DROP"), kw("PROCEDURE"), qualifiedName);
            gb.Prod("DropProcStatement").Is(kw("DROP"), kw("PROC"), dropIfExistsClause, qualifiedName);
            gb.Prod("DropProcStatement").Is(kw("DROP"), kw("PROCEDURE"), dropIfExistsClause, qualifiedName);
            gb.Prod("DropProcStatement").Is(kw("DROP"), kw("FUNCTION"), qualifiedName);
            gb.Prod("DropProcStatement").Is(kw("DROP"), kw("FUNCTION"), dropIfExistsClause, qualifiedName);
            gb.Prod("DropIfExistsClause").Is(kw("IF"), kw("EXISTS"));

            gb.Prod("DropTableStatement").Is(kw("DROP"), kw("TABLE"), dropTableTargetList);
            gb.Prod("DropTableStatement").Is(kw("DROP"), kw("TABLE"), dropIfExistsClause, dropTableTargetList);
            gb.Prod("DropTableTargetList").Is(qualifiedName);
            gb.Prod("DropTableTargetList").Is(dropTableTargetList, ",", qualifiedName);

            gb.Prod("DropViewStatement").Is(kw("DROP"), kw("VIEW"), dropViewTargetList);
            gb.Prod("DropViewStatement").Is(kw("DROP"), kw("VIEW"), dropIfExistsClause, dropViewTargetList);
            gb.Prod("DropViewTargetList").Is(qualifiedName);
            gb.Prod("DropViewTargetList").Is(dropViewTargetList, ",", qualifiedName);

            gb.Prod("DropIndexStatement").Is(kw("DROP"), kw("INDEX"), dropIndexSpecList);
            gb.Prod("DropIndexStatement").Is(kw("DROP"), kw("INDEX"), dropIfExistsClause, dropIndexSpecList);
            gb.Prod("DropIndexSpecList").Is(dropIndexSpec);
            gb.Prod("DropIndexSpecList").Is(dropIndexSpecList, ",", dropIndexSpec);
            gb.Prod("DropIndexSpec").Is(qualifiedName, kw("ON"), qualifiedName);
            gb.Prod("DropIndexSpec").Is(qualifiedName, kw("ON"), qualifiedName, kw("WITH"), "(", dropIndexOptionList, ")");
            gb.Prod("DropIndexSpec").Is(qualifiedName, ".", identifierTerm);
            gb.Prod("DropIndexOptionList").Is(dropIndexOption);
            gb.Prod("DropIndexOptionList").Is(dropIndexOptionList, ",", dropIndexOption);
            gb.Prod("DropIndexOption").Is(kw("MAXDOP"), "=", expression);
            gb.Prod("DropIndexOption").Is(kw("ONLINE"), "=", kw("ON"));
            gb.Prod("DropIndexOption").Is(kw("ONLINE"), "=", kw("OFF"));
            gb.Prod("DropIndexOption").Is(kw("MOVE"), kw("TO"), dropMoveToTarget);
            gb.Prod("DropIndexOption").Is(kw("MOVE"), kw("TO"), dropMoveToTarget, kw("FILESTREAM_ON"), dropFileStreamTarget);
            gb.Prod("DropIndexOption").Is(kw("FILESTREAM_ON"), dropFileStreamTarget);
            gb.Prod("DropMoveToTarget").Is(qualifiedName);
            gb.Prod("DropMoveToTarget").Is(kw("DEFAULT"));
            gb.Prod("DropMoveToTarget").Is(qualifiedName, "(", identifierTerm, ")");
            gb.Prod("DropFileStreamTarget").Is(qualifiedName);
            gb.Prod("DropFileStreamTarget").Is(kw("DEFAULT"));

            gb.Prod("DropStatisticsStatement").Is(kw("DROP"), kw("STATISTICS"), dropStatisticsTargetList);
            gb.Prod("DropStatisticsTargetList").Is(dropStatisticsTarget);
            gb.Prod("DropStatisticsTargetList").Is(dropStatisticsTargetList, ",", dropStatisticsTarget);
            gb.Prod("DropStatisticsTarget").Is(qualifiedName, ".", identifierTerm);

            gb.Prod("DropDatabaseStatement").Is(kw("DROP"), kw("DATABASE"), identifierTerm);
            gb.Prod("DropDatabaseStatement").Is(kw("DROP"), kw("DATABASE"), dropIfExistsClause, identifierTerm);

            gb.Prod("CreateTriggerHead").Is(kw("CREATE"), kw("TRIGGER"));
            gb.Prod("CreateTriggerHead").Is(kw("CREATE"), kw("OR"), kw("ALTER"), kw("TRIGGER"));
            gb.Prod("CreateTriggerHead").Is(kw("ALTER"), kw("TRIGGER"));

            gb.Prod("CreateTriggerFireClause").Is(kw("FOR"), createTriggerEventList);
            gb.Prod("CreateTriggerFireClause").Is(kw("AFTER"), createTriggerEventList);
            gb.Prod("CreateTriggerFireClause").Is(kw("INSTEAD"), kw("OF"), createTriggerEventList);

            gb.Prod("CreateTriggerEventList").Is(createTriggerEvent);
            gb.Prod("CreateTriggerEventList").Is(createTriggerEventList, ",", createTriggerEvent);
            gb.Prod("CreateTriggerEvent").Is(kw("INSERT"));
            gb.Prod("CreateTriggerEvent").Is(kw("UPDATE"));
            gb.Prod("CreateTriggerEvent").Is(kw("DELETE"));
            gb.Prod("CreateTriggerEvent").Is(identifierTerm); // DDL events: CREATE_TABLE, LOGON, etc.

            gb.Prod("CreateTriggerWithOptionList").Is(createTriggerWithOption);
            gb.Prod("CreateTriggerWithOptionList").Is(createTriggerWithOptionList, ",", createTriggerWithOption);
            gb.Prod("CreateTriggerWithOption").Is(kw("ENCRYPTION"));
            gb.Prod("CreateTriggerWithOption").Is(kw("SCHEMABINDING"));
            gb.Prod("CreateTriggerWithOption").Is(kw("NATIVE_COMPILATION"));
            gb.Prod("CreateTriggerWithOption").Is(createProcExecuteAsClause);

            // DML trigger: ON table [WITH opts] fireClause [NOT FOR REPLICATION] AS body
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, kw("ON"), qualifiedName, createTriggerFireClause, kw("AS"), createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, kw("ON"), qualifiedName, kw("WITH"), createTriggerWithOptionList, createTriggerFireClause, kw("AS"), createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, kw("ON"), qualifiedName, createTriggerFireClause, kw("NOT"), kw("FOR"), kw("REPLICATION"), kw("AS"), createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, kw("ON"), qualifiedName, kw("WITH"), createTriggerWithOptionList, createTriggerFireClause, kw("NOT"), kw("FOR"), kw("REPLICATION"), kw("AS"), createProcBodyBlock);
            // DDL trigger: ON ALL SERVER | DATABASE
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, kw("ON"), kw("ALL"), kw("SERVER"), createTriggerFireClause, kw("AS"), createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, kw("ON"), kw("DATABASE"), createTriggerFireClause, kw("AS"), createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, kw("ON"), kw("ALL"), kw("SERVER"), kw("WITH"), createTriggerWithOptionList, createTriggerFireClause, kw("AS"), createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, kw("ON"), kw("DATABASE"), kw("WITH"), createTriggerWithOptionList, createTriggerFireClause, kw("AS"), createProcBodyBlock);

            gb.Prod("DropTriggerStatement").Is(kw("DROP"), kw("TRIGGER"), qualifiedName);
            gb.Prod("DropTriggerStatement").Is(kw("DROP"), kw("TRIGGER"), dropIfExistsClause, qualifiedName);
            gb.Prod("DropTriggerStatement").Is(kw("DROP"), kw("TRIGGER"), qualifiedName, kw("ON"), kw("DATABASE"));
            gb.Prod("DropTriggerStatement").Is(kw("DROP"), kw("TRIGGER"), dropIfExistsClause, qualifiedName, kw("ON"), kw("DATABASE"));
            gb.Prod("DropTriggerStatement").Is(kw("DROP"), kw("TRIGGER"), qualifiedName, kw("ON"), kw("ALL"), kw("SERVER"));
            gb.Prod("DropTriggerStatement").Is(kw("DROP"), kw("TRIGGER"), dropIfExistsClause, qualifiedName, kw("ON"), kw("ALL"), kw("SERVER"));

            gb.Prod("ProcStatementList").Is(statement);
            gb.Prod("ProcStatementList").Is(statementSeparatorList, statement);
            gb.Prod("ProcStatementList").Is(procStatementList, ";", statement);
            gb.Prod("ProcStatementList").Is(procStatementList, statement);

            gb.Prod("TryCatchStatement").Is(
                kw("BEGIN"), kw("TRY"),
                procStatementList,
                kw("END"), kw("TRY"),
                kw("BEGIN"), kw("CATCH"),
                kw("END"), kw("CATCH"));
            gb.Prod("TryCatchStatement").Is(
                kw("BEGIN"), kw("TRY"),
                procStatementList,
                kw("END"), kw("TRY"),
                kw("BEGIN"), kw("CATCH"),
                procStatementList,
                kw("END"), kw("CATCH"));
            gb.Prod("TryCatchStatement").Is(
                kw("BEGIN"), kw("TRY"),
                procStatementList, statementSeparatorList,
                kw("END"), kw("TRY"),
                kw("BEGIN"), kw("CATCH"),
                kw("END"), kw("CATCH"));
            gb.Prod("TryCatchStatement").Is(
                kw("BEGIN"), kw("TRY"),
                procStatementList, statementSeparatorList,
                kw("END"), kw("TRY"),
                kw("BEGIN"), kw("CATCH"),
                procStatementList,
                kw("END"), kw("CATCH"));
            gb.Prod("TryCatchStatement").Is(
                kw("BEGIN"), kw("TRY"),
                procStatementList, statementSeparatorList,
                kw("END"), kw("TRY"),
                kw("BEGIN"), kw("CATCH"),
                procStatementList, statementSeparatorList,
                kw("END"), kw("CATCH"));
            gb.Prod("TryCatchStatement").Is(
                kw("BEGIN"), kw("TRY"),
                procStatementList,
                kw("END"), kw("TRY"),
                kw("BEGIN"), kw("CATCH"),
                procStatementList, statementSeparatorList,
                kw("END"), kw("CATCH"));

            gb.Prod("CreateRoleStatement").Is(kw("CREATE"), kw("ROLE"), identifierTerm);
            gb.Prod("CreateRoleStatement").Is(kw("CREATE"), kw("ROLE"), identifierTerm, kw("AUTHORIZATION"), identifierTerm);

            gb.Prod("CreateSchemaStatement").Is(kw("CREATE"), kw("SCHEMA"), schemaNameClause);
            gb.Prod("SchemaNameClause").Is(identifierTerm);
            gb.Prod("SchemaNameClause").Is(kw("AUTHORIZATION"), identifierTerm);
            gb.Prod("SchemaNameClause").Is(identifierTerm, kw("AUTHORIZATION"), identifierTerm);

            gb.Prod("CreateViewHead").Is(kw("CREATE"), kw("VIEW"));
            gb.Prod("CreateViewHead").Is(kw("CREATE"), kw("OR"), kw("ALTER"), kw("VIEW"));
            gb.Prod("CreateViewHead").Is(kw("ALTER"), kw("VIEW"));
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, kw("AS"), queryExpression);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, kw("AS"), withClause, queryExpression);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, kw("WITH"), identifierTerm, kw("AS"), queryExpression);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, kw("WITH"), identifierTerm, kw("AS"), withClause, queryExpression);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, kw("WITH"), identifierTerm, ",", identifierTerm, kw("AS"), queryExpression);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, kw("WITH"), identifierTerm, ",", identifierTerm, kw("AS"), withClause, queryExpression);

            gb.Prod("CreateTableFileTableClause").Is(kw("AS"), kw("FILETABLE"));
            gb.Prod("CreateTableStatement").Is(kw("CREATE"), kw("TABLE"), qualifiedName, "(", createTableElementList, ")");
            gb.Prod("CreateTableStatement").Is(kw("CREATE"), kw("TABLE"), qualifiedName, createTableFileTableClause, "(", createTableElementList, ")");
            gb.Prod("CreateTableStatement").Is(kw("CREATE"), kw("TABLE"), qualifiedName, "(", createTableElementList, ")", createTableTailClauseList);
            gb.Prod("CreateTableStatement").Is(kw("CREATE"), kw("TABLE"), qualifiedName, createTableFileTableClause, "(", createTableElementList, ")", createTableTailClauseList);
            // SQL Graph node/edge tables
            gb.Prod("CreateTableStatement").Is(kw("CREATE"), kw("TABLE"), qualifiedName, "(", createTableElementList, ")", kw("AS"), kw("EDGE"));
            gb.Prod("CreateTableStatement").Is(kw("CREATE"), kw("TABLE"), qualifiedName, "(", createTableElementList, ")", kw("AS"), kw("NODE"));
            gb.Prod("CreateTableStatement").Is(kw("CREATE"), kw("TABLE"), qualifiedName, "(", createTableElementList, ")", createTableTailClauseList, kw("AS"), kw("EDGE"));
            gb.Prod("CreateTableStatement").Is(kw("CREATE"), kw("TABLE"), qualifiedName, "(", createTableElementList, ")", createTableTailClauseList, kw("AS"), kw("NODE"));
            gb.Prod("CreateTableTailClauseList").Is(createTableTailClause);
            gb.Prod("CreateTableTailClauseList").Is(createTableTailClauseList, createTableTailClause);
            gb.Prod("CreateTableTailClause").Is(createTablePeriodClause);
            gb.Prod("CreateTableTailClause").Is(createTableOptions);
            gb.Prod("CreateTableTailClause").Is(createTableOnClause);
            gb.Prod("CreateTableTailClause").Is(createTableTextImageClause);

            gb.Prod("CreateTableElementList").Is(createTableElement);
            gb.Prod("CreateTableElementList").Is(createTableElementList, ",", createTableElement);
            // SQL Server memory-optimized tables may declare inline indices after columns without a comma
            gb.Prod("CreateTableElementList").Is(createTableElementList, createTableTableIndex);
            gb.Prod("CreateTableElement").Is(createTableColumnDefinition);
            gb.Prod("CreateTableElement").Is(createTableComputedColumn);
            gb.Prod("CreateTableElement").Is(createTableColumnSet);
            gb.Prod("CreateTableElement").Is(createTableConstraint);
            gb.Prod("CreateTableElement").Is(createTableTableIndex);

            gb.Prod("CreateTableColumnDefinition").Is(identifierTerm, typeSpec);
            gb.Prod("CreateTableColumnDefinition").Is(identifierTerm, typeSpec, createTableColumnOptionList);
            gb.Prod("CreateTableColumnOptionList").Is(createTableColumnOption);
            gb.Prod("CreateTableColumnOptionList").Is(createTableColumnOptionList, createTableColumnOption);
            gb.Prod("CreateTableColumnOption").Is(kw("NULL"));
            gb.Prod("CreateTableColumnOption").Is(kw("NOT"), kw("NULL"));
            gb.Prod("CreateTableColumnOption").Is(kw("PRIMARY"), kw("KEY"));
            gb.Prod("CreateTableColumnOption").Is(kw("UNIQUE"));
            gb.Prod("CreateTableColumnOption").Is(kw("SPARSE"));
            gb.Prod("CreateTableColumnOption").Is(kw("PERSISTED"));
            gb.Prod("CreateTableColumnOption").Is(kw("ROWGUIDCOL"));
            gb.Prod("CreateTableColumnOption").Is(kw("COLUMN_SET"));
            gb.Prod("CreateTableColumnOption").Is(kw("FOR"), kw("ALL_SPARSE_COLUMNS"));
            gb.Prod("CreateTableColumnOption").Is(kw("DEFAULT"), expression);
            gb.Prod("CreateTableColumnOption").Is(kw("DEFAULT"), "(", expression, ")");
            gb.Prod("CreateTableColumnOption").Is(kw("IDENTITY"));
            gb.Prod("CreateTableColumnOption").Is(kw("IDENTITY"), "(", expression, ",", expression, ")");
            gb.Prod("CreateTableColumnOption").Is(kw("COLLATE"), identifierTerm);
            gb.Prod("CreateTableColumnOption").Is(kw("MASKED"), kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("CreateTableColumnOption").Is(kw("ENCRYPTED"), kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("CreateTableColumnOption").Is(kw("NOT"), kw("FOR"), kw("REPLICATION"));
            gb.Prod("CreateTableColumnOption").Is(kw("CHECK"), "(", searchCondition, ")");
            gb.Prod("CreateTableColumnOption").Is(kw("REFERENCES"), qualifiedName);
            gb.Prod("CreateTableColumnOption").Is(kw("REFERENCES"), qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateTableColumnOption").Is(kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("CreateTableColumnOption").Is(kw("CONSTRAINT"), identifierTerm, createTableConstraintBody);
            gb.Prod("CreateTableColumnOption").Is(identifierTerm);
            gb.Prod("CreateTableColumnOption").Is(identifierTerm, identifierTerm);
            gb.Prod("CreateTableColumnOption").Is(qualifiedName, "(", expressionList, ")");
            gb.Prod("CreateTableColumnOption").Is(identifierTerm, "(", expressionList, ")");

            gb.Prod("CreateTableComputedColumn").Is(identifierTerm, kw("AS"), expression);
            gb.Prod("CreateTableComputedColumn").Is(identifierTerm, kw("AS"), expression, createTableColumnOptionList);

            gb.Prod("CreateTableColumnSet").Is(identifierTerm, typeSpec, kw("COLUMN_SET"), kw("FOR"), kw("ALL_SPARSE_COLUMNS"));

            gb.Prod("CreateTableConstraint").Is(createTableConstraintBody);
            gb.Prod("CreateTableConstraint").Is(kw("CONSTRAINT"), identifierTerm, createTableConstraintBody);

            gb.Prod("CreateTableConstraintBody").Is(kw("PRIMARY"), kw("KEY"), "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("PRIMARY"), kw("KEY"));
            gb.Prod("CreateTableConstraintBody").Is(kw("PRIMARY"), kw("KEY"), createTableClusterType);
            gb.Prod("CreateTableConstraintBody").Is(kw("PRIMARY"), kw("KEY"), createTableClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("PRIMARY"), kw("KEY"), createTableClusterType, "(", createTableKeyColumnList, ")", kw("ON"), indexStorageTarget);
            gb.Prod("CreateTableConstraintBody").Is(kw("PRIMARY"), kw("KEY"), "(", createTableKeyColumnList, ")", kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("PRIMARY"), kw("KEY"), createTableClusterType, "(", createTableKeyColumnList, ")", kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("PRIMARY"), kw("KEY"), createTableClusterType, "(", createTableKeyColumnList, ")", kw("WITH"), "(", indexOptionList, ")", kw("ON"), indexStorageTarget);
            gb.Prod("CreateTableConstraintBody").Is(kw("UNIQUE"), "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("UNIQUE"));
            gb.Prod("CreateTableConstraintBody").Is(kw("UNIQUE"), createTableClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("UNIQUE"), "(", createTableKeyColumnList, ")", kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("UNIQUE"), createTableClusterType, "(", createTableKeyColumnList, ")", kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("UNIQUE"), createTableClusterType, "(", createTableKeyColumnList, ")", kw("WITH"), "(", indexOptionList, ")", kw("ON"), indexStorageTarget);            gb.Prod("CreateTableConstraintBody").Is(kw("CHECK"), "(", searchCondition, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("FOREIGN"), kw("KEY"), "(", identifierList, ")", kw("REFERENCES"), qualifiedName);
            gb.Prod("CreateTableConstraintBody").Is(kw("FOREIGN"), kw("KEY"), "(", identifierList, ")", kw("REFERENCES"), qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("FOREIGN"), kw("KEY"), kw("REFERENCES"), qualifiedName);
            gb.Prod("CreateTableConstraintBody").Is(kw("FOREIGN"), kw("KEY"), kw("REFERENCES"), qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateTableConstraintBody").Is(kw("DEFAULT"), expression, kw("FOR"), identifierTerm);
            gb.Prod("CreateTableConstraintBody").Is(kw("DEFAULT"), "(", expression, ")", kw("FOR"), identifierTerm);

            gb.Prod("CreateTableClusterType").Is(kw("CLUSTERED"));
            gb.Prod("CreateTableClusterType").Is(kw("NONCLUSTERED"));
            gb.Prod("CreateTableClusterType").Is(kw("NONCLUSTERED"), kw("HASH"));
            gb.Prod("CreateTableClusterType").Is(kw("CLUSTERED"), kw("COLUMNSTORE"));
            gb.Prod("CreateTableClusterType").Is(kw("NONCLUSTERED"), kw("COLUMNSTORE"));

            gb.Prod("CreateTableKeyColumnList").Is(createTableKeyColumn);
            gb.Prod("CreateTableKeyColumnList").Is(createTableKeyColumnList, ",", createTableKeyColumn);
            gb.Prod("CreateTableKeyColumn").Is(identifierTerm);
            gb.Prod("CreateTableKeyColumn").Is(identifierTerm, kw("ASC"));
            gb.Prod("CreateTableKeyColumn").Is(identifierTerm, kw("DESC"));

            gb.Prod("CreateTableTableIndex").Is(kw("INDEX"), identifierTerm, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableIndex").Is(kw("INDEX"), identifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableIndex").Is(kw("INDEX"), identifierTerm, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod("CreateTableTableIndex").Is(kw("INDEX"), identifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod("CreateTableTableIndex").Is(kw("INDEX"), identifierTerm, kw("CLUSTERED"), kw("COLUMNSTORE"));
            gb.Prod("CreateTableTableIndex").Is(kw("INDEX"), identifierTerm, kw("CLUSTERED"), kw("COLUMNSTORE"), createIndexWithClause);

            gb.Prod("CreateTablePeriodClause").Is(kw("PERIOD"), forSystemTime, "(", identifierTerm, ",", identifierTerm, ")");
            gb.Prod("CreateTableOptions").Is(kw("WITH"), "(", createTableOptionList, ")");
            gb.Prod("CreateTableOptionList").Is(createTableOption);
            gb.Prod("CreateTableOptionList").Is(createTableOptionList, ",", createTableOption);
            gb.Prod("CreateTableOption").Is(identifierTerm, "=", indexOptionValue);
            gb.Prod("CreateTableOption").Is(qualifiedName, "=", indexOptionValue);
            gb.Prod("CreateTableOption").Is(identifierTerm);
            gb.Prod("CreateTableOption").Is(identifierTerm, identifierTerm);
            gb.Prod("CreateTableOption").Is(kw("CLUSTERED"), kw("COLUMNSTORE"), kw("INDEX"));
            gb.Prod("CreateTableOnClause").Is(kw("ON"), indexStorageTarget);
            gb.Prod("CreateTableTextImageClause").Is(kw("TEXTIMAGE_ON"), indexStorageTarget);

            gb.Prod("AlterTableStatement").Is(kw("ALTER"), kw("TABLE"), qualifiedName, alterTableAction);
            gb.Prod("AlterTableAction").Is(kw("ADD"), alterTableAddItemList);
            gb.Prod("AlterTableAction").Is(kw("ALTER"), kw("COLUMN"), alterTableAlterColumnAction);
            gb.Prod("AlterTableAction").Is(kw("DROP"), alterTableDropItemList);
            gb.Prod("AlterTableAction").Is(kw("WITH"), alterTableCheckMode, kw("ADD"), createTableConstraint);
            gb.Prod("AlterTableAction").Is(alterTableCheckMode, kw("CONSTRAINT"), alterTableConstraintTarget);
            gb.Prod("AlterTableAction").Is(kw("ENABLE"), kw("TRIGGER"), alterTableTriggerTarget);
            gb.Prod("AlterTableAction").Is(kw("DISABLE"), kw("TRIGGER"), alterTableTriggerTarget);
            gb.Prod("AlterTableAction").Is(kw("ADD"), createTablePeriodClause);
            gb.Prod("AlterTableAction").Is(kw("DROP"), kw("PERIOD"), forSystemTime);
            gb.Prod("AlterTableAction").Is(kw("SET"), "(", indexOptionList, ")");
            gb.Prod("AlterTableAddItemList").Is(alterTableAddItem);
            gb.Prod("AlterTableAddItemList").Is(alterTableAddItemList, ",", alterTableAddItem);
            gb.Prod("AlterTableAddItem").Is(createTableColumnDefinition);
            gb.Prod("AlterTableAddItem").Is(createTableConstraint);
            gb.Prod("AlterTableAddItem").Is(createTableTableIndex);
            gb.Prod("AlterTableAddItem").Is(createTableComputedColumn);
            gb.Prod("AlterTableAlterColumnAction").Is(identifierTerm, typeSpec);
            gb.Prod("AlterTableAlterColumnAction").Is(identifierTerm, typeSpec, alterTableColumnOptionList);
            gb.Prod("AlterTableAlterColumnAction").Is(identifierTerm, kw("ADD"), alterTableColumnOptionList);
            gb.Prod("AlterTableAlterColumnAction").Is(identifierTerm, kw("DROP"), alterTableColumnOptionList);
            gb.Prod("AlterTableColumnOptionList").Is(alterTableColumnOption);
            gb.Prod("AlterTableColumnOptionList").Is(alterTableColumnOptionList, alterTableColumnOption);
            gb.Prod("AlterTableColumnOption").Is(createTableColumnOption);
            gb.Prod("AlterTableDropItemList").Is(alterTableDropItem);
            gb.Prod("AlterTableDropItemList").Is(alterTableDropItemList, ",", alterTableDropItem);
            gb.Prod("AlterTableDropItem").Is(kw("COLUMN"), identifierTerm);
            gb.Prod("AlterTableDropItem").Is(kw("CONSTRAINT"), identifierTerm);
            gb.Prod("AlterTableDropItem").Is(kw("CONSTRAINT"), kw("ALL"));
            gb.Prod("AlterTableCheckMode").Is(kw("CHECK"));
            gb.Prod("AlterTableCheckMode").Is(kw("NOCHECK"));
            gb.Prod("AlterTableConstraintTarget").Is(identifierTerm);
            gb.Prod("AlterTableConstraintTarget").Is(kw("ALL"));
            gb.Prod("AlterTableTriggerTarget").Is(identifierTerm);
            gb.Prod("AlterTableTriggerTarget").Is(kw("ALL"));

            gb.Prod("CreateIndexHead").Is(kw("CREATE"), kw("INDEX"), identifierTerm);
            gb.Prod("CreateIndexHead").Is(kw("CREATE"), kw("UNIQUE"), kw("INDEX"), identifierTerm);
            gb.Prod("CreateIndexHead").Is(kw("CREATE"), kw("CLUSTERED"), kw("INDEX"), identifierTerm);
            gb.Prod("CreateIndexHead").Is(kw("CREATE"), kw("NONCLUSTERED"), kw("INDEX"), identifierTerm);
            gb.Prod("CreateIndexHead").Is(kw("CREATE"), kw("UNIQUE"), kw("CLUSTERED"), kw("INDEX"), identifierTerm);
            gb.Prod("CreateIndexHead").Is(kw("CREATE"), kw("UNIQUE"), kw("NONCLUSTERED"), kw("INDEX"), identifierTerm);
            gb.Prod("CreateIndexHead").Is(kw("CREATE"), kw("NONCLUSTERED"), kw("COLUMNSTORE"), kw("INDEX"), identifierTerm);
            gb.Prod("CreateIndexHead").Is(kw("CREATE"), kw("CLUSTERED"), kw("COLUMNSTORE"), kw("INDEX"), identifierTerm);
            gb.Prod("CreateIndexStatement").Is(createIndexHead, kw("ON"), qualifiedName);
            gb.Prod("CreateIndexStatement").Is(createIndexHead, kw("ON"), qualifiedName, createIndexTailClauseList);
            gb.Prod("CreateIndexStatement").Is(createIndexHead, kw("ON"), qualifiedName, "(", createIndexKeyList, ")");
            gb.Prod("CreateIndexStatement").Is(createIndexHead, kw("ON"), qualifiedName, "(", createIndexKeyList, ")", createIndexTailClauseList);

            gb.Prod("CreateIndexKeyList").Is(createIndexKeyItem);
            gb.Prod("CreateIndexKeyList").Is(createIndexKeyList, ",", createIndexKeyItem);
            gb.Prod("CreateIndexKeyItem").Is(identifierTerm);
            gb.Prod("CreateIndexKeyItem").Is(identifierTerm, kw("ASC"));
            gb.Prod("CreateIndexKeyItem").Is(identifierTerm, kw("DESC"));

            gb.Prod("CreateIndexTailClauseList").Is(createIndexTailClause);
            gb.Prod("CreateIndexTailClauseList").Is(createIndexTailClauseList, createIndexTailClause);
            gb.Prod("CreateIndexTailClause").Is(createIndexIncludeClause);
            gb.Prod("CreateIndexTailClause").Is(createIndexWhereClause);
            gb.Prod("CreateIndexTailClause").Is(createIndexWithClause);
            gb.Prod("CreateIndexTailClause").Is(createIndexStorageClause);
            gb.Prod("CreateIndexTailClause").Is(createIndexFileStreamClause);

            gb.Prod("CreateIndexIncludeClause").Is(kw("INCLUDE"), "(", createIndexIncludeList, ")");
            gb.Prod("CreateIndexIncludeList").Is(identifierTerm);
            gb.Prod("CreateIndexIncludeList").Is(createIndexIncludeList, ",", identifierTerm);
            gb.Prod("CreateIndexWhereClause").Is(kw("WHERE"), searchCondition);
            gb.Prod("CreateIndexWithClause").Is(kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("CreateIndexStorageClause").Is(kw("ON"), indexStorageTarget);
            gb.Prod("CreateIndexFileStreamClause").Is(kw("FILESTREAM_ON"), indexFileStreamTarget);

            gb.Prod("IndexStorageTarget").Is(qualifiedName);
            gb.Prod("IndexStorageTarget").Is(kw("DEFAULT"));
            gb.Prod("IndexStorageTarget").Is(qualifiedName, "(", identifierTerm, ")");
            gb.Prod("IndexFileStreamTarget").Is(qualifiedName);
            gb.Prod("IndexFileStreamTarget").Is(kw("NULL"));
            gb.Prod("IndexFileStreamTarget").Is(qualifiedName, "(", identifierTerm, ")");

            gb.Prod("IndexOptionList").Is(indexOption);
            gb.Prod("IndexOptionList").Is(indexOptionList, ",", indexOption);
            gb.Prod("IndexOptionList").Is(indexOptionList, ","); // allow trailing comma
            gb.Prod("IndexOption").Is(indexOptionName, "=", indexOptionValue);
            gb.Prod("IndexOption").Is(indexOptionName, "(", indexOptionList, ")");
            gb.Prod("IndexOption").Is(indexOptionName, "=", indexOptionValue, kw("ON"), kw("PARTITIONS"), "(", indexPartitionList, ")");

            gb.Prod("IndexOptionName").Is(identifierTerm);
            gb.Prod("IndexOptionName").Is(qualifiedName);
            gb.Prod("IndexOptionName").Is(kw("FUNCTION"));
            gb.Prod("IndexOptionName").Is(kw("ONLINE"));
            gb.Prod("IndexOptionName").Is(kw("MAXDOP"));

            gb.Prod("IndexOptionValue").Is(expression);
            gb.Prod("IndexOptionValue").Is(indexOnOffValue);
            gb.Prod("IndexOptionValue").Is(identifierTerm);
            gb.Prod("IndexOptionValue").Is(kw("NONE"));
            gb.Prod("IndexOptionValue").Is(kw("SELF"));
            gb.Prod("IndexOptionValue").Is(kw("ROW"));
            gb.Prod("IndexOptionValue").Is(kw("PAGE"));
            gb.Prod("IndexOptionValue").Is(kw("COLUMNSTORE"));
            gb.Prod("IndexOptionValue").Is(kw("COLUMNSTORE_ARCHIVE"));
            gb.Prod("IndexOptionValue").Is(expression, identifierTerm);
            gb.Prod("IndexOptionValue").Is(identifierTerm, identifierTerm);
            gb.Prod("IndexOptionValue").Is(indexOnOffValue, "(", indexOptionList, ")");
            gb.Prod("IndexOptionValue").Is("(", indexOptionList, ")");
            gb.Prod("IndexOnOffValue").Is(kw("ON"));
            gb.Prod("IndexOnOffValue").Is(kw("OFF"));

            gb.Prod("IndexPartitionList").Is(indexPartitionItem);
            gb.Prod("IndexPartitionList").Is(indexPartitionList, ",", indexPartitionItem);
            gb.Prod("IndexPartitionItem").Is(expression);
            gb.Prod("IndexPartitionItem").Is(expression, kw("TO"), expression);
            gb.Prod("IndexPartitionItem").Is(kw("ALL"));

            gb.Prod("AlterIndexStatement").Is(kw("ALTER"), kw("INDEX"), alterIndexTarget, kw("ON"), qualifiedName, alterIndexAction);
            gb.Prod("AlterIndexTarget").Is(identifierTerm);
            gb.Prod("AlterIndexTarget").Is(qualifiedName);
            gb.Prod("AlterIndexTarget").Is(kw("ALL"));
            gb.Prod("AlterIndexAction").Is(kw("REBUILD"));
            gb.Prod("AlterIndexAction").Is(kw("REBUILD"), alterIndexRebuildSpec);
            gb.Prod("AlterIndexAction").Is(kw("DISABLE"));
            gb.Prod("AlterIndexAction").Is(kw("REORGANIZE"));
            gb.Prod("AlterIndexAction").Is(kw("REORGANIZE"), alterIndexReorganizeSpec);
            gb.Prod("AlterIndexAction").Is(kw("SET"), "(", indexOptionList, ")");
            gb.Prod("AlterIndexAction").Is(kw("RESUME"));
            gb.Prod("AlterIndexAction").Is(kw("RESUME"), alterIndexResumeSpec);
            gb.Prod("AlterIndexAction").Is(kw("PAUSE"));
            gb.Prod("AlterIndexAction").Is(kw("ABORT"));

            gb.Prod("AlterIndexRebuildSpec").Is(kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("AlterIndexRebuildSpec").Is(kw("PARTITION"), "=", alterIndexPartitionSelector);
            gb.Prod("AlterIndexRebuildSpec").Is(kw("PARTITION"), "=", alterIndexPartitionSelector, kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("AlterIndexReorganizeSpec").Is(kw("PARTITION"), "=", alterIndexPartitionSelector);
            gb.Prod("AlterIndexReorganizeSpec").Is(kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("AlterIndexReorganizeSpec").Is(kw("PARTITION"), "=", alterIndexPartitionSelector, kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("AlterIndexResumeSpec").Is(kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("AlterIndexPartitionSelector").Is(expression);
            gb.Prod("AlterIndexPartitionSelector").Is(kw("ALL"));

            gb.Prod("CreateDatabaseStatement").Is(kw("CREATE"), kw("DATABASE"), identifierTerm);
            gb.Prod("CreateDatabaseStatement").Is(kw("CREATE"), kw("DATABASE"), identifierTerm, createDatabaseClauseList);

            gb.Prod("CreateDatabaseClauseList").Is(createDatabaseClause);
            gb.Prod("CreateDatabaseClauseList").Is(createDatabaseClauseList, createDatabaseClause);
            gb.Prod("CreateDatabaseClause").Is(createDatabaseContainmentClause);
            gb.Prod("CreateDatabaseClause").Is(createDatabaseOnClause);
            gb.Prod("CreateDatabaseClause").Is(createDatabaseCollateClause);
            gb.Prod("CreateDatabaseClause").Is(createDatabaseWithClause);

            gb.Prod("CreateDatabaseContainmentClause").Is(kw("CONTAINMENT"), "=", kw("NONE"));
            gb.Prod("CreateDatabaseContainmentClause").Is(kw("CONTAINMENT"), "=", kw("PARTIAL"));

            gb.Prod("CreateDatabaseOnClause").Is(kw("ON"), createDatabaseOnItemList);
            gb.Prod("CreateDatabaseOnItemList").Is(createDatabaseOnItem);
            gb.Prod("CreateDatabaseOnItemList").Is(createDatabaseOnItemList, ",", createDatabaseOnItem);
            gb.Prod("CreateDatabaseOnItem").Is(createDatabaseFilespecList);
            gb.Prod("CreateDatabaseOnItem").Is(kw("PRIMARY"), createDatabaseFilespecList);
            gb.Prod("CreateDatabaseOnItem").Is(createDatabaseFilegroup);
            gb.Prod("CreateDatabaseOnItem").Is(kw("LOG"), kw("ON"), createDatabaseFilespecList);

            gb.Prod("CreateDatabaseFilespecList").Is(createDatabaseFilespec);

            gb.Prod("CreateDatabaseFilegroup").Is(kw("FILEGROUP"), identifierTerm, createDatabaseFilespecList);
            gb.Prod("CreateDatabaseFilegroup").Is(kw("FILEGROUP"), identifierTerm, kw("DEFAULT"), createDatabaseFilespecList);
            gb.Prod("CreateDatabaseFilegroup").Is(kw("FILEGROUP"), identifierTerm, kw("CONTAINS"), kw("FILESTREAM"), createDatabaseFilespecList);
            gb.Prod("CreateDatabaseFilegroup").Is(kw("FILEGROUP"), identifierTerm, kw("CONTAINS"), kw("FILESTREAM"), kw("DEFAULT"), createDatabaseFilespecList);
            gb.Prod("CreateDatabaseFilegroup").Is(kw("FILEGROUP"), identifierTerm, kw("CONTAINS"), kw("MEMORY_OPTIMIZED_DATA"), createDatabaseFilespecList);

            gb.Prod("CreateDatabaseCollateClause").Is(kw("COLLATE"), identifierTerm);

            gb.Prod("CreateDatabaseWithClause").Is(kw("WITH"), createDatabaseOptionList);
            gb.Prod("CreateDatabaseOptionList").Is(createDatabaseOption);
            gb.Prod("CreateDatabaseOptionList").Is(createDatabaseOptionList, ",", createDatabaseOption);

            gb.Prod("CreateDatabaseOption").Is(kw("FILESTREAM"), "(", createDatabaseFilestreamOptionList, ")");
            gb.Prod("CreateDatabaseOption").Is(kw("DEFAULT_FULLTEXT_LANGUAGE"), "=", createDatabaseOptionValue);
            gb.Prod("CreateDatabaseOption").Is(kw("DEFAULT_LANGUAGE"), "=", createDatabaseOptionValue);
            gb.Prod("CreateDatabaseOption").Is(kw("NESTED_TRIGGERS"), "=", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is(kw("TRANSFORM_NOISE_WORDS"), "=", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is(kw("TWO_DIGIT_YEAR_CUTOFF"), "=", number);
            gb.Prod("CreateDatabaseOption").Is(kw("DB_CHAINING"), createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is(kw("DB_CHAINING"), "=", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is(kw("TRUSTWORTHY"), createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is(kw("TRUSTWORTHY"), "=", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is(kw("LEDGER"), "=", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is(
                kw("PERSISTENT_LOG_BUFFER"),
                "=",
                kw("ON"),
                "(",
                kw("DIRECTORY_NAME"),
                "=",
                stringLiteral,
                ")");

            gb.Prod("CreateDatabaseOptionValue").Is(number);
            gb.Prod("CreateDatabaseOptionValue").Is(identifierTerm);
            gb.Prod("CreateDatabaseOptionValue").Is(stringLiteral);

            gb.Prod("CreateDatabaseOnOffValue").Is(kw("ON"));
            gb.Prod("CreateDatabaseOnOffValue").Is(kw("OFF"));

            gb.Prod("CreateDatabaseFilestreamOptionList").Is(createDatabaseFilestreamOption);
            gb.Prod("CreateDatabaseFilestreamOptionList").Is(createDatabaseFilestreamOptionList, ",", createDatabaseFilestreamOption);
            gb.Prod("CreateDatabaseFilestreamOption").Is(kw("NON_TRANSACTED_ACCESS"), "=", createDatabaseNonTransactedAccessValue);
            gb.Prod("CreateDatabaseFilestreamOption").Is(kw("DIRECTORY_NAME"), "=", stringLiteral);
            gb.Prod("CreateDatabaseNonTransactedAccessValue").Is(kw("OFF"));
            gb.Prod("CreateDatabaseNonTransactedAccessValue").Is(kw("READ_ONLY"));
            gb.Prod("CreateDatabaseNonTransactedAccessValue").Is(kw("FULL"));

            gb.Prod("CreateDatabaseFilespec").Is(
                "(",
                identifierTerm,
                "=",
                createDatabaseFileName,
                ",",
                identifierTerm,
                "=",
                stringLiteral,
                ")");
            gb.Prod("CreateDatabaseFilespec").Is(
                "(",
                identifierTerm,
                "=",
                createDatabaseFileName,
                ",",
                identifierTerm,
                "=",
                stringLiteral,
                ",",
                createDatabaseFilespecOptionList,
                ")");

            gb.Prod("CreateDatabaseFileName").Is(identifierTerm);
            gb.Prod("CreateDatabaseFileName").Is(stringLiteral);

            gb.Prod("CreateDatabaseFilespecOptionList").Is(createDatabaseFilespecOption);
            gb.Prod("CreateDatabaseFilespecOptionList").Is(createDatabaseFilespecOptionList, ",", createDatabaseFilespecOption);
            gb.Prod("CreateDatabaseFilespecOption").Is(kw("SIZE"), "=", createDatabaseSizeSpec);
            gb.Prod("CreateDatabaseFilespecOption").Is(kw("MAXSIZE"), "=", createDatabaseMaxSizeSpec);
            gb.Prod("CreateDatabaseFilespecOption").Is(kw("FILEGROWTH"), "=", createDatabaseGrowthSpec);

            gb.Prod("CreateDatabaseSizeSpec").Is(number);
            gb.Prod("CreateDatabaseSizeSpec").Is(number, createDatabaseSizeUnit);
            gb.Prod("CreateDatabaseSizeSpec").Is(number, identifierTerm);

            gb.Prod("CreateDatabaseMaxSizeSpec").Is(kw("UNLIMITED"));
            gb.Prod("CreateDatabaseMaxSizeSpec").Is(number);
            gb.Prod("CreateDatabaseMaxSizeSpec").Is(number, createDatabaseSizeUnit);
            gb.Prod("CreateDatabaseMaxSizeSpec").Is(number, identifierTerm);

            gb.Prod("CreateDatabaseGrowthSpec").Is(number);
            gb.Prod("CreateDatabaseGrowthSpec").Is(number, createDatabaseGrowthUnit);
            gb.Prod("CreateDatabaseGrowthSpec").Is(number, identifierTerm);

            gb.Prod("CreateDatabaseSizeUnit").Is(kw("KB"));
            gb.Prod("CreateDatabaseSizeUnit").Is(kw("MB"));
            gb.Prod("CreateDatabaseSizeUnit").Is(kw("GB"));
            gb.Prod("CreateDatabaseSizeUnit").Is(kw("TB"));

            gb.Prod("CreateDatabaseGrowthUnit").Is(createDatabaseSizeUnit);
            gb.Prod("CreateDatabaseGrowthUnit").Is("%");

            gb.Prod("WithXmlNamespacesClause").Is(kw("WITH"), kw("XMLNAMESPACES"), "(", xmlNamespaceItemList, ")");
            gb.Prod("XmlNamespaceItemList").Is(xmlNamespaceItem);
            gb.Prod("XmlNamespaceItemList").Is(xmlNamespaceItemList, ",", xmlNamespaceItem);
            gb.Prod("XmlNamespaceItem").Is(expression, kw("AS"), identifierTerm);
            gb.Prod("XmlNamespaceItem").Is(kw("DEFAULT"), expression);

            gb.Prod("WithClause").Is(kw("WITH"), cteDefinitionList);
            gb.Prod("CteDefinitionList").Is(cteDefinition);
            gb.Prod("CteDefinitionList").Is(cteDefinitionList, ",", cteDefinition);
            gb.Prod("CteDefinition").Is(identifierTerm, kw("AS"), "(", queryExpression, ")");
            gb.Prod("CteDefinition").Is(identifierTerm, "(", identifierList, ")", kw("AS"), "(", queryExpression, ")");

            gb.Prod("QueryExpression").Is(queryUnionExpression);
            gb.Prod("QueryUnionExpression").Is(queryIntersectExpression);
            gb.Prod("QueryUnionExpression").Is(queryUnionExpression, setOperator, queryIntersectExpression);
            gb.Prod("QueryIntersectExpression").Is(queryPrimary);
            gb.Prod("QueryIntersectExpression").Is(queryIntersectExpression, kw("INTERSECT"), queryPrimary);

            gb.Prod("SetOperator").Is(kw("UNION"));
            gb.Prod("SetOperator").Is(kw("UNION"), kw("ALL"));
            gb.Prod("SetOperator").Is(kw("EXCEPT"));

            gb.Prod("QueryPrimary").Is(querySpecification);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause, offsetFetchClause);
            gb.Prod("QueryPrimary").Is(querySpecification, forClause);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause, forClause);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause, offsetFetchClause, forClause);
            gb.Prod("QueryPrimary").Is(querySpecification, optionClause);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause, optionClause);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause, offsetFetchClause, optionClause);
            gb.Prod("QueryPrimary").Is(querySpecification, forClause, optionClause);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause, forClause, optionClause);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause, offsetFetchClause, forClause, optionClause);
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")");
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")", orderByClause);
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")", orderByClause, offsetFetchClause);

            gb.Prod("ForClause").Is(kw("FOR"), kw("BROWSE"));
            gb.Prod("ForClause").Is(kw("FOR"), kw("JSON"), forJsonMode);
            gb.Prod("ForClause").Is(kw("FOR"), kw("JSON"), forJsonMode, ",", forJsonOptionList);
            gb.Prod("ForClause").Is(kw("FOR"), kw("XML"), forXmlMode);
            gb.Prod("ForClause").Is(kw("FOR"), kw("XML"), forXmlMode, ",", forXmlOptionList);

            gb.Prod("ForJsonMode").Is(kw("AUTO"));
            gb.Prod("ForJsonMode").Is(kw("PATH"));
            gb.Prod("ForJsonMode").Is(kw("NONE"));

            gb.Prod("ForJsonOptionList").Is(forJsonOption);
            gb.Prod("ForJsonOptionList").Is(forJsonOptionList, ",", forJsonOption);
            gb.Prod("ForJsonOption").Is(kw("WITHOUT_ARRAY_WRAPPER"));
            gb.Prod("ForJsonOption").Is(kw("INCLUDE_NULL_VALUES"));
            gb.Prod("ForJsonOption").Is(kw("ROOT"));
            gb.Prod("ForJsonOption").Is(kw("ROOT"), "(", expression, ")");

            gb.Prod("ForXmlMode").Is(kw("AUTO"));
            gb.Prod("ForXmlMode").Is(kw("PATH"));
            gb.Prod("ForXmlMode").Is(kw("PATH"), "(", expression, ")");
            gb.Prod("ForXmlMode").Is(kw("RAW"));
            gb.Prod("ForXmlMode").Is(kw("RAW"), "(", expression, ")");
            gb.Prod("ForXmlMode").Is(kw("EXPLICIT"));

            gb.Prod("ForXmlOptionList").Is(forXmlOption);
            gb.Prod("ForXmlOptionList").Is(forXmlOptionList, ",", forXmlOption);
            gb.Prod("ForXmlOption").Is(kw("TYPE"));
            gb.Prod("ForXmlOption").Is(kw("XMLDATA"));
            gb.Prod("ForXmlOption").Is(kw("XMLSCHEMA"));
            gb.Prod("ForXmlOption").Is(kw("XMLSCHEMA"), "(", expression, ")");
            gb.Prod("ForXmlOption").Is(kw("ELEMENTS"));
            gb.Prod("ForXmlOption").Is(kw("ELEMENTS"), kw("XSINIL"));
            gb.Prod("ForXmlOption").Is(kw("ELEMENTS"), kw("ABSENT"));
            gb.Prod("ForXmlOption").Is(kw("ROOT"));
            gb.Prod("ForXmlOption").Is(kw("ROOT"), "(", expression, ")");
            gb.Prod("ForXmlOption").Is(kw("BINARY"), kw("BASE64"));
            gb.Prod("ForXmlOption").Is(kw("WITHOUT_ARRAY_WRAPPER"));

            gb.Prod("SelectCore").Is(kw("SELECT"), selectList, kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(kw("SELECT"), setQuantifier, selectList, kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(kw("SELECT"), topClause, selectList, kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(kw("SELECT"), setQuantifier, topClause, selectList, kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(kw("SELECT"), selectList, kw("INTO"), qualifiedName, kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(kw("SELECT"), setQuantifier, selectList, kw("INTO"), qualifiedName, kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(kw("SELECT"), topClause, selectList, kw("INTO"), qualifiedName, kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(kw("SELECT"), selectList);
            gb.Prod("SelectCore").Is(kw("SELECT"), setQuantifier, selectList);
            gb.Prod("SelectCore").Is(kw("SELECT"), topClause, selectList);
            gb.Prod("SelectCore").Is(kw("SELECT"), setQuantifier, topClause, selectList);

            gb.Prod("QuerySpecification").Is(selectCore);
            gb.Prod("QuerySpecification").Is(selectCore, kw("WHERE"), searchCondition);
            gb.Prod("QuerySpecification").Is(selectCore, kw("GROUP"), kw("BY"), expressionList);
            gb.Prod("QuerySpecification").Is(selectCore, kw("WHERE"), searchCondition, kw("GROUP"), kw("BY"), expressionList);
            gb.Prod("QuerySpecification").Is(selectCore, kw("GROUP"), kw("BY"), expressionList, kw("HAVING"), searchCondition);
            gb.Prod("QuerySpecification").Is(selectCore, kw("WHERE"), searchCondition, kw("GROUP"), kw("BY"), expressionList, kw("HAVING"), searchCondition);
            gb.Prod("QuerySpecification").Is(selectCore, kw("GROUP"), kw("BY"), expressionList, kw("WITH"), identifierTerm);
            gb.Prod("QuerySpecification").Is(selectCore, kw("WHERE"), searchCondition, kw("GROUP"), kw("BY"), expressionList, kw("WITH"), identifierTerm);
            gb.Prod("QuerySpecification").Is(selectCore, kw("GROUP"), kw("BY"), expressionList, kw("WITH"), identifierTerm, kw("HAVING"), searchCondition);
            gb.Prod("QuerySpecification").Is(selectCore, kw("WHERE"), searchCondition, kw("GROUP"), kw("BY"), expressionList, kw("WITH"), identifierTerm, kw("HAVING"), searchCondition);

            gb.Prod("SetQuantifier").Is(kw("ALL"));
            gb.Prod("SetQuantifier").Is(kw("DISTINCT"));

            gb.Prod("TopClause").Is(kw("TOP"), topValue);
            gb.Prod("TopClause").Is(kw("TOP"), topValue, kw("PERCENT"));
            gb.Prod("TopClause").Is(kw("TOP"), topValue, kw("WITH"), kw("TIES"));
            gb.Prod("TopClause").Is(kw("TOP"), topValue, kw("PERCENT"), kw("WITH"), kw("TIES"));
            gb.Prod("TopValue").Is(number);
            gb.Prod("TopValue").Is("(", expression, ")");

            gb.Prod("SelectList").Is(selectItemList);
            gb.Prod("SelectItemList").Is(selectItem);
            gb.Prod("SelectItemList").Is(selectItemList, ",", selectItem);
            gb.Prod("SelectItem").Is("*");
            gb.Prod("SelectItem").Is(expression, kw("AS"), identifierTerm);
            gb.Prod("SelectItem").Is(expression, kw("AS"), stringLiteral);
            gb.Prod("SelectItem").Is(expression, identifierTerm);
            gb.Prod("SelectItem").Is(expression, stringLiteral);
            gb.Prod("SelectItem").Is(expression);
            gb.Prod("SelectItem").Is(qualifiedName, ".", "*");
            gb.Prod("SelectItem").Is(variableReference, "=", expression);
            gb.Prod("SelectItem").Is(variableReference, compoundAssignOp, expression);

            gb.Prod("TableSourceList").Is(tableSource);
            gb.Prod("TableSourceList").Is(tableSourceList, ",", tableSource);
            gb.Prod("TableSource").Is(tableFactor);
            gb.Prod("TableSource").Is(tableSource, joinPart);
            gb.Prod("TableFactor").Is(qualifiedName);
            gb.Prod("TableFactor").Is(qualifiedName, kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, forPath);
            gb.Prod("TableFactor").Is(qualifiedName, forPath, identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, kw("WITH"), "(", tableHintLimitedList, ")");
            gb.Prod("TableFactor").Is(qualifiedName, kw("AS"), identifierTerm, kw("WITH"), "(", tableHintLimitedList, ")");
            gb.Prod("TableFactor").Is(qualifiedName, identifierTerm, kw("WITH"), "(", tableHintLimitedList, ")");
            gb.Prod("TableFactor").Is(qualifiedName, temporalClause);
            gb.Prod("TableFactor").Is(qualifiedName, temporalClause, kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, temporalClause, identifierTerm);
            gb.Prod("TemporalClause").Is(forSystemTime, kw("AS"), kw("OF"), additiveExpression);
            gb.Prod("TemporalClause").Is(forSystemTime, kw("ALL"));
            gb.Prod("TemporalClause").Is(forSystemTime, kw("BETWEEN"), additiveExpression, kw("AND"), additiveExpression);
            gb.Prod("TemporalClause").Is(forSystemTime, kw("FROM"), additiveExpression, kw("TO"), additiveExpression);
            gb.Prod("TemporalClause").Is(forSystemTime, kw("CONTAINED"), kw("IN"), "(", additiveExpression, ",", additiveExpression, ")");
            gb.Prod("TableFactor").Is(variableReference);
            gb.Prod("TableFactor").Is(variableReference, kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is(variableReference, identifierTerm);
            gb.Prod("TableFactor").Is(functionCall);
            gb.Prod("TableFactor").Is(functionCall, kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is(functionCall, identifierTerm);
            gb.Prod("TableFactor").Is(functionCall, kw("AS"), identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is(functionCall, identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is(functionCall, openJsonWithClause);
            gb.Prod("TableFactor").Is(functionCall, openJsonWithClause, kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is(functionCall, openJsonWithClause, identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", kw("AS"), identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, "(", insertColumnList, ")");
            // PIVOT / UNPIVOT applied to derived table
            gb.Prod("TableFactor").Is("(", queryExpression, ")", kw("AS"), identifierTerm, kw("PIVOT"), "(", pivotClause, ")", kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, kw("PIVOT"), "(", pivotClause, ")", kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", kw("AS"), identifierTerm, kw("PIVOT"), "(", pivotClause, ")", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, kw("PIVOT"), "(", pivotClause, ")", identifierTerm);
            gb.Prod("PivotClause").Is(functionCall, kw("FOR"), identifierTerm, kw("IN"), "(", pivotValueList, ")");
            gb.Prod("PivotValueList").Is(expression);
            gb.Prod("PivotValueList").Is(pivotValueList, ",", expression);
            gb.Prod("TableFactor").Is("(", kw("VALUES"), rowValueList, ")", kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is("(", kw("VALUES"), rowValueList, ")", identifierTerm);
            gb.Prod("TableFactor").Is("(", kw("VALUES"), rowValueList, ")", kw("AS"), identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is("(", kw("VALUES"), rowValueList, ")", identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is("(", tableSource, ")");
            gb.Prod("TableFactor").Is("(", tableSource, ")", kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is("(", tableSource, ")", identifierTerm);

            gb.Prod("OpenJsonWithClause").Is(kw("WITH"), "(", openJsonColumnList, ")");
            gb.Prod("OpenJsonColumnList").Is(openJsonColumnDef);
            gb.Prod("OpenJsonColumnList").Is(openJsonColumnList, ",", openJsonColumnDef);
            // col_name typeSpec [ path_expr ] [ AS JSON ]
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec);
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec, expression);
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec, kw("AS"), kw("JSON"));
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec, expression, kw("AS"), kw("JSON"));

            gb.Prod("JoinPart").Is(kw("JOIN"), tableFactor, kw("ON"), searchCondition);
            gb.Prod("JoinPart").Is(joinType, kw("JOIN"), tableFactor, kw("ON"), searchCondition);
            gb.Prod("JoinPart").Is(kw("CROSS"), kw("JOIN"), tableFactor);
            gb.Prod("JoinPart").Is(kw("CROSS"), kw("APPLY"), tableFactor);
            gb.Prod("JoinPart").Is(kw("OUTER"), kw("APPLY"), tableFactor);

            gb.Prod("JoinType").Is(kw("INNER"));
            gb.Prod("JoinType").Is(kw("INNER"), kw("HASH"));
            gb.Prod("JoinType").Is(kw("INNER"), kw("LOOP"));
            gb.Prod("JoinType").Is(kw("INNER"), kw("MERGE"));
            gb.Prod("JoinType").Is(kw("LEFT"));
            gb.Prod("JoinType").Is(kw("LEFT"), kw("OUTER"));
            gb.Prod("JoinType").Is(kw("LEFT"), kw("HASH"));
            gb.Prod("JoinType").Is(kw("LEFT"), kw("OUTER"), kw("HASH"));
            gb.Prod("JoinType").Is(kw("RIGHT"));
            gb.Prod("JoinType").Is(kw("RIGHT"), kw("OUTER"));
            gb.Prod("JoinType").Is(kw("RIGHT"), kw("HASH"));
            gb.Prod("JoinType").Is(kw("RIGHT"), kw("OUTER"), kw("HASH"));
            gb.Prod("JoinType").Is(kw("FULL"));
            gb.Prod("JoinType").Is(kw("FULL"), kw("OUTER"));

            gb.Prod("OrderByClause").Is(kw("ORDER"), kw("BY"), orderItemList);
            gb.Prod("OrderItemList").Is(orderItem);
            gb.Prod("OrderItemList").Is(orderItemList, ",", orderItem);
            gb.Prod("OrderItem").Is(expression);
            gb.Prod("OrderItem").Is(expression, kw("ASC"));
            gb.Prod("OrderItem").Is(expression, kw("DESC"));

            gb.Prod("OffsetFetchClause").Is(kw("OFFSET"), expression, kw("ROWS"));
            gb.Prod("OffsetFetchClause").Is(
                kw("OFFSET"),
                expression,
                kw("ROWS"),
                kw("FETCH"),
                kw("NEXT"),
                expression,
                kw("ROWS"),
                kw("ONLY"));

            gb.Prod("SearchCondition").Is(expression);

            gb.Prod("Expression").Is(logicalOrExpression);

            gb.Prod("LogicalOrExpression").Is(logicalAndExpression);
            gb.Prod("LogicalOrExpression").Is(logicalOrExpression, kw("OR"), logicalAndExpression);

            gb.Prod("LogicalAndExpression").Is(logicalNotExpression);
            gb.Prod("LogicalAndExpression").Is(logicalAndExpression, kw("AND"), logicalNotExpression);

            gb.Prod("LogicalNotExpression").Is(comparisonExpression);
            gb.Prod("LogicalNotExpression").Is(kw("NOT"), logicalNotExpression);

            gb.Prod("ComparisonExpression").Is(additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, comparisonOperator, additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, likeOperator, additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, inOperator, additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, isOperator, additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, betweenOperator, additiveExpression, kw("AND"), additiveExpression);

            gb.Prod("ComparisonOperator").Is("=");
            gb.Prod("ComparisonOperator").Is("<>");
            gb.Prod("ComparisonOperator").Is("!=");
            gb.Prod("ComparisonOperator").Is("<");
            gb.Prod("ComparisonOperator").Is("<=");
            gb.Prod("ComparisonOperator").Is(">");
            gb.Prod("ComparisonOperator").Is(">=");

            gb.Prod("LikeOperator").Is(kw("LIKE"));
            gb.Prod("LikeOperator").Is(kw("NOT"), kw("LIKE"));

            gb.Prod("InOperator").Is(kw("IN"));
            gb.Prod("InOperator").Is(kw("NOT"), kw("IN"));

            gb.Prod("IsOperator").Is(kw("IS"));
            gb.Prod("IsOperator").Is(kw("IS"), kw("NOT"));

            gb.Prod("BetweenOperator").Is(kw("BETWEEN"));
            gb.Prod("BetweenOperator").Is(kw("NOT"), kw("BETWEEN"));

            gb.Prod("AdditiveExpression").Is(multiplicativeExpression);
            gb.Prod("AdditiveExpression").Is(additiveExpression, "+", multiplicativeExpression);
            gb.Prod("AdditiveExpression").Is(additiveExpression, "-", multiplicativeExpression);
            gb.Prod("AdditiveExpression").Is(additiveExpression, "&", multiplicativeExpression);

            gb.Prod("MultiplicativeExpression").Is(unaryExpression);
            gb.Prod("MultiplicativeExpression").Is(multiplicativeExpression, "*", unaryExpression);
            gb.Prod("MultiplicativeExpression").Is(multiplicativeExpression, "/", unaryExpression);
            gb.Prod("MultiplicativeExpression").Is(multiplicativeExpression, "%", unaryExpression);

            gb.Prod("UnaryExpression").Is(collateExpression);
            gb.Prod("UnaryExpression").Is("+", unaryExpression);
            gb.Prod("UnaryExpression").Is("-", unaryExpression);
            gb.Prod("UnaryExpression").Is("~", unaryExpression);

            gb.Prod("CollateExpression").Is(primaryExpression);
            gb.Prod("CollateExpression").Is(collateExpression, kw("COLLATE"), identifierTerm);

            gb.Prod("PrimaryExpression").Is(literal);
            gb.Prod("PrimaryExpression").Is(unicodeStringLiteral);
            gb.Prod("PrimaryExpression").Is(sqlcmdVariable);
            gb.Prod("PrimaryExpression").Is(variableReference);
            gb.Prod("PrimaryExpression").Is(graphColumnRef);
            gb.Prod("PrimaryExpression").Is(qualifiedName);
            gb.Prod("PrimaryExpression").Is(functionCall);
            gb.Prod("PrimaryExpression").Is(functionCall, overClause);
            gb.Prod("PrimaryExpression").Is(functionCall, graphWithinGroupClause);
            gb.Prod("PrimaryExpression").Is(kw("CAST"), "(", expression, kw("AS"), typeSpec, ")");
            gb.Prod("PrimaryExpression").Is("(", expression, ")");
            gb.Prod("PrimaryExpression").Is("(", expressionList, ")");
            gb.Prod("PrimaryExpression").Is("(", queryExpression, ")");
            gb.Prod("PrimaryExpression").Is(kw("EXISTS"), "(", queryExpression, ")");
            gb.Prod("PrimaryExpression").Is(kw("NOT"), kw("EXISTS"), "(", queryExpression, ")");
            gb.Prod("PrimaryExpression").Is(caseExpression);
            // LANGUAGE language_term used in full-text function calls (FREETEXTTABLE, CONTAINSTABLE, etc.)
            gb.Prod("PrimaryExpression").Is(kw("LANGUAGE"), primaryExpression);

            gb.Prod("FunctionCall").Is(qualifiedName, "(", ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", "*", ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("::", qualifiedName, "(", ")");
            gb.Prod("FunctionCall").Is("::", qualifiedName, "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", kw("DISTINCT"), functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", kw("ALL"), functionArgumentList, ")");
            // FREETEXTTABLE/CONTAINSTABLE with wildcard column: func(table, *, search, ...)
            gb.Prod("FunctionCall").Is(qualifiedName, "(", functionArgumentList, ",", "*", ",", functionArgumentList, ")");
            // XML method calls: @variable.nodes(xpath), column_ref.value(xpath, type)
            gb.Prod("FunctionCall").Is(variableReference, ".", identifierTerm, "(", ")");
            gb.Prod("FunctionCall").Is(variableReference, ".", identifierTerm, "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(kw("LEFT"), "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(kw("RIGHT"), "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(kw("COALESCE"), "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(kw("NULLIF"), "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(kw("IIF"), "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(kw("UPDATE"), "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(kw("NEXT"), identifierTerm, kw("FOR"), qualifiedName);
            // OPENROWSET(BULK ...) special form
            gb.Prod("FunctionCall").Is(kw("OPENROWSET"), "(", openRowsetBulk, ")");
            gb.Prod("OpenRowsetBulk").Is(kw("BULK"), expression, ",", identifierTerm);  // BULK 'file', SINGLE_BLOB
            gb.Prod("OpenRowsetBulk").Is(kw("BULK"), expression, ",", openRowsetBulkOptionList);
            gb.Prod("OpenRowsetBulkOptionList").Is(openRowsetBulkOption);
            gb.Prod("OpenRowsetBulkOptionList").Is(openRowsetBulkOptionList, ",", openRowsetBulkOption);
            gb.Prod("OpenRowsetBulkOption").Is(identifierTerm);
            gb.Prod("OpenRowsetBulkOption").Is(identifierTerm, "=", expression);
            gb.Prod("FunctionArgumentList").Is(expression);
            gb.Prod("FunctionArgumentList").Is(functionArgumentList, ",", expression);
            gb.Prod("GraphWithinGroupClause").Is(kw("WITHIN"), kw("GROUP"), "(", kw("GRAPH"), kw("PATH"), ")");

            gb.Prod("OverClause").Is(kw("OVER"), "(", overSpec, ")");
            gb.Prod("OverSpec").Is(EmptyTerm.Empty);
            gb.Prod("OverSpec").Is(kw("ORDER"), kw("BY"), orderItemList);
            gb.Prod("OverSpec").Is(kw("ORDER"), kw("BY"), orderItemList, kw("ROWS"), frameClause);
            gb.Prod("OverSpec").Is(kw("ORDER"), kw("BY"), orderItemList, kw("RANGE"), frameClause);
            gb.Prod("OverSpec").Is(kw("PARTITION"), kw("BY"), expressionList);
            gb.Prod("OverSpec").Is(kw("PARTITION"), kw("BY"), expressionList, kw("ORDER"), kw("BY"), orderItemList);
            gb.Prod("OverSpec").Is(kw("PARTITION"), kw("BY"), expressionList, kw("ORDER"), kw("BY"), orderItemList, kw("ROWS"), frameClause);
            gb.Prod("OverSpec").Is(kw("PARTITION"), kw("BY"), expressionList, kw("ORDER"), kw("BY"), orderItemList, kw("RANGE"), frameClause);

            gb.Prod("FrameClause").Is(frameBoundary);
            gb.Prod("FrameClause").Is(kw("BETWEEN"), frameBoundary, kw("AND"), frameBoundary);

            gb.Prod("FrameBoundary").Is(kw("UNBOUNDED"), kw("PRECEDING"));
            gb.Prod("FrameBoundary").Is(kw("UNBOUNDED"), kw("FOLLOWING"));
            gb.Prod("FrameBoundary").Is(kw("CURRENT"), kw("ROW"));
            gb.Prod("FrameBoundary").Is(number, kw("PRECEDING"));
            gb.Prod("FrameBoundary").Is(number, kw("FOLLOWING"));

            gb.Prod("Literal").Is(number);
            gb.Prod("Literal").Is(stringLiteral);
            gb.Prod("Literal").Is(kw("NULL"));
            gb.Prod("UnicodeStringLiteral").Is(kw("N"), stringLiteral);
            gb.Prod("VariableReference").Is(variable);

            gb.Prod("CaseExpression").Is(kw("CASE"), caseWhenList, kw("END"));
            gb.Prod("CaseExpression").Is(kw("CASE"), caseWhenList, kw("ELSE"), expression, kw("END"));
            gb.Prod("CaseExpression").Is(kw("CASE"), expression, caseWhenList, kw("END"));
            gb.Prod("CaseExpression").Is(kw("CASE"), expression, caseWhenList, kw("ELSE"), expression, kw("END"));
            gb.Prod("CaseWhenList").Is(caseWhen);
            gb.Prod("CaseWhenList").Is(caseWhenList, caseWhen);
            gb.Prod("CaseWhen").Is(kw("WHEN"), expression, kw("THEN"), expression);

            gb.Prod("ExpressionList").Is(expression);
            gb.Prod("ExpressionList").Is(expressionList, ",", expression);
            gb.Prod("IdentifierList").Is(identifierTerm);
            gb.Prod("IdentifierList").Is(identifierList, ",", identifierTerm);

            gb.Prod("IdentifierTerm").Is(identifier);
            gb.Prod("IdentifierTerm").Is(bracketIdentifier);
            gb.Prod("IdentifierTerm").Is(quotedIdentifier);
            gb.Prod("IdentifierTerm").Is(tempIdentifier);
            gb.Prod("IdentifierTerm").Is(sqlcmdVariable);
            // contextual keywords used as identifiers in SQL Server
            gb.Prod("IdentifierTerm").Is(kw("TYPE"));
            gb.Prod("IdentifierTerm").Is(kw("OPENQUERY"));
            gb.Prod("IdentifierTerm").Is(kw("OPENROWSET"));
            gb.Prod("IdentifierTerm").Is(kw("BINARY"));
            gb.Prod("IdentifierTerm").Is(kw("XML"));
            gb.Prod("IdentifierTerm").Is(kw("JSON"));
            gb.Prod("IdentifierTerm").Is(kw("AUTO"));
            gb.Prod("IdentifierTerm").Is(kw("PATH"));
            gb.Prod("IdentifierTerm").Is(kw("SIZE"));
            gb.Prod("IdentifierTerm").Is(kw("STATISTICS"));
            gb.Prod("IdentifierTerm").Is(kw("AT"));
            gb.Prod("IdentifierTerm").Is(kw("NEXT"));
            gb.Prod("IdentifierTerm").Is(kw("ROWS"));
            gb.Prod("IdentifierTerm").Is(kw("OBJECT"));
            gb.Prod("IdentifierTerm").Is(kw("SCHEMA"));
            gb.Prod("IdentifierTerm").Is(kw("FUNCTION"));
            gb.Prod("IdentifierTerm").Is(kw("LOGIN"));
            gb.Prod("IdentifierTerm").Is(kw("DEFAULT"));
            gb.Prod("IdentifierTerm").Is(kw("PARTITION"));
            gb.Prod("IdentifierTerm").Is(kw("COLUMN"));
            gb.Prod("IdentifierTerm").Is(kw("CONSTRAINT"));
            gb.Prod("IdentifierTerm").Is(kw("HASH"));
            gb.Prod("IdentifierTerm").Is(kw("USER"));
            gb.Prod("IdentifierTerm").Is(kw("ROLE"));
            gb.Prod("IdentifierTerm").Is(kw("MERGE"));
            gb.Prod("IdentifierTerm").Is(kw("AFTER"));
            gb.Prod("IdentifierTerm").Is(kw("SERVER"));
            gb.Prod("IdentifierTerm").Is(kw("INSTEAD"));
            gb.Prod("IdentifierTerm").Is(kw("SCOPED"));
            gb.Prod("IdentifierTerm").Is(kw("CONFIGURATION"));
            gb.Prod("IdentifierTerm").Is(kw("CLEAR"));
            gb.Prod("IdentifierTerm").Is(kw("SCHEMABINDING"));
            gb.Prod("IdentifierTerm").Is(kw("CURRENT"));
            gb.Prod("IdentifierTerm").Is(kw("PARTITIONS"));
            gb.Prod("IdentifierTerm").Is(kw("LOOP"));
            gb.Prod("IdentifierTerm").Is(kw("EXTERNAL"));
            gb.Prod("IdentifierTerm").Is(kw("LOG"));
            gb.Prod("IdentifierTerm").Is(kw("PAGE"));
            gb.Prod("IdentifierTerm").Is(kw("N"));
            gb.Prod("IdentifierTerm").Is(kw("WAITFOR"));
            gb.Prod("IdentifierTerm").Is(kw("BULK"));
            gb.Prod("IdentifierTerm").Is(kw("CURSOR"));
            gb.Prod("IdentifierTerm").Is(kw("DELAY"));
            gb.Prod("IdentifierTerm").Is(kw("TIME"));
            gb.Prod("IdentifierTerm").Is(kw("LOGIN"));
            gb.Prod("IdentifierTerm").Is(kw("PASSWORD"));
            gb.Prod("IdentifierTerm").Is(kw("READ_ONLY"));
            gb.Prod("IdentifierTerm").Is(kw("ALL"));
            gb.Prod("IdentifierTerm").Is(kw("DATA_SOURCE"));
            gb.Prod("IdentifierTerm").Is(kw("SOURCE"));
            gb.Prod("IdentifierTerm").Is(kw("TARGET"));
            gb.Prod("IdentifierTerm").Is(kw("RESUME"));
            gb.Prod("IdentifierTerm").Is(kw("CLUSTERED"));
            gb.Prod("IdentifierTerm").Is(kw("NONCLUSTERED"));
            gb.Prod("IdentifierTerm").Is(kw("COLUMNSTORE"));
            gb.Prod("IdentifierTerm").Is(kw("INCLUDE"));
            gb.Prod("IdentifierTerm").Is(kw("MATCHED"));
            gb.Prod("IdentifierTerm").Is(kw("GOTO"));
            gb.Prod("IdentifierTerm").Is(kw("USER"));
            gb.Prod("IdentifierTerm").Is(kw("TYPE"));
            gb.Prod("IdentifierTerm").Is(kw("EXTERNAL"));
            gb.Prod("IdentifierTerm").Is(kw("ROWCOUNT"));
            gb.Prod("IdentifierTerm").Is(kw("PAGECOUNT"));
            gb.Prod("IdentifierTerm").Is(kw("MASTER"));
            gb.Prod("IdentifierTerm").Is(kw("EDGE"));
            gb.Prod("IdentifierTerm").Is(kw("NODE"));
            gb.Prod("IdentifierTerm").Is(kw("PREDICT"));
            gb.Prod("IdentifierTerm").Is(kw("MODEL"));
            gb.Prod("IdentifierTerm").Is(kw("NATIVE"));
            gb.Prod("IdentifierTerm").Is(kw("SCHEMABINDING"));
            gb.Prod("IdentifierTerm").Is(kw("DISTRIBUTED"));
            gb.Prod("IdentifierTerm").Is(kw("DATA"));
            gb.Prod("IdentifierTerm").Is(kw("SECURITY"));
            gb.Prod("IdentifierTerm").Is(kw("POLICY"));
            gb.Prod("IdentifierTerm").Is(kw("FILTER"));
            gb.Prod("IdentifierTerm").Is(kw("PREDICATE"));
            gb.Prod("IdentifierTerm").Is(kw("BLOCK"));
            gb.Prod("IdentifierTerm").Is(kw("PIVOT"));
            gb.Prod("IdentifierTerm").Is(kw("UNPIVOT"));
            gb.Prod("IdentifierTerm").Is(kw("LANGUAGE"));
            gb.Prod("IdentifierTerm").Is(kw("GRAPH"));

            gb.Prod("TruncateStatement").Is(kw("TRUNCATE"), kw("TABLE"), qualifiedName);

            gb.Prod("CreateTableAsSelectStatement").Is(kw("CREATE"), kw("TABLE"), qualifiedName, kw("AS"), queryExpression);
            gb.Prod("CreateTableAsSelectStatement").Is(kw("CREATE"), kw("TABLE"), qualifiedName, kw("WITH"), "(", createTableOptionList, ")", kw("AS"), queryExpression);

            gb.Prod("AlterDatabaseStatement").Is(kw("ALTER"), kw("DATABASE"), identifierTerm, kw("SET"), alterDatabaseSetOption);
            gb.Prod("AlterDatabaseStatement").Is(kw("ALTER"), kw("DATABASE"), kw("SCOPED"), kw("CONFIGURATION"), kw("CLEAR"), identifierTerm);
            gb.Prod("AlterDatabaseStatement").Is(kw("ALTER"), kw("DATABASE"), kw("SCOPED"), kw("CONFIGURATION"), kw("SET"), identifierTerm, "=", expression);
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, identifierTerm);
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, identifierTerm, identifierTerm);
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, identifierTerm, kw("WITH"), identifierTerm);
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "=", expression);
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "=", kw("ON"));
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "=", kw("OFF"));
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "=", kw("ON"), "(", indexOptionList, ")");
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "=", kw("OFF"), "(", indexOptionList, ")");
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "(", indexOptionList, ")");

            // DECLARE CURSOR
            gb.Prod("DeclareCursorStatement").Is(kw("DECLARE"), identifierTerm, kw("CURSOR"), kw("FOR"), queryExpression);
            gb.Prod("DeclareCursorStatement").Is(kw("DECLARE"), identifierTerm, kw("CURSOR"), cursorOptionList, kw("FOR"), queryExpression);
            gb.Prod("CursorOptionList").Is(identifierTerm);
            gb.Prod("CursorOptionList").Is(cursorOptionList, identifierTerm);

            // OPEN/FETCH/CLOSE cursor operations
            gb.Prod("CursorOperationStatement").Is(kw("OPEN"), identifierTerm);
            gb.Prod("CursorOperationStatement").Is(kw("CLOSE"), identifierTerm);
            gb.Prod("CursorOperationStatement").Is(kw("DEALLOCATE"), identifierTerm);
            gb.Prod("CursorOperationStatement").Is(fetchStatement);
            gb.Prod("FetchStatement").Is(kw("FETCH"), kw("FROM"), identifierTerm);
            gb.Prod("FetchStatement").Is(kw("FETCH"), kw("FROM"), identifierTerm, kw("INTO"), fetchTargetList);
            gb.Prod("FetchStatement").Is(kw("FETCH"), fetchDirection, kw("FROM"), identifierTerm);
            gb.Prod("FetchStatement").Is(kw("FETCH"), fetchDirection, kw("FROM"), identifierTerm, kw("INTO"), fetchTargetList);
            gb.Prod("FetchStatement").Is(kw("FETCH"), identifierTerm);
            gb.Prod("FetchStatement").Is(kw("FETCH"), identifierTerm, kw("INTO"), fetchTargetList);
            gb.Prod("FetchDirection").Is(kw("NEXT"));
            gb.Prod("FetchDirection").Is(kw("PRIOR"));
            gb.Prod("FetchDirection").Is(kw("FIRST"));
            gb.Prod("FetchDirection").Is(kw("LAST"));
            gb.Prod("FetchDirection").Is(kw("ABSOLUTE"), expression);
            gb.Prod("FetchDirection").Is(kw("RELATIVE"), expression);
            gb.Prod("FetchTargetList").Is(variableReference);
            gb.Prod("FetchTargetList").Is(fetchTargetList, ",", variableReference);

            // WAITFOR
            gb.Prod("WaitforStatement").Is(kw("WAITFOR"), identifierTerm, expression);
            gb.Prod("WaitforStatement").Is(kw("WAITFOR"), kw("DELAY"), expression);
            gb.Prod("WaitforStatement").Is(kw("WAITFOR"), kw("TIME"), expression);

            // CREATE LOGIN
            gb.Prod("CreateLoginStatement").Is(kw("CREATE"), kw("LOGIN"), identifierTerm, kw("WITH"), createLoginOptionList);
            gb.Prod("CreateLoginOptionList").Is(createLoginOption);
            gb.Prod("CreateLoginOptionList").Is(createLoginOptionList, ",", createLoginOption);
            gb.Prod("CreateLoginOption").Is(identifierTerm, "=", expression);
            gb.Prod("CreateLoginOption").Is(identifierTerm, "=", kw("ON"));
            gb.Prod("CreateLoginOption").Is(identifierTerm, "=", kw("OFF"));

            gb.Prod("CreateUserStatement").Is(kw("CREATE"), kw("USER"), identifierTerm, kw("FOR"), kw("LOGIN"), identifierTerm);
            gb.Prod("CreateUserStatement").Is(kw("CREATE"), kw("USER"), identifierTerm, kw("WITHOUT"), kw("LOGIN"));

            gb.Prod("CreateStatisticsStatement").Is(kw("CREATE"), kw("STATISTICS"), identifierTerm, kw("ON"), qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateStatisticsStatement").Is(kw("CREATE"), kw("STATISTICS"), identifierTerm, kw("ON"), qualifiedName, "(", identifierList, ")", kw("WITH"), "(", indexOptionList, ")");

            // UPDATE STATISTICS statement
            gb.Prod("UpdateStatisticsStatement").Is(kw("UPDATE"), kw("STATISTICS"), qualifiedName);
            gb.Prod("UpdateStatisticsStatement").Is(kw("UPDATE"), kw("STATISTICS"), qualifiedName, identifierTerm);
            gb.Prod("UpdateStatisticsStatement").Is(kw("UPDATE"), kw("STATISTICS"), qualifiedName, kw("WITH"), updateStatisticsOptionList);
            gb.Prod("UpdateStatisticsStatement").Is(kw("UPDATE"), kw("STATISTICS"), qualifiedName, identifierTerm, kw("WITH"), updateStatisticsOptionList);
            gb.Prod("UpdateStatisticsOptionList").Is(updateStatisticsOption);
            gb.Prod("UpdateStatisticsOptionList").Is(updateStatisticsOptionList, ",", updateStatisticsOption);
            gb.Prod("UpdateStatisticsOption").Is(identifierTerm);
            gb.Prod("UpdateStatisticsOption").Is(identifierTerm, "=", expression);
            gb.Prod("UpdateStatisticsOption").Is(kw("ROWCOUNT"), "=", expression);
            gb.Prod("UpdateStatisticsOption").Is(kw("PAGECOUNT"), "=", expression);

            gb.Prod("DropTypeStatement").Is(kw("DROP"), kw("TYPE"), qualifiedName);
            gb.Prod("DropTypeStatement").Is(kw("DROP"), kw("TYPE"), kw("IF"), kw("EXISTS"), qualifiedName);

            gb.Prod("DropColumnEncryptionKeyStatement").Is(kw("DROP"), kw("COLUMN"), kw("ENCRYPTION"), kw("KEY"), identifierTerm);
            gb.Prod("DropColumnEncryptionKeyStatement").Is(kw("DROP"), kw("COLUMN"), kw("MASTER"), kw("KEY"), identifierTerm);

            gb.Prod("RevertStatement").Is(kw("REVERT"));
            gb.Prod("RevertStatement").Is(kw("REVERT"), kw("WITH"), kw("COOKIE"), "=", expression);
            gb.Prod("IdentifierTerm").Is(kw("REVERT"));

            // DROP EVENT SESSION ON DATABASE/SERVER
            gb.Prod("DropEventSessionStatement").Is(kw("DROP"), kw("EVENT"), kw("SESSION"), identifierTerm, kw("ON"), kw("DATABASE"));
            gb.Prod("DropEventSessionStatement").Is(kw("DROP"), kw("EVENT"), kw("SESSION"), identifierTerm, kw("ON"), kw("SERVER"));
            gb.Prod("IdentifierTerm").Is(kw("EVENT"));
            gb.Prod("IdentifierTerm").Is(kw("SESSION"));

            // CREATE TYPE ... AS TABLE
            gb.Prod("CreateTypeStatement").Is(kw("CREATE"), kw("TYPE"), qualifiedName, kw("AS"), tableTypeDefinition);
            gb.Prod("CreateTypeStatement").Is(kw("CREATE"), kw("TYPE"), qualifiedName, kw("FROM"), typeSpec);

            // MERGE DML statement
            gb.Prod("MergeStatement").Is(kw("MERGE"), tableFactor, kw("USING"), tableFactor, kw("ON"), searchCondition, mergeWhenList);
            gb.Prod("MergeStatement").Is(kw("MERGE"), kw("INTO"), tableFactor, kw("USING"), tableFactor, kw("ON"), searchCondition, mergeWhenList);
            gb.Prod("MergeStatement").Is(kw("MERGE"), kw("TOP"), topValue, tableFactor, kw("USING"), tableFactor, kw("ON"), searchCondition, mergeWhenList);
            gb.Prod("MergeStatement").Is(kw("MERGE"), kw("TOP"), topValue, kw("INTO"), tableFactor, kw("USING"), tableFactor, kw("ON"), searchCondition, mergeWhenList);
            gb.Prod("MergeWhenList").Is(mergeWhen);
            gb.Prod("MergeWhenList").Is(mergeWhenList, mergeWhen);
            gb.Prod("MergeWhen").Is(kw("WHEN"), kw("MATCHED"), kw("THEN"), mergeMatchedAction);
            gb.Prod("MergeWhen").Is(kw("WHEN"), kw("MATCHED"), kw("AND"), searchCondition, kw("THEN"), mergeMatchedAction);
            gb.Prod("MergeWhen").Is(kw("WHEN"), kw("NOT"), kw("MATCHED"), kw("THEN"), mergeNotMatchedAction);
            gb.Prod("MergeWhen").Is(kw("WHEN"), kw("NOT"), kw("MATCHED"), kw("AND"), searchCondition, kw("THEN"), mergeNotMatchedAction);
            gb.Prod("MergeWhen").Is(kw("WHEN"), kw("NOT"), kw("MATCHED"), kw("BY"), kw("TARGET"), kw("THEN"), mergeNotMatchedAction);
            gb.Prod("MergeWhen").Is(kw("WHEN"), kw("NOT"), kw("MATCHED"), kw("BY"), kw("TARGET"), kw("AND"), searchCondition, kw("THEN"), mergeNotMatchedAction);
            gb.Prod("MergeWhen").Is(kw("WHEN"), kw("NOT"), kw("MATCHED"), kw("BY"), kw("SOURCE"), kw("THEN"), mergeMatchedAction);
            gb.Prod("MergeWhen").Is(kw("WHEN"), kw("NOT"), kw("MATCHED"), kw("BY"), kw("SOURCE"), kw("AND"), searchCondition, kw("THEN"), mergeMatchedAction);
            gb.Prod("MergeMatchedAction").Is(kw("UPDATE"), kw("SET"), updateSetList);
            gb.Prod("MergeMatchedAction").Is(kw("DELETE"));
            gb.Prod("MergeNotMatchedAction").Is(kw("INSERT"), "(", insertColumnList, ")", kw("VALUES"), "(", insertValueList, ")");
            gb.Prod("MergeNotMatchedAction").Is(kw("INSERT"), kw("VALUES"), "(", insertValueList, ")");
            gb.Prod("MergeNotMatchedAction").Is(kw("INSERT"), "(", insertColumnList, ")", kw("DEFAULT"), kw("VALUES"));
            gb.Prod("MergeNotMatchedAction").Is(kw("INSERT"), kw("DEFAULT"), kw("VALUES"));

            // BULK INSERT
            gb.Prod("BulkInsertStatement").Is(kw("BULK"), kw("INSERT"), qualifiedName, kw("FROM"), expression);
            gb.Prod("BulkInsertStatement").Is(kw("BULK"), kw("INSERT"), qualifiedName, kw("FROM"), expression, kw("WITH"), "(", tableHintLimitedList, ")");

            gb.Prod("CheckpointStatement").Is(kw("CHECKPOINT"));

            // SQL Graph: MATCH search condition (as PrimaryExpression so AND works after it)
            gb.Prod("PrimaryExpression").Is(kw("MATCH"), "(", matchGraphPattern, ")");
            gb.Prod("MatchGraphPattern").Is(matchGraphPath);
            gb.Prod("MatchGraphPattern").Is(kw("SHORTEST_PATH"), "(", matchGraphShortestPath, ")");
            gb.Prod("MatchGraphPattern").Is(matchGraphPattern, kw("AND"), matchGraphPath);
            gb.Prod("MatchGraphPattern").Is(matchGraphPattern, kw("AND"), kw("SHORTEST_PATH"), "(", matchGraphShortestPath, ")");
            gb.Prod("MatchGraphShortestPath").Is(matchGraphPath);
            gb.Prod("MatchGraphShortestPath").Is(matchGraphPath, "+");
            gb.Prod("MatchGraphShortestPath").Is(matchGraphPath, "{", number, ",", number, "}");
            // MatchGraphPath: identifierTerm  or  N1-(E)->N2  or  N1-(E)->N2-(E2)->N3 (chained)
            gb.Prod("MatchGraphPath").Is(identifierTerm);  // simple node ref
            gb.Prod("MatchGraphPath").Is(identifierTerm, "(", "-", "(", identifierTerm, ")", "-", ">", identifierTerm, ")");
            gb.Prod("MatchGraphPath").Is(matchGraphPath, "-", "(", identifierTerm, ")", "-", ">", identifierTerm); // forward: ...(E)->N
            gb.Prod("MatchGraphPath").Is(identifierTerm, "<", "-", "(", identifierTerm, ")", "-", identifierTerm); // backward N<-(E)-N
            gb.Prod("MatchGraphPath").Is(matchGraphPath, "<", "-", "(", identifierTerm, ")", "-", identifierTerm); // backward chain: path<-(E)-N
            gb.Prod("MatchGraphPath").Is(matchGraphPath, "-", "(", identifierTerm, ")", "-", identifierTerm); // undirected: ...(E)-N

            // PREDICT ML function
            gb.Prod("FunctionCall").Is(kw("PREDICT"), "(", predictArgList, ")");
            gb.Prod("PredictArgList").Is(predictArg);
            gb.Prod("PredictArgList").Is(predictArgList, ",", predictArg);
            gb.Prod("PredictArg").Is(identifierTerm, "=", expression);
            gb.Prod("PredictArg").Is(identifierTerm, "=", expression, kw("AS"), identifierTerm);

            gb.Prod("QualifiedName").Is(identifierTerm);
            gb.Prod("QualifiedName").Is(qualifiedName, ".", identifierTerm);
            gb.Prod("QualifiedName").Is(qualifiedName, ".", graphColumnRef);
            gb.Prod("QualifiedName").Is(qualifiedName, ".", ".", identifierTerm); // double-dot: master..table

            // CREATE/ALTER SECURITY POLICY
            gb.Prod("CreateSecurityPolicyStatement").Is(kw("CREATE"), kw("SECURITY"), kw("POLICY"), qualifiedName, securityPolicyClauseList);
            gb.Prod("CreateSecurityPolicyStatement").Is(kw("CREATE"), kw("SECURITY"), kw("POLICY"), qualifiedName, securityPolicyClauseList, kw("WITH"), "(", securityPolicyOptionList, ")");
            gb.Prod("AlterSecurityPolicyStatement").Is(kw("ALTER"), kw("SECURITY"), kw("POLICY"), qualifiedName, securityPolicyClauseList);
            gb.Prod("AlterSecurityPolicyStatement").Is(kw("ALTER"), kw("SECURITY"), kw("POLICY"), qualifiedName, securityPolicyClauseList, kw("WITH"), "(", securityPolicyOptionList, ")");
            gb.Prod("SecurityPolicyClauseList").Is(securityPolicyClause);
            gb.Prod("SecurityPolicyClauseList").Is(securityPolicyClauseList, ",", securityPolicyClause);
            gb.Prod("SecurityPolicyClause").Is(kw("ADD"), kw("FILTER"), kw("PREDICATE"), functionCall, kw("ON"), qualifiedName);
            gb.Prod("SecurityPolicyClause").Is(kw("ADD"), kw("BLOCK"), kw("PREDICATE"), functionCall, kw("ON"), qualifiedName);
            gb.Prod("SecurityPolicyClause").Is(kw("DROP"), kw("FILTER"), kw("PREDICATE"), kw("ON"), qualifiedName);
            gb.Prod("SecurityPolicyClause").Is(kw("DROP"), kw("BLOCK"), kw("PREDICATE"), kw("ON"), qualifiedName);
            gb.Prod("SecurityPolicyOptionList").Is(securityPolicyOption);
            gb.Prod("SecurityPolicyOptionList").Is(securityPolicyOptionList, ",", securityPolicyOption);
            gb.Prod("SecurityPolicyOption").Is(identifierTerm, "=", kw("ON"));
            gb.Prod("SecurityPolicyOption").Is(identifierTerm, "=", kw("OFF"));
            gb.Prod("SecurityPolicyOption").Is(identifierTerm, "=", expression);

            // CREATE EXTERNAL TABLE
            gb.Prod("CreateExternalTableStatement").Is(kw("CREATE"), kw("EXTERNAL"), kw("TABLE"), qualifiedName, "(", createTableElementList, ")");
            gb.Prod("CreateExternalTableStatement").Is(kw("CREATE"), kw("EXTERNAL"), kw("TABLE"), qualifiedName, "(", createTableElementList, ")", kw("WITH"), "(", tableHintLimitedList, ")");

            // CREATE EXTERNAL DATA SOURCE name WITH (TYPE=..., LOCATION=..., ...)
            gb.Prod("CreateExternalDataSourceStatement").Is(kw("CREATE"), kw("EXTERNAL"), kw("DATA"), kw("SOURCE"), identifierTerm, kw("WITH"), "(", indexOptionList, ")");
            gb.Prod("CreateExternalDataSourceStatement").Is(kw("CREATE"), kw("EXTERNAL"), kw("DATA"), kw("SOURCE"), qualifiedName, kw("WITH"), "(", indexOptionList, ")");

            return gb.BuildGrammar("Start");
        }
    }
}

