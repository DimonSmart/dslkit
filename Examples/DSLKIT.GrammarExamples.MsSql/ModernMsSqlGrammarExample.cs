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
                .WithKeywordPolicy(wholeWord: true, ignoreCase: true)
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
            var statementNoLeadingWith = gb.NT("StatementNoLeadingWith");
            var leadingWithStatement = gb.NT("LeadingWithStatement");
            var queryStatement = gb.NT("QueryStatement");
            var queryStatementNoLeadingWith = gb.NT("QueryStatementNoLeadingWith");
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
            var deleteStatementTailNoOutput = gb.NT("DeleteStatementTailNoOutput");
            var deleteStatementTailNoFrom = gb.NT("DeleteStatementTailNoFrom");
            var deleteOptionOpt = gb.NT("DeleteOptionOpt");
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
            var setTransactionIsolationLevel = gb.NT("SetTransactionIsolationLevel");
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
            var createFunctionSignatureParameterListOpt = gb.NT("CreateFunctionSignatureParameterListOpt");
            var createFunctionSignatureWithClauseOpt = gb.NT("CreateFunctionSignatureWithClauseOpt");
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
            var createProcSignatureParameterListOpt = gb.NT("CreateProcSignatureParameterListOpt");
            var createProcSignatureWithClauseOpt = gb.NT("CreateProcSignatureWithClauseOpt");
            var createProcSignatureForReplicationOpt = gb.NT("CreateProcSignatureForReplicationOpt");
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
            var queryPrimaryTail = gb.NT("QueryPrimaryTail");
            var queryPrimaryOrderByAndOffsetOpt = gb.NT("QueryPrimaryOrderByAndOffsetOpt");
            var queryPrimaryForOpt = gb.NT("QueryPrimaryForOpt");
            var queryPrimaryOptionOpt = gb.NT("QueryPrimaryOptionOpt");
            var parenQueryPrimaryOrderByAndOffsetOpt = gb.NT("ParenQueryPrimaryOrderByAndOffsetOpt");
            var querySpecification = gb.NT("QuerySpecification");
            var selectCore = gb.NT("SelectCore");
            var selectCorePrefix = gb.NT("SelectCorePrefix");
            var selectCoreTail = gb.NT("SelectCoreTail");
            var selectCoreIntoClause = gb.NT("SelectCoreIntoClause");
            var querySpecificationWhereClause = gb.NT("QuerySpecificationWhereClause");
            var querySpecificationGroupByClause = gb.NT("QuerySpecificationGroupByClause");
            var querySpecificationGroupByExpressionList = gb.NT("QuerySpecificationGroupByExpressionList");
            var querySpecificationGroupByGroupingSets = gb.NT("QuerySpecificationGroupByGroupingSets");
            var querySpecificationGroupByWithOpt = gb.NT("QuerySpecificationGroupByWithOpt");
            var querySpecificationHavingOpt = gb.NT("QuerySpecificationHavingOpt");
            var setQuantifier = gb.NT("SetQuantifier");
            var topClause = gb.NT("TopClause");
            var topClauseTail = gb.NT("TopClauseTail");
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
            var groupingSetList = gb.NT("GroupingSetList");
            var groupingSet = gb.NT("GroupingSet");
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
            var overPartitionClause = gb.NT("OverPartitionClause");
            var overOrderClause = gb.NT("OverOrderClause");
            var overFrameExtentOpt = gb.NT("OverFrameExtentOpt");
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

            gb.Rule("Start").CanBe(script);
            gb.Rule("Script").OneOf(
                statementList,
                gb.Seq(statementList, statementSeparatorList),
                gb.Seq(statementSeparatorList, statementList),
                gb.Seq(statementSeparatorList, statementList, statementSeparatorList),
                statementSeparatorList,
                EmptyTerm.Empty);
            gb.Rule("StatementList").OneOf(
                statement,
                gb.Seq(statementList, statementSeparatorList, statement),
                gb.Seq(statementList, statementNoLeadingWith));
            gb.Rule("StatementSeparatorList").Plus(statementSeparator);
            gb.Rule("StatementSeparator").OneOf(";", "GO", gb.Seq("GO", number)); // GO N (batch repeat)
            gb.Rule("Statement").OneOf(statementNoLeadingWith, leadingWithStatement);
            gb.Rule("StatementNoLeadingWith").OneOf(
                queryStatementNoLeadingWith,
                updateStatement,
                insertStatement,
                deleteStatement,
                ifStatement,
                beginEndStatement,
                whileStatement,
                setStatement,
                printStatement,
                declareStatement,
                returnStatement,
                transactionStatement,
                raiserrorStatement,
                throwStatement,
                loopControlStatement,
                gotoStatement,
                labelStatement,
                executeStatement,
                sqlcmdPreprocessorStatement,
                useStatement,
                createProcStatement,
                createFunctionStatement,
                grantStatement,
                dbccStatement,
                dropProcStatement,
                dropTableStatement,
                dropViewStatement,
                dropIndexStatement,
                dropStatisticsStatement,
                dropDatabaseStatement,
                createRoleStatement,
                createSchemaStatement,
                createViewStatement,
                createTableStatement,
                alterTableStatement,
                createIndexStatement,
                alterIndexStatement,
                createDatabaseStatement,
                createTriggerStatement,
                dropTriggerStatement,
                tryCatchStatement,
                truncateStatement,
                createTableAsSelectStatement,
                alterDatabaseStatement,
                declareCursorStatement,
                cursorOperationStatement,
                waitforStatement,
                createLoginStatement,
                bulkInsertStatement,
                checkpointStatement,
                createUserStatement,
                createStatisticsStatement,
                updateStatisticsStatement,
                dropTypeStatement,
                dropColumnEncryptionKeyStatement,
                revertStatement,
                dropEventSessionStatement,
                createTypeStatement,
                createSecurityPolicyStatement,
                alterSecurityPolicyStatement,
                createExternalTableStatement,
                createExternalDataSourceStatement,
                mergeStatement);
            gb.Rule("LeadingWithStatement").OneOf(
                gb.Seq(withClause, queryExpression),
                gb.Seq(withClause, queryExpression, optionClause),
                gb.Seq(withClause, updateStatement),
                gb.Seq(withClause, insertStatement),
                gb.Seq(withClause, deleteStatement),
                gb.Seq(withClause, mergeStatement),
                gb.Seq(withXmlNamespacesClause, updateStatement),
                gb.Seq(withXmlNamespacesClause, insertStatement),
                gb.Seq(withXmlNamespacesClause, deleteStatement),
                gb.Seq(withXmlNamespacesClause, queryStatement));

            gb.Rule("QueryStatement").OneOf(
                queryExpression,
                gb.Seq(withClause, queryExpression),
                gb.Seq(queryExpression, optionClause),
                gb.Seq(withClause, queryExpression, optionClause));
            gb.Rule("QueryStatementNoLeadingWith").OneOf(
                queryExpression,
                gb.Seq(queryExpression, optionClause));
            gb.Prod("UpdateStatement").Is("UPDATE", tableFactor, "SET", updateSetList);
            gb.Prod("UpdateStatement").Is("UPDATE", tableFactor, "SET", updateSetList, "WHERE", searchCondition);
            gb.Prod("UpdateStatement").Is("UPDATE", tableFactor, "SET", updateSetList, "FROM", tableSourceList);
            gb.Prod("UpdateStatement").Is("UPDATE", tableFactor, "SET", updateSetList, "FROM", tableSourceList, "WHERE", searchCondition);
            gb.Prod("UpdateSetList").Is(updateSetItem);
            gb.Prod("UpdateSetList").Is(updateSetList, ",", updateSetItem);
            gb.Prod("UpdateSetItem").Is(qualifiedName, "=", expression);
            gb.Prod("UpdateSetItem").Is(qualifiedName, compoundAssignOp, expression);
            gb.Prod("UpdateSetItem").Is(variableReference, "=", expression);
            gb.Prod("UpdateSetItem").Is(variableReference, compoundAssignOp, expression);
            gb.Prod("UpdateSetItem").Is(functionCall); // XML modify: col.modify(...)

            gb.Rule("CompoundAssignOp")
                .CanBe("+=")
                .Or("-=")
                .Or("*=")
                .Or("/=")
                .Or("%=")
                .Or("&=")
                .Or("|=")
                .Or("^=");

            gb.Prod("InsertStatement").Is("INSERT", insertTarget, "VALUES", rowValueList);
            gb.Prod("InsertStatement").Is("INSERT", insertTarget, executeStatement);
            gb.Prod("InsertStatement").Is("INSERT", insertTarget, queryExpression);
            gb.Prod("InsertStatement").Is("INSERT", insertTarget, deleteOutputClause, "VALUES", rowValueList);
            gb.Prod("InsertStatement").Is("INSERT", insertTarget, deleteOutputClause, queryExpression);
            gb.Prod("InsertTarget").Is("INTO", qualifiedName);
            gb.Prod("InsertTarget").Is(qualifiedName);
            gb.Prod("InsertTarget").Is("INTO", qualifiedName, "(", insertColumnList, ")");
            gb.Prod("InsertTarget").Is(qualifiedName, "(", insertColumnList, ")");
            gb.Prod("InsertTarget").Is("INTO", variableReference);
            gb.Prod("InsertTarget").Is(variableReference);
            gb.Prod("InsertTarget").Is("INTO", variableReference, "(", insertColumnList, ")");
            gb.Prod("InsertTarget").Is(variableReference, "(", insertColumnList, ")");
            gb.Prod("InsertTarget").Is("INTO", qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("InsertTarget").Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("InsertTarget").Is("INTO", qualifiedName, "(", insertColumnList, ")", "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("InsertTarget").Is("INTO", qualifiedName, "WITH", "(", tableHintLimitedList, ")", "(", insertColumnList, ")");
            gb.Prod("InsertTarget").Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")", "(", insertColumnList, ")");
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
                "DELETE",
                deleteTarget,
                deleteStatementTail);
            gb.Prod("DeleteStatement").Is(
                "DELETE",
                "FROM",
                deleteTarget,
                deleteStatementTail);
            gb.Prod("DeleteStatement").Is(
                "DELETE",
                deleteTopClause,
                deleteTarget,
                deleteStatementTail);
            gb.Prod("DeleteStatement").Is(
                "DELETE",
                deleteTopClause,
                "FROM",
                deleteTarget,
                deleteStatementTail);

            gb.Prod("DeleteTopClause").Is("TOP", "(", expression, ")");
            gb.Prod("DeleteTopClause").Is("TOP", "(", expression, ")", "PERCENT");

            gb.Prod("DeleteTarget").Is(deleteTargetSimple);
            gb.Prod("DeleteTarget").Is(deleteTargetRowset);

            gb.Prod("DeleteTargetSimple").Is(identifierTerm);
            gb.Prod("DeleteTargetSimple").Is(qualifiedName);
            gb.Prod("DeleteTargetSimple").Is(variableReference);
            gb.Prod("DeleteTargetSimple").Is(identifierTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("DeleteTargetSimple").Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");

            gb.Prod("DeleteTargetRowset").Is(rowsetFunctionLimited);
            gb.Prod("DeleteTargetRowset").Is(rowsetFunctionLimited, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("RowsetFunctionLimited").Is("OPENQUERY", "(", expressionList, ")");
            gb.Prod("RowsetFunctionLimited").Is("OPENROWSET", "(", expressionList, ")");

            gb.Prod("TableHintLimitedList").Is(tableHintLimited);
            gb.Prod("TableHintLimitedList").Is(tableHintLimitedList, ",", tableHintLimited);
            gb.Rule("TableHintLimited")
                .CanBe(tableHintLimitedName)
                .Or(tableHintLimitedName, "=", expression)
                .Or(tableHintLimitedName, "(", expressionList, ")")
                .Or(qualifiedName)
                .Or(qualifiedName, "=", expression)
                .Or(qualifiedName, "(", expressionList, ")");
            gb.Rule("TableHintLimitedName")
                .CanBe(identifierTerm)
                .OrKeywords("INDEX");

            gb.Prod("DeleteStatementTail").Is(deleteOutputClause, deleteStatementTailNoOutput);
            gb.Prod("DeleteStatementTail").Is(deleteStatementTailNoOutput);
            gb.Prod("DeleteStatementTailNoOutput").Is(deleteSourceFromClause, deleteStatementTailNoFrom);
            gb.Prod("DeleteStatementTailNoOutput").Is(deleteStatementTailNoFrom);
            gb.Prod("DeleteStatementTailNoFrom").Is(deleteWhereClause, deleteOptionOpt);
            gb.Prod("DeleteStatementTailNoFrom").Is(deleteOptionOpt);
            gb.Opt(deleteOptionOpt, deleteOptionClause);

            gb.Prod("DeleteOutputClause").Is("OUTPUT", selectItemList);
            gb.Prod("DeleteOutputClause").Is("OUTPUT", selectItemList, "INTO", deleteOutputTarget, deleteOutputIntoColumnListOpt);
            gb.Prod("DeleteOutputTarget").Is(qualifiedName);
            gb.Prod("DeleteOutputTarget").Is(variableReference);
            gb.Opt(deleteOutputIntoColumnListOpt, "(", identifierList, ")");

            gb.Prod("DeleteSourceFromClause").Is("FROM", tableSourceList);

            gb.Rule("DeleteWhereClause")
                .CanBe("WHERE", searchCondition)
                .Or("WHERE", "CURRENT", "OF", identifierTerm)
                .Or("WHERE", "CURRENT", "OF", variableReference)
                .Or("WHERE", "CURRENT", "OF", "GLOBAL", identifierTerm)
                .Or("WHERE", "CURRENT", "OF", "GLOBAL", variableReference);

            gb.Prod("DeleteOptionClause").Is("OPTION", "(", deleteQueryHintList, ")");
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
            gb.Rule(deleteQueryHintName)
                .CanBe(identifierTerm)
                .OrKeywords("RECOMPILE", "MAXDOP", "USE", "JOIN", "ORDER");

            gb.Prod("OptionClause").Is("OPTION", "(", deleteQueryHintList, ")");

            gb.Prod("IfBranchStatement").Is(statement);
            gb.Prod("IfBranchStatement").Is(statement, ";");
            gb.Prod("IfStatement").Is("IF", searchCondition, ifBranchStatement);
            gb.Prod("IfStatement").Is("IF", searchCondition, ifBranchStatement, "ELSE", ifBranchStatement);
            gb.Prod("IfStatement").Is("IF", searchCondition, ifBranchStatement, "ELSE", ifStatement);
            gb.Prod("BeginEndStatement").Is("BEGIN", statementList, "END");
            gb.Prod("BeginEndStatement").Is("BEGIN", statementList, statementSeparatorList, "END");
            gb.Prod("WhileStatement").Is("WHILE", searchCondition, statement);

            gb.Prod("SetStatement").Is("SET", variableReference, "=", expression);
            gb.Prod("SetStatement").Is("SET", variableReference, compoundAssignOp, expression);
            gb.Prod("SetStatement").Is("SET", identifierTerm, "ON");
            gb.Prod("SetStatement").Is("SET", identifierTerm, "OFF");
            gb.Prod("SetStatement").Is("SET", identifierTerm, "=", expression);
            gb.Prod("SetStatement").Is("SET", identifierTerm, expression);
            gb.Prod("SetStatement").Is("SET", identifierTerm, identifierTerm);
            gb.Prod("SetStatement").Is("SET", identifierTerm, identifierTerm, "ON");
            gb.Prod("SetStatement").Is("SET", identifierTerm, identifierTerm, "OFF");
            gb.Prod("SetStatement").Is("SET", "IDENTITY_INSERT", qualifiedName, "ON");
            gb.Prod("SetStatement").Is("SET", "IDENTITY_INSERT", qualifiedName, "OFF");
            gb.Prod("SetStatement").Is("SET", "TRANSACTION", identifierTerm, identifierTerm, setTransactionIsolationLevel);

            gb.Prod("SetTransactionIsolationLevel").Is(identifierTerm);
            gb.Prod("SetTransactionIsolationLevel").Is(identifierTerm, identifierTerm);

            gb.Prod("PrintStatement").Is("PRINT", expression);

            gb.Rule(returnStatement)
                .CanBe("RETURN")
                .Or("RETURN", expression);

            gb.Rule(transactionStatement)
                .CanBe("BEGIN", "TRAN")
                .Or("BEGIN", "TRANSACTION")
                .Or("BEGIN", "TRAN", identifierTerm)
                .Or("BEGIN", "TRANSACTION", identifierTerm)
                .Or("COMMIT")
                .Or("COMMIT", "TRAN")
                .Or("COMMIT", "TRANSACTION")
                .Or("ROLLBACK")
                .Or("ROLLBACK", "TRAN")
                .Or("ROLLBACK", "TRANSACTION");

            gb.Prod("RaiserrorStatement").Is("RAISERROR", "(", raiserrorArgList, ")");
            gb.Prod("RaiserrorStatement").Is("RAISERROR", "(", raiserrorArgList, ")", "WITH", raiserrorWithOptionList);
            gb.Prod("RaiserrorArgList").Is(expression);
            gb.Prod("RaiserrorArgList").Is(raiserrorArgList, ",", expression);
            gb.Prod("RaiserrorWithOptionList").Is(identifierTerm);
            gb.Prod("RaiserrorWithOptionList").Is(raiserrorWithOptionList, ",", identifierTerm);

            gb.Rule(throwStatement)
                .CanBe("THROW")
                .Or("THROW", expression, ",", expression, ",", expression);
            gb.Rule(loopControlStatement)
                .CanBe("BREAK")
                .OrKeywords("CONTINUE");
            gb.Prod("GotoStatement").Is("GOTO", identifierTerm);
            gb.Prod("LabelStatement").Is(identifierTerm, ":");
            gb.Prod("LabelStatement").Is(identifierTerm, ":", statement);
            gb.Prod("SqlcmdPreprocessorStatement").Is(sqlcmdPreprocessorCommand);

            gb.Prod("DeclareStatement").Is("DECLARE", declareItemList);
            gb.Prod("DeclareStatement").Is("DECLARE", declareTableVariable);
            gb.Prod("DeclareItemList").Is(declareItem);
            gb.Prod("DeclareItemList").Is(declareItemList, ",", declareItem);
            gb.Prod("DeclareItem").Is(variableReference, typeSpec);
            gb.Prod("DeclareItem").Is(variableReference, typeSpec, "=", expression);
            gb.Prod("DeclareItem").Is(variableReference, typeSpec, "NOT", "NULL");
            gb.Prod("DeclareItem").Is(variableReference, typeSpec, "NOT", "NULL", "=", expression);
            gb.Prod("DeclareItem").Is(variableReference, typeSpec, "NULL");
            gb.Prod("DeclareItem").Is(variableReference, typeSpec, "NULL", "=", expression);
            gb.Prod("DeclareItem").Is(variableReference, "AS", typeSpec);
            gb.Prod("DeclareItem").Is(variableReference, "AS", typeSpec, "=", expression);
            gb.Prod("DeclareItem").Is(variableReference, "AS", typeSpec, "NOT", "NULL");
            gb.Prod("DeclareItem").Is(variableReference, "AS", typeSpec, "NOT", "NULL", "=", expression);
            gb.Prod("DeclareTableVariable").Is(variableReference, tableTypeDefinition);
            gb.Prod("DeclareTableVariable").Is(variableReference, "AS", tableTypeDefinition);
            gb.Prod("TableTypeDefinition").Is("TABLE", "(", createTableElementList, ")");
            gb.Prod("TableTypeDefinition").Is("TABLE", "(", createTableElementList, ")", createTableOptions);
            gb.Prod("TypeSpec").Is(qualifiedName);
            gb.Prod("TypeSpec").Is(qualifiedName, "(", expression, ")");
            gb.Prod("TypeSpec").Is(qualifiedName, "(", expression, ",", expression, ")");

            gb.Rule("ExecuteStatement")
                .CanBe("EXEC", executeModuleCall)
                .Or("EXECUTE", executeModuleCall)
                .Or("EXEC", executeDynamicCall)
                .Or("EXECUTE", executeDynamicCall)
                .Or("EXEC", executeAsContext)
                .Or("EXECUTE", executeAsContext);

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
            gb.Prod("ExecuteArgValue").Is(variableReference, "OUTPUT");
            gb.Prod("ExecuteArgValue").Is(variableReference, "OUT");
            gb.Prod("ExecuteArgValue").Is("DEFAULT");

            gb.Prod("ExecuteWithOptions").Is("WITH", executeOptionList);
            gb.Prod("ExecuteOptionList").Is(executeOption);
            gb.Prod("ExecuteOptionList").Is(executeOptionList, ",", executeOption);
            gb.Rule("ExecuteOption")
                .CanBe("RECOMPILE")
                .Or("RESULT", "SETS", "UNDEFINED")
                .Or("RESULT", "SETS", "NONE")
                .Or("RESULT", "SETS", "(", executeResultSetsDefList, ")");
            gb.Prod("ExecuteResultSetsDefList").Is(executeResultSetsDef);
            gb.Prod("ExecuteResultSetsDefList").Is(executeResultSetsDefList, ",", executeResultSetsDef);
            gb.Prod("ExecuteResultSetsDef").Is("(", executeColumnDefList, ")");
            gb.Prod("ExecuteResultSetsDef").Is("AS", "OBJECT", qualifiedName);
            gb.Prod("ExecuteResultSetsDef").Is("AS", "TYPE", qualifiedName);
            gb.Prod("ExecuteResultSetsDef").Is("AS", "FOR", "XML");
            gb.Prod("ExecuteColumnDefList").Is(executeColumnDef);
            gb.Prod("ExecuteColumnDefList").Is(executeColumnDefList, ",", executeColumnDef);
            gb.Prod("ExecuteColumnDef").Is(identifierTerm, typeSpec);
            gb.Prod("ExecuteColumnDef").Is(identifierTerm, typeSpec, "COLLATE", identifierTerm);
            gb.Prod("ExecuteColumnDef").Is(identifierTerm, typeSpec, executeNullability);
            gb.Prod("ExecuteColumnDef").Is(identifierTerm, typeSpec, "COLLATE", identifierTerm, executeNullability);
            gb.Rule("ExecuteNullability")
                .CanBe("NULL")
                .Or("NOT", "NULL");

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
            gb.Prod("ExecuteLinkedArg").Is(variableReference, "OUTPUT");
            gb.Prod("ExecuteLinkedArg").Is(variableReference, "OUT");
            gb.Rule("ExecuteAsContext")
                .CanBe("AS", "LOGIN", "=", stringLiteral)
                .Or("AS", "USER", "=", stringLiteral)
                .Or("AS", "LOGIN", "=", unicodeStringLiteral)
                .Or("AS", "USER", "=", unicodeStringLiteral)
                .Or("AS", "LOGIN", "=", identifierTerm)
                .Or("AS", "USER", "=", identifierTerm);
            gb.Prod("ExecuteAtClause").Is("AT", identifierTerm);
            gb.Prod("ExecuteAtClause").Is("AT", "DATA_SOURCE", identifierTerm);

            gb.Prod("UseStatement").Is("USE", identifierTerm);

            gb.Prod("CreateProcKeyword").Is("PROC");
            gb.Prod("CreateProcKeyword").Is("PROCEDURE");

            gb.Prod("CreateProcHead").Is("CREATE", createProcKeyword);
            gb.Prod("CreateProcHead").Is("CREATE", "OR", "ALTER", createProcKeyword);
            gb.Prod("CreateProcHead").Is("ALTER", createProcKeyword);

            gb.Prod("CreateProcName").Is(qualifiedName);
            gb.Prod("CreateProcName").Is(qualifiedName, ";", number);

            gb.Opt(createProcSignatureParameterListOpt, createProcParameterList);
            gb.Opt(createProcSignatureWithClauseOpt, createProcWithClause);
            gb.Opt(createProcSignatureForReplicationOpt, createProcForReplicationClause);
            gb.Rule("CreateProcSignature").OneOf(
                gb.Seq(createProcName, createProcSignatureParameterListOpt, createProcSignatureWithClauseOpt, createProcSignatureForReplicationOpt),
                gb.Seq(createProcName, "(", createProcSignatureParameterListOpt, ")", createProcSignatureWithClauseOpt, createProcSignatureForReplicationOpt));

            gb.Prod("CreateProcParameterList").Is(createProcParameter);
            gb.Prod("CreateProcParameterList").Is(createProcParameterList, ",", createProcParameter);
            gb.Prod("CreateProcParameter").Is(variableReference, typeSpec);
            gb.Prod("CreateProcParameter").Is(variableReference, typeSpec, createProcParameterOptionList);
            gb.Prod("CreateProcParameter").Is(variableReference, "AS", typeSpec);
            gb.Prod("CreateProcParameter").Is(variableReference, "AS", typeSpec, createProcParameterOptionList);
            gb.Prod("CreateProcParameterOptionList").Is(createProcParameterOption);
            gb.Prod("CreateProcParameterOptionList").Is(createProcParameterOptionList, createProcParameterOption);
            gb.Rule("CreateProcParameterOption")
                .CanBe("VARYING")
                .Or("NULL")
                .Or("NOT", "NULL")
                .Or("=", expression)
                .Or("OUT")
                .Or("OUTPUT")
                .Or("READONLY");

            gb.Prod("CreateProcWithClause").Is("WITH", createProcOptionList);
            gb.Prod("CreateProcOptionList").Is(createProcOption);
            gb.Prod("CreateProcOptionList").Is(createProcOptionList, ",", createProcOption);
            gb.Rule("CreateProcOption")
                .CanBe(createProcExecuteAsClause)
                .OrKeywords("ENCRYPTION", "RECOMPILE", "NATIVE_COMPILATION", "SCHEMABINDING");

            gb.Rule("CreateProcExecuteAsClause")
                .CanBe("EXECUTE", "AS", "CALLER")
                .Or("EXECUTE", "AS", "SELF")
                .Or("EXECUTE", "AS", "OWNER")
                .Or("EXECUTE", "AS", stringLiteral)
                .Or("EXECUTE", "AS", unicodeStringLiteral)
                .Or("EXECUTE", "AS", identifierTerm);

            gb.Prod("CreateProcForReplicationClause").Is("FOR", "REPLICATION");

            gb.Prod("CreateProcBody").Is("AS", createProcBodyBlock);
            gb.Prod("CreateProcBody").Is("AS", "EXTERNAL", identifierTerm, createProcExternalName);
            gb.Prod("CreateProcBody").Is(createProcNativeWithClause, "AS", "BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, "END");
            gb.Prod("CreateProcBody").Is(createProcNativeWithClause, "AS", "BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, statementSeparatorList, "END");

            gb.Prod("CreateProcNativeWithClause").Is("WITH", "NATIVE_COMPILATION", ",", "SCHEMABINDING");
            gb.Prod("CreateProcNativeWithClause").Is("WITH", "NATIVE_COMPILATION", ",", "SCHEMABINDING", ",", createProcExecuteAsClause);

            gb.Prod("CreateProcNativeAtomicOptionList").Is(createProcNativeAtomicOption);
            gb.Prod("CreateProcNativeAtomicOptionList").Is(createProcNativeAtomicOptionList, ",", createProcNativeAtomicOption);
            gb.Prod("CreateProcNativeAtomicOption").Is(identifierTerm, "=", expression);
            gb.Prod("CreateProcNativeAtomicOption").Is(qualifiedName, "=", expression);
            gb.Prod("CreateProcNativeAtomicOption").Is(identifierTerm, identifierTerm, "=", expression);
            gb.Prod("CreateProcNativeAtomicOption").Is(identifierTerm, identifierTerm, identifierTerm, "=", expression);
            gb.Prod("CreateProcNativeAtomicOption").Is("TRANSACTION", identifierTerm, identifierTerm, "=", expression);

            gb.Prod("CreateProcExternalName").Is(qualifiedName);

            gb.Prod("CreateProcBodyBlock").Is(statementList);
            gb.Prod("CreateProcBodyBlock").Is(statementList, statementSeparatorList);
            gb.Prod("CreateProcBodyBlock").Is(statementSeparatorList, statementList);
            gb.Prod("CreateProcBodyBlock").Is(statementSeparatorList, statementList, statementSeparatorList);
            gb.Prod("CreateProcBodyBlock").Is("BEGIN", statementList, "END");
            gb.Prod("CreateProcBodyBlock").Is("BEGIN", statementList, statementSeparatorList, "END");
            gb.Prod("CreateProcBodyBlock").Is("BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, "END");
            gb.Prod("CreateProcBodyBlock").Is("BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, statementSeparatorList, "END");

            gb.Prod("CreateProcStatement").Is(createProcHead, createProcSignature, createProcBody);

            gb.Prod("CreateFunctionHead").Is("CREATE", "FUNCTION");
            gb.Prod("CreateFunctionHead").Is("CREATE", "OR", "ALTER", "FUNCTION");
            gb.Prod("CreateFunctionHead").Is("ALTER", "FUNCTION");
            gb.Prod("CreateFunctionName").Is(qualifiedName);

            gb.Opt(createFunctionSignatureParameterListOpt, createFunctionParameterList);
            gb.Opt(createFunctionSignatureWithClauseOpt, createFunctionWithClause);
            gb.Prod("CreateFunctionSignature").Is(
                createFunctionName,
                "(",
                createFunctionSignatureParameterListOpt,
                ")",
                createFunctionReturnsClause,
                createFunctionSignatureWithClauseOpt);

            gb.Prod("CreateFunctionParameterList").Is(createFunctionParameter);
            gb.Prod("CreateFunctionParameterList").Is(createFunctionParameterList, ",", createFunctionParameter);
            gb.Prod("CreateFunctionParameter").Is(variableReference, typeSpec);
            gb.Prod("CreateFunctionParameter").Is(variableReference, "AS", typeSpec);
            gb.Prod("CreateFunctionParameter").Is(variableReference, typeSpec, createFunctionParameterOptionList);
            gb.Prod("CreateFunctionParameter").Is(variableReference, "AS", typeSpec, createFunctionParameterOptionList);
            gb.Prod("CreateFunctionParameterOptionList").Is(createFunctionParameterOption);
            gb.Prod("CreateFunctionParameterOptionList").Is(createFunctionParameterOptionList, createFunctionParameterOption);
            gb.Rule("CreateFunctionParameterOption")
                .CanBe("NULL")
                .Or("NOT", "NULL")
                .Or("=", expression)
                .Or("READONLY");

            gb.Prod("CreateFunctionReturnsClause").Is("RETURNS", typeSpec);
            gb.Prod("CreateFunctionReturnsClause").Is("RETURNS", "TABLE");
            gb.Prod("CreateFunctionReturnsClause").Is("RETURNS", createFunctionTableReturnDefinition);
            gb.Prod("CreateFunctionTableReturnDefinition").Is(variableReference, "TABLE", "(", createFunctionTableReturnItemList, ")");
            gb.Prod("CreateFunctionTableReturnItemList").Is(createFunctionTableReturnItem);
            gb.Prod("CreateFunctionTableReturnItemList").Is(createFunctionTableReturnItemList, ",", createFunctionTableReturnItem);
            gb.Prod("CreateFunctionTableReturnItem").Is(createTableColumnDefinition);
            gb.Prod("CreateFunctionTableReturnItem").Is(createTableComputedColumn);
            gb.Prod("CreateFunctionTableReturnItem").Is(createTableConstraint);
            gb.Prod("CreateFunctionTableReturnItem").Is(createTableTableIndex);

            gb.Prod("CreateFunctionWithClause").Is("WITH", createFunctionOptionList);
            gb.Prod("CreateFunctionOptionList").Is(createFunctionOption);
            gb.Prod("CreateFunctionOptionList").Is(createFunctionOptionList, ",", createFunctionOption);
            gb.Rule("CreateFunctionOption")
                .CanBe(createProcExecuteAsClause)
                .OrKeywords("ENCRYPTION", "SCHEMABINDING")
                .Or("RETURNS", "NULL", "ON", "NULL", "INPUT")
                .Or("CALLED", "ON", "NULL", "INPUT")
                .Or("INLINE", "=", "ON")
                .Or("INLINE", "=", "OFF");

            gb.Prod("CreateFunctionBody").Is("AS", "RETURN", queryExpression);
            gb.Prod("CreateFunctionBody").Is("AS", "RETURN", "(", queryExpression, ")");
            gb.Prod("CreateFunctionBody").Is("AS", "RETURN", "(", withClause, queryExpression, ")");
            gb.Prod("CreateFunctionBody").Is("RETURN", queryExpression);
            gb.Prod("CreateFunctionBody").Is("RETURN", "(", queryExpression, ")");
            gb.Prod("CreateFunctionBody").Is("RETURN", "(", withClause, queryExpression, ")");
            gb.Prod("CreateFunctionBody").Is("AS", "BEGIN", statementList, "END");
            gb.Prod("CreateFunctionBody").Is("AS", "BEGIN", statementList, statementSeparatorList, "END");
            gb.Prod("CreateFunctionBody").Is("BEGIN", statementList, "END");
            gb.Prod("CreateFunctionBody").Is("BEGIN", statementList, statementSeparatorList, "END");

            gb.Prod("CreateFunctionStatement").Is(createFunctionHead, createFunctionSignature, createFunctionBody);

            gb.Prod("GrantPermissionSet").Is("ALL");
            gb.Prod("GrantPermissionSet").Is("ALL", "PRIVILEGES");
            gb.Prod("GrantPermissionSet").Is(grantPermissionList);
            gb.Prod("GrantPermissionList").Is(grantPermissionItem);
            gb.Prod("GrantPermissionList").Is(grantPermissionList, ",", grantPermissionItem);
            gb.Prod("GrantPermissionItem").Is(grantPermission);
            gb.Prod("GrantPermissionItem").Is(grantPermission, "(", identifierList, ")");

            gb.Prod("GrantPermission").Is(grantPermissionWord);
            gb.Prod("GrantPermission").Is(grantPermissionWord, grantPermissionWord);
            gb.Prod("GrantPermission").Is(grantPermissionWord, grantPermissionWord, grantPermissionWord);
            gb.Prod("GrantPermission").Is(grantPermissionWord, grantPermissionWord, grantPermissionWord, grantPermissionWord);

            gb.Rule(grantPermissionWord)
                .CanBe(identifierTerm)
                .OrKeywords(
                    "SELECT",
                    "INSERT",
                    "UPDATE",
                    "DELETE",
                    "EXECUTE",
                    "REFERENCES",
                    "CONNECT",
                    "ALTER",
                    "CONTROL",
                    "VIEW",
                    "DEFINITION",
                    "TAKE",
                    "OWNERSHIP",
                    "IMPERSONATE",
                    "RECEIVE",
                    "SEND",
                    "CREATE",
                    "ANY",
                    "SCHEMA",
                    "DATABASE",
                    "OBJECT",
                    "ROLE",
                    "LOGIN",
                    "USER");

            gb.Prod("GrantOnClause").Is("ON", grantSecurable);
            gb.Prod("GrantOnClause").Is("ON", grantClassType, "::", grantSecurable);
            gb.Rule(grantClassType).Keywords("LOGIN", "DATABASE", "OBJECT", "ROLE", "SCHEMA", "USER");
            gb.Prod("GrantSecurable").Is(qualifiedName);
            gb.Prod("GrantSecurable").Is(identifierTerm);

            gb.Prod("GrantPrincipalList").Is(grantPrincipal);
            gb.Prod("GrantPrincipalList").Is(grantPrincipalList, ",", grantPrincipal);
            gb.Prod("GrantPrincipal").Is(identifierTerm);
            gb.Prod("GrantPrincipal").Is(qualifiedName);
            gb.Prod("GrantPrincipal").Is("PUBLIC");

            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, "TO", grantPrincipalList);
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList);
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION");
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION");
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, "TO", grantPrincipalList, "AS", grantPrincipal);
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList, "AS", grantPrincipal);
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION", "AS", grantPrincipal);
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION", "AS", grantPrincipal);

            gb.Prod("DbccCommand").Is(identifierTerm);
            gb.Prod("DbccCommand").Is(qualifiedName);
            gb.Prod("DbccStatement").Is("DBCC", dbccCommand);
            gb.Prod("DbccStatement").Is("DBCC", dbccCommand, "(", dbccParamList, ")");
            gb.Prod("DbccStatement").Is("DBCC", dbccCommand, "WITH", dbccOptionList);
            gb.Prod("DbccStatement").Is("DBCC", dbccCommand, "(", dbccParamList, ")", "WITH", dbccOptionList);
            gb.Prod("DbccParamList").Is(dbccParam);
            gb.Prod("DbccParamList").Is(dbccParamList, ",", dbccParam);
            gb.Prod("DbccParam").Is(expression);
            gb.Prod("DbccParam").Is(identifierTerm);
            gb.Prod("DbccParam").Is(qualifiedName);
            gb.Prod("DbccOptionList").Is(dbccOption);
            gb.Prod("DbccOptionList").Is(dbccOptionList, ",", dbccOption);
            gb.Prod("DbccOption").Is(dbccOptionName);
            gb.Prod("DbccOption").Is(dbccOptionName, "=", dbccOptionValue);
            gb.Rule("DbccOptionName")
                .CanBe(identifierTerm)
                .Or(qualifiedName)
                .OrKeywords("MAXDOP");
            gb.Rule("DbccOptionValue")
                .CanBe(expression)
                .Or(identifierTerm)
                .OrKeywords("ON", "OFF");

            gb.Rule("DropProcStatement")
                .CanBe("DROP", "PROC", qualifiedName)
                .Or("DROP", "PROCEDURE", qualifiedName)
                .Or("DROP", "PROC", dropIfExistsClause, qualifiedName)
                .Or("DROP", "PROCEDURE", dropIfExistsClause, qualifiedName)
                .Or("DROP", "FUNCTION", qualifiedName)
                .Or("DROP", "FUNCTION", dropIfExistsClause, qualifiedName);
            gb.Prod("DropIfExistsClause").Is("IF", "EXISTS");

            gb.Prod("DropTableStatement").Is("DROP", "TABLE", dropTableTargetList);
            gb.Prod("DropTableStatement").Is("DROP", "TABLE", dropIfExistsClause, dropTableTargetList);
            gb.Prod("DropTableTargetList").Is(qualifiedName);
            gb.Prod("DropTableTargetList").Is(dropTableTargetList, ",", qualifiedName);

            gb.Prod("DropViewStatement").Is("DROP", "VIEW", dropViewTargetList);
            gb.Prod("DropViewStatement").Is("DROP", "VIEW", dropIfExistsClause, dropViewTargetList);
            gb.Prod("DropViewTargetList").Is(qualifiedName);
            gb.Prod("DropViewTargetList").Is(dropViewTargetList, ",", qualifiedName);

            gb.Prod("DropIndexStatement").Is("DROP", "INDEX", dropIndexSpecList);
            gb.Prod("DropIndexStatement").Is("DROP", "INDEX", dropIfExistsClause, dropIndexSpecList);
            gb.Prod("DropIndexSpecList").Is(dropIndexSpec);
            gb.Prod("DropIndexSpecList").Is(dropIndexSpecList, ",", dropIndexSpec);
            gb.Prod("DropIndexSpec").Is(qualifiedName, "ON", qualifiedName);
            gb.Prod("DropIndexSpec").Is(qualifiedName, "ON", qualifiedName, "WITH", "(", dropIndexOptionList, ")");
            gb.Prod("DropIndexSpec").Is(qualifiedName, ".", identifierTerm);
            gb.Prod("DropIndexOptionList").Is(dropIndexOption);
            gb.Prod("DropIndexOptionList").Is(dropIndexOptionList, ",", dropIndexOption);
            gb.Rule("DropIndexOption")
                .CanBe("MAXDOP", "=", expression)
                .Or("ONLINE", "=", "ON")
                .Or("ONLINE", "=", "OFF")
                .Or("MOVE", "TO", dropMoveToTarget)
                .Or("MOVE", "TO", dropMoveToTarget, "FILESTREAM_ON", dropFileStreamTarget)
                .Or("FILESTREAM_ON", dropFileStreamTarget);
            gb.Rule("DropMoveToTarget")
                .CanBe(qualifiedName)
                .OrKeywords("DEFAULT")
                .Or(qualifiedName, "(", identifierTerm, ")");
            gb.Rule("DropFileStreamTarget")
                .CanBe(qualifiedName)
                .OrKeywords("DEFAULT");

            gb.Prod("DropStatisticsStatement").Is("DROP", "STATISTICS", dropStatisticsTargetList);
            gb.Prod("DropStatisticsTargetList").Is(dropStatisticsTarget);
            gb.Prod("DropStatisticsTargetList").Is(dropStatisticsTargetList, ",", dropStatisticsTarget);
            gb.Prod("DropStatisticsTarget").Is(qualifiedName, ".", identifierTerm);

            gb.Prod("DropDatabaseStatement").Is("DROP", "DATABASE", identifierTerm);
            gb.Prod("DropDatabaseStatement").Is("DROP", "DATABASE", dropIfExistsClause, identifierTerm);

            gb.Prod("CreateTriggerHead").Is("CREATE", "TRIGGER");
            gb.Prod("CreateTriggerHead").Is("CREATE", "OR", "ALTER", "TRIGGER");
            gb.Prod("CreateTriggerHead").Is("ALTER", "TRIGGER");

            gb.Prod("CreateTriggerFireClause").Is("FOR", createTriggerEventList);
            gb.Prod("CreateTriggerFireClause").Is("AFTER", createTriggerEventList);
            gb.Prod("CreateTriggerFireClause").Is("INSTEAD", "OF", createTriggerEventList);

            gb.Prod("CreateTriggerEventList").Is(createTriggerEvent);
            gb.Prod("CreateTriggerEventList").Is(createTriggerEventList, ",", createTriggerEvent);
            gb.Rule(createTriggerEvent)
                .CanBe(identifierTerm) // DDL events: CREATE_TABLE, LOGON, etc.
                .OrKeywords("INSERT", "UPDATE", "DELETE");

            gb.Prod("CreateTriggerWithOptionList").Is(createTriggerWithOption);
            gb.Prod("CreateTriggerWithOptionList").Is(createTriggerWithOptionList, ",", createTriggerWithOption);
            gb.Rule(createTriggerWithOption)
                .CanBe(createProcExecuteAsClause)
                .OrKeywords("ENCRYPTION", "SCHEMABINDING", "NATIVE_COMPILATION");

            // DML trigger: ON table [WITH opts] fireClause [NOT FOR REPLICATION] AS body
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, "ON", qualifiedName, createTriggerFireClause, "AS", createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, "ON", qualifiedName, "WITH", createTriggerWithOptionList, createTriggerFireClause, "AS", createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, "ON", qualifiedName, createTriggerFireClause, "NOT", "FOR", "REPLICATION", "AS", createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, "ON", qualifiedName, "WITH", createTriggerWithOptionList, createTriggerFireClause, "NOT", "FOR", "REPLICATION", "AS", createProcBodyBlock);
            // DDL trigger: ON ALL SERVER | DATABASE
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, "ON", "ALL", "SERVER", createTriggerFireClause, "AS", createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, "ON", "DATABASE", createTriggerFireClause, "AS", createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, "ON", "ALL", "SERVER", "WITH", createTriggerWithOptionList, createTriggerFireClause, "AS", createProcBodyBlock);
            gb.Prod("CreateTriggerStatement").Is(createTriggerHead, qualifiedName, "ON", "DATABASE", "WITH", createTriggerWithOptionList, createTriggerFireClause, "AS", createProcBodyBlock);

            gb.Prod("DropTriggerStatement").Is("DROP", "TRIGGER", qualifiedName);
            gb.Prod("DropTriggerStatement").Is("DROP", "TRIGGER", dropIfExistsClause, qualifiedName);
            gb.Prod("DropTriggerStatement").Is("DROP", "TRIGGER", qualifiedName, "ON", "DATABASE");
            gb.Prod("DropTriggerStatement").Is("DROP", "TRIGGER", dropIfExistsClause, qualifiedName, "ON", "DATABASE");
            gb.Prod("DropTriggerStatement").Is("DROP", "TRIGGER", qualifiedName, "ON", "ALL", "SERVER");
            gb.Prod("DropTriggerStatement").Is("DROP", "TRIGGER", dropIfExistsClause, qualifiedName, "ON", "ALL", "SERVER");

            gb.Prod("ProcStatementList").Is(statement);
            gb.Prod("ProcStatementList").Is(statementSeparatorList, statement);
            gb.Prod("ProcStatementList").Is(procStatementList, ";", statement);
            gb.Prod("ProcStatementList").Is(procStatementList, statementNoLeadingWith);

            gb.Prod("TryCatchStatement").Is(
                "BEGIN", "TRY",
                procStatementList,
                "END", "TRY",
                "BEGIN", "CATCH",
                "END", "CATCH");
            gb.Prod("TryCatchStatement").Is(
                "BEGIN", "TRY",
                procStatementList,
                "END", "TRY",
                "BEGIN", "CATCH",
                procStatementList,
                "END", "CATCH");
            gb.Prod("TryCatchStatement").Is(
                "BEGIN", "TRY",
                procStatementList, statementSeparatorList,
                "END", "TRY",
                "BEGIN", "CATCH",
                "END", "CATCH");
            gb.Prod("TryCatchStatement").Is(
                "BEGIN", "TRY",
                procStatementList, statementSeparatorList,
                "END", "TRY",
                "BEGIN", "CATCH",
                procStatementList,
                "END", "CATCH");
            gb.Prod("TryCatchStatement").Is(
                "BEGIN", "TRY",
                procStatementList, statementSeparatorList,
                "END", "TRY",
                "BEGIN", "CATCH",
                procStatementList, statementSeparatorList,
                "END", "CATCH");
            gb.Prod("TryCatchStatement").Is(
                "BEGIN", "TRY",
                procStatementList,
                "END", "TRY",
                "BEGIN", "CATCH",
                procStatementList, statementSeparatorList,
                "END", "CATCH");

            gb.Prod("CreateRoleStatement").Is("CREATE", "ROLE", identifierTerm);
            gb.Prod("CreateRoleStatement").Is("CREATE", "ROLE", identifierTerm, "AUTHORIZATION", identifierTerm);

            gb.Prod("CreateSchemaStatement").Is("CREATE", "SCHEMA", schemaNameClause);
            gb.Prod("SchemaNameClause").Is(identifierTerm);
            gb.Prod("SchemaNameClause").Is("AUTHORIZATION", identifierTerm);
            gb.Prod("SchemaNameClause").Is(identifierTerm, "AUTHORIZATION", identifierTerm);

            gb.Prod("CreateViewHead").Is("CREATE", "VIEW");
            gb.Prod("CreateViewHead").Is("CREATE", "OR", "ALTER", "VIEW");
            gb.Prod("CreateViewHead").Is("ALTER", "VIEW");
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, "AS", queryExpression);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, "AS", withClause, queryExpression);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, "WITH", identifierTerm, "AS", queryExpression);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, "WITH", identifierTerm, "AS", withClause, queryExpression);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, "WITH", identifierTerm, ",", identifierTerm, "AS", queryExpression);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, "WITH", identifierTerm, ",", identifierTerm, "AS", withClause, queryExpression);

            gb.Prod("CreateTableFileTableClause").Is("AS", "FILETABLE");
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")");
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, "(", createTableElementList, ")");
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", createTableTailClauseList);
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, "(", createTableElementList, ")", createTableTailClauseList);
            // SQL Graph node/edge tables
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", "AS", "EDGE");
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", "AS", "NODE");
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", createTableTailClauseList, "AS", "EDGE");
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", createTableTailClauseList, "AS", "NODE");
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
            gb.Prod("CreateTableColumnOption").Is("NULL");
            gb.Prod("CreateTableColumnOption").Is("NOT", "NULL");
            gb.Prod("CreateTableColumnOption").Is("PRIMARY", "KEY");
            gb.Prod("CreateTableColumnOption").Is("UNIQUE");
            gb.Prod("CreateTableColumnOption").Is("SPARSE");
            gb.Prod("CreateTableColumnOption").Is("PERSISTED");
            gb.Prod("CreateTableColumnOption").Is("ROWGUIDCOL");
            gb.Prod("CreateTableColumnOption").Is("COLUMN_SET");
            gb.Prod("CreateTableColumnOption").Is("FOR", "ALL_SPARSE_COLUMNS");
            gb.Prod("CreateTableColumnOption").Is("DEFAULT", expression);
            gb.Prod("CreateTableColumnOption").Is("DEFAULT", "(", expression, ")");
            gb.Prod("CreateTableColumnOption").Is("IDENTITY");
            gb.Prod("CreateTableColumnOption").Is("IDENTITY", "(", expression, ",", expression, ")");
            gb.Prod("CreateTableColumnOption").Is("COLLATE", identifierTerm);
            gb.Prod("CreateTableColumnOption").Is("MASKED", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableColumnOption").Is("ENCRYPTED", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableColumnOption").Is("NOT", "FOR", "REPLICATION");
            gb.Prod("CreateTableColumnOption").Is("CHECK", "(", searchCondition, ")");
            gb.Prod("CreateTableColumnOption").Is("REFERENCES", qualifiedName);
            gb.Prod("CreateTableColumnOption").Is("REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateTableColumnOption").Is("WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableColumnOption").Is("CONSTRAINT", identifierTerm, createTableConstraintBody);
            gb.Prod("CreateTableColumnOption").Is(identifierTerm);
            gb.Prod("CreateTableColumnOption").Is(identifierTerm, identifierTerm);
            gb.Prod("CreateTableColumnOption").Is(qualifiedName, "(", expressionList, ")");
            gb.Prod("CreateTableColumnOption").Is(identifierTerm, "(", expressionList, ")");

            gb.Prod("CreateTableComputedColumn").Is(identifierTerm, "AS", expression);
            gb.Prod("CreateTableComputedColumn").Is(identifierTerm, "AS", expression, createTableColumnOptionList);

            gb.Prod("CreateTableColumnSet").Is(identifierTerm, typeSpec, "COLUMN_SET", "FOR", "ALL_SPARSE_COLUMNS");

            gb.Prod("CreateTableConstraint").Is(createTableConstraintBody);
            gb.Prod("CreateTableConstraint").Is("CONSTRAINT", identifierTerm, createTableConstraintBody);

            gb.Prod("CreateTableConstraintBody").Is("PRIMARY", "KEY", "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableConstraintBody").Is("PRIMARY", "KEY");
            gb.Prod("CreateTableConstraintBody").Is("PRIMARY", "KEY", createTableClusterType);
            gb.Prod("CreateTableConstraintBody").Is("PRIMARY", "KEY", createTableClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableConstraintBody").Is("PRIMARY", "KEY", createTableClusterType, "(", createTableKeyColumnList, ")", "ON", indexStorageTarget);
            gb.Prod("CreateTableConstraintBody").Is("PRIMARY", "KEY", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableConstraintBody").Is("PRIMARY", "KEY", createTableClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableConstraintBody").Is("PRIMARY", "KEY", createTableClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")", "ON", indexStorageTarget);
            gb.Prod("CreateTableConstraintBody").Is("UNIQUE", "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableConstraintBody").Is("UNIQUE");
            gb.Prod("CreateTableConstraintBody").Is("UNIQUE", createTableClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableConstraintBody").Is("UNIQUE", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableConstraintBody").Is("UNIQUE", createTableClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableConstraintBody").Is("UNIQUE", createTableClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")", "ON", indexStorageTarget);            gb.Prod("CreateTableConstraintBody").Is("CHECK", "(", searchCondition, ")");
            gb.Prod("CreateTableConstraintBody").Is("FOREIGN", "KEY", "(", identifierList, ")", "REFERENCES", qualifiedName);
            gb.Prod("CreateTableConstraintBody").Is("FOREIGN", "KEY", "(", identifierList, ")", "REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateTableConstraintBody").Is("FOREIGN", "KEY", "REFERENCES", qualifiedName);
            gb.Prod("CreateTableConstraintBody").Is("FOREIGN", "KEY", "REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateTableConstraintBody").Is("DEFAULT", expression, "FOR", identifierTerm);
            gb.Prod("CreateTableConstraintBody").Is("DEFAULT", "(", expression, ")", "FOR", identifierTerm);

            gb.Rule(createTableClusterType)
                .CanBe("CLUSTERED")
                .Or("NONCLUSTERED")
                .Or("NONCLUSTERED", "HASH")
                .Or("CLUSTERED", "COLUMNSTORE")
                .Or("NONCLUSTERED", "COLUMNSTORE");

            gb.Prod("CreateTableKeyColumnList").Is(createTableKeyColumn);
            gb.Prod("CreateTableKeyColumnList").Is(createTableKeyColumnList, ",", createTableKeyColumn);
            gb.Prod("CreateTableKeyColumn").Is(identifierTerm);
            gb.Prod("CreateTableKeyColumn").Is(identifierTerm, "ASC");
            gb.Prod("CreateTableKeyColumn").Is(identifierTerm, "DESC");

            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, "CLUSTERED", "COLUMNSTORE");
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, "CLUSTERED", "COLUMNSTORE", createIndexWithClause);

            gb.Prod("CreateTablePeriodClause").Is("PERIOD", forSystemTime, "(", identifierTerm, ",", identifierTerm, ")");
            gb.Prod("CreateTableOptions").Is("WITH", "(", createTableOptionList, ")");
            gb.Prod("CreateTableOptionList").Is(createTableOption);
            gb.Prod("CreateTableOptionList").Is(createTableOptionList, ",", createTableOption);
            gb.Prod("CreateTableOption").Is(identifierTerm, "=", indexOptionValue);
            gb.Prod("CreateTableOption").Is(qualifiedName, "=", indexOptionValue);
            gb.Prod("CreateTableOption").Is(identifierTerm);
            gb.Prod("CreateTableOption").Is(identifierTerm, identifierTerm);
            gb.Prod("CreateTableOption").Is("CLUSTERED", "COLUMNSTORE", "INDEX");
            gb.Prod("CreateTableOnClause").Is("ON", indexStorageTarget);
            gb.Prod("CreateTableTextImageClause").Is("TEXTIMAGE_ON", indexStorageTarget);

            gb.Prod("AlterTableStatement").Is("ALTER", "TABLE", qualifiedName, alterTableAction);
            gb.Prod("AlterTableAction").Is("ADD", alterTableAddItemList);
            gb.Prod("AlterTableAction").Is("ALTER", "COLUMN", alterTableAlterColumnAction);
            gb.Prod("AlterTableAction").Is("DROP", alterTableDropItemList);
            gb.Prod("AlterTableAction").Is("WITH", alterTableCheckMode, "ADD", createTableConstraint);
            gb.Prod("AlterTableAction").Is(alterTableCheckMode, "CONSTRAINT", alterTableConstraintTarget);
            gb.Prod("AlterTableAction").Is("ENABLE", "TRIGGER", alterTableTriggerTarget);
            gb.Prod("AlterTableAction").Is("DISABLE", "TRIGGER", alterTableTriggerTarget);
            gb.Prod("AlterTableAction").Is("ADD", createTablePeriodClause);
            gb.Prod("AlterTableAction").Is("DROP", "PERIOD", forSystemTime);
            gb.Prod("AlterTableAction").Is("SET", "(", indexOptionList, ")");
            gb.Prod("AlterTableAddItemList").Is(alterTableAddItem);
            gb.Prod("AlterTableAddItemList").Is(alterTableAddItemList, ",", alterTableAddItem);
            gb.Prod("AlterTableAddItem").Is(createTableColumnDefinition);
            gb.Prod("AlterTableAddItem").Is(createTableConstraint);
            gb.Prod("AlterTableAddItem").Is(createTableTableIndex);
            gb.Prod("AlterTableAddItem").Is(createTableComputedColumn);
            gb.Prod("AlterTableAlterColumnAction").Is(identifierTerm, typeSpec);
            gb.Prod("AlterTableAlterColumnAction").Is(identifierTerm, typeSpec, alterTableColumnOptionList);
            gb.Prod("AlterTableAlterColumnAction").Is(identifierTerm, "ADD", alterTableColumnOptionList);
            gb.Prod("AlterTableAlterColumnAction").Is(identifierTerm, "DROP", alterTableColumnOptionList);
            gb.Prod("AlterTableColumnOptionList").Is(alterTableColumnOption);
            gb.Prod("AlterTableColumnOptionList").Is(alterTableColumnOptionList, alterTableColumnOption);
            gb.Prod("AlterTableColumnOption").Is(createTableColumnOption);
            gb.Prod("AlterTableDropItemList").Is(alterTableDropItem);
            gb.Prod("AlterTableDropItemList").Is(alterTableDropItemList, ",", alterTableDropItem);
            gb.Prod("AlterTableDropItem").Is("COLUMN", identifierTerm);
            gb.Prod("AlterTableDropItem").Is("CONSTRAINT", identifierTerm);
            gb.Prod("AlterTableDropItem").Is("CONSTRAINT", "ALL");
            gb.Rule(alterTableCheckMode).Keywords("CHECK", "NOCHECK");
            gb.Rule(alterTableConstraintTarget)
                .CanBe(identifierTerm)
                .OrKeywords("ALL");
            gb.Rule(alterTableTriggerTarget)
                .CanBe(identifierTerm)
                .OrKeywords("ALL");

            gb.Prod("CreateIndexHead").Is("CREATE", "INDEX", identifierTerm);
            gb.Prod("CreateIndexHead").Is("CREATE", "UNIQUE", "INDEX", identifierTerm);
            gb.Prod("CreateIndexHead").Is("CREATE", "CLUSTERED", "INDEX", identifierTerm);
            gb.Prod("CreateIndexHead").Is("CREATE", "NONCLUSTERED", "INDEX", identifierTerm);
            gb.Prod("CreateIndexHead").Is("CREATE", "UNIQUE", "CLUSTERED", "INDEX", identifierTerm);
            gb.Prod("CreateIndexHead").Is("CREATE", "UNIQUE", "NONCLUSTERED", "INDEX", identifierTerm);
            gb.Prod("CreateIndexHead").Is("CREATE", "NONCLUSTERED", "COLUMNSTORE", "INDEX", identifierTerm);
            gb.Prod("CreateIndexHead").Is("CREATE", "CLUSTERED", "COLUMNSTORE", "INDEX", identifierTerm);
            gb.Prod("CreateIndexStatement").Is(createIndexHead, "ON", qualifiedName);
            gb.Prod("CreateIndexStatement").Is(createIndexHead, "ON", qualifiedName, createIndexTailClauseList);
            gb.Prod("CreateIndexStatement").Is(createIndexHead, "ON", qualifiedName, "(", createIndexKeyList, ")");
            gb.Prod("CreateIndexStatement").Is(createIndexHead, "ON", qualifiedName, "(", createIndexKeyList, ")", createIndexTailClauseList);

            gb.Prod("CreateIndexKeyList").Is(createIndexKeyItem);
            gb.Prod("CreateIndexKeyList").Is(createIndexKeyList, ",", createIndexKeyItem);
            gb.Prod("CreateIndexKeyItem").Is(identifierTerm);
            gb.Prod("CreateIndexKeyItem").Is(identifierTerm, "ASC");
            gb.Prod("CreateIndexKeyItem").Is(identifierTerm, "DESC");

            gb.Prod("CreateIndexTailClauseList").Is(createIndexTailClause);
            gb.Prod("CreateIndexTailClauseList").Is(createIndexTailClauseList, createIndexTailClause);
            gb.Prod("CreateIndexTailClause").Is(createIndexIncludeClause);
            gb.Prod("CreateIndexTailClause").Is(createIndexWhereClause);
            gb.Prod("CreateIndexTailClause").Is(createIndexWithClause);
            gb.Prod("CreateIndexTailClause").Is(createIndexStorageClause);
            gb.Prod("CreateIndexTailClause").Is(createIndexFileStreamClause);

            gb.Prod("CreateIndexIncludeClause").Is("INCLUDE", "(", createIndexIncludeList, ")");
            gb.Prod("CreateIndexIncludeList").Is(identifierTerm);
            gb.Prod("CreateIndexIncludeList").Is(createIndexIncludeList, ",", identifierTerm);
            gb.Prod("CreateIndexWhereClause").Is("WHERE", searchCondition);
            gb.Prod("CreateIndexWithClause").Is("WITH", "(", indexOptionList, ")");
            gb.Prod("CreateIndexStorageClause").Is("ON", indexStorageTarget);
            gb.Prod("CreateIndexFileStreamClause").Is("FILESTREAM_ON", indexFileStreamTarget);

            gb.Prod("IndexStorageTarget").Is(qualifiedName);
            gb.Prod("IndexStorageTarget").Is("DEFAULT");
            gb.Prod("IndexStorageTarget").Is(qualifiedName, "(", identifierTerm, ")");
            gb.Prod("IndexFileStreamTarget").Is(qualifiedName);
            gb.Prod("IndexFileStreamTarget").Is("NULL");
            gb.Prod("IndexFileStreamTarget").Is(qualifiedName, "(", identifierTerm, ")");

            gb.Prod("IndexOptionList").Is(indexOption);
            gb.Prod("IndexOptionList").Is(indexOptionList, ",", indexOption);
            gb.Prod("IndexOptionList").Is(indexOptionList, ","); // allow trailing comma
            gb.Prod("IndexOption").Is(indexOptionName, "=", indexOptionValue);
            gb.Prod("IndexOption").Is(indexOptionName, "(", indexOptionList, ")");
            gb.Prod("IndexOption").Is(indexOptionName, "=", indexOptionValue, "ON", "PARTITIONS", "(", indexPartitionList, ")");

            gb.Prod("IndexOptionName").Is(identifierTerm);
            gb.Rule(indexOptionName)
                .CanBe(qualifiedName)
                .OrKeywords("FUNCTION", "ONLINE", "MAXDOP");

            gb.Prod("IndexOptionValue").Is(expression);
            gb.Prod("IndexOptionValue").Is(indexOnOffValue);
            gb.Prod("IndexOptionValue").Is(identifierTerm);
            gb.Prod("IndexOptionValue").Is("NONE");
            gb.Prod("IndexOptionValue").Is("SELF");
            gb.Prod("IndexOptionValue").Is("ROW");
            gb.Prod("IndexOptionValue").Is("PAGE");
            gb.Prod("IndexOptionValue").Is("COLUMNSTORE");
            gb.Prod("IndexOptionValue").Is("COLUMNSTORE_ARCHIVE");
            gb.Prod("IndexOptionValue").Is(expression, identifierTerm);
            gb.Prod("IndexOptionValue").Is(identifierTerm, identifierTerm);
            gb.Prod("IndexOptionValue").Is(indexOnOffValue, "(", indexOptionList, ")");
            gb.Prod("IndexOptionValue").Is("(", indexOptionList, ")");
            gb.Rule(indexOnOffValue).Keywords("ON", "OFF");

            gb.Prod("IndexPartitionList").Is(indexPartitionItem);
            gb.Prod("IndexPartitionList").Is(indexPartitionList, ",", indexPartitionItem);
            gb.Prod("IndexPartitionItem").Is(expression);
            gb.Prod("IndexPartitionItem").Is(expression, "TO", expression);
            gb.Prod("IndexPartitionItem").Is("ALL");

            gb.Prod("AlterIndexStatement").Is("ALTER", "INDEX", alterIndexTarget, "ON", qualifiedName, alterIndexAction);
            gb.Rule(alterIndexTarget)
                .CanBe(identifierTerm)
                .Or(qualifiedName)
                .OrKeywords("ALL");
            gb.Rule(alterIndexAction)
                .CanBe("REBUILD")
                .Or("REBUILD", alterIndexRebuildSpec)
                .Or("DISABLE")
                .Or("REORGANIZE")
                .Or("REORGANIZE", alterIndexReorganizeSpec)
                .Or("SET", "(", indexOptionList, ")")
                .Or("RESUME")
                .Or("RESUME", alterIndexResumeSpec)
                .Or("PAUSE")
                .Or("ABORT");

            gb.Prod("AlterIndexRebuildSpec").Is("WITH", "(", indexOptionList, ")");
            gb.Prod("AlterIndexRebuildSpec").Is("PARTITION", "=", alterIndexPartitionSelector);
            gb.Prod("AlterIndexRebuildSpec").Is("PARTITION", "=", alterIndexPartitionSelector, "WITH", "(", indexOptionList, ")");
            gb.Prod("AlterIndexReorganizeSpec").Is("PARTITION", "=", alterIndexPartitionSelector);
            gb.Prod("AlterIndexReorganizeSpec").Is("WITH", "(", indexOptionList, ")");
            gb.Prod("AlterIndexReorganizeSpec").Is("PARTITION", "=", alterIndexPartitionSelector, "WITH", "(", indexOptionList, ")");
            gb.Prod("AlterIndexResumeSpec").Is("WITH", "(", indexOptionList, ")");
            gb.Prod("AlterIndexPartitionSelector").Is(expression);
            gb.Prod("AlterIndexPartitionSelector").Is("ALL");

            gb.Prod("CreateDatabaseStatement").Is("CREATE", "DATABASE", identifierTerm);
            gb.Prod("CreateDatabaseStatement").Is("CREATE", "DATABASE", identifierTerm, createDatabaseClauseList);

            gb.Prod("CreateDatabaseClauseList").Is(createDatabaseClause);
            gb.Prod("CreateDatabaseClauseList").Is(createDatabaseClauseList, createDatabaseClause);
            gb.Prod("CreateDatabaseClause").Is(createDatabaseContainmentClause);
            gb.Prod("CreateDatabaseClause").Is(createDatabaseOnClause);
            gb.Prod("CreateDatabaseClause").Is(createDatabaseCollateClause);
            gb.Prod("CreateDatabaseClause").Is(createDatabaseWithClause);

            gb.Prod("CreateDatabaseContainmentClause").Is("CONTAINMENT", "=", "NONE");
            gb.Prod("CreateDatabaseContainmentClause").Is("CONTAINMENT", "=", "PARTIAL");

            gb.Prod("CreateDatabaseOnClause").Is("ON", createDatabaseOnItemList);
            gb.Prod("CreateDatabaseOnItemList").Is(createDatabaseOnItem);
            gb.Prod("CreateDatabaseOnItemList").Is(createDatabaseOnItemList, ",", createDatabaseOnItem);
            gb.Prod("CreateDatabaseOnItem").Is(createDatabaseFilespecList);
            gb.Prod("CreateDatabaseOnItem").Is("PRIMARY", createDatabaseFilespecList);
            gb.Prod("CreateDatabaseOnItem").Is(createDatabaseFilegroup);
            gb.Prod("CreateDatabaseOnItem").Is("LOG", "ON", createDatabaseFilespecList);

            gb.Prod("CreateDatabaseFilespecList").Is(createDatabaseFilespec);

            gb.Prod("CreateDatabaseFilegroup").Is("FILEGROUP", identifierTerm, createDatabaseFilespecList);
            gb.Prod("CreateDatabaseFilegroup").Is("FILEGROUP", identifierTerm, "DEFAULT", createDatabaseFilespecList);
            gb.Prod("CreateDatabaseFilegroup").Is("FILEGROUP", identifierTerm, "CONTAINS", "FILESTREAM", createDatabaseFilespecList);
            gb.Prod("CreateDatabaseFilegroup").Is("FILEGROUP", identifierTerm, "CONTAINS", "FILESTREAM", "DEFAULT", createDatabaseFilespecList);
            gb.Prod("CreateDatabaseFilegroup").Is("FILEGROUP", identifierTerm, "CONTAINS", "MEMORY_OPTIMIZED_DATA", createDatabaseFilespecList);

            gb.Prod("CreateDatabaseCollateClause").Is("COLLATE", identifierTerm);

            gb.Prod("CreateDatabaseWithClause").Is("WITH", createDatabaseOptionList);
            gb.Prod("CreateDatabaseOptionList").Is(createDatabaseOption);
            gb.Prod("CreateDatabaseOptionList").Is(createDatabaseOptionList, ",", createDatabaseOption);

            gb.Prod("CreateDatabaseOption").Is("FILESTREAM", "(", createDatabaseFilestreamOptionList, ")");
            gb.Prod("CreateDatabaseOption").Is("DEFAULT_FULLTEXT_LANGUAGE", "=", createDatabaseOptionValue);
            gb.Prod("CreateDatabaseOption").Is("DEFAULT_LANGUAGE", "=", createDatabaseOptionValue);
            gb.Prod("CreateDatabaseOption").Is("NESTED_TRIGGERS", "=", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is("TRANSFORM_NOISE_WORDS", "=", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is("TWO_DIGIT_YEAR_CUTOFF", "=", number);
            gb.Prod("CreateDatabaseOption").Is("DB_CHAINING", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is("DB_CHAINING", "=", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is("TRUSTWORTHY", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is("TRUSTWORTHY", "=", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is("LEDGER", "=", createDatabaseOnOffValue);
            gb.Prod("CreateDatabaseOption").Is(
                "PERSISTENT_LOG_BUFFER",
                "=",
                "ON",
                "(",
                "DIRECTORY_NAME",
                "=",
                stringLiteral,
                ")");

            gb.Prod("CreateDatabaseOptionValue").Is(number);
            gb.Prod("CreateDatabaseOptionValue").Is(identifierTerm);
            gb.Prod("CreateDatabaseOptionValue").Is(stringLiteral);

            gb.Rule(createDatabaseOnOffValue).Keywords("ON", "OFF");

            gb.Prod("CreateDatabaseFilestreamOptionList").Is(createDatabaseFilestreamOption);
            gb.Prod("CreateDatabaseFilestreamOptionList").Is(createDatabaseFilestreamOptionList, ",", createDatabaseFilestreamOption);
            gb.Prod("CreateDatabaseFilestreamOption").Is("NON_TRANSACTED_ACCESS", "=", createDatabaseNonTransactedAccessValue);
            gb.Prod("CreateDatabaseFilestreamOption").Is("DIRECTORY_NAME", "=", stringLiteral);
            gb.Rule(createDatabaseNonTransactedAccessValue).Keywords("OFF", "READ_ONLY", "FULL");

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
            gb.Prod("CreateDatabaseFilespecOption").Is("SIZE", "=", createDatabaseSizeSpec);
            gb.Prod("CreateDatabaseFilespecOption").Is("MAXSIZE", "=", createDatabaseMaxSizeSpec);
            gb.Prod("CreateDatabaseFilespecOption").Is("FILEGROWTH", "=", createDatabaseGrowthSpec);

            gb.Prod("CreateDatabaseSizeSpec").Is(number);
            gb.Prod("CreateDatabaseSizeSpec").Is(number, createDatabaseSizeUnit);
            gb.Prod("CreateDatabaseSizeSpec").Is(number, identifierTerm);

            gb.Prod("CreateDatabaseMaxSizeSpec").Is("UNLIMITED");
            gb.Prod("CreateDatabaseMaxSizeSpec").Is(number);
            gb.Prod("CreateDatabaseMaxSizeSpec").Is(number, createDatabaseSizeUnit);
            gb.Prod("CreateDatabaseMaxSizeSpec").Is(number, identifierTerm);

            gb.Prod("CreateDatabaseGrowthSpec").Is(number);
            gb.Prod("CreateDatabaseGrowthSpec").Is(number, createDatabaseGrowthUnit);
            gb.Prod("CreateDatabaseGrowthSpec").Is(number, identifierTerm);

            gb.Rule(createDatabaseSizeUnit).Keywords("KB", "MB", "GB", "TB");

            gb.Prod("CreateDatabaseGrowthUnit").Is(createDatabaseSizeUnit);
            gb.Prod("CreateDatabaseGrowthUnit").Is("%");

            gb.Prod("WithXmlNamespacesClause").Is("WITH", "XMLNAMESPACES", "(", xmlNamespaceItemList, ")");
            gb.Prod("XmlNamespaceItemList").Is(xmlNamespaceItem);
            gb.Prod("XmlNamespaceItemList").Is(xmlNamespaceItemList, ",", xmlNamespaceItem);
            gb.Prod("XmlNamespaceItem").Is(expression, "AS", identifierTerm);
            gb.Prod("XmlNamespaceItem").Is("DEFAULT", expression);

            gb.Prod("WithClause").Is("WITH", cteDefinitionList);
            gb.Prod("CteDefinitionList").Is(cteDefinition);
            gb.Prod("CteDefinitionList").Is(cteDefinitionList, ",", cteDefinition);
            gb.Prod("CteDefinition").Is(identifierTerm, "AS", "(", queryExpression, ")");
            gb.Prod("CteDefinition").Is(identifierTerm, "(", identifierList, ")", "AS", "(", queryExpression, ")");

            gb.Prod("QueryExpression").Is(queryUnionExpression);
            gb.Prod("QueryUnionExpression").Is(queryIntersectExpression);
            gb.Prod("QueryUnionExpression").Is(queryUnionExpression, setOperator, queryIntersectExpression);
            gb.Prod("QueryIntersectExpression").Is(queryPrimary);
            gb.Prod("QueryIntersectExpression").Is(queryIntersectExpression, "INTERSECT", queryPrimary);

            gb.Rule(setOperator)
                .CanBe("UNION")
                .Or("UNION", "ALL")
                .OrKeywords("EXCEPT");

            gb.Prod("QueryPrimary").Is(querySpecification, queryPrimaryTail);
            gb.Prod("QueryPrimaryTail").Is(queryPrimaryOrderByAndOffsetOpt, queryPrimaryForOpt, queryPrimaryOptionOpt);
            gb.Opt(queryPrimaryOrderByAndOffsetOpt, orderByClause);
            gb.Prod("QueryPrimaryOrderByAndOffsetOpt").Is(orderByClause, offsetFetchClause);
            gb.Opt(queryPrimaryForOpt, forClause);
            gb.Opt(queryPrimaryOptionOpt, optionClause);
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")", parenQueryPrimaryOrderByAndOffsetOpt);
            gb.Opt(parenQueryPrimaryOrderByAndOffsetOpt, orderByClause);
            gb.Prod("ParenQueryPrimaryOrderByAndOffsetOpt").Is(orderByClause, offsetFetchClause);

            gb.Rule("ForClause")
                .CanBe("FOR", "BROWSE")
                .Or("FOR", "JSON", forJsonMode)
                .Or("FOR", "JSON", forJsonMode, ",", forJsonOptionList)
                .Or("FOR", "XML", forXmlMode)
                .Or("FOR", "XML", forXmlMode, ",", forXmlOptionList);

            gb.Rule(forJsonMode).Keywords("AUTO", "PATH", "NONE");

            gb.Prod("ForJsonOptionList").Is(forJsonOption);
            gb.Prod("ForJsonOptionList").Is(forJsonOptionList, ",", forJsonOption);
            gb.Rule(forJsonOption).Keywords("WITHOUT_ARRAY_WRAPPER", "INCLUDE_NULL_VALUES", "ROOT");
            gb.Prod("ForJsonOption").Is("ROOT", "(", expression, ")");

            gb.Rule("ForXmlMode")
                .CanBe("AUTO")
                .Or("PATH")
                .Or("PATH", "(", expression, ")")
                .Or("RAW")
                .Or("RAW", "(", expression, ")")
                .Or("EXPLICIT");

            gb.Prod("ForXmlOptionList").Is(forXmlOption);
            gb.Prod("ForXmlOptionList").Is(forXmlOptionList, ",", forXmlOption);
            gb.Rule("ForXmlOption")
                .CanBe("TYPE")
                .Or("XMLDATA")
                .Or("XMLSCHEMA")
                .Or("XMLSCHEMA", "(", expression, ")")
                .Or("ELEMENTS")
                .Or("ELEMENTS", "XSINIL")
                .Or("ELEMENTS", "ABSENT")
                .Or("ROOT")
                .Or("ROOT", "(", expression, ")")
                .Or("BINARY", "BASE64")
                .Or("WITHOUT_ARRAY_WRAPPER");

            gb.Rule("SelectCorePrefix").OneOf(
                EmptyTerm.Empty,
                setQuantifier,
                topClause,
                gb.Seq(setQuantifier, topClause));
            gb.Rule("SelectCoreTail").OneOf(
                EmptyTerm.Empty,
                gb.Seq("FROM", tableSourceList),
                selectCoreIntoClause);
            gb.Prod("SelectCoreIntoClause").Is("INTO", qualifiedName, "FROM", tableSourceList);
            gb.Prod("SelectCore").Is("SELECT", selectCorePrefix, selectList, selectCoreTail);

            gb.Prod("QuerySpecificationWhereClause").Is("WHERE", searchCondition);
            gb.Opt(querySpecificationHavingOpt, "HAVING", searchCondition);
            gb.Opt(querySpecificationGroupByWithOpt, "WITH", identifierTerm);
            gb.Prod("QuerySpecificationGroupByExpressionList").Is(expressionList, querySpecificationGroupByWithOpt);
            gb.Prod("QuerySpecificationGroupByGroupingSets").Is("GROUPING", "SETS", "(", groupingSetList, ")");
            gb.Rule("QuerySpecificationGroupByClause").OneOf(
                gb.Seq("GROUP", "BY", querySpecificationGroupByExpressionList, querySpecificationHavingOpt),
                gb.Seq("GROUP", "BY", querySpecificationGroupByGroupingSets, querySpecificationHavingOpt));
            gb.Rule("QuerySpecification").OneOf(
                selectCore,
                gb.Seq(selectCore, querySpecificationWhereClause),
                gb.Seq(selectCore, querySpecificationGroupByClause),
                gb.Seq(selectCore, querySpecificationWhereClause, querySpecificationGroupByClause));

            gb.Rule("SetQuantifier").Keywords("ALL", "DISTINCT");

            gb.Rule("TopClauseTail").OneOf(
                EmptyTerm.Empty,
                "PERCENT",
                gb.Seq("WITH", "TIES"),
                gb.Seq("PERCENT", "WITH", "TIES"));
            gb.Prod("TopClause").Is("TOP", topValue, topClauseTail);
            gb.Prod("TopValue").Is(number);
            gb.Prod("TopValue").Is("(", expression, ")");

            gb.Rule("SelectList").CanBe(selectItemList);
            gb.Rule("SelectItemList").SeparatedBy(",", selectItem);
            gb.Prod("SelectItem").Is("*");
            gb.Prod("SelectItem").Is(expression, "AS", identifierTerm);
            gb.Prod("SelectItem").Is(expression, "AS", stringLiteral);
            gb.Prod("SelectItem").Is(expression, identifierTerm);
            gb.Prod("SelectItem").Is(expression, stringLiteral);
            gb.Prod("SelectItem").Is(expression);
            gb.Prod("SelectItem").Is(qualifiedName, ".", "*");
            gb.Prod("SelectItem").Is(variableReference, "=", expression);
            gb.Prod("SelectItem").Is(variableReference, compoundAssignOp, expression);

            gb.Rule("TableSourceList").SeparatedBy(",", tableSource);
            gb.Prod("TableSource").Is(tableFactor);
            gb.Prod("TableSource").Is(tableSource, joinPart);
            gb.Prod("TableFactor").Is(qualifiedName);
            gb.Prod("TableFactor").Is(qualifiedName, "AS", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, forPath);
            gb.Prod("TableFactor").Is(qualifiedName, forPath, identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("TableFactor").Is(qualifiedName, "AS", identifierTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("TableFactor").Is(qualifiedName, identifierTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("TableFactor").Is(qualifiedName, temporalClause);
            gb.Prod("TableFactor").Is(qualifiedName, temporalClause, "AS", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, temporalClause, identifierTerm);
            gb.Rule("TemporalClause").OneOf(
                gb.Seq(forSystemTime, "AS", "OF", additiveExpression),
                gb.Seq(forSystemTime, "ALL"),
                gb.Seq(forSystemTime, "BETWEEN", additiveExpression, "AND", additiveExpression),
                gb.Seq(forSystemTime, "FROM", additiveExpression, "TO", additiveExpression),
                gb.Seq(forSystemTime, "CONTAINED", "IN", "(", additiveExpression, ",", additiveExpression, ")"));
            gb.Prod("TableFactor").Is(variableReference);
            gb.Prod("TableFactor").Is(variableReference, "AS", identifierTerm);
            gb.Prod("TableFactor").Is(variableReference, identifierTerm);
            gb.Prod("TableFactor").Is(functionCall);
            gb.Prod("TableFactor").Is(functionCall, "AS", identifierTerm);
            gb.Prod("TableFactor").Is(functionCall, identifierTerm);
            gb.Prod("TableFactor").Is(functionCall, "AS", identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is(functionCall, identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is(functionCall, openJsonWithClause);
            gb.Prod("TableFactor").Is(functionCall, openJsonWithClause, "AS", identifierTerm);
            gb.Prod("TableFactor").Is(functionCall, openJsonWithClause, identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", "AS", identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, "(", insertColumnList, ")");
            // PIVOT / UNPIVOT applied to derived table
            gb.Prod("TableFactor").Is("(", queryExpression, ")", "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod("PivotClause").Is(functionCall, "FOR", identifierTerm, "IN", "(", pivotValueList, ")");
            gb.Prod("PivotValueList").Is(expression);
            gb.Prod("PivotValueList").Is(pivotValueList, ",", expression);
            gb.Prod("TableFactor").Is("(", "VALUES", rowValueList, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", "VALUES", rowValueList, ")", identifierTerm);
            gb.Prod("TableFactor").Is("(", "VALUES", rowValueList, ")", "AS", identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is("(", "VALUES", rowValueList, ")", identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is("(", tableSource, ")");
            gb.Prod("TableFactor").Is("(", tableSource, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", tableSource, ")", identifierTerm);

            gb.Prod("OpenJsonWithClause").Is("WITH", "(", openJsonColumnList, ")");
            gb.Prod("OpenJsonColumnList").Is(openJsonColumnDef);
            gb.Prod("OpenJsonColumnList").Is(openJsonColumnList, ",", openJsonColumnDef);
            // col_name typeSpec [ path_expr ] [ AS JSON ]
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec);
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec, expression);
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec, "AS", "JSON");
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec, expression, "AS", "JSON");

            gb.Prod("JoinPart").Is("JOIN", tableFactor, "ON", searchCondition);
            gb.Prod("JoinPart").Is(joinType, "JOIN", tableFactor, "ON", searchCondition);
            gb.Prod("JoinPart").Is("CROSS", "JOIN", tableFactor);
            gb.Prod("JoinPart").Is("CROSS", "APPLY", tableFactor);
            gb.Prod("JoinPart").Is("OUTER", "APPLY", tableFactor);

            gb.Rule("JoinType")
                .CanBe("INNER")
                .Or("INNER", "HASH")
                .Or("INNER", "LOOP")
                .Or("INNER", "MERGE")
                .Or("LEFT")
                .Or("LEFT", "OUTER")
                .Or("LEFT", "HASH")
                .Or("LEFT", "OUTER", "HASH")
                .Or("RIGHT")
                .Or("RIGHT", "OUTER")
                .Or("RIGHT", "HASH")
                .Or("RIGHT", "OUTER", "HASH")
                .Or("FULL")
                .Or("FULL", "OUTER");

            gb.Prod("OrderByClause").Is("ORDER", "BY", orderItemList);
            gb.Prod("OrderItemList").Is(orderItem);
            gb.Prod("OrderItemList").Is(orderItemList, ",", orderItem);
            gb.Prod("OrderItem").Is(expression);
            gb.Prod("OrderItem").Is(expression, "ASC");
            gb.Prod("OrderItem").Is(expression, "DESC");

            gb.Prod("OffsetFetchClause").Is("OFFSET", expression, "ROWS");
            gb.Prod("OffsetFetchClause").Is(
                "OFFSET",
                expression,
                "ROWS",
                "FETCH",
                "NEXT",
                expression,
                "ROWS",
                "ONLY");

            gb.Prod("SearchCondition").Is(expression);

            gb.Prod("Expression").Is(logicalOrExpression);

            gb.Prod("LogicalOrExpression").Is(logicalAndExpression);
            gb.Prod("LogicalOrExpression").Is(logicalOrExpression, "OR", logicalAndExpression);

            gb.Prod("LogicalAndExpression").Is(logicalNotExpression);
            gb.Prod("LogicalAndExpression").Is(logicalAndExpression, "AND", logicalNotExpression);

            gb.Prod("LogicalNotExpression").Is(comparisonExpression);
            gb.Prod("LogicalNotExpression").Is("NOT", logicalNotExpression);

            gb.Prod("ComparisonExpression").Is(additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, comparisonOperator, additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, likeOperator, additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, likeOperator, additiveExpression, "ESCAPE", additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, inOperator, additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, isOperator, additiveExpression);
            gb.Prod("ComparisonExpression").Is(additiveExpression, betweenOperator, additiveExpression, "AND", additiveExpression);

            gb.Rule("ComparisonOperator")
                .CanBe("=")
                .Or("<>")
                .Or("!=")
                .Or("<")
                .Or("<=")
                .Or(">")
                .Or(">=");

            gb.Rule("LikeOperator")
                .CanBe("LIKE")
                .Or("NOT", "LIKE");

            gb.Rule("InOperator")
                .CanBe("IN")
                .Or("NOT", "IN");

            gb.Prod("IsOperator").Is("IS");
            gb.Prod("IsOperator").Is("IS", "NOT");

            gb.Prod("BetweenOperator").Is("BETWEEN");
            gb.Prod("BetweenOperator").Is("NOT", "BETWEEN");

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
            gb.Prod("CollateExpression").Is(collateExpression, "COLLATE", identifierTerm);

            gb.Prod("PrimaryExpression").Is(literal);
            gb.Prod("PrimaryExpression").Is(unicodeStringLiteral);
            gb.Prod("PrimaryExpression").Is(sqlcmdVariable);
            gb.Prod("PrimaryExpression").Is(variableReference);
            gb.Prod("PrimaryExpression").Is(graphColumnRef);
            gb.Prod("PrimaryExpression").Is(qualifiedName);
            gb.Prod("PrimaryExpression").Is(functionCall);
            gb.Prod("PrimaryExpression").Is(functionCall, overClause);
            gb.Prod("PrimaryExpression").Is(functionCall, graphWithinGroupClause);
            gb.Prod("PrimaryExpression").Is("CAST", "(", expression, "AS", typeSpec, ")");
            gb.Prod("PrimaryExpression").Is("(", expression, ")");
            gb.Prod("PrimaryExpression").Is("(", expressionList, ")");
            gb.Prod("PrimaryExpression").Is("(", queryExpression, ")");
            gb.Prod("PrimaryExpression").Is("EXISTS", "(", queryExpression, ")");
            gb.Prod("PrimaryExpression").Is("NOT", "EXISTS", "(", queryExpression, ")");
            gb.Prod("PrimaryExpression").Is(caseExpression);
            // LANGUAGE language_term used in full-text function calls (FREETEXTTABLE, CONTAINSTABLE, etc.)
            gb.Prod("PrimaryExpression").Is("LANGUAGE", primaryExpression);

            gb.Prod("FunctionCall").Is(qualifiedName, "(", ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", "*", ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("::", qualifiedName, "(", ")");
            gb.Prod("FunctionCall").Is("::", qualifiedName, "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", "DISTINCT", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", "ALL", functionArgumentList, ")");
            // FREETEXTTABLE/CONTAINSTABLE with wildcard column: func(table, *, search, ...)
            gb.Prod("FunctionCall").Is(qualifiedName, "(", functionArgumentList, ",", "*", ",", functionArgumentList, ")");
            // XML method calls: @variable.nodes(xpath), column_ref.value(xpath, type)
            gb.Prod("FunctionCall").Is(variableReference, ".", identifierTerm, "(", ")");
            gb.Prod("FunctionCall").Is(variableReference, ".", identifierTerm, "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("LEFT", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("RIGHT", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("COALESCE", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("NULLIF", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("IIF", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("UPDATE", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("NEXT", identifierTerm, "FOR", qualifiedName);
            // OPENROWSET(BULK ...) special form
            gb.Prod("FunctionCall").Is("OPENROWSET", "(", openRowsetBulk, ")");
            gb.Prod("OpenRowsetBulk").Is("BULK", expression, ",", identifierTerm);  // BULK 'file', SINGLE_BLOB
            gb.Prod("OpenRowsetBulk").Is("BULK", expression, ",", openRowsetBulkOptionList);
            gb.Prod("OpenRowsetBulkOptionList").Is(openRowsetBulkOption);
            gb.Prod("OpenRowsetBulkOptionList").Is(openRowsetBulkOptionList, ",", openRowsetBulkOption);
            gb.Prod("OpenRowsetBulkOption").Is(identifierTerm);
            gb.Prod("OpenRowsetBulkOption").Is(identifierTerm, "=", expression);
            gb.Prod("FunctionArgumentList").Is(expression);
            gb.Prod("FunctionArgumentList").Is(functionArgumentList, ",", expression);
            gb.Prod("GraphWithinGroupClause").Is("WITHIN", "GROUP", "(", "GRAPH", "PATH", ")");
            gb.Prod("GraphWithinGroupClause").Is("WITHIN", "GROUP", "(", "ORDER", "BY", orderItemList, ")");

            gb.Prod("OverClause").Is("OVER", "(", overSpec, ")");
            gb.Prod("OverPartitionClause").Is("PARTITION", "BY", expressionList);
            gb.Rule("OverFrameExtentOpt").OneOf(
                EmptyTerm.Empty,
                gb.Seq("ROWS", frameClause),
                gb.Seq("RANGE", frameClause));
            gb.Prod("OverOrderClause").Is("ORDER", "BY", orderItemList, overFrameExtentOpt);
            gb.Rule("OverSpec").OneOf(
                EmptyTerm.Empty,
                overPartitionClause,
                overOrderClause,
                gb.Seq(overPartitionClause, overOrderClause));

            gb.Prod("FrameClause").Is(frameBoundary);
            gb.Prod("FrameClause").Is("BETWEEN", frameBoundary, "AND", frameBoundary);

            gb.Prod("FrameBoundary").Is("UNBOUNDED", "PRECEDING");
            gb.Prod("FrameBoundary").Is("UNBOUNDED", "FOLLOWING");
            gb.Prod("FrameBoundary").Is("CURRENT", "ROW");
            gb.Prod("FrameBoundary").Is(number, "PRECEDING");
            gb.Prod("FrameBoundary").Is(number, "FOLLOWING");

            gb.Prod("Literal").Is(number);
            gb.Prod("Literal").Is(stringLiteral);
            gb.Prod("Literal").Is("NULL");
            gb.Prod("UnicodeStringLiteral").Is("N", stringLiteral);
            gb.Prod("VariableReference").Is(variable);

            gb.Prod("CaseExpression").Is("CASE", caseWhenList, "END");
            gb.Prod("CaseExpression").Is("CASE", caseWhenList, "ELSE", expression, "END");
            gb.Prod("CaseExpression").Is("CASE", expression, caseWhenList, "END");
            gb.Prod("CaseExpression").Is("CASE", expression, caseWhenList, "ELSE", expression, "END");
            gb.Prod("CaseWhenList").Is(caseWhen);
            gb.Prod("CaseWhenList").Is(caseWhenList, caseWhen);
            gb.Prod("CaseWhen").Is("WHEN", expression, "THEN", expression);

            gb.Prod("ExpressionList").Is(expression);
            gb.Prod("ExpressionList").Is(expressionList, ",", expression);
            gb.Prod("GroupingSetList").Is(groupingSet);
            gb.Prod("GroupingSetList").Is(groupingSetList, ",", groupingSet);
            gb.Prod("GroupingSet").Is("(", expressionList, ")");
            gb.Prod("GroupingSet").Is("(", ")");
            gb.Prod("IdentifierList").Is(identifierTerm);
            gb.Prod("IdentifierList").Is(identifierList, ",", identifierTerm);

            gb.Rule("IdentifierTerm").OneOf(
                identifier,
                bracketIdentifier,
                quotedIdentifier,
                tempIdentifier,
                sqlcmdVariable);
            // contextual keywords used as identifiers in SQL Server
            gb.Rule("IdentifierTerm").Keywords(
                "TYPE",
                "OPENQUERY",
                "OPENROWSET",
                "BINARY",
                "XML",
                "JSON",
                "AUTO",
                "PATH",
                "SIZE",
                "STATISTICS",
                "AT",
                "NEXT",
                "ROWS",
                "OBJECT",
                "SCHEMA",
                "FUNCTION",
                "LOGIN",
                "DEFAULT",
                "PARTITION",
                "COLUMN",
                "CONSTRAINT",
                "HASH",
                "USER",
                "ROLE",
                "MERGE",
                "AFTER",
                "SERVER",
                "INSTEAD",
                "SCOPED",
                "CONFIGURATION",
                "CLEAR",
                "SCHEMABINDING",
                "CURRENT",
                "PARTITIONS",
                "LOOP",
                "EXTERNAL",
                "LOG",
                "PAGE",
                "N",
                "WAITFOR",
                "BULK",
                "CURSOR",
                "DELAY",
                "TIME",
                "LOGIN",
                "PASSWORD",
                "READ_ONLY",
                "ALL",
                "DATA_SOURCE",
                "SOURCE",
                "TARGET",
                "RESUME",
                "CLUSTERED",
                "NONCLUSTERED",
                "COLUMNSTORE",
                "INCLUDE",
                "MATCHED",
                "GOTO",
                "USER",
                "TYPE",
                "EXTERNAL",
                "ROWCOUNT",
                "PAGECOUNT",
                "MASTER",
                "EDGE",
                "NODE",
                "PREDICT",
                "MODEL",
                "NATIVE",
                "SCHEMABINDING",
                "DISTRIBUTED",
                "DATA",
                "SECURITY",
                "POLICY",
                "FILTER",
                "PREDICATE",
                "BLOCK",
                "GROUPING",
                "SETS",
                "PIVOT",
                "UNPIVOT",
                "LANGUAGE",
                "GRAPH");

            gb.Prod("TruncateStatement").Is("TRUNCATE", "TABLE", qualifiedName);

            gb.Prod("CreateTableAsSelectStatement").Is("CREATE", "TABLE", qualifiedName, "AS", queryExpression);
            gb.Prod("CreateTableAsSelectStatement").Is("CREATE", "TABLE", qualifiedName, "WITH", "(", createTableOptionList, ")", "AS", queryExpression);

            gb.Prod("AlterDatabaseStatement").Is("ALTER", "DATABASE", identifierTerm, "SET", alterDatabaseSetOption);
            gb.Prod("AlterDatabaseStatement").Is("ALTER", "DATABASE", "SCOPED", "CONFIGURATION", "CLEAR", identifierTerm);
            gb.Prod("AlterDatabaseStatement").Is("ALTER", "DATABASE", "SCOPED", "CONFIGURATION", "SET", identifierTerm, "=", expression);
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, identifierTerm);
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, identifierTerm, identifierTerm);
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, identifierTerm, "WITH", identifierTerm);
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "=", expression);
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "=", "ON");
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "=", "OFF");
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "=", "ON", "(", indexOptionList, ")");
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "=", "OFF", "(", indexOptionList, ")");
            gb.Prod("AlterDatabaseSetOption").Is(identifierTerm, "(", indexOptionList, ")");

            // DECLARE CURSOR
            gb.Rule("DeclareCursorStatement").OneOf(
                gb.Seq("DECLARE", identifierTerm, "CURSOR", "FOR", queryExpression),
                gb.Seq("DECLARE", identifierTerm, "CURSOR", cursorOptionList, "FOR", queryExpression));
            gb.Rule("CursorOptionList").Plus(identifierTerm);

            // OPEN/FETCH/CLOSE cursor operations
            gb.Rule("CursorOperationStatement").OneOf(
                gb.Seq("OPEN", identifierTerm),
                gb.Seq("CLOSE", identifierTerm),
                gb.Seq("DEALLOCATE", identifierTerm),
                fetchStatement);
            gb.Rule("FetchStatement").OneOf(
                gb.Seq("FETCH", "FROM", identifierTerm),
                gb.Seq("FETCH", "FROM", identifierTerm, "INTO", fetchTargetList),
                gb.Seq("FETCH", fetchDirection, "FROM", identifierTerm),
                gb.Seq("FETCH", fetchDirection, "FROM", identifierTerm, "INTO", fetchTargetList),
                gb.Seq("FETCH", identifierTerm),
                gb.Seq("FETCH", identifierTerm, "INTO", fetchTargetList));
            gb.Rule("FetchDirection").OneOf(
                "NEXT",
                "PRIOR",
                "FIRST",
                "LAST",
                gb.Seq("ABSOLUTE", expression),
                gb.Seq("RELATIVE", expression));
            gb.Rule("FetchTargetList").SeparatedBy(",", variableReference);

            // WAITFOR
            gb.Rule("WaitforStatement").OneOf(
                gb.Seq("WAITFOR", identifierTerm, expression),
                gb.Seq("WAITFOR", "DELAY", expression),
                gb.Seq("WAITFOR", "TIME", expression));

            // CREATE LOGIN
            gb.Prod("CreateLoginStatement").Is("CREATE", "LOGIN", identifierTerm, "WITH", createLoginOptionList);
            gb.Rule("CreateLoginOptionList").SeparatedBy(",", createLoginOption);
            gb.Prod("CreateLoginOption").Is(identifierTerm, "=", expression);
            gb.Prod("CreateLoginOption").Is(identifierTerm, "=", "ON");
            gb.Prod("CreateLoginOption").Is(identifierTerm, "=", "OFF");

            gb.Prod("CreateUserStatement").Is("CREATE", "USER", identifierTerm, "FOR", "LOGIN", identifierTerm);
            gb.Prod("CreateUserStatement").Is("CREATE", "USER", identifierTerm, "WITHOUT", "LOGIN");

            gb.Prod("CreateStatisticsStatement").Is("CREATE", "STATISTICS", identifierTerm, "ON", qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateStatisticsStatement").Is("CREATE", "STATISTICS", identifierTerm, "ON", qualifiedName, "(", identifierList, ")", "WITH", "(", indexOptionList, ")");

            // UPDATE STATISTICS statement
            gb.Rule("UpdateStatisticsStatement")
                .CanBe("UPDATE", "STATISTICS", qualifiedName)
                .Or("UPDATE", "STATISTICS", qualifiedName, identifierTerm)
                .Or("UPDATE", "STATISTICS", qualifiedName, "WITH", updateStatisticsOptionList)
                .Or("UPDATE", "STATISTICS", qualifiedName, identifierTerm, "WITH", updateStatisticsOptionList);
            gb.Rule("UpdateStatisticsOptionList").SeparatedBy(",", updateStatisticsOption);
            gb.Rule("UpdateStatisticsOption")
                .CanBe(identifierTerm)
                .Or(identifierTerm, "=", expression)
                .Or("ROWCOUNT", "=", expression)
                .Or("PAGECOUNT", "=", expression);

            gb.Prod("DropTypeStatement").Is("DROP", "TYPE", qualifiedName);
            gb.Prod("DropTypeStatement").Is("DROP", "TYPE", "IF", "EXISTS", qualifiedName);

            gb.Prod("DropColumnEncryptionKeyStatement").Is("DROP", "COLUMN", "ENCRYPTION", "KEY", identifierTerm);
            gb.Prod("DropColumnEncryptionKeyStatement").Is("DROP", "COLUMN", "MASTER", "KEY", identifierTerm);

            gb.Rule("RevertStatement").OneOf(
                "REVERT",
                gb.Seq("REVERT", "WITH", "COOKIE", "=", expression));
            gb.Rule("IdentifierTerm").Keywords("REVERT");

            // DROP EVENT SESSION ON DATABASE/SERVER
            gb.Rule("DropEventSessionStatement").OneOf(
                gb.Seq("DROP", "EVENT", "SESSION", identifierTerm, "ON", "DATABASE"),
                gb.Seq("DROP", "EVENT", "SESSION", identifierTerm, "ON", "SERVER"));
            gb.Rule("IdentifierTerm").Keywords("EVENT", "SESSION");

            // CREATE TYPE ... AS TABLE
            gb.Prod("CreateTypeStatement").Is("CREATE", "TYPE", qualifiedName, "AS", tableTypeDefinition);
            gb.Prod("CreateTypeStatement").Is("CREATE", "TYPE", qualifiedName, "FROM", typeSpec);

            // MERGE DML statement
            gb.Rule("MergeStatement").OneOf(
                gb.Seq("MERGE", tableFactor, "USING", tableFactor, "ON", searchCondition, mergeWhenList),
                gb.Seq("MERGE", "INTO", tableFactor, "USING", tableFactor, "ON", searchCondition, mergeWhenList),
                gb.Seq("MERGE", "TOP", topValue, tableFactor, "USING", tableFactor, "ON", searchCondition, mergeWhenList),
                gb.Seq("MERGE", "TOP", topValue, "INTO", tableFactor, "USING", tableFactor, "ON", searchCondition, mergeWhenList));
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

            // BULK INSERT
            gb.Prod("BulkInsertStatement").Is("BULK", "INSERT", qualifiedName, "FROM", expression);
            gb.Prod("BulkInsertStatement").Is("BULK", "INSERT", qualifiedName, "FROM", expression, "WITH", "(", tableHintLimitedList, ")");

            gb.Prod("CheckpointStatement").Is("CHECKPOINT");

            // SQL Graph: MATCH search condition (as PrimaryExpression so AND works after it)
            gb.Prod("PrimaryExpression").Is("MATCH", "(", matchGraphPattern, ")");
            gb.Prod("MatchGraphPattern").Is(matchGraphPath);
            gb.Prod("MatchGraphPattern").Is("SHORTEST_PATH", "(", matchGraphShortestPath, ")");
            gb.Prod("MatchGraphPattern").Is(matchGraphPattern, "AND", matchGraphPath);
            gb.Prod("MatchGraphPattern").Is(matchGraphPattern, "AND", "SHORTEST_PATH", "(", matchGraphShortestPath, ")");
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
            gb.Prod("FunctionCall").Is("PREDICT", "(", predictArgList, ")");
            gb.Prod("PredictArgList").Is(predictArg);
            gb.Prod("PredictArgList").Is(predictArgList, ",", predictArg);
            gb.Prod("PredictArg").Is(identifierTerm, "=", expression);
            gb.Prod("PredictArg").Is(identifierTerm, "=", expression, "AS", identifierTerm);

            gb.Prod("QualifiedName").Is(identifierTerm);
            gb.Prod("QualifiedName").Is(qualifiedName, ".", identifierTerm);
            gb.Prod("QualifiedName").Is(qualifiedName, ".", graphColumnRef);
            gb.Prod("QualifiedName").Is(qualifiedName, ".", ".", identifierTerm); // double-dot: master..table

            // CREATE/ALTER SECURITY POLICY
            gb.Prod("CreateSecurityPolicyStatement").Is("CREATE", "SECURITY", "POLICY", qualifiedName, securityPolicyClauseList);
            gb.Prod("CreateSecurityPolicyStatement").Is("CREATE", "SECURITY", "POLICY", qualifiedName, securityPolicyClauseList, "WITH", "(", securityPolicyOptionList, ")");
            gb.Prod("AlterSecurityPolicyStatement").Is("ALTER", "SECURITY", "POLICY", qualifiedName, securityPolicyClauseList);
            gb.Prod("AlterSecurityPolicyStatement").Is("ALTER", "SECURITY", "POLICY", qualifiedName, securityPolicyClauseList, "WITH", "(", securityPolicyOptionList, ")");
            gb.Prod("SecurityPolicyClauseList").Is(securityPolicyClause);
            gb.Prod("SecurityPolicyClauseList").Is(securityPolicyClauseList, ",", securityPolicyClause);
            gb.Rule("SecurityPolicyClause")
                .CanBe("ADD", "FILTER", "PREDICATE", functionCall, "ON", qualifiedName)
                .Or("ADD", "BLOCK", "PREDICATE", functionCall, "ON", qualifiedName)
                .Or("DROP", "FILTER", "PREDICATE", "ON", qualifiedName)
                .Or("DROP", "BLOCK", "PREDICATE", "ON", qualifiedName);
            gb.Prod("SecurityPolicyOptionList").Is(securityPolicyOption);
            gb.Prod("SecurityPolicyOptionList").Is(securityPolicyOptionList, ",", securityPolicyOption);
            gb.Prod("SecurityPolicyOption").Is(identifierTerm, "=", "ON");
            gb.Prod("SecurityPolicyOption").Is(identifierTerm, "=", "OFF");
            gb.Prod("SecurityPolicyOption").Is(identifierTerm, "=", expression);

            // CREATE EXTERNAL TABLE
            gb.Prod("CreateExternalTableStatement").Is("CREATE", "EXTERNAL", "TABLE", qualifiedName, "(", createTableElementList, ")");
            gb.Prod("CreateExternalTableStatement").Is("CREATE", "EXTERNAL", "TABLE", qualifiedName, "(", createTableElementList, ")", "WITH", "(", tableHintLimitedList, ")");

            // CREATE EXTERNAL DATA SOURCE name WITH (TYPE=..., LOCATION=..., ...)
            gb.Prod("CreateExternalDataSourceStatement").Is("CREATE", "EXTERNAL", "DATA", "SOURCE", identifierTerm, "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateExternalDataSourceStatement").Is("CREATE", "EXTERNAL", "DATA", "SOURCE", qualifiedName, "WITH", "(", indexOptionList, ")");

            return gb.BuildGrammar("Start");
        }
    }
}

