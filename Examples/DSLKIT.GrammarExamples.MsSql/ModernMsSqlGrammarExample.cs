using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Formatting;
using DSLKIT.Lexer;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using DSLKIT.Tokens;

namespace DSLKIT.GrammarExamples.MsSql
{
    [Flags]
    public enum MsSqlDialectFeatures
    {
        SqlServerCore = 1,
        ExternalObjects = 2,
        SynapseExtensions = 4,
        GraphExtensions = 8,
        SqlCmdPreprocessing = 16,
        All = SqlServerCore | ExternalObjects | SynapseExtensions | GraphExtensions | SqlCmdPreprocessing
    }

    /// <summary>
    /// SQL Server 2022 / Azure SQL query-language grammar subset.
    /// Focus: SELECT, CTE, joins, set operators, window functions and CASE.
    /// </summary>
    public static class ModernMsSqlGrammarExample
    {
        private static readonly ConcurrentDictionary<MsSqlDialectFeatures, Lazy<GrammarResources>> GrammarCache = new();

        public static IGrammar BuildGrammar()
        {
            return BuildGrammar(MsSqlDialectFeatures.All);
        }

        public static IGrammar BuildGrammar(MsSqlDialectFeatures dialectFeatures)
        {
            return GetGrammarResources(dialectFeatures).Grammar;
        }

        public static ParseResult ParseBatch(string source)
        {
            return ParseBatch(source, MsSqlDialectFeatures.All);
        }

        public static ParseResult ParseBatch(string source, MsSqlDialectFeatures dialectFeatures)
        {
            ArgumentNullException.ThrowIfNull(source);

            var grammarResources = GetGrammarResources(dialectFeatures);
            var lexer = new Lexer.Lexer(grammarResources.LexerSettings);
            var parser = new SyntaxParser(grammarResources.Grammar);

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
            // EOF is excluded from significant tokens, so trivia-only batches land here.
            if (tokens.Count == 0)
            {
                return CreateEmptyScriptParseResult(grammarResources);
            }

            return parser.Parse(tokens);
        }

        public static SqlScriptDocumentParseResult ParseDocument(string source)
        {
            return ParseDocument(source, MsSqlDialectFeatures.All);
        }

        public static SqlScriptDocumentParseResult ParseDocument(string source, MsSqlDialectFeatures dialectFeatures)
        {
            ArgumentNullException.ThrowIfNull(source);

            var segments = SqlServerScriptPreprocessor.Split(
                source,
                HasFeature(dialectFeatures, MsSqlDialectFeatures.SqlCmdPreprocessing));
            if (segments.Count == 0)
            {
                var emptyBatchParseResult = ParseBatch(source, dialectFeatures);
                if (!emptyBatchParseResult.IsSuccess)
                {
                    return new SqlScriptDocumentParseResult
                    {
                        Error = emptyBatchParseResult.Error
                    };
                }

                return new SqlScriptDocumentParseResult
                {
                    Document = new SqlScriptDocument
                    {
                        Segments =
                        [
                            new SqlBatchDocumentNode
                            {
                                StartPosition = 0,
                                Text = source,
                                ParseResult = emptyBatchParseResult
                            }
                        ]
                    }
                };
            }

            var documentNodes = new List<SqlScriptDocumentNode>(segments.Count);
            foreach (var segment in segments)
            {
                if (segment.Kind == SqlScriptSegmentKind.Batch)
                {
                    var batchParseResult = ParseBatch(segment.Text, dialectFeatures);
                    if (!batchParseResult.IsSuccess)
                    {
                        return new SqlScriptDocumentParseResult
                        {
                            Error = OffsetParseError(batchParseResult, segment.StartPosition).Error
                        };
                    }

                    documentNodes.Add(new SqlBatchDocumentNode
                    {
                        StartPosition = segment.StartPosition,
                        Text = segment.Text,
                        ParseResult = batchParseResult
                    });
                    continue;
                }

                if (segment.Kind == SqlScriptSegmentKind.BatchSeparator)
                {
                    documentNodes.Add(new SqlBatchSeparatorDocumentNode
                    {
                        StartPosition = segment.StartPosition,
                        Text = segment.Text,
                        RepeatCount = segment.BatchRepeatCount
                    });
                    continue;
                }

                documentNodes.Add(new SqlcmdCommandDocumentNode
                {
                    StartPosition = segment.StartPosition,
                    Text = segment.Text
                });
            }

            return new SqlScriptDocumentParseResult
            {
                Document = new SqlScriptDocument
                {
                    Segments = documentNodes
                }
            };
        }

        public static void ParseBatchOrThrow(string source)
        {
            var parseResult = ParseBatch(source);
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

        private static ParseResult OffsetParseError(ParseResult parseResult, int startPosition)
        {
            if (parseResult.Error == null)
            {
                return parseResult;
            }

            return new ParseResult
            {
                Error = parseResult.Error with
                {
                    ErrorPosition = parseResult.Error.ErrorPosition + startPosition
                }
            };
        }

        private static IReadOnlyList<IToken> BuildSignificantTokensWithTrivia(IReadOnlyList<IToken> rawTokens)
        {
            var significantTokens = new List<IToken>(rawTokens.Count);
            var pendingTrivia = new List<IToken>();

            foreach (var token in rawTokens)
            {
                if (IsTriviaToken(token))
                {
                    pendingTrivia.Add(token);
                    continue;
                }

                if (token.Terminal is IEofTerminal)
                {
                    if (pendingTrivia.Count > 0 && significantTokens.Count > 0)
                    {
                        significantTokens[^1] = WithTrailingTrivia(significantTokens[^1], pendingTrivia);
                        pendingTrivia.Clear();
                    }

                    continue;
                }

                if (pendingTrivia.Count == 0)
                {
                    significantTokens.Add(token);
                    continue;
                }

                var tokenWithLeadingTrivia = WithLeadingTrivia(token, pendingTrivia);
                significantTokens.Add(tokenWithLeadingTrivia);
                pendingTrivia.Clear();
            }

            if (pendingTrivia.Count == 0 || significantTokens.Count == 0)
            {
                return significantTokens;
            }

            var lastToken = significantTokens[^1];
            significantTokens[^1] = WithTrailingTrivia(lastToken, pendingTrivia);
            return significantTokens;
        }

        private static IGrammar BuildGrammarCore(MsSqlDialectFeatures dialectFeatures)
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

            var forSystemTimeStart = new ContextualKeywordTerminal(
                "FOR",
                "FOR_SYSTEM_TIME_START",
                "SYSTEM_TIME");

            var forPathStart = new ContextualKeywordTerminal(
                "FOR",
                "FOR_PATH_START",
                "PATH");

            var withCheckOptionStart = new ContextualKeywordTerminal(
                "WITH",
                "WITH_CHECK_OPTION_START",
                "CHECK",
                "OPTION");

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
                .AddTerminal(forSystemTimeStart)
                .AddTerminal(forPathStart)
                .AddTerminal(withCheckOptionStart);
            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.GraphExtensions))
            {
                gb.AddTerminal(graphColumnRef);
            }

            // Resolve known LALR(1) ambiguities explicitly.
            gb.OnShiftReduce("IfStatement", "ELSE", Resolve.Shift); // dangling ELSE binds to nearest IF
            gb.OnShiftReduce("QueryUnionExpression", "UNION", Resolve.Reduce);
            gb.OnShiftReduce("QueryUnionExpression", "EXCEPT", Resolve.Reduce);
            gb.OnShiftReduce("QueryIntersectExpression", "INTERSECT", Resolve.Reduce);
            var script = gb.NT("Script");
            var statementList = gb.NT("StatementList");
            var statementListOpt = gb.NT("StatementListOpt");
            var statementSeparator = gb.NT("StatementSeparator");
            var statementSeparatorList = gb.NT("StatementSeparatorList");
            var statement = gb.NT("Statement");
            var statementNoLeadingWith = gb.NT("StatementNoLeadingWith");
            var implicitStatementNoLeadingWith = gb.NT("ImplicitStatementNoLeadingWith");
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
            var dmlOutputClause = gb.NT("DeleteOutputClause");
            var dmlOutputTarget = gb.NT("DeleteOutputTarget");
            var dmlOutputIntoColumnListOpt = gb.NT("DeleteOutputIntoColumnListOpt");
            var deleteSourceFromClause = gb.NT("DeleteSourceFromClause");
            var deleteWhereClause = gb.NT("DeleteWhereClause");
            var queryOptionClause = gb.NT("DeleteOptionClause");
            var queryHintList = gb.NT("DeleteQueryHintList");
            var queryHint = gb.NT("DeleteQueryHint");
            var queryHintName = gb.NT("DeleteQueryHintName");
            var optionClause = gb.NT("OptionClause");
            var ifStatement = gb.NT("IfStatement");
            var ifBranchStatement = gb.NT("IfBranchStatement");
            var beginEndStatement = gb.NT("BeginEndStatement");
            var setStatement = gb.NT("SetStatement");
            var setOptionName = gb.NT("SetOptionName");
            var setStatisticsOption = gb.NT("SetStatisticsOption");
            var setTransactionIsolationLevel = gb.NT("SetTransactionIsolationLevel");
            var printStatement = gb.NT("PrintStatement");
            var declareStatement = gb.NT("DeclareStatement");
            var declareItemList = gb.NT("DeclareItemList");
            var declareItem = gb.NT("DeclareItem");
            var declareTableVariable = gb.NT("DeclareTableVariable");
            var tableTypeDefinition = gb.NT("TableTypeDefinition");
            var typeSpec = gb.NT("TypeSpec");
            var typeArgument = gb.NT("TypeArgument");
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
            var createFunctionParameterList = gb.NT("CreateFunctionParameterList");
            var createFunctionParameter = gb.NT("CreateFunctionParameter");
            var createFunctionParameterOptionList = gb.NT("CreateFunctionParameterOptionList");
            var createFunctionParameterOption = gb.NT("CreateFunctionParameterOption");
            var createFunctionScalarReturnsClause = gb.NT("CreateFunctionScalarReturnsClause");
            var createFunctionInlineTableReturnsClause = gb.NT("CreateFunctionInlineTableReturnsClause");
            var createFunctionTableVariableReturnsClause = gb.NT("CreateFunctionTableVariableReturnsClause");
            var createFunctionWithClause = gb.NT("CreateFunctionWithClause");
            var createFunctionOptionList = gb.NT("CreateFunctionOptionList");
            var createFunctionOption = gb.NT("CreateFunctionOption");
            var createFunctionPreludeStatement = gb.NT("CreateFunctionPreludeStatement");
            var createFunctionPreludeStatementNoLeadingWith = gb.NT("CreateFunctionPreludeStatementNoLeadingWith");
            var createFunctionImplicitPreludeStatementNoLeadingWith = gb.NT("CreateFunctionImplicitPreludeStatementNoLeadingWith");
            var createFunctionPreludeStatementList = gb.NT("CreateFunctionPreludeStatementList");
            var createFunctionPreludeBeforeReturnOpt = gb.NT("CreateFunctionPreludeBeforeReturnOpt");
            var createFunctionBodyTrailingSeparatorsOpt = gb.NT("CreateFunctionBodyTrailingSeparatorsOpt");
            var createFunctionScalarReturnStatement = gb.NT("CreateFunctionScalarReturnStatement");
            var createFunctionTableVariableReturnStatement = gb.NT("CreateFunctionTableVariableReturnStatement");
            var createFunctionScalarBody = gb.NT("CreateFunctionScalarBody");
            var createFunctionInlineTableBody = gb.NT("CreateFunctionInlineTableBody");
            var createFunctionTableVariableBody = gb.NT("CreateFunctionTableVariableBody");
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
            var raiserrorWithOption = gb.NT("RaiserrorWithOption");
            var throwStatement = gb.NT("ThrowStatement");
            var loopControlStatement = gb.NT("LoopControlStatement");
            var gotoStatement = gb.NT("GotoStatement");
            var labelStatement = gb.NT("LabelStatement");
            var labelOnlyStatement = gb.NT("LabelOnlyStatement");
            var createRoleStatement = gb.NT("CreateRoleStatement");
            var createSchemaStatement = gb.NT("CreateSchemaStatement");
            var schemaNameClause = gb.NT("SchemaNameClause");
            var createViewHead = gb.NT("CreateViewHead");
            var createViewStatement = gb.NT("CreateViewStatement");
            var createViewColumnList = gb.NT("CreateViewColumnList");
            var createViewOptionClause = gb.NT("CreateViewOptionClause");
            var createViewBody = gb.NT("CreateViewBody");
            var createViewQuery = gb.NT("CreateViewQuery");
            var createViewCheckOptionOpt = gb.NT("CreateViewCheckOptionOpt");
            var createViewOptionList = gb.NT("CreateViewOptionList");
            var createViewOption = gb.NT("CreateViewOption");
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
            var createTableColumnConstraintBody = gb.NT("CreateTableColumnConstraintBody");
            var createTableTableConstraintBody = gb.NT("CreateTableTableConstraintBody");
            var createTableColumnKeyClusterType = gb.NT("CreateTableColumnKeyClusterType");
            var createTableConstraintClusterType = gb.NT("CreateTableConstraintClusterType");
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
            var createTableDurabilityMode = gb.NT("CreateTableDurabilityMode");
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
            var createKeyListIndexHead = gb.NT("CreateKeyListIndexHead");
            var createKeylessIndexHead = gb.NT("CreateKeylessIndexHead");
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
            var namedOptionValue = gb.NT("NamedOptionValue");
            var maskingOptionList = gb.NT("MaskingOptionList");
            var encryptionOptionList = gb.NT("EncryptionOptionList");
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
            var createDatabaseOnFilespecSequence = gb.NT("CreateDatabaseOnFilespecSequence");
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
            var alterDatabaseSetOnOffOption = gb.NT("AlterDatabaseSetOnOffOption");
            var alterDatabaseSetEqualsOnOffOption = gb.NT("AlterDatabaseSetEqualsOnOffOption");
            var alterDatabaseSetModeOption = gb.NT("AlterDatabaseSetModeOption");
            var alterDatabaseTerminationClause = gb.NT("AlterDatabaseTerminationClause");
            var alterDatabaseTerminationOpt = gb.NT("AlterDatabaseTerminationOpt");
            var alterDatabaseRecoveryModel = gb.NT("AlterDatabaseRecoveryModel");
            var alterDatabasePageVerifyMode = gb.NT("AlterDatabasePageVerifyMode");
            var alterDatabaseCursorDefaultMode = gb.NT("AlterDatabaseCursorDefaultMode");
            var alterDatabaseParameterizationMode = gb.NT("AlterDatabaseParameterizationMode");
            var alterDatabaseTargetRecoveryUnit = gb.NT("AlterDatabaseTargetRecoveryUnit");
            var alterDatabaseDelayedDurabilityMode = gb.NT("AlterDatabaseDelayedDurabilityMode");
            var declareCursorStatement = gb.NT("DeclareCursorStatement");
            var cursorReference = gb.NT("CursorReference");
            var cursorOptionList = gb.NT("CursorOptionList");
            var cursorOption = gb.NT("CursorOption");
            var cursorOperationStatement = gb.NT("CursorOperationStatement");
            var fetchStatement = gb.NT("FetchStatement");
            var fetchDirection = gb.NT("FetchDirection");
            var fetchTargetList = gb.NT("FetchTargetList");
            var waitforStatement = gb.NT("WaitforStatement");
            var waitforTimeValue = gb.NT("WaitforTimeValue");
            var createLoginStatement = gb.NT("CreateLoginStatement");
            var createLoginPasswordSpec = gb.NT("CreateLoginPasswordSpec");
            var createLoginOptionList = gb.NT("CreateLoginOptionList");
            var createLoginOption = gb.NT("CreateLoginOption");
            var createLoginWindowsOptionList = gb.NT("CreateLoginWindowsOptionList");
            var createLoginWindowsOption = gb.NT("CreateLoginWindowsOption");
            var bulkInsertStatement = gb.NT("BulkInsertStatement");
            var bulkInsertOptionList = gb.NT("BulkInsertOptionList");
            var checkpointStatement = gb.NT("CheckpointStatement");
            var createUserStatement = gb.NT("CreateUserStatement");
            var createStatisticsStatement = gb.NT("CreateStatisticsStatement");
            var updateStatisticsStatement = gb.NT("UpdateStatisticsStatement");
            var updateStatisticsOptionList = gb.NT("UpdateStatisticsOptionList");
            var updateStatisticsOption = gb.NT("UpdateStatisticsOption");
            var updateStatisticsSimpleOption = gb.NT("UpdateStatisticsSimpleOption");
            var updateStatisticsOnOffOptionName = gb.NT("UpdateStatisticsOnOffOptionName");
            var dropTypeStatement = gb.NT("DropTypeStatement");
            var dropColumnEncryptionKeyStatement = gb.NT("DropColumnEncryptionKeyStatement");
            var matchGraphPattern = gb.NT("MatchGraphPattern");
            var matchGraphPath = gb.NT("MatchGraphPath");
            var matchGraphStep = gb.NT("MatchGraphStep");
            var matchGraphStepChain = gb.NT("MatchGraphStepChain");
            var matchGraphShortestPath = gb.NT("MatchGraphShortestPath");
            var matchGraphShortestPathBody = gb.NT("MatchGraphShortestPathBody");
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
            var securityPolicyOptionName = gb.NT("SecurityPolicyOptionName");
            var createExternalTableStatement = gb.NT("CreateExternalTableStatement");
            var externalTableOptionList = gb.NT("ExternalTableOptionList");
            var createExternalDataSourceStatement = gb.NT("CreateExternalDataSourceStatement");
            var externalDataSourceOptionList = gb.NT("ExternalDataSourceOptionList");
            var mergeStatement = gb.NT("MergeStatement");
            var mergeTargetTable = gb.NT("MergeTargetTable");
            var mergeSourceTable = gb.NT("MergeSourceTable");
            var mergeOutputClauseOpt = gb.NT("MergeOutputClauseOpt");
            var mergeOptionClauseOpt = gb.NT("MergeOptionClauseOpt");
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
            var implicitQueryStatementNoLeadingWith = gb.NT("ImplicitQueryStatementNoLeadingWith");
            var implicitQueryExpression = gb.NT("ImplicitQueryExpression");
            var implicitQueryUnionExpression = gb.NT("ImplicitQueryUnionExpression");
            var implicitQueryIntersectExpression = gb.NT("ImplicitQueryIntersectExpression");
            var queryExpression = gb.NT("QueryExpression");
            var queryUnionExpression = gb.NT("QueryUnionExpression");
            var queryIntersectExpression = gb.NT("QueryIntersectExpression");
            var setOperator = gb.NT("SetOperator");
            var queryPrimary = gb.NT("QueryPrimary");
            var queryExpressionTail = gb.NT("QueryExpressionTail");
            var queryExpressionOrderByAndOffsetOpt = gb.NT("QueryExpressionOrderByAndOffsetOpt");
            var queryExpressionForOpt = gb.NT("QueryExpressionForOpt");
            var queryExpressionOptionOpt = gb.NT("QueryExpressionOptionOpt");
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
            var unpivotClause = gb.NT("UnpivotClause");
            var unpivotColumnList = gb.NT("UnpivotColumnList");
            var openJsonCall = gb.NT("OpenJsonCall");
            var openJsonWithClause = gb.NT("OpenJsonWithClause");
            var openJsonColumnList = gb.NT("OpenJsonColumnList");
            var openJsonColumnDef = gb.NT("OpenJsonColumnDef");
            var openJsonPath = gb.NT("OpenJsonPath");
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
            var booleanOrExpression = gb.NT("BooleanOrExpression");
            var booleanAndExpression = gb.NT("BooleanAndExpression");
            var booleanNotExpression = gb.NT("BooleanNotExpression");
            var booleanPrimary = gb.NT("BooleanPrimary");
            var comparisonOperator = gb.NT("ComparisonOperator");
            var inPredicateValue = gb.NT("InPredicateValue");
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
            var iifArgumentList = gb.NT("IifArgumentList");
            var literal = gb.NT("Literal");
            var unicodeStringLiteral = gb.NT("UnicodeStringLiteral");
            var caseExpression = gb.NT("CaseExpression");
            var caseWhenList = gb.NT("CaseWhenList");
            var caseWhen = gb.NT("CaseWhen");
            var expressionList = gb.NT("ExpressionList");
            var identifierList = gb.NT("IdentifierList");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var identifierTerm = gb.NT("IdentifierTerm");
            var strictQualifiedName = gb.NT("StrictQualifiedName");
            var qualifiedName = gb.NT("QualifiedName");
            var variableReference = gb.NT("VariableReference");
            var forClause = gb.NT("ForClause");
            var forJsonMode = gb.NT("ForJsonMode");
            var forJsonOptionList = gb.NT("ForJsonOptionList");
            var forJsonOption = gb.NT("ForJsonOption");
            var forXmlMode = gb.NT("ForXmlMode");
            var forXmlOptionList = gb.NT("ForXmlOptionList");
            var forXmlOption = gb.NT("ForXmlOption");
            var symbols = new MsSqlGrammarSymbols
            {
                ImplicitQueryExpression = implicitQueryExpression,
                ImplicitQueryUnionExpression = implicitQueryUnionExpression,
                ImplicitQueryIntersectExpression = implicitQueryIntersectExpression,
                QueryExpression = queryExpression,
                QueryUnionExpression = queryUnionExpression,
                QueryIntersectExpression = queryIntersectExpression,
                SetOperator = setOperator,
                QueryExpressionTail = queryExpressionTail,
                QueryExpressionOrderByAndOffsetOpt = queryExpressionOrderByAndOffsetOpt,
                QueryExpressionForOpt = queryExpressionForOpt,
                QueryExpressionOptionOpt = queryExpressionOptionOpt,
                QueryPrimary = queryPrimary,
                QuerySpecification = querySpecification,
                QuerySpecificationWhereClause = querySpecificationWhereClause,
                QuerySpecificationHavingOpt = querySpecificationHavingOpt,
                QuerySpecificationGroupByWithOpt = querySpecificationGroupByWithOpt,
                QuerySpecificationGroupByExpressionList = querySpecificationGroupByExpressionList,
                QuerySpecificationGroupByGroupingSets = querySpecificationGroupByGroupingSets,
                QuerySpecificationGroupByClause = querySpecificationGroupByClause,
                ForClause = forClause,
                ForJsonMode = forJsonMode,
                ForJsonOptionList = forJsonOptionList,
                ForJsonOption = forJsonOption,
                ForXmlMode = forXmlMode,
                ForXmlOptionList = forXmlOptionList,
                ForXmlOption = forXmlOption,
                SelectCorePrefix = selectCorePrefix,
                SelectCoreTail = selectCoreTail,
                SelectCoreIntoClause = selectCoreIntoClause,
                SelectCore = selectCore,
                SetQuantifier = setQuantifier,
                TopClauseTail = topClauseTail,
                TopClause = topClause,
                TopValue = topValue,
                SelectList = selectList,
                SelectItemList = selectItemList,
                SelectItem = selectItem,
                OrderByClause = orderByClause,
                OrderItemList = orderItemList,
                OrderItem = orderItem,
                OffsetFetchClause = offsetFetchClause,
                Expression = expression,
                ExpressionList = expressionList,
                GroupingSetList = groupingSetList,
                GroupingSet = groupingSet,
                IdentifierList = identifierList,
                IdentifierTerm = identifierTerm,
                StrictIdentifierTerm = strictIdentifierTerm,
                StrictQualifiedName = strictQualifiedName,
                QualifiedName = qualifiedName,
                VariableReference = variableReference,
                SearchCondition = searchCondition,
                BooleanOrExpression = booleanOrExpression,
                BooleanAndExpression = booleanAndExpression,
                BooleanNotExpression = booleanNotExpression,
                BooleanPrimary = booleanPrimary,
                InPredicateValue = inPredicateValue,
                ComparisonOperator = comparisonOperator,
                AdditiveExpression = additiveExpression,
                MultiplicativeExpression = multiplicativeExpression,
                UnaryExpression = unaryExpression,
                CollateExpression = collateExpression,
                PrimaryExpression = primaryExpression,
                FunctionCall = functionCall,
                FunctionArgumentList = functionArgumentList,
                IifArgumentList = iifArgumentList,
                Literal = literal,
                UnicodeStringLiteral = unicodeStringLiteral,
                CaseExpression = caseExpression,
                CaseWhenList = caseWhenList,
                CaseWhen = caseWhen,
                OverClause = overClause,
                OverSpec = overSpec,
                OverPartitionClause = overPartitionClause,
                OverFrameExtentOpt = overFrameExtentOpt,
                OverOrderClause = overOrderClause,
                FrameClause = frameClause,
                FrameBoundary = frameBoundary,
                GraphWithinGroupClause = graphWithinGroupClause,
                TypeSpec = typeSpec,
                OpenRowsetBulk = openRowsetBulk,
                OpenRowsetBulkOptionList = openRowsetBulkOptionList,
                OpenRowsetBulkOption = openRowsetBulkOption,
                NamedOptionValue = namedOptionValue,
                CreateTableKeyColumnList = createTableKeyColumnList,
                MatchGraphPattern = matchGraphPattern,
                TableSourceList = tableSourceList,
                TableSource = tableSource,
                TableFactor = tableFactor,
                TableHintLimitedList = tableHintLimitedList,
                TemporalClause = temporalClause,
                InsertColumnList = insertColumnList,
                RowValueList = rowValueList,
                OpenJsonCall = openJsonCall,
                OpenJsonWithClause = openJsonWithClause,
                OpenJsonColumnList = openJsonColumnList,
                OpenJsonColumnDef = openJsonColumnDef,
                OpenJsonPath = openJsonPath,
                JoinPart = joinPart,
                JoinType = joinType,
                PivotClause = pivotClause,
                PivotValueList = pivotValueList,
                UnpivotClause = unpivotClause,
                UnpivotColumnList = unpivotColumnList,
                CompoundAssignOp = compoundAssignOp,
                OptionClause = optionClause,
                IndexOptionList = indexOptionList,
                IndexOnOffValue = indexOnOffValue
            };
            var grammarContext = new MsSqlGrammarContext(
                gb,
                dialectFeatures,
                symbols,
                identifier,
                bracketIdentifier,
                quotedIdentifier,
                tempIdentifier,
                variable,
                sqlcmdVariable,
                number,
                stringLiteral,
                forSystemTimeStart,
                forPathStart,
                graphColumnRef);
            var schemaDdlSymbols = new MsSqlSchemaDdlSymbols
            {
                CreateTableFileTableClause = createTableFileTableClause,
                CreateTableStatement = createTableStatement,
                CreateIndexFileStreamClause = createIndexFileStreamClause,
                CreateTableOptions = createTableOptions,
                CreateTableElementList = createTableElementList,
                CreateTableTailClauseList = createTableTailClauseList,
                CreateTableTailClause = createTableTailClause,
                CreateTablePeriodClause = createTablePeriodClause,
                CreateTableOnClause = createTableOnClause,
                CreateTableTextImageClause = createTableTextImageClause,
                CreateTableElement = createTableElement,
                CreateTableColumnDefinition = createTableColumnDefinition,
                CreateTableComputedColumn = createTableComputedColumn,
                CreateTableColumnSet = createTableColumnSet,
                CreateTableConstraint = createTableConstraint,
                CreateTableTableIndex = createTableTableIndex,
                CreateTableColumnOptionList = createTableColumnOptionList,
                CreateTableColumnOption = createTableColumnOption,
                MaskingOptionList = maskingOptionList,
                EncryptionOptionList = encryptionOptionList,
                CreateTableColumnConstraintBody = createTableColumnConstraintBody,
                CreateTableTableConstraintBody = createTableTableConstraintBody,
                CreateTableColumnKeyClusterType = createTableColumnKeyClusterType,
                CreateTableConstraintClusterType = createTableConstraintClusterType,
                CreateTableClusterType = createTableClusterType,
                CreateTableKeyColumn = createTableKeyColumn,
                CreateIndexWithClause = createIndexWithClause,
                CreateTableOptionList = createTableOptionList,
                CreateTableOption = createTableOption,
                CreateTableDurabilityMode = createTableDurabilityMode,
                AlterTableStatement = alterTableStatement,
                AlterTableAction = alterTableAction,
                AlterTableAddItemList = alterTableAddItemList,
                AlterTableAddItem = alterTableAddItem,
                AlterTableAlterColumnAction = alterTableAlterColumnAction,
                AlterTableColumnOptionList = alterTableColumnOptionList,
                AlterTableColumnOption = alterTableColumnOption,
                AlterTableDropItemList = alterTableDropItemList,
                AlterTableDropItem = alterTableDropItem,
                AlterTableCheckMode = alterTableCheckMode,
                AlterTableConstraintTarget = alterTableConstraintTarget,
                AlterTableTriggerTarget = alterTableTriggerTarget,
                CreateKeyListIndexHead = createKeyListIndexHead,
                CreateKeylessIndexHead = createKeylessIndexHead,
                CreateIndexStatement = createIndexStatement,
                CreateIndexKeyList = createIndexKeyList,
                CreateIndexKeyItem = createIndexKeyItem,
                CreateIndexTailClauseList = createIndexTailClauseList,
                CreateIndexTailClause = createIndexTailClause,
                CreateIndexIncludeClause = createIndexIncludeClause,
                CreateIndexIncludeList = createIndexIncludeList,
                CreateIndexWhereClause = createIndexWhereClause,
                CreateIndexStorageClause = createIndexStorageClause,
                IndexStorageTarget = indexStorageTarget,
                IndexFileStreamTarget = indexFileStreamTarget,
                IndexOption = indexOption,
                IndexOptionName = indexOptionName,
                IndexOptionValue = indexOptionValue,
                IndexPartitionList = indexPartitionList,
                IndexPartitionItem = indexPartitionItem,
                AlterIndexStatement = alterIndexStatement,
                AlterIndexTarget = alterIndexTarget,
                AlterIndexAction = alterIndexAction,
                AlterIndexRebuildSpec = alterIndexRebuildSpec,
                AlterIndexReorganizeSpec = alterIndexReorganizeSpec,
                AlterIndexResumeSpec = alterIndexResumeSpec,
                AlterIndexPartitionSelector = alterIndexPartitionSelector,
                CreateDatabaseStatement = createDatabaseStatement,
                CreateDatabaseClauseList = createDatabaseClauseList,
                CreateDatabaseClause = createDatabaseClause,
                CreateDatabaseContainmentClause = createDatabaseContainmentClause,
                CreateDatabaseOnClause = createDatabaseOnClause,
                CreateDatabaseOnFilespecSequence = createDatabaseOnFilespecSequence,
                CreateDatabaseFilespec = createDatabaseFilespec,
                CreateDatabaseFilegroup = createDatabaseFilegroup,
                CreateDatabaseFilespecList = createDatabaseFilespecList,
                CreateDatabaseCollateClause = createDatabaseCollateClause,
                CreateDatabaseWithClause = createDatabaseWithClause,
                CreateDatabaseOptionList = createDatabaseOptionList,
                CreateDatabaseOption = createDatabaseOption,
                CreateDatabaseOptionValue = createDatabaseOptionValue,
                CreateDatabaseOnOffValue = createDatabaseOnOffValue,
                CreateDatabaseFilestreamOptionList = createDatabaseFilestreamOptionList,
                CreateDatabaseFilestreamOption = createDatabaseFilestreamOption,
                CreateDatabaseNonTransactedAccessValue = createDatabaseNonTransactedAccessValue,
                CreateDatabaseFileName = createDatabaseFileName,
                CreateDatabaseFilespecOptionList = createDatabaseFilespecOptionList,
                CreateDatabaseFilespecOption = createDatabaseFilespecOption,
                CreateDatabaseSizeSpec = createDatabaseSizeSpec,
                CreateDatabaseMaxSizeSpec = createDatabaseMaxSizeSpec,
                CreateDatabaseGrowthSpec = createDatabaseGrowthSpec,
                CreateDatabaseSizeUnit = createDatabaseSizeUnit,
                CreateDatabaseGrowthUnit = createDatabaseGrowthUnit
            };
            var statementRegistry = new MsSqlStatementRegistry();
            statementRegistry.Add(queryStatementNoLeadingWith, implicitQueryStatementNoLeadingWith);
            statementRegistry.Add(updateStatement);
            statementRegistry.Add(insertStatement);
            statementRegistry.Add(deleteStatement);
            statementRegistry.Add(ifStatement);
            statementRegistry.Add(beginEndStatement);
            statementRegistry.Add(whileStatement);
            statementRegistry.Add(setStatement);
            statementRegistry.Add(printStatement);
            statementRegistry.Add(declareStatement);
            statementRegistry.Add(returnStatement, allowedInFunctionPrelude: false);
            statementRegistry.Add(transactionStatement);
            statementRegistry.Add(raiserrorStatement);
            statementRegistry.Add(throwStatement);
            statementRegistry.Add(loopControlStatement);
            statementRegistry.Add(gotoStatement);
            statementRegistry.Add(labelStatement, labelOnlyStatement);
            statementRegistry.Add(executeStatement);
            statementRegistry.Add(useStatement);
            statementRegistry.Add(createProcStatement);
            statementRegistry.Add(createFunctionStatement);
            statementRegistry.Add(grantStatement);
            statementRegistry.Add(dbccStatement);
            statementRegistry.Add(dropProcStatement);
            statementRegistry.Add(dropTableStatement);
            statementRegistry.Add(dropViewStatement);
            statementRegistry.Add(dropIndexStatement);
            statementRegistry.Add(dropStatisticsStatement);
            statementRegistry.Add(dropDatabaseStatement);
            statementRegistry.Add(createRoleStatement);
            statementRegistry.Add(createSchemaStatement);
            statementRegistry.Add(createViewStatement);
            statementRegistry.Add(createTableStatement);
            statementRegistry.Add(alterTableStatement);
            statementRegistry.Add(createIndexStatement);
            statementRegistry.Add(alterIndexStatement);
            statementRegistry.Add(createDatabaseStatement);
            statementRegistry.Add(createTriggerStatement);
            statementRegistry.Add(dropTriggerStatement);
            statementRegistry.Add(tryCatchStatement);
            statementRegistry.Add(truncateStatement);
            statementRegistry.Add(alterDatabaseStatement);
            statementRegistry.Add(declareCursorStatement);
            statementRegistry.Add(cursorOperationStatement);
            statementRegistry.Add(waitforStatement);
            statementRegistry.Add(createLoginStatement);
            statementRegistry.Add(bulkInsertStatement);
            statementRegistry.Add(checkpointStatement);
            statementRegistry.Add(createUserStatement);
            statementRegistry.Add(createStatisticsStatement);
            statementRegistry.Add(updateStatisticsStatement);
            statementRegistry.Add(dropTypeStatement);
            statementRegistry.Add(dropColumnEncryptionKeyStatement);
            statementRegistry.Add(revertStatement);
            statementRegistry.Add(dropEventSessionStatement);
            statementRegistry.Add(createTypeStatement);
            statementRegistry.Add(createSecurityPolicyStatement);
            statementRegistry.Add(alterSecurityPolicyStatement);
            statementRegistry.Add(mergeStatement);

            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.SynapseExtensions))
            {
                statementRegistry.Add(createTableAsSelectStatement);
            }

            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.ExternalObjects))
            {
                statementRegistry.Add(createExternalTableStatement);
                statementRegistry.Add(createExternalDataSourceStatement);
            }

            var statementNoLeadingWithAlternatives = statementRegistry.CreateTopLevelAlternatives();
            var implicitStatementNoLeadingWithAlternatives = statementRegistry.CreateImplicitAlternatives();
            var createFunctionPreludeStatementNoLeadingWithAlternatives = statementRegistry.CreateFunctionPreludeAlternatives();
            var createFunctionImplicitPreludeStatementNoLeadingWithAlternatives = statementRegistry.CreateFunctionImplicitPreludeAlternatives();

            void BuildQueryExpressionGrammar() => MsSqlQueryGrammar.Build(grammarContext);

            void BuildTableSourceGrammar() => MsSqlTableSourceGrammar.Build(grammarContext);

            void BuildExpressionGrammar() => MsSqlExpressionGrammar.Build(grammarContext);

            void BuildDmlGrammar() => MsSqlDmlGrammar.Build(
                grammarContext,
                updateStatement,
                updateSetList,
                updateSetItem,
                compoundAssignOp,
                insertStatement,
                insertTarget,
                insertColumnList,
                insertValueList,
                rowValue,
                rowValueList,
                deleteStatement,
                deleteTopClause,
                deleteTarget,
                deleteTargetSimple,
                deleteTargetRowset,
                rowsetFunctionLimited,
                tableHintLimitedList,
                tableHintLimited,
                tableHintLimitedName,
                deleteStatementTail,
                deleteStatementTailNoOutput,
                deleteStatementTailNoFrom,
                deleteOptionOpt,
                dmlOutputClause,
                dmlOutputTarget,
                dmlOutputIntoColumnListOpt,
                deleteSourceFromClause,
                deleteWhereClause,
                queryOptionClause,
                queryHintList,
                queryHint,
                queryHintName,
                optionClause,
                executeStatement);

            void BuildScriptGrammar() => MsSqlScriptGrammar.Build(
                grammarContext,
                script,
                statementList,
                statementListOpt,
                statementSeparator,
                statementSeparatorList,
                statement,
                statementNoLeadingWith,
                implicitStatementNoLeadingWith,
                leadingWithStatement,
                queryStatement,
                queryStatementNoLeadingWith,
                implicitQueryStatementNoLeadingWith,
                withXmlNamespacesClause,
                xmlNamespaceItemList,
                xmlNamespaceItem,
                withClause,
                cteDefinitionList,
                cteDefinition,
                updateStatement,
                insertStatement,
                deleteStatement,
                mergeStatement,
                queryExpression,
                implicitQueryExpression,
                optionClause,
                statementNoLeadingWithAlternatives,
                implicitStatementNoLeadingWithAlternatives);

            void BuildProceduralGrammar() => MsSqlProceduralGrammar.Build(
                grammarContext,
                ifBranchStatement,
                statement,
                statementListOpt,
                ifStatement,
                beginEndStatement,
                whileStatement,
                setOptionName,
                setStatisticsOption,
                setStatement,
                setTransactionIsolationLevel,
                printStatement,
                returnStatement,
                transactionStatement,
                raiserrorStatement,
                raiserrorArgList,
                raiserrorWithOptionList,
                raiserrorWithOption,
                throwStatement,
                loopControlStatement,
                gotoStatement,
                labelOnlyStatement,
                labelStatement,
                declareStatement,
                declareItemList,
                declareItem,
                declareTableVariable,
                tableTypeDefinition,
                createTableElementList,
                createTableOptions,
                typeArgument,
                procStatementList,
                statementSeparatorList,
                implicitStatementNoLeadingWith,
                tryCatchStatement,
                cursorReference,
                declareCursorStatement,
                cursorOptionList,
                cursorOption,
                cursorOperationStatement,
                fetchStatement,
                fetchDirection,
                fetchTargetList,
                waitforTimeValue,
                waitforStatement);

            void BuildExecutionGrammar() => MsSqlExecutionGrammar.Build(
                grammarContext,
                executeStatement,
                executeModuleCall,
                executeModuleCallCore,
                executeWithOptions,
                executeDynamicCall,
                executeAsContext,
                executeAtClause,
                executeReturnAssignment,
                executeModuleTarget,
                executeArgList,
                executeArg,
                executeArgNamePrefix,
                executeArgValue,
                executeOptionList,
                executeOption,
                executeResultSetsDefList,
                executeResultSetsDef,
                executeColumnDefList,
                executeColumnDef,
                executeNullability,
                executeLinkedArgList,
                executeLinkedArg,
                useStatement);

            void BuildProgrammableObjectsGrammar() => MsSqlProgrammableObjectsGrammar.Build(
                grammarContext,
                createProcKeyword,
                createProcHead,
                createProcName,
                createProcSignature,
                createProcSignatureParameterListOpt,
                createProcSignatureWithClauseOpt,
                createProcSignatureForReplicationOpt,
                createProcParameterList,
                createProcParameter,
                createProcParameterOptionList,
                createProcParameterOption,
                createProcWithClause,
                createProcOptionList,
                createProcOption,
                createProcExecuteAsClause,
                createProcForReplicationClause,
                createProcBody,
                createProcBodyBlock,
                createProcNativeWithClause,
                createProcNativeAtomicOptionList,
                createProcNativeAtomicOption,
                setTransactionIsolationLevel,
                createProcExternalName,
                createProcStatement,
                createFunctionHead,
                createFunctionName,
                createFunctionSignature,
                createFunctionSignatureParameterListOpt,
                createFunctionParameterList,
                createFunctionParameter,
                createFunctionParameterOptionList,
                createFunctionParameterOption,
                createFunctionScalarReturnsClause,
                createFunctionInlineTableReturnsClause,
                createFunctionTableVariableReturnsClause,
                createFunctionTableReturnDefinition,
                createFunctionTableReturnItemList,
                createFunctionTableReturnItem,
                createFunctionWithClause,
                createFunctionOptionList,
                createFunctionOption,
                createFunctionPreludeStatement,
                createFunctionPreludeStatementNoLeadingWith,
                createFunctionImplicitPreludeStatementNoLeadingWith,
                createFunctionPreludeStatementList,
                createFunctionPreludeBeforeReturnOpt,
                createFunctionBodyTrailingSeparatorsOpt,
                createFunctionScalarReturnStatement,
                createFunctionTableVariableReturnStatement,
                createFunctionScalarBody,
                createFunctionInlineTableBody,
                createFunctionTableVariableBody,
                createFunctionStatement,
                createViewHead,
                createViewColumnList,
                createViewOptionClause,
                createViewQuery,
                createViewBody,
                createViewCheckOptionOpt,
                createViewOptionList,
                createViewOption,
                createViewStatement,
                createTableColumnDefinition,
                createTableComputedColumn,
                createTableConstraint,
                createTableTableIndex,
                leadingWithStatement,
                statementList,
                statementSeparatorList,
                withClause,
                withCheckOptionStart,
                createFunctionPreludeStatementNoLeadingWithAlternatives,
                createFunctionImplicitPreludeStatementNoLeadingWithAlternatives);

            void BuildTableAndIndexSchemaDdlGrammar() => MsSqlSchemaDdlGrammar.BuildTableAndIndexGrammar(
                grammarContext,
                schemaDdlSymbols);

            void BuildDatabaseSchemaDdlGrammar() => MsSqlSchemaDdlGrammar.BuildDatabaseGrammar(
                grammarContext,
                schemaDdlSymbols);

            void BuildPreQuerySecurityAndAdminGrammar() => MsSqlSecurityAndAdminGrammar.BuildPreQuery(
                grammarContext,
                grantPermissionSet,
                grantPermissionList,
                grantPermissionItem,
                grantPermission,
                grantPermissionWord,
                grantOnClause,
                grantClassType,
                grantSecurable,
                grantPrincipalList,
                grantPrincipal,
                grantStatement,
                dbccCommand,
                dbccParamList,
                dbccParam,
                dbccOptionList,
                dbccOption,
                dbccOptionName,
                dbccOptionValue,
                dbccStatement,
                dropProcStatement,
                dropIfExistsClause,
                dropTableStatement,
                dropTableTargetList,
                dropViewStatement,
                dropViewTargetList,
                dropIndexStatement,
                dropIndexSpecList,
                dropIndexSpec,
                dropIndexOptionList,
                dropIndexOption,
                dropMoveToTarget,
                dropFileStreamTarget,
                dropStatisticsStatement,
                dropStatisticsTargetList,
                dropStatisticsTarget,
                dropDatabaseStatement,
                createTriggerHead,
                createTriggerFireClause,
                createTriggerEventList,
                createTriggerEvent,
                createTriggerWithOptionList,
                createTriggerWithOption,
                createTriggerStatement,
                createProcExecuteAsClause,
                createProcBodyBlock,
                dropTriggerStatement,
                createRoleStatement,
                createSchemaStatement,
                schemaNameClause);

            void BuildPostQuerySecurityAndAdminGrammar() => MsSqlSecurityAndAdminGrammar.BuildPostQuery(
                grammarContext,
                truncateStatement,
                createTableAsSelectStatement,
                createTableOptionList,
                alterDatabaseStatement,
                alterDatabaseSetOption,
                alterDatabaseSetOnOffOption,
                alterDatabaseSetEqualsOnOffOption,
                alterDatabaseSetModeOption,
                alterDatabaseRecoveryModel,
                alterDatabasePageVerifyMode,
                alterDatabaseCursorDefaultMode,
                alterDatabaseParameterizationMode,
                alterDatabaseTargetRecoveryUnit,
                alterDatabaseDelayedDurabilityMode,
                alterDatabaseTerminationClause,
                alterDatabaseTerminationOpt,
                createLoginPasswordSpec,
                createLoginOptionList,
                createLoginOption,
                createLoginWindowsOptionList,
                createLoginWindowsOption,
                createLoginStatement,
                createUserStatement,
                createStatisticsStatement,
                dropTypeStatement,
                dropColumnEncryptionKeyStatement,
                revertStatement,
                dropEventSessionStatement,
                createTypeStatement,
                tableTypeDefinition,
                checkpointStatement);

            void BuildUpdateStatisticsGrammar() => MsSqlUpdateStatisticsGrammar.Build(
                grammarContext,
                updateStatisticsStatement,
                updateStatisticsOptionList,
                updateStatisticsSimpleOption,
                updateStatisticsOnOffOptionName,
                updateStatisticsOption);

            BuildScriptGrammar();
            BuildDmlGrammar();
            BuildProceduralGrammar();

            BuildExecutionGrammar();
            BuildProgrammableObjectsGrammar();

            BuildPreQuerySecurityAndAdminGrammar();
            BuildTableAndIndexSchemaDdlGrammar();

            BuildDatabaseSchemaDdlGrammar();

            BuildQueryExpressionGrammar();
            BuildTableSourceGrammar();
            BuildExpressionGrammar();

            BuildPostQuerySecurityAndAdminGrammar();
            BuildUpdateStatisticsGrammar();

            MsSqlExtensionsGrammar.BuildMergeGrammar(
                gb,
                mergeTargetTable,
                mergeSourceTable,
                mergeOutputClauseOpt,
                mergeOptionClauseOpt,
                mergeWhenList,
                mergeWhen,
                mergeMatchedAction,
                mergeNotMatchedAction,
                qualifiedName,
                identifierTerm,
                tableHintLimitedList,
                tableSource,
                dmlOutputClause,
                optionClause,
                topValue,
                searchCondition,
                updateSetList,
                insertColumnList,
                insertValueList);

            MsSqlExtensionsGrammar.BuildBulkInsertGrammar(
                gb,
                bulkInsertOptionList,
                qualifiedName,
                expression,
                namedOptionValue,
                createTableKeyColumnList);

            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.GraphExtensions))
            {
                MsSqlExtensionsGrammar.BuildGraphGrammar(
                    gb,
                    matchGraphPattern,
                    matchGraphPath,
                    matchGraphStep,
                    matchGraphStepChain,
                    matchGraphShortestPath,
                    matchGraphShortestPathBody,
                    identifierTerm,
                    number);
            }

            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.SynapseExtensions))
            {
                MsSqlExtensionsGrammar.BuildSynapseGrammar(
                    gb,
                    functionCall,
                    predictArgList,
                    predictArg,
                    identifierTerm,
                    expression);
            }

            MsSqlExtensionsGrammar.BuildSecurityPolicyGrammar(
                gb,
                createSecurityPolicyStatement,
                alterSecurityPolicyStatement,
                securityPolicyClauseList,
                securityPolicyClause,
                securityPolicyOptionList,
                securityPolicyOption,
                securityPolicyOptionName,
                functionCall,
                qualifiedName);

            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.ExternalObjects))
            {
                MsSqlExtensionsGrammar.BuildExternalObjectGrammar(
                    gb,
                    createExternalTableStatement,
                    externalTableOptionList,
                    createExternalDataSourceStatement,
                    externalDataSourceOptionList,
                    qualifiedName,
                    identifierTerm,
                    createTableElementList,
                    namedOptionValue);
            }

            return gb.BuildGrammar("Start");
        }

        private static GrammarResources GetGrammarResources(MsSqlDialectFeatures dialectFeatures)
        {
            var normalizedFeatures = NormalizeGrammarFeatures(dialectFeatures);
            return GrammarCache.GetOrAdd(
                normalizedFeatures,
                static features => new Lazy<GrammarResources>(() => CreateGrammarResources(features))).Value;
        }

        private static GrammarResources CreateGrammarResources(MsSqlDialectFeatures dialectFeatures)
        {
            var grammar = BuildGrammarCore(dialectFeatures);
            var lexerSettings = CreateLexerSettings(grammar);
            var productions = grammar.Productions.ToList();
            var startProduction = productions.First(p => p.LeftNonTerminal == grammar.Root);
            if (startProduction.ProductionDefinition.Count != 1 ||
                startProduction.ProductionDefinition[0] is not INonTerminal scriptNonTerminal)
            {
                throw new InvalidOperationException("Unexpected SQL grammar root shape.");
            }

            var emptyScriptProduction = productions
                .First(p => p.LeftNonTerminal == scriptNonTerminal &&
                    p.ProductionDefinition.Count == 1 &&
                    p.ProductionDefinition[0] is EmptyTerm);

            return new GrammarResources(
                grammar,
                lexerSettings,
                startProduction,
                emptyScriptProduction,
                productions.IndexOf(startProduction),
                productions.IndexOf(emptyScriptProduction));
        }

        private static ParseResult CreateEmptyScriptParseResult(GrammarResources grammarResources)
        {
            var scriptNode = new NonTerminalNode(
                grammarResources.EmptyScriptProduction.LeftNonTerminal,
                grammarResources.EmptyScriptProduction,
                []);
            var startNode = new NonTerminalNode(
                grammarResources.StartProduction.LeftNonTerminal,
                grammarResources.StartProduction,
                [scriptNode]);

            return new ParseResult
            {
                ParseTree = startNode,
                Productions =
                [
                    grammarResources.StartProductionIndex,
                    grammarResources.EmptyScriptProductionIndex
                ]
            };
        }

        private static bool IsTriviaToken(IToken token)
        {
            var terminalFlags = token.Terminal?.Flags;
            return terminalFlags == TermFlags.Space || terminalFlags == TermFlags.Comment;
        }

        private static bool HasFeature(MsSqlDialectFeatures dialectFeatures, MsSqlDialectFeatures feature)
        {
            return (dialectFeatures & feature) == feature;
        }

        private static MsSqlDialectFeatures NormalizeGrammarFeatures(MsSqlDialectFeatures dialectFeatures)
        {
            return dialectFeatures & (
                MsSqlDialectFeatures.ExternalObjects |
                MsSqlDialectFeatures.SynapseExtensions |
                MsSqlDialectFeatures.GraphExtensions);
        }

        private static IToken WithLeadingTrivia(IToken token, IEnumerable<IToken> leadingTrivia)
        {
            var trivia = token.Trivia;
            return token.WithTrivia(new FormattingTrivia(
                [.. trivia.LeadingTrivia, .. leadingTrivia],
                trivia.TrailingTrivia));
        }

        private static IToken WithTrailingTrivia(IToken token, IEnumerable<IToken> trailingTrivia)
        {
            var trivia = token.Trivia;
            return token.WithTrivia(new FormattingTrivia(
                trivia.LeadingTrivia,
                [.. trivia.TrailingTrivia, .. trailingTrivia]));
        }

        private sealed record GrammarResources(
            IGrammar Grammar,
            LexerSettings LexerSettings,
            Production StartProduction,
            Production EmptyScriptProduction,
            int StartProductionIndex,
            int EmptyScriptProductionIndex);
    }
}

