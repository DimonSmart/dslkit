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
            var statementNoLeadingWithAlternatives = new List<object>
            {
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
                mergeStatement
            };
            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.SynapseExtensions))
            {
                statementNoLeadingWithAlternatives.Add(createTableAsSelectStatement);
            }

            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.ExternalObjects))
            {
                statementNoLeadingWithAlternatives.Add(createExternalTableStatement);
                statementNoLeadingWithAlternatives.Add(createExternalDataSourceStatement);
            }

            var implicitStatementNoLeadingWithAlternatives = new List<object>
            {
                implicitQueryStatementNoLeadingWith,
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
                labelOnlyStatement,
                executeStatement,
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
                mergeStatement
            };
            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.SynapseExtensions))
            {
                implicitStatementNoLeadingWithAlternatives.Add(createTableAsSelectStatement);
            }

            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.ExternalObjects))
            {
                implicitStatementNoLeadingWithAlternatives.Add(createExternalTableStatement);
                implicitStatementNoLeadingWithAlternatives.Add(createExternalDataSourceStatement);
            }
            var createFunctionPreludeStatementNoLeadingWithAlternatives = statementNoLeadingWithAlternatives
                .Where(alternative => !ReferenceEquals(alternative, returnStatement))
                .ToArray();
            var createFunctionImplicitPreludeStatementNoLeadingWithAlternatives = implicitStatementNoLeadingWithAlternatives
                .Where(alternative => !ReferenceEquals(alternative, returnStatement))
                .ToArray();

            void BuildQueryExpressionGrammar()
            {
                gb.Prod(implicitQueryExpression).Is(implicitQueryUnionExpression, queryExpressionTail);
                gb.Prod(implicitQueryUnionExpression).Is(implicitQueryIntersectExpression);
                gb.Prod(implicitQueryUnionExpression).Is(implicitQueryUnionExpression, setOperator, queryIntersectExpression);
                gb.Prod(implicitQueryIntersectExpression).Is(querySpecification);
                gb.Prod(implicitQueryIntersectExpression).Is(implicitQueryIntersectExpression, "INTERSECT", queryPrimary);
                gb.Prod(queryExpression).Is(queryUnionExpression, queryExpressionTail);
                gb.Prod(queryUnionExpression).Is(queryIntersectExpression);
                gb.Prod(queryUnionExpression).Is(queryUnionExpression, setOperator, queryIntersectExpression);
                gb.Prod(queryIntersectExpression).Is(queryPrimary);
                gb.Prod(queryIntersectExpression).Is(queryIntersectExpression, "INTERSECT", queryPrimary);

                gb.Rule(setOperator)
                    .CanBe("UNION")
                    .Or("UNION", "ALL")
                    .OrKeywords("EXCEPT");

                gb.Prod(queryExpressionTail).Is(queryExpressionOrderByAndOffsetOpt, queryExpressionForOpt, queryExpressionOptionOpt);
                gb.Opt(queryExpressionOrderByAndOffsetOpt, orderByClause);
                gb.Prod(queryExpressionOrderByAndOffsetOpt).Is(orderByClause, offsetFetchClause);
                gb.Opt(queryExpressionForOpt, forClause);
                gb.Opt(queryExpressionOptionOpt, optionClause);
                gb.Prod(queryPrimary).Is(querySpecification);
                gb.Prod(queryPrimary).Is("(", queryExpression, ")");

                gb.Rule(forClause)
                    .CanBe("FOR", "BROWSE")
                    .Or("FOR", "JSON", forJsonMode)
                    .Or("FOR", "JSON", forJsonMode, ",", forJsonOptionList)
                    .Or("FOR", "XML", forXmlMode)
                    .Or("FOR", "XML", forXmlMode, ",", forXmlOptionList);

                gb.Rule(forJsonMode).Keywords("AUTO", "PATH", "NONE");

                gb.Prod(forJsonOptionList).Is(forJsonOption);
                gb.Prod(forJsonOptionList).Is(forJsonOptionList, ",", forJsonOption);
                gb.Rule(forJsonOption).Keywords("WITHOUT_ARRAY_WRAPPER", "INCLUDE_NULL_VALUES", "ROOT");
                gb.Prod(forJsonOption).Is("ROOT", "(", expression, ")");

                gb.Rule(forXmlMode)
                    .CanBe("AUTO")
                    .Or("PATH")
                    .Or("PATH", "(", expression, ")")
                    .Or("RAW")
                    .Or("RAW", "(", expression, ")")
                    .Or("EXPLICIT");

                gb.Prod(forXmlOptionList).Is(forXmlOption);
                gb.Prod(forXmlOptionList).Is(forXmlOptionList, ",", forXmlOption);
                gb.Rule(forXmlOption)
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

                gb.Rule(selectCorePrefix).OneOf(
                    EmptyTerm.Empty,
                    setQuantifier,
                    topClause,
                    gb.Seq(setQuantifier, topClause));
                gb.Rule(selectCoreTail).OneOf(
                    EmptyTerm.Empty,
                    gb.Seq("FROM", tableSourceList),
                    selectCoreIntoClause);
                gb.Prod(selectCoreIntoClause).Is("INTO", qualifiedName);
                gb.Prod(selectCoreIntoClause).Is("INTO", qualifiedName, "FROM", tableSourceList);
                gb.Prod(selectCore).Is("SELECT", selectCorePrefix, selectList, selectCoreTail);

                gb.Prod(querySpecificationWhereClause).Is("WHERE", searchCondition);
                gb.Opt(querySpecificationHavingOpt, "HAVING", searchCondition);
                gb.Rule(querySpecificationGroupByWithOpt).OneOf(
                    EmptyTerm.Empty,
                    gb.Seq("WITH", "ROLLUP"),
                    gb.Seq("WITH", "CUBE"));
                gb.Prod(querySpecificationGroupByExpressionList).Is(expressionList, querySpecificationGroupByWithOpt);
                gb.Prod(querySpecificationGroupByGroupingSets).Is("GROUPING", "SETS", "(", groupingSetList, ")");
                gb.Rule(querySpecificationGroupByClause).OneOf(
                    gb.Seq("GROUP", "BY", querySpecificationGroupByExpressionList, querySpecificationHavingOpt),
                    gb.Seq("GROUP", "BY", querySpecificationGroupByGroupingSets, querySpecificationHavingOpt));
                gb.Rule(querySpecification).OneOf(
                    selectCore,
                    gb.Seq(selectCore, querySpecificationWhereClause),
                    gb.Seq(selectCore, querySpecificationGroupByClause),
                    gb.Seq(selectCore, querySpecificationWhereClause, querySpecificationGroupByClause));

                gb.Rule(setQuantifier).Keywords("ALL", "DISTINCT");

                gb.Rule(topClauseTail).OneOf(
                    EmptyTerm.Empty,
                    "PERCENT",
                    gb.Seq("WITH", "TIES"),
                    gb.Seq("PERCENT", "WITH", "TIES"));
                gb.Prod(topClause).Is("TOP", topValue, topClauseTail);
                gb.Prod(topValue).Is(number);
                gb.Prod(topValue).Is("(", expression, ")");

                gb.Rule(selectList).CanBe(selectItemList);
                gb.Rule(selectItemList).SeparatedBy(",", selectItem);
                gb.Prod(selectItem).Is("*");
                gb.Prod(selectItem).Is(expression, "AS", identifierTerm);
                gb.Prod(selectItem).Is(expression, "AS", stringLiteral);
                gb.Prod(selectItem).Is(expression, identifierTerm);
                gb.Prod(selectItem).Is(expression, stringLiteral);
                gb.Prod(selectItem).Is(expression);
                gb.Prod(selectItem).Is(qualifiedName, ".", "*");
                gb.Prod(selectItem).Is(variableReference, "=", expression);
                gb.Prod(selectItem).Is(variableReference, compoundAssignOp, expression);
            }

            void BuildTableSourceGrammar()
            {
                gb.Rule(tableSourceList).SeparatedBy(",", tableSource);
                gb.Prod(tableSource).Is(tableFactor);
                gb.Prod(tableSource).Is(tableSource, joinPart);
                gb.Prod(tableFactor).Is(qualifiedName);
                gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, identifierTerm);
                if (HasFeature(dialectFeatures, MsSqlDialectFeatures.GraphExtensions))
                {
                    gb.Prod(tableFactor).Is(qualifiedName, forPathStart, "PATH");
                    gb.Prod(tableFactor).Is(qualifiedName, forPathStart, "PATH", identifierTerm);
                }
                gb.Prod(tableFactor).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");
                gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm, "WITH", "(", tableHintLimitedList, ")");
                gb.Prod(tableFactor).Is(qualifiedName, identifierTerm, "WITH", "(", tableHintLimitedList, ")");
                gb.Prod(tableFactor).Is(qualifiedName, temporalClause);
                gb.Prod(tableFactor).Is(qualifiedName, temporalClause, "AS", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, temporalClause, identifierTerm);
                gb.Rule(temporalClause).OneOf(
                    gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "AS", "OF", additiveExpression),
                    gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "ALL"),
                    gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "BETWEEN", additiveExpression, "AND", additiveExpression),
                    gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "FROM", additiveExpression, "TO", additiveExpression),
                    gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "CONTAINED", "IN", "(", additiveExpression, ",", additiveExpression, ")"));
                gb.Prod(tableFactor).Is(variableReference);
                gb.Prod(tableFactor).Is(variableReference, "AS", identifierTerm);
                gb.Prod(tableFactor).Is(variableReference, identifierTerm);
                gb.Prod(tableFactor).Is(functionCall);
                gb.Prod(tableFactor).Is(functionCall, "AS", identifierTerm);
                gb.Prod(tableFactor).Is(functionCall, identifierTerm);
                gb.Prod(tableFactor).Is(functionCall, "AS", identifierTerm, "(", insertColumnList, ")");
                gb.Prod(tableFactor).Is(functionCall, identifierTerm, "(", insertColumnList, ")");
                gb.Prod(tableFactor).Is(openJsonCall);
                gb.Prod(tableFactor).Is(openJsonCall, "AS", identifierTerm);
                gb.Prod(tableFactor).Is(openJsonCall, identifierTerm);
                gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm);
                gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm, "(", insertColumnList, ")");
                gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm, "(", insertColumnList, ")");
                gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
                gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, "PIVOT", "(", pivotClause, ")", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
                gb.Prod(pivotClause).Is(functionCall, "FOR", identifierTerm, "IN", "(", pivotValueList, ")");
                gb.Prod(pivotValueList).Is(expression);
                gb.Prod(pivotValueList).Is(pivotValueList, ",", expression);
                gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is("(", queryExpression, ")", "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
                gb.Prod(tableFactor).Is("(", queryExpression, ")", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
                gb.Prod(tableFactor).Is(qualifiedName, identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
                gb.Prod(unpivotClause).Is(identifierTerm, "FOR", identifierTerm, "IN", "(", unpivotColumnList, ")");
                gb.Prod(unpivotColumnList).Is(identifierTerm);
                gb.Prod(unpivotColumnList).Is(unpivotColumnList, ",", identifierTerm);
                gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", identifierTerm);
                gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", "AS", identifierTerm, "(", insertColumnList, ")");
                gb.Prod(tableFactor).Is("(", "VALUES", rowValueList, ")", identifierTerm, "(", insertColumnList, ")");
                gb.Prod(tableFactor).Is("(", tableSource, ")");
                gb.Prod(tableFactor).Is("(", tableSource, ")", "AS", identifierTerm);
                gb.Prod(tableFactor).Is("(", tableSource, ")", identifierTerm);

                gb.Prod(openJsonCall).Is("OPENJSON", "(", functionArgumentList, ")");
                gb.Prod(openJsonCall).Is("OPENJSON", "(", functionArgumentList, ")", openJsonWithClause);
                gb.Prod(openJsonWithClause).Is("WITH", "(", openJsonColumnList, ")");
                gb.Prod(openJsonColumnList).Is(openJsonColumnDef);
                gb.Prod(openJsonColumnList).Is(openJsonColumnList, ",", openJsonColumnDef);
                gb.Prod(openJsonColumnDef).Is(identifierTerm, typeSpec);
                gb.Prod(openJsonPath).Is(stringLiteral);
                gb.Prod(openJsonPath).Is(unicodeStringLiteral);
                gb.Prod(openJsonColumnDef).Is(identifierTerm, typeSpec, openJsonPath);
                gb.Prod(openJsonColumnDef).Is(identifierTerm, typeSpec, "AS", "JSON");
                gb.Prod(openJsonColumnDef).Is(identifierTerm, typeSpec, openJsonPath, "AS", "JSON");

                gb.Prod(joinPart).Is("JOIN", tableFactor, "ON", searchCondition);
                gb.Prod(joinPart).Is(joinType, "JOIN", tableFactor, "ON", searchCondition);
                gb.Prod(joinPart).Is("CROSS", "JOIN", tableFactor);
                gb.Prod(joinPart).Is("CROSS", "APPLY", tableFactor);
                gb.Prod(joinPart).Is("OUTER", "APPLY", tableFactor);

                gb.Rule(joinType)
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
            }

            void BuildProceduralGrammar()
            {
                gb.Prod(ifBranchStatement).Is(statement);
                gb.Prod(ifBranchStatement).Is(statement, ";");
                gb.Prod(ifStatement).Is("IF", searchCondition, ifBranchStatement);
                gb.Prod(ifStatement).Is("IF", searchCondition, ifBranchStatement, "ELSE", ifBranchStatement);
                gb.Prod(ifStatement).Is("IF", searchCondition, ifBranchStatement, "ELSE", ifStatement);
                gb.Prod(beginEndStatement).Is("BEGIN", statementListOpt, "END");
                gb.Prod(whileStatement).Is("WHILE", searchCondition, statement);

                gb.Rule(setOptionName).Keywords(
                    "ANSI_DEFAULTS",
                    "ANSI_NULL_DFLT_OFF",
                    "ANSI_NULL_DFLT_ON",
                    "ANSI_NULLS",
                    "ANSI_PADDING",
                    "ANSI_WARNINGS",
                    "ARITHABORT",
                    "ARITHIGNORE",
                    "CONCAT_NULL_YIELDS_NULL",
                    "CURSOR_CLOSE_ON_COMMIT",
                    "DATEFIRST",
                    "DATEFORMAT",
                    "DEADLOCK_PRIORITY",
                    "FMTONLY",
                    "FORCEPLAN",
                    "IMPLICIT_TRANSACTIONS",
                    "LANGUAGE",
                    "LOCK_TIMEOUT",
                    "NOCOUNT",
                    "NOEXEC",
                    "NUMERIC_ROUNDABORT",
                    "PARSEONLY",
                    "QUERY_GOVERNOR_COST_LIMIT",
                    "QUOTED_IDENTIFIER",
                    "REMOTE_PROC_TRANSACTIONS",
                    "ROWCOUNT",
                    "SHOWPLAN_ALL",
                    "SHOWPLAN_TEXT",
                    "SHOWPLAN_XML",
                    "TEXTSIZE",
                    "XACT_ABORT");
                gb.Rule(setStatisticsOption)
                    .CanBe("STATISTICS", "IO")
                    .Or("STATISTICS", "TIME")
                    .Or("STATISTICS", "XML")
                    .Or("STATISTICS", "PROFILE");
                gb.Prod(setStatement).Is("SET", variableReference, "=", expression);
                gb.Prod(setStatement).Is("SET", variableReference, compoundAssignOp, expression);
                gb.Prod(setStatement).Is("SET", setOptionName, "ON");
                gb.Prod(setStatement).Is("SET", setOptionName, "OFF");
                gb.Prod(setStatement).Is("SET", setOptionName, "=", expression);
                gb.Prod(setStatement).Is("SET", setOptionName, expression);
                gb.Prod(setStatement).Is("SET", setStatisticsOption, "ON");
                gb.Prod(setStatement).Is("SET", setStatisticsOption, "OFF");
                gb.Prod(setStatement).Is("SET", "IDENTITY_INSERT", qualifiedName, "ON");
                gb.Prod(setStatement).Is("SET", "IDENTITY_INSERT", qualifiedName, "OFF");
                gb.Prod(setStatement).Is("SET", "TRANSACTION", "ISOLATION", "LEVEL", setTransactionIsolationLevel);

                gb.Rule(setTransactionIsolationLevel)
                    .CanBe("READ", "UNCOMMITTED")
                    .Or("READ", "COMMITTED")
                    .Or("REPEATABLE", "READ")
                    .Or("SNAPSHOT")
                    .Or("SERIALIZABLE");

                gb.Prod(printStatement).Is("PRINT", expression);

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

                gb.Prod(raiserrorStatement).Is("RAISERROR", "(", raiserrorArgList, ")");
                gb.Prod(raiserrorStatement).Is("RAISERROR", "(", raiserrorArgList, ")", "WITH", raiserrorWithOptionList);
                gb.Prod(raiserrorArgList).Is(expression);
                gb.Prod(raiserrorArgList).Is(raiserrorArgList, ",", expression);
                gb.Prod(raiserrorWithOptionList).Is(raiserrorWithOption);
                gb.Prod(raiserrorWithOptionList).Is(raiserrorWithOptionList, ",", raiserrorWithOption);
                gb.Rule(raiserrorWithOption).Keywords("LOG", "NOWAIT", "SETERROR");

                gb.Rule(throwStatement)
                    .CanBe("THROW")
                    .Or("THROW", expression, ",", expression, ",", expression);
                gb.Rule(loopControlStatement)
                    .CanBe("BREAK")
                    .OrKeywords("CONTINUE");
                gb.Prod(gotoStatement).Is("GOTO", strictIdentifierTerm);
                gb.Prod(labelOnlyStatement).Is(strictIdentifierTerm, ":");
                gb.Prod(labelStatement).Is(labelOnlyStatement);

                gb.Prod(declareStatement).Is("DECLARE", declareItemList);
                gb.Prod(declareStatement).Is("DECLARE", declareTableVariable);
                gb.Prod(declareItemList).Is(declareItem);
                gb.Prod(declareItemList).Is(declareItemList, ",", declareItem);
                gb.Prod(declareItem).Is(variableReference, typeSpec);
                gb.Prod(declareItem).Is(variableReference, typeSpec, "=", expression);
                gb.Prod(declareItem).Is(variableReference, typeSpec, "NOT", "NULL");
                gb.Prod(declareItem).Is(variableReference, typeSpec, "NOT", "NULL", "=", expression);
                gb.Prod(declareItem).Is(variableReference, typeSpec, "NULL");
                gb.Prod(declareItem).Is(variableReference, typeSpec, "NULL", "=", expression);
                gb.Prod(declareItem).Is(variableReference, "AS", typeSpec);
                gb.Prod(declareItem).Is(variableReference, "AS", typeSpec, "=", expression);
                gb.Prod(declareItem).Is(variableReference, "AS", typeSpec, "NOT", "NULL");
                gb.Prod(declareItem).Is(variableReference, "AS", typeSpec, "NOT", "NULL", "=", expression);
                gb.Prod(declareTableVariable).Is(variableReference, tableTypeDefinition);
                gb.Prod(declareTableVariable).Is(variableReference, "AS", tableTypeDefinition);
                gb.Prod(tableTypeDefinition).Is("TABLE", "(", createTableElementList, ")");
                gb.Prod(tableTypeDefinition).Is("TABLE", "(", createTableElementList, ")", createTableOptions);
                gb.Rule(typeArgument)
                    .CanBe(expression)
                    .OrKeywords("MAX");
                gb.Prod(typeSpec).Is(qualifiedName);
                gb.Prod(typeSpec).Is(qualifiedName, "(", typeArgument, ")");
                gb.Prod(typeSpec).Is(qualifiedName, "(", typeArgument, ",", typeArgument, ")");

                gb.Prod(procStatementList).Is(statement);
                gb.Prod(procStatementList).Is(statementSeparatorList, statement);
                gb.Prod(procStatementList).Is(procStatementList, ";", statement);
                gb.Prod(procStatementList).Is(procStatementList, implicitStatementNoLeadingWith);

                gb.Prod(tryCatchStatement).Is(
                    "BEGIN", "TRY",
                    procStatementList,
                    "END", "TRY",
                    "BEGIN", "CATCH",
                    "END", "CATCH");
                gb.Prod(tryCatchStatement).Is(
                    "BEGIN", "TRY",
                    procStatementList,
                    "END", "TRY",
                    "BEGIN", "CATCH",
                    procStatementList,
                    "END", "CATCH");
                gb.Prod(tryCatchStatement).Is(
                    "BEGIN", "TRY",
                    procStatementList, statementSeparatorList,
                    "END", "TRY",
                    "BEGIN", "CATCH",
                    "END", "CATCH");
                gb.Prod(tryCatchStatement).Is(
                    "BEGIN", "TRY",
                    procStatementList, statementSeparatorList,
                    "END", "TRY",
                    "BEGIN", "CATCH",
                    procStatementList,
                    "END", "CATCH");
                gb.Prod(tryCatchStatement).Is(
                    "BEGIN", "TRY",
                    procStatementList, statementSeparatorList,
                    "END", "TRY",
                    "BEGIN", "CATCH",
                    procStatementList, statementSeparatorList,
                    "END", "CATCH");
                gb.Prod(tryCatchStatement).Is(
                    "BEGIN", "TRY",
                    procStatementList,
                    "END", "TRY",
                    "BEGIN", "CATCH",
                    procStatementList, statementSeparatorList,
                    "END", "CATCH");

                gb.Rule(cursorReference).CanBe(strictIdentifierTerm);
                gb.Rule(declareCursorStatement).OneOf(
                    gb.Seq("DECLARE", strictIdentifierTerm, "CURSOR", "FOR", queryExpression),
                    gb.Seq("DECLARE", strictIdentifierTerm, "CURSOR", cursorOptionList, "FOR", queryExpression));
                gb.Rule(cursorOptionList).Plus(cursorOption);
                gb.Rule(cursorOption).Keywords(
                    "LOCAL",
                    "GLOBAL",
                    "FORWARD_ONLY",
                    "SCROLL",
                    "STATIC",
                    "KEYSET",
                    "DYNAMIC",
                    "FAST_FORWARD",
                    "READ_ONLY",
                    "SCROLL_LOCKS",
                    "OPTIMISTIC",
                    "TYPE_WARNING",
                    "INSENSITIVE");
                gb.Rule(cursorOperationStatement).OneOf(
                    gb.Seq("OPEN", cursorReference),
                    gb.Seq("CLOSE", cursorReference),
                    gb.Seq("DEALLOCATE", cursorReference),
                    fetchStatement);
                gb.Rule(fetchStatement).OneOf(
                    gb.Seq("FETCH", "FROM", cursorReference),
                    gb.Seq("FETCH", "FROM", cursorReference, "INTO", fetchTargetList),
                    gb.Seq("FETCH", fetchDirection, "FROM", cursorReference),
                    gb.Seq("FETCH", fetchDirection, "FROM", cursorReference, "INTO", fetchTargetList),
                    gb.Seq("FETCH", cursorReference),
                    gb.Seq("FETCH", cursorReference, "INTO", fetchTargetList));
                gb.Rule(fetchDirection).OneOf(
                    "NEXT",
                    "PRIOR",
                    "FIRST",
                    "LAST",
                    gb.Seq("ABSOLUTE", expression),
                    gb.Seq("RELATIVE", expression));
                gb.Rule(fetchTargetList).SeparatedBy(",", variableReference);

                gb.Rule(waitforTimeValue).OneOf(
                    stringLiteral,
                    unicodeStringLiteral,
                    variableReference);
                gb.Rule(waitforStatement).OneOf(
                    gb.Seq("WAITFOR", "DELAY", waitforTimeValue),
                    gb.Seq("WAITFOR", "TIME", waitforTimeValue));
            }

            void BuildUpdateStatisticsGrammar()
            {
                gb.Rule(updateStatisticsStatement)
                    .CanBe("UPDATE", "STATISTICS", qualifiedName)
                    .Or("UPDATE", "STATISTICS", qualifiedName, strictIdentifierTerm)
                    .Or("UPDATE", "STATISTICS", qualifiedName, "WITH", updateStatisticsOptionList)
                    .Or("UPDATE", "STATISTICS", qualifiedName, strictIdentifierTerm, "WITH", updateStatisticsOptionList);
                gb.Rule(updateStatisticsOptionList).SeparatedBy(",", updateStatisticsOption);
                gb.Rule(updateStatisticsSimpleOption).Keywords("FULLSCAN", "NORECOMPUTE", "RESAMPLE");
                gb.Rule(updateStatisticsOnOffOptionName).Keywords("AUTO_DROP", "INCREMENTAL", "PERSIST_SAMPLE_PERCENT");
                gb.Rule(updateStatisticsOption).OneOf(
                    updateStatisticsSimpleOption,
                    gb.Seq("SAMPLE", expression, "PERCENT"),
                    gb.Seq("SAMPLE", expression, "ROWS"),
                    gb.Seq("STATS_STREAM", "=", expression),
                    gb.Seq("MAXDOP", "=", expression),
                    gb.Seq(updateStatisticsOnOffOptionName, "=", indexOnOffValue),
                    gb.Seq("ROWCOUNT", "=", expression),
                    gb.Seq("PAGECOUNT", "=", expression));
            }

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
                gb.Seq(statementList, implicitStatementNoLeadingWith));
            gb.Rule("StatementListOpt").OneOf(
                EmptyTerm.Empty,
                statementList,
                gb.Seq(statementList, statementSeparatorList));
            gb.Rule("StatementSeparatorList").Plus(statementSeparator);
            gb.Rule("StatementSeparator").OneOf(";");
            gb.Rule("Statement").OneOf(statementNoLeadingWith, leadingWithStatement);
            gb.Rule("StatementNoLeadingWith").OneOf([.. statementNoLeadingWithAlternatives]);
            // Only allow omitted separators before statements with a keyword-led start.
            gb.Rule("ImplicitStatementNoLeadingWith").OneOf([.. implicitStatementNoLeadingWithAlternatives]);
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
                gb.Seq(withClause, queryExpression));
            gb.Rule("QueryStatementNoLeadingWith").OneOf(
                queryExpression);
            gb.Rule("ImplicitQueryStatementNoLeadingWith").OneOf(
                implicitQueryExpression);
            gb.Prod(updateStatement).Is("UPDATE", tableFactor, "SET", updateSetList);
            gb.Prod(updateStatement).Is("UPDATE", tableFactor, "SET", updateSetList, "WHERE", searchCondition);
            gb.Prod(updateStatement).Is("UPDATE", tableFactor, "SET", updateSetList, "FROM", tableSourceList);
            gb.Prod(updateStatement).Is("UPDATE", tableFactor, "SET", updateSetList, "FROM", tableSourceList, "WHERE", searchCondition);
            gb.Prod(updateSetList).Is(updateSetItem);
            gb.Prod(updateSetList).Is(updateSetList, ",", updateSetItem);
            gb.Prod(updateSetItem).Is(qualifiedName, "=", expression);
            gb.Prod(updateSetItem).Is(qualifiedName, compoundAssignOp, expression);
            gb.Prod(updateSetItem).Is(variableReference, "=", expression);
            gb.Prod(updateSetItem).Is(variableReference, compoundAssignOp, expression);
            gb.Prod(updateSetItem).Is(functionCall); // XML modify: col.modify(...)

            gb.Rule("CompoundAssignOp")
                .CanBe("+=")
                .Or("-=")
                .Or("*=")
                .Or("/=")
                .Or("%=")
                .Or("&=")
                .Or("|=")
                .Or("^=");

            gb.Prod(insertStatement).Is("INSERT", insertTarget, "VALUES", rowValueList);
            gb.Prod(insertStatement).Is("INSERT", insertTarget, executeStatement);
            gb.Prod(insertStatement).Is("INSERT", insertTarget, queryExpression);
            gb.Prod(insertStatement).Is("INSERT", insertTarget, deleteOutputClause, "VALUES", rowValueList);
            gb.Prod(insertStatement).Is("INSERT", insertTarget, deleteOutputClause, queryExpression);
            gb.Prod(insertTarget).Is("INTO", qualifiedName);
            gb.Prod(insertTarget).Is(qualifiedName);
            gb.Prod(insertTarget).Is("INTO", qualifiedName, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is(qualifiedName, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is("INTO", variableReference);
            gb.Prod(insertTarget).Is(variableReference);
            gb.Prod(insertTarget).Is("INTO", variableReference, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is(variableReference, "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is("INTO", qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(insertTarget).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(insertTarget).Is("INTO", qualifiedName, "(", insertColumnList, ")", "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(insertTarget).Is("INTO", qualifiedName, "WITH", "(", tableHintLimitedList, ")", "(", insertColumnList, ")");
            gb.Prod(insertTarget).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")", "(", insertColumnList, ")");
            gb.Prod(insertColumnList).Is(identifierTerm);
            gb.Prod(insertColumnList).Is(insertColumnList, ",", identifierTerm);
            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Prod(insertColumnList).Is(graphColumnRef);
                gb.Prod(insertColumnList).Is(insertColumnList, ",", graphColumnRef);
            }
            gb.Prod(insertValueList).Is(expression);
            gb.Prod(insertValueList).Is(insertValueList, ",", expression);
            gb.Prod(rowValue).Is("(", insertValueList, ")");
            gb.Prod(rowValueList).Is(rowValue);
            gb.Prod(rowValueList).Is(rowValueList, ",", rowValue);

            gb.Prod(deleteStatement).Is(
                "DELETE",
                deleteTarget,
                deleteStatementTail);
            gb.Prod(deleteStatement).Is(
                "DELETE",
                "FROM",
                deleteTarget,
                deleteStatementTail);
            gb.Prod(deleteStatement).Is(
                "DELETE",
                deleteTopClause,
                deleteTarget,
                deleteStatementTail);
            gb.Prod(deleteStatement).Is(
                "DELETE",
                deleteTopClause,
                "FROM",
                deleteTarget,
                deleteStatementTail);

            gb.Prod(deleteTopClause).Is("TOP", "(", expression, ")");
            gb.Prod(deleteTopClause).Is("TOP", "(", expression, ")", "PERCENT");

            gb.Prod(deleteTarget).Is(deleteTargetSimple);
            gb.Prod(deleteTarget).Is(deleteTargetRowset);

            gb.Prod(deleteTargetSimple).Is(identifierTerm);
            gb.Prod(deleteTargetSimple).Is(qualifiedName);
            gb.Prod(deleteTargetSimple).Is(variableReference);
            gb.Prod(deleteTargetSimple).Is(identifierTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(deleteTargetSimple).Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");

            gb.Prod(deleteTargetRowset).Is(rowsetFunctionLimited);
            gb.Prod(deleteTargetRowset).Is(rowsetFunctionLimited, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod(rowsetFunctionLimited).Is("OPENQUERY", "(", expressionList, ")");
            gb.Prod(rowsetFunctionLimited).Is("OPENROWSET", "(", expressionList, ")");

            gb.Prod(tableHintLimitedList).Is(tableHintLimited);
            gb.Prod(tableHintLimitedList).Is(tableHintLimitedList, ",", tableHintLimited);
            gb.Rule(tableHintLimited)
                .CanBe(tableHintLimitedName)
                .Or(tableHintLimitedName, "=", expression)
                .Or(tableHintLimitedName, "(", expressionList, ")")
                .Or(qualifiedName)
                .Or(qualifiedName, "=", expression)
                .Or(qualifiedName, "(", expressionList, ")");
            gb.Rule(tableHintLimitedName)
                .CanBe(identifierTerm)
                .OrKeywords("INDEX");

            gb.Prod(deleteStatementTail).Is(deleteOutputClause, deleteStatementTailNoOutput);
            gb.Prod(deleteStatementTail).Is(deleteStatementTailNoOutput);
            gb.Prod(deleteStatementTailNoOutput).Is(deleteSourceFromClause, deleteStatementTailNoFrom);
            gb.Prod(deleteStatementTailNoOutput).Is(deleteStatementTailNoFrom);
            gb.Prod(deleteStatementTailNoFrom).Is(deleteWhereClause, deleteOptionOpt);
            gb.Prod(deleteStatementTailNoFrom).Is(deleteOptionOpt);
            gb.Opt(deleteOptionOpt, deleteOptionClause);

            gb.Prod(deleteOutputClause).Is("OUTPUT", selectItemList);
            gb.Prod(deleteOutputClause).Is("OUTPUT", selectItemList, "INTO", deleteOutputTarget, deleteOutputIntoColumnListOpt);
            gb.Prod(deleteOutputTarget).Is(qualifiedName);
            gb.Prod(deleteOutputTarget).Is(variableReference);
            gb.Opt(deleteOutputIntoColumnListOpt, "(", identifierList, ")");

            gb.Prod(deleteSourceFromClause).Is("FROM", tableSourceList);

            gb.Rule(deleteWhereClause)
                .CanBe("WHERE", searchCondition)
                .Or("WHERE", "CURRENT", "OF", identifierTerm)
                .Or("WHERE", "CURRENT", "OF", variableReference)
                .Or("WHERE", "CURRENT", "OF", "GLOBAL", identifierTerm)
                .Or("WHERE", "CURRENT", "OF", "GLOBAL", variableReference);

            gb.Prod(deleteOptionClause).Is("OPTION", "(", deleteQueryHintList, ")");
            gb.Prod(deleteQueryHintList).Is(deleteQueryHint);
            gb.Prod(deleteQueryHintList).Is(deleteQueryHintList, ",", deleteQueryHint);
            gb.Prod(deleteQueryHint).Is(deleteQueryHintName);
            gb.Prod(deleteQueryHint).Is("MAXDOP", expression);
            gb.Prod(deleteQueryHint).Is("MAXDOP", "=", expression);
            gb.Prod(deleteQueryHint).Is("MAXRECURSION", expression);
            gb.Prod(deleteQueryHint).Is("MAXRECURSION", "=", expression);
            gb.Prod(deleteQueryHint).Is("QUERYTRACEON", expression);
            gb.Prod(deleteQueryHint).Is("MIN_GRANT_PERCENT", "=", expression);
            gb.Prod(deleteQueryHint).Is("MAX_GRANT_PERCENT", "=", expression);
            gb.Prod(deleteQueryHint).Is("LABEL", "=", expression);
            gb.Prod(deleteQueryHint).Is("USE", "HINT", "(", expressionList, ")");
            gb.Prod(deleteQueryHint).Is("HASH", "JOIN");
            gb.Prod(deleteQueryHint).Is("MERGE", "JOIN");
            gb.Prod(deleteQueryHint).Is("LOOP", "JOIN");
            gb.Prod(deleteQueryHint).Is("HASH", "GROUP");
            gb.Prod(deleteQueryHint).Is("ORDER", "GROUP");
            gb.Prod(deleteQueryHint).Is("MERGE", "UNION");
            gb.Prod(deleteQueryHint).Is("HASH", "UNION");
            gb.Prod(deleteQueryHint).Is("CONCAT", "UNION");
            gb.Prod(deleteQueryHint).Is("FORCE", "ORDER");
            gb.Prod(deleteQueryHint).Is("KEEP", "PLAN");
            gb.Prod(deleteQueryHint).Is("KEEPFIXED", "PLAN");
            gb.Prod(deleteQueryHint).Is("ROBUST", "PLAN");
            gb.Rule(deleteQueryHintName)
                .Keywords("RECOMPILE", "IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX");

            gb.Prod(optionClause).Is("OPTION", "(", deleteQueryHintList, ")");
            BuildProceduralGrammar();

            gb.Rule(executeStatement)
                .CanBe("EXEC", executeModuleCall)
                .Or("EXECUTE", executeModuleCall)
                .Or("EXEC", executeDynamicCall)
                .Or("EXECUTE", executeDynamicCall)
                .Or("EXEC", executeAsContext)
                .Or("EXECUTE", executeAsContext);

            gb.Prod(executeModuleCall).Is(executeModuleCallCore);
            gb.Prod(executeModuleCall).Is(executeModuleCallCore, executeWithOptions);
            gb.Prod(executeModuleCallCore).Is(executeModuleTarget);
            gb.Prod(executeModuleCallCore).Is(executeReturnAssignment, executeModuleTarget);
            gb.Prod(executeModuleCallCore).Is(executeModuleTarget, executeArgList);
            gb.Prod(executeModuleCallCore).Is(executeReturnAssignment, executeModuleTarget, executeArgList);
            gb.Prod(executeReturnAssignment).Is(variableReference, "=");
            gb.Prod(executeModuleTarget).Is(qualifiedName);
            gb.Prod(executeModuleTarget).Is(variableReference);

            gb.Prod(executeArgList).Is(executeArg);
            gb.Prod(executeArgList).Is(executeArgList, ",", executeArg);
            gb.Prod(executeArg).Is(executeArgValue);
            gb.Prod(executeArg).Is(executeArgNamePrefix, executeArgValue);
            gb.Prod(executeArgNamePrefix).Is(variableReference, "=");
            gb.Prod(executeArgValue).Is(expression);
            gb.Prod(executeArgValue).Is(variableReference, "OUTPUT");
            gb.Prod(executeArgValue).Is(variableReference, "OUT");
            gb.Prod(executeArgValue).Is("DEFAULT");

            gb.Prod(executeWithOptions).Is("WITH", executeOptionList);
            gb.Prod(executeOptionList).Is(executeOption);
            gb.Prod(executeOptionList).Is(executeOptionList, ",", executeOption);
            gb.Rule(executeOption)
                .CanBe("RECOMPILE")
                .Or("RESULT", "SETS", "UNDEFINED")
                .Or("RESULT", "SETS", "NONE")
                .Or("RESULT", "SETS", "(", executeResultSetsDefList, ")");
            gb.Prod(executeResultSetsDefList).Is(executeResultSetsDef);
            gb.Prod(executeResultSetsDefList).Is(executeResultSetsDefList, ",", executeResultSetsDef);
            gb.Prod(executeResultSetsDef).Is("(", executeColumnDefList, ")");
            gb.Prod(executeResultSetsDef).Is("AS", "OBJECT", qualifiedName);
            gb.Prod(executeResultSetsDef).Is("AS", "TYPE", qualifiedName);
            gb.Prod(executeResultSetsDef).Is("AS", "FOR", "XML");
            gb.Prod(executeColumnDefList).Is(executeColumnDef);
            gb.Prod(executeColumnDefList).Is(executeColumnDefList, ",", executeColumnDef);
            gb.Prod(executeColumnDef).Is(identifierTerm, typeSpec);
            gb.Prod(executeColumnDef).Is(identifierTerm, typeSpec, "COLLATE", identifierTerm);
            gb.Prod(executeColumnDef).Is(identifierTerm, typeSpec, executeNullability);
            gb.Prod(executeColumnDef).Is(identifierTerm, typeSpec, "COLLATE", identifierTerm, executeNullability);
            gb.Rule(executeNullability)
                .CanBe("NULL")
                .Or("NOT", "NULL");

            gb.Prod(executeDynamicCall).Is("(", expression, ")");
            gb.Prod(executeDynamicCall).Is("(", expression, ")", executeAsContext);
            gb.Prod(executeDynamicCall).Is("(", expression, ")", executeAtClause);
            gb.Prod(executeDynamicCall).Is("(", expression, ")", executeAsContext, executeAtClause);
            gb.Prod(executeDynamicCall).Is("(", expression, executeLinkedArgList, ")");
            gb.Prod(executeDynamicCall).Is("(", expression, executeLinkedArgList, ")", executeAsContext);
            gb.Prod(executeDynamicCall).Is("(", expression, executeLinkedArgList, ")", executeAtClause);
            gb.Prod(executeDynamicCall).Is("(", expression, executeLinkedArgList, ")", executeAsContext, executeAtClause);
            gb.Prod(executeLinkedArgList).Is(",", executeLinkedArg);
            gb.Prod(executeLinkedArgList).Is(executeLinkedArgList, ",", executeLinkedArg);
            gb.Prod(executeLinkedArg).Is(expression);
            gb.Prod(executeLinkedArg).Is(variableReference, "OUTPUT");
            gb.Prod(executeLinkedArg).Is(variableReference, "OUT");
            gb.Rule(executeAsContext)
                .CanBe("AS", "LOGIN", "=", stringLiteral)
                .Or("AS", "USER", "=", stringLiteral)
                .Or("AS", "LOGIN", "=", unicodeStringLiteral)
                .Or("AS", "USER", "=", unicodeStringLiteral)
                .Or("AS", "LOGIN", "=", identifierTerm)
                .Or("AS", "USER", "=", identifierTerm);
            gb.Prod(executeAtClause).Is("AT", identifierTerm);
            gb.Prod(executeAtClause).Is("AT", "DATA_SOURCE", identifierTerm);

            gb.Prod(useStatement).Is("USE", identifierTerm);

            gb.Prod(createProcKeyword).Is("PROC");
            gb.Prod(createProcKeyword).Is("PROCEDURE");

            gb.Prod(createProcHead).Is("CREATE", createProcKeyword);
            gb.Prod(createProcHead).Is("CREATE", "OR", "ALTER", createProcKeyword);
            gb.Prod(createProcHead).Is("ALTER", createProcKeyword);

            gb.Prod(createProcName).Is(qualifiedName);
            gb.Prod(createProcName).Is(qualifiedName, ";", number);

            gb.Opt(createProcSignatureParameterListOpt, createProcParameterList);
            gb.Opt(createProcSignatureWithClauseOpt, createProcWithClause);
            gb.Opt(createProcSignatureForReplicationOpt, createProcForReplicationClause);
            gb.Rule(createProcSignature).OneOf(
                gb.Seq(createProcName, createProcSignatureParameterListOpt, createProcSignatureWithClauseOpt, createProcSignatureForReplicationOpt));

            gb.Prod(createProcParameterList).Is(createProcParameter);
            gb.Prod(createProcParameterList).Is(createProcParameterList, ",", createProcParameter);
            gb.Prod(createProcParameter).Is(variableReference, typeSpec);
            gb.Prod(createProcParameter).Is(variableReference, typeSpec, createProcParameterOptionList);
            gb.Prod(createProcParameter).Is(variableReference, "AS", typeSpec);
            gb.Prod(createProcParameter).Is(variableReference, "AS", typeSpec, createProcParameterOptionList);
            gb.Prod(createProcParameterOptionList).Is(createProcParameterOption);
            gb.Prod(createProcParameterOptionList).Is(createProcParameterOptionList, createProcParameterOption);
            gb.Rule(createProcParameterOption)
                .CanBe("VARYING")
                .Or("NULL")
                .Or("NOT", "NULL")
                .Or("=", expression)
                .Or("OUT")
                .Or("OUTPUT")
                .Or("READONLY");

            gb.Prod(createProcWithClause).Is("WITH", createProcOptionList);
            gb.Prod(createProcOptionList).Is(createProcOption);
            gb.Prod(createProcOptionList).Is(createProcOptionList, ",", createProcOption);
            gb.Rule(createProcOption)
                .CanBe(createProcExecuteAsClause)
                .OrKeywords("ENCRYPTION", "RECOMPILE", "NATIVE_COMPILATION", "SCHEMABINDING");

            gb.Rule(createProcExecuteAsClause)
                .CanBe("EXECUTE", "AS", "CALLER")
                .Or("EXECUTE", "AS", "SELF")
                .Or("EXECUTE", "AS", "OWNER")
                .Or("EXECUTE", "AS", stringLiteral)
                .Or("EXECUTE", "AS", unicodeStringLiteral)
                .Or("EXECUTE", "AS", identifierTerm);

            gb.Prod(createProcForReplicationClause).Is("FOR", "REPLICATION");

            gb.Prod(createProcBody).Is("AS", createProcBodyBlock);
            gb.Prod(createProcBody).Is("AS", "EXTERNAL", "NAME", createProcExternalName);
            gb.Prod(createProcBody).Is(createProcNativeWithClause, "AS", "BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, "END");
            gb.Prod(createProcBody).Is(createProcNativeWithClause, "AS", "BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, statementSeparatorList, "END");

            gb.Prod(createProcNativeWithClause).Is("WITH", "NATIVE_COMPILATION", ",", "SCHEMABINDING");
            gb.Prod(createProcNativeWithClause).Is("WITH", "NATIVE_COMPILATION", ",", "SCHEMABINDING", ",", createProcExecuteAsClause);

            gb.Prod(createProcNativeAtomicOptionList).Is(createProcNativeAtomicOption);
            gb.Prod(createProcNativeAtomicOptionList).Is(createProcNativeAtomicOptionList, ",", createProcNativeAtomicOption);
            gb.Prod(createProcNativeAtomicOption).Is(identifierTerm, "=", expression);
            gb.Prod(createProcNativeAtomicOption).Is(qualifiedName, "=", expression);
            gb.Prod(createProcNativeAtomicOption).Is(identifierTerm, identifierTerm, "=", expression);
            gb.Prod(createProcNativeAtomicOption).Is(identifierTerm, identifierTerm, identifierTerm, "=", expression);
            gb.Prod(createProcNativeAtomicOption).Is("TRANSACTION", "ISOLATION", "LEVEL", "=", setTransactionIsolationLevel);

            gb.Prod(createProcExternalName).Is(qualifiedName);

            gb.Prod(createProcBodyBlock).Is(statementList);
            gb.Prod(createProcBodyBlock).Is(statementList, statementSeparatorList);
            gb.Prod(createProcBodyBlock).Is(statementSeparatorList, statementList);
            gb.Prod(createProcBodyBlock).Is(statementSeparatorList, statementList, statementSeparatorList);
            gb.Prod(createProcBodyBlock).Is("BEGIN", statementList, "END");
            gb.Prod(createProcBodyBlock).Is("BEGIN", statementList, statementSeparatorList, "END");
            gb.Prod(createProcBodyBlock).Is("BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, "END");
            gb.Prod(createProcBodyBlock).Is("BEGIN", "ATOMIC", "WITH", "(", createProcNativeAtomicOptionList, ")", statementList, statementSeparatorList, "END");

            gb.Prod(createProcStatement).Is(createProcHead, createProcSignature, createProcBody);

            gb.Prod(createFunctionHead).Is("CREATE", "FUNCTION");
            gb.Prod(createFunctionHead).Is("CREATE", "OR", "ALTER", "FUNCTION");
            gb.Prod(createFunctionHead).Is("ALTER", "FUNCTION");
            gb.Prod(createFunctionName).Is(qualifiedName);

            gb.Opt(createFunctionSignatureParameterListOpt, createFunctionParameterList);
            gb.Prod(createFunctionSignature).Is(
                createFunctionName,
                "(",
                createFunctionSignatureParameterListOpt,
                ")");

            gb.Prod(createFunctionParameterList).Is(createFunctionParameter);
            gb.Prod(createFunctionParameterList).Is(createFunctionParameterList, ",", createFunctionParameter);
            gb.Prod(createFunctionParameter).Is(variableReference, typeSpec);
            gb.Prod(createFunctionParameter).Is(variableReference, "AS", typeSpec);
            gb.Prod(createFunctionParameter).Is(variableReference, typeSpec, createFunctionParameterOptionList);
            gb.Prod(createFunctionParameter).Is(variableReference, "AS", typeSpec, createFunctionParameterOptionList);
            gb.Prod(createFunctionParameterOptionList).Is(createFunctionParameterOption);
            gb.Prod(createFunctionParameterOptionList).Is(createFunctionParameterOptionList, createFunctionParameterOption);
            gb.Rule(createFunctionParameterOption)
                .CanBe("NULL")
                .Or("NOT", "NULL")
                .Or("=", expression)
                .Or("READONLY");

            gb.Prod(createFunctionScalarReturnsClause).Is("RETURNS", typeSpec);
            gb.Prod(createFunctionInlineTableReturnsClause).Is("RETURNS", "TABLE");
            gb.Prod(createFunctionTableVariableReturnsClause).Is("RETURNS", createFunctionTableReturnDefinition);
            gb.Prod(createFunctionTableReturnDefinition).Is(variableReference, "TABLE", "(", createFunctionTableReturnItemList, ")");
            gb.Prod(createFunctionTableReturnItemList).Is(createFunctionTableReturnItem);
            gb.Prod(createFunctionTableReturnItemList).Is(createFunctionTableReturnItemList, ",", createFunctionTableReturnItem);
            gb.Prod(createFunctionTableReturnItem).Is(createTableColumnDefinition);
            gb.Prod(createFunctionTableReturnItem).Is(createTableComputedColumn);
            gb.Prod(createFunctionTableReturnItem).Is(createTableConstraint);
            gb.Prod(createFunctionTableReturnItem).Is(createTableTableIndex);

            gb.Prod(createFunctionWithClause).Is("WITH", createFunctionOptionList);
            gb.Prod(createFunctionOptionList).Is(createFunctionOption);
            gb.Prod(createFunctionOptionList).Is(createFunctionOptionList, ",", createFunctionOption);
            gb.Rule(createFunctionOption)
                .CanBe(createProcExecuteAsClause)
                .OrKeywords("ENCRYPTION", "SCHEMABINDING")
                .Or("RETURNS", "NULL", "ON", "NULL", "INPUT")
                .Or("CALLED", "ON", "NULL", "INPUT")
                .Or("INLINE", "=", "ON")
                .Or("INLINE", "=", "OFF");

            gb.Rule(createFunctionPreludeStatement).OneOf(
                createFunctionPreludeStatementNoLeadingWith,
                leadingWithStatement);
            gb.Rule(createFunctionPreludeStatementNoLeadingWith).OneOf(createFunctionPreludeStatementNoLeadingWithAlternatives);
            gb.Rule(createFunctionImplicitPreludeStatementNoLeadingWith).OneOf(createFunctionImplicitPreludeStatementNoLeadingWithAlternatives);
            gb.Rule(createFunctionPreludeStatementList).OneOf(
                createFunctionPreludeStatement,
                gb.Seq(createFunctionPreludeStatementList, statementSeparatorList, createFunctionPreludeStatement),
                gb.Seq(createFunctionPreludeStatementList, createFunctionImplicitPreludeStatementNoLeadingWith));
            gb.Rule(createFunctionPreludeBeforeReturnOpt).OneOf(
                EmptyTerm.Empty,
                createFunctionPreludeStatementList,
                gb.Seq(createFunctionPreludeStatementList, statementSeparatorList));
            gb.Opt(createFunctionBodyTrailingSeparatorsOpt, statementSeparatorList);
            gb.Prod(createFunctionScalarReturnStatement).Is("RETURN", expression);
            gb.Prod(createFunctionTableVariableReturnStatement).Is("RETURN");

            gb.Prod(createFunctionScalarBody).Is(
                "AS",
                "BEGIN",
                createFunctionPreludeBeforeReturnOpt,
                createFunctionScalarReturnStatement,
                createFunctionBodyTrailingSeparatorsOpt,
                "END");
            gb.Prod(createFunctionInlineTableBody).Is("AS", "RETURN", queryExpression);
            gb.Prod(createFunctionInlineTableBody).Is("AS", "RETURN", "(", queryExpression, ")");
            gb.Prod(createFunctionInlineTableBody).Is("AS", "RETURN", "(", withClause, queryExpression, ")");
            gb.Prod(createFunctionTableVariableBody).Is(
                "AS",
                "BEGIN",
                createFunctionPreludeBeforeReturnOpt,
                createFunctionTableVariableReturnStatement,
                createFunctionBodyTrailingSeparatorsOpt,
                "END");

            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionScalarReturnsClause,
                createFunctionScalarBody);
            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionScalarReturnsClause,
                createFunctionWithClause,
                createFunctionScalarBody);
            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionInlineTableReturnsClause,
                createFunctionInlineTableBody);
            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionInlineTableReturnsClause,
                createFunctionWithClause,
                createFunctionInlineTableBody);
            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionTableVariableReturnsClause,
                createFunctionTableVariableBody);
            gb.Prod(createFunctionStatement).Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionTableVariableReturnsClause,
                createFunctionWithClause,
                createFunctionTableVariableBody);

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
            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.SynapseExtensions))
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
            gb.Prod(dropIndexSpec).Is(qualifiedName, ".", identifierTerm);
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
            gb.Prod(dropStatisticsTarget).Is(qualifiedName, ".", identifierTerm);

            gb.Prod(dropDatabaseStatement).Is("DROP", "DATABASE", identifierTerm);
            gb.Prod(dropDatabaseStatement).Is("DROP", "DATABASE", dropIfExistsClause, identifierTerm);

            BuildTriggerGrammar(
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

            gb.Prod(createRoleStatement).Is("CREATE", "ROLE", identifierTerm);
            gb.Prod(createRoleStatement).Is("CREATE", "ROLE", identifierTerm, "AUTHORIZATION", identifierTerm);

            gb.Prod(createSchemaStatement).Is("CREATE", "SCHEMA", schemaNameClause);
            gb.Prod(schemaNameClause).Is(identifierTerm);
            gb.Prod(schemaNameClause).Is("AUTHORIZATION", identifierTerm);
            gb.Prod(schemaNameClause).Is(identifierTerm, "AUTHORIZATION", identifierTerm);

            gb.Prod(createViewHead).Is("CREATE", "VIEW");
            gb.Prod(createViewHead).Is("CREATE", "OR", "ALTER", "VIEW");
            gb.Prod(createViewHead).Is("ALTER", "VIEW");
            gb.Prod(createViewColumnList).Is("(", identifierList, ")");
            gb.Prod(createViewOptionClause).Is("WITH", createViewOptionList);
            gb.Prod(createViewQuery).Is(queryExpression);
            gb.Prod(createViewQuery).Is(withClause, queryExpression);
            gb.Prod(createViewBody).Is("AS", createViewQuery);
            gb.Prod(createViewBody).Is("AS", createViewQuery, createViewCheckOptionOpt);
            gb.Opt(createViewCheckOptionOpt, withCheckOptionStart, "CHECK", "OPTION");
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewBody);
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewColumnList, createViewBody);
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewOptionClause, createViewBody);
            gb.Prod(createViewStatement).Is(createViewHead, qualifiedName, createViewColumnList, createViewOptionClause, createViewBody);
            gb.Rule(createViewOptionList).SeparatedBy(",", createViewOption);
            gb.Rule(createViewOption).Keywords("ENCRYPTION", "SCHEMABINDING", "VIEW_METADATA");

            gb.Prod(createTableFileTableClause).Is("AS", "FILETABLE");
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")");
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause);
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, createIndexFileStreamClause);
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, createTableOptions);
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, createIndexFileStreamClause, createTableOptions);
            gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", createTableTailClauseList);
            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", "AS", "EDGE");
                gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", "AS", "NODE");
                gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", createTableTailClauseList, "AS", "EDGE");
                gb.Prod(createTableStatement).Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", createTableTailClauseList, "AS", "NODE");
            }
            gb.Prod(createTableTailClauseList).Is(createTableTailClause);
            gb.Prod(createTableTailClauseList).Is(createTableTailClauseList, createTableTailClause);
            gb.Prod(createTableTailClause).Is(createTablePeriodClause);
            gb.Prod(createTableTailClause).Is(createTableOptions);
            gb.Prod(createTableTailClause).Is(createTableOnClause);
            gb.Prod(createTableTailClause).Is(createTableTextImageClause);
            gb.Prod(createTableTailClause).Is(createIndexFileStreamClause);

            gb.Prod(createTableElementList).Is(createTableElement);
            gb.Prod(createTableElementList).Is(createTableElementList, ",", createTableElement);
            gb.Prod(createTableElement).Is(createTableColumnDefinition);
            gb.Prod(createTableElement).Is(createTableComputedColumn);
            gb.Prod(createTableElement).Is(createTableColumnSet);
            gb.Prod(createTableElement).Is(createTableConstraint);
            gb.Prod(createTableElement).Is(createTableTableIndex);

            gb.Prod(createTableColumnDefinition).Is(identifierTerm, typeSpec);
            gb.Prod(createTableColumnDefinition).Is(identifierTerm, typeSpec, createTableColumnOptionList);
            gb.Prod(createTableColumnOptionList).Is(createTableColumnOption);
            gb.Prod(createTableColumnOptionList).Is(createTableColumnOptionList, createTableColumnOption);
            gb.Prod(createTableColumnOption).Is("NULL");
            gb.Prod(createTableColumnOption).Is("NOT", "NULL");
            gb.Prod(createTableColumnOption).Is("PRIMARY", "KEY");
            gb.Prod(createTableColumnOption).Is("UNIQUE");
            gb.Prod(createTableColumnOption).Is("SPARSE");
            gb.Prod(createTableColumnOption).Is("PERSISTED");
            gb.Prod(createTableColumnOption).Is("ROWGUIDCOL");
            gb.Prod(createTableColumnOption).Is("COLUMN_SET");
            gb.Prod(createTableColumnOption).Is("FOR", "ALL_SPARSE_COLUMNS");
            gb.Prod(createTableColumnOption).Is("FILESTREAM");
            gb.Prod(createTableColumnOption).Is("DEFAULT", expression);
            gb.Prod(createTableColumnOption).Is("DEFAULT", "(", expression, ")");
            gb.Prod(createTableColumnOption).Is("IDENTITY");
            gb.Prod(createTableColumnOption).Is("IDENTITY", "(", expression, ",", expression, ")");
            gb.Prod(createTableColumnOption).Is("COLLATE", strictIdentifierTerm);
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "ROW", "START");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "ROW", "START", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "ROW", "END");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "ROW", "END", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "START");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "START", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "END");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "END", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "START");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "START", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "END");
            gb.Prod(createTableColumnOption).Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "END", "HIDDEN");
            gb.Prod(createTableColumnOption).Is("MASKED", "WITH", "(", maskingOptionList, ")");
            gb.Prod(createTableColumnOption).Is("ENCRYPTED", "WITH", "(", encryptionOptionList, ")");
            gb.Prod(createTableColumnOption).Is("NOT", "FOR", "REPLICATION");
            gb.Prod(createTableColumnOption).Is("CHECK", "(", searchCondition, ")");
            gb.Prod(createTableColumnOption).Is("REFERENCES", qualifiedName);
            gb.Prod(createTableColumnOption).Is("REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod(createTableColumnOption).Is("CONSTRAINT", strictIdentifierTerm, createTableColumnConstraintBody);

            gb.Prod(createTableComputedColumn).Is(identifierTerm, "AS", expression);
            gb.Prod(createTableComputedColumn).Is(identifierTerm, "AS", expression, createTableColumnOptionList);

            gb.Prod(createTableColumnSet).Is(identifierTerm, typeSpec, "COLUMN_SET", "FOR", "ALL_SPARSE_COLUMNS");

            gb.Prod(createTableConstraint).Is(createTableTableConstraintBody);
            gb.Prod(createTableConstraint).Is("CONSTRAINT", identifierTerm, createTableTableConstraintBody);

            gb.Prod(createTableColumnConstraintBody).Is("PRIMARY", "KEY");
            gb.Prod(createTableColumnConstraintBody).Is("PRIMARY", "KEY", createTableColumnKeyClusterType);
            gb.Prod(createTableColumnConstraintBody).Is("UNIQUE");
            gb.Prod(createTableColumnConstraintBody).Is("UNIQUE", createTableColumnKeyClusterType);
            gb.Prod(createTableColumnConstraintBody).Is("CHECK", "(", searchCondition, ")");
            gb.Prod(createTableColumnConstraintBody).Is("FOREIGN", "KEY", "REFERENCES", qualifiedName);
            gb.Prod(createTableColumnConstraintBody).Is("FOREIGN", "KEY", "REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod(createTableColumnConstraintBody).Is("REFERENCES", qualifiedName);
            gb.Prod(createTableColumnConstraintBody).Is("REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod(createTableColumnConstraintBody).Is("DEFAULT", expression);
            gb.Prod(createTableColumnConstraintBody).Is("DEFAULT", "(", expression, ")");

            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "ON", indexStorageTarget);
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod(createTableTableConstraintBody).Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")", "ON", indexStorageTarget);
            gb.Prod(createTableTableConstraintBody).Is("UNIQUE", "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableConstraintBody).Is("UNIQUE", createTableConstraintClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableConstraintBody).Is("UNIQUE", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod(createTableTableConstraintBody).Is("UNIQUE", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod(createTableTableConstraintBody).Is("UNIQUE", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")", "ON", indexStorageTarget);
            gb.Prod(createTableTableConstraintBody).Is("CHECK", "(", searchCondition, ")");
            gb.Prod(createTableTableConstraintBody).Is("FOREIGN", "KEY", "(", identifierList, ")", "REFERENCES", qualifiedName);
            gb.Prod(createTableTableConstraintBody).Is("FOREIGN", "KEY", "(", identifierList, ")", "REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod(createTableTableConstraintBody).Is("DEFAULT", expression, "FOR", identifierTerm);
            gb.Prod(createTableTableConstraintBody).Is("DEFAULT", "(", expression, ")", "FOR", identifierTerm);

            gb.Rule(createTableColumnKeyClusterType)
                .CanBe("CLUSTERED")
                .Or("NONCLUSTERED");

            gb.Rule(createTableConstraintClusterType)
                .CanBe("NONCLUSTERED", "HASH")
                .Or("CLUSTERED")
                .Or("NONCLUSTERED");

            gb.Rule(createTableClusterType)
                .CanBe("NONCLUSTERED", "HASH")
                .Or("CLUSTERED", "COLUMNSTORE")
                .Or("NONCLUSTERED", "COLUMNSTORE")
                .Or("CLUSTERED")
                .Or("NONCLUSTERED");

            gb.Prod(createTableKeyColumnList).Is(createTableKeyColumn);
            gb.Prod(createTableKeyColumnList).Is(createTableKeyColumnList, ",", createTableKeyColumn);
            gb.Prod(createTableKeyColumn).Is(identifierTerm);
            gb.Prod(createTableKeyColumn).Is(identifierTerm, "ASC");
            gb.Prod(createTableKeyColumn).Is(identifierTerm, "DESC");

            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "CLUSTERED", "COLUMNSTORE");
            gb.Prod(createTableTableIndex).Is("INDEX", identifierTerm, "CLUSTERED", "COLUMNSTORE", createIndexWithClause);

            gb.Prod(createTablePeriodClause).Is("PERIOD", forSystemTimeStart, "SYSTEM_TIME", "(", identifierTerm, ",", identifierTerm, ")");
            gb.Prod(createTableOptions).Is("WITH", "(", createTableOptionList, ")");
            gb.Prod(createTableOptionList).Is(createTableOption);
            gb.Prod(createTableOptionList).Is(createTableOptionList, ",", createTableOption);
            gb.Prod(createTableOption).Is("MEMORY_OPTIMIZED", "=", indexOnOffValue);
            gb.Prod(createTableOption).Is("DURABILITY", "=", createTableDurabilityMode);
            gb.Prod(createTableOption).Is("FILETABLE_DIRECTORY", "=", namedOptionValue);
            gb.Prod(createTableOption).Is("FILETABLE_COLLATE_FILENAME", "=", namedOptionValue);
            gb.Prod(createTableOption).Is("CLUSTERED", "COLUMNSTORE", "INDEX");
            gb.Rule(createTableDurabilityMode).Keywords("SCHEMA_AND_DATA", "SCHEMA_ONLY");
            gb.Prod(createTableOnClause).Is("ON", indexStorageTarget);
            gb.Prod(createTableTextImageClause).Is("TEXTIMAGE_ON", indexStorageTarget);

            gb.Prod(alterTableStatement).Is("ALTER", "TABLE", qualifiedName, alterTableAction);
            gb.Prod(alterTableAction).Is("ADD", alterTableAddItemList);
            gb.Prod(alterTableAction).Is("ALTER", "COLUMN", alterTableAlterColumnAction);
            gb.Prod(alterTableAction).Is("DROP", alterTableDropItemList);
            gb.Prod(alterTableAction).Is("WITH", alterTableCheckMode, "ADD", createTableConstraint);
            gb.Prod(alterTableAction).Is(alterTableCheckMode, "CONSTRAINT", alterTableConstraintTarget);
            gb.Prod(alterTableAction).Is("ENABLE", "TRIGGER", alterTableTriggerTarget);
            gb.Prod(alterTableAction).Is("DISABLE", "TRIGGER", alterTableTriggerTarget);
            gb.Prod(alterTableAction).Is("ADD", createTablePeriodClause);
            gb.Prod(alterTableAction).Is("DROP", "PERIOD", forSystemTimeStart, "SYSTEM_TIME");
            gb.Prod(alterTableAction).Is("SET", "(", indexOptionList, ")");
            gb.Prod(alterTableAddItemList).Is(alterTableAddItem);
            gb.Prod(alterTableAddItemList).Is(alterTableAddItemList, ",", alterTableAddItem);
            gb.Prod(alterTableAddItem).Is(createTableColumnDefinition);
            gb.Prod(alterTableAddItem).Is(createTableConstraint);
            gb.Prod(alterTableAddItem).Is(createTableTableIndex);
            gb.Prod(alterTableAddItem).Is(createTableComputedColumn);
            gb.Prod(alterTableAlterColumnAction).Is(identifierTerm, typeSpec);
            gb.Prod(alterTableAlterColumnAction).Is(identifierTerm, typeSpec, alterTableColumnOptionList);
            gb.Prod(alterTableAlterColumnAction).Is(identifierTerm, "ADD", alterTableColumnOptionList);
            gb.Prod(alterTableAlterColumnAction).Is(identifierTerm, "DROP", alterTableColumnOptionList);
            gb.Prod(alterTableColumnOptionList).Is(alterTableColumnOption);
            gb.Prod(alterTableColumnOptionList).Is(alterTableColumnOptionList, alterTableColumnOption);
            gb.Prod(alterTableColumnOption).Is(createTableColumnOption);
            gb.Prod(alterTableDropItemList).Is(alterTableDropItem);
            gb.Prod(alterTableDropItemList).Is(alterTableDropItemList, ",", alterTableDropItem);
            gb.Prod(alterTableDropItem).Is("COLUMN", identifierTerm);
            gb.Prod(alterTableDropItem).Is("CONSTRAINT", identifierTerm);
            gb.Prod(alterTableDropItem).Is("CONSTRAINT", "ALL");
            gb.Rule(alterTableCheckMode).Keywords("CHECK", "NOCHECK");
            gb.Rule(alterTableConstraintTarget)
                .CanBe(identifierTerm)
                .OrKeywords("ALL");
            gb.Rule(alterTableTriggerTarget)
                .CanBe(identifierTerm)
                .OrKeywords("ALL");

            gb.Prod(createKeyListIndexHead).Is("CREATE", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "UNIQUE", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "CLUSTERED", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "NONCLUSTERED", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "UNIQUE", "CLUSTERED", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "UNIQUE", "NONCLUSTERED", "INDEX", identifierTerm);
            gb.Prod(createKeyListIndexHead).Is("CREATE", "NONCLUSTERED", "COLUMNSTORE", "INDEX", identifierTerm);
            gb.Prod(createKeylessIndexHead).Is("CREATE", "CLUSTERED", "COLUMNSTORE", "INDEX", identifierTerm);
            gb.Prod(createIndexStatement).Is(createKeyListIndexHead, "ON", qualifiedName, "(", createIndexKeyList, ")");
            gb.Prod(createIndexStatement).Is(createKeyListIndexHead, "ON", qualifiedName, "(", createIndexKeyList, ")", createIndexTailClauseList);
            gb.Prod(createIndexStatement).Is(createKeylessIndexHead, "ON", qualifiedName);
            gb.Prod(createIndexStatement).Is(createKeylessIndexHead, "ON", qualifiedName, createIndexTailClauseList);

            gb.Prod(createIndexKeyList).Is(createIndexKeyItem);
            gb.Prod(createIndexKeyList).Is(createIndexKeyList, ",", createIndexKeyItem);
            gb.Prod(createIndexKeyItem).Is(identifierTerm);
            gb.Prod(createIndexKeyItem).Is(identifierTerm, "ASC");
            gb.Prod(createIndexKeyItem).Is(identifierTerm, "DESC");

            gb.Prod(createIndexTailClauseList).Is(createIndexTailClause);
            gb.Prod(createIndexTailClauseList).Is(createIndexTailClauseList, createIndexTailClause);
            gb.Prod(createIndexTailClause).Is(createIndexIncludeClause);
            gb.Prod(createIndexTailClause).Is(createIndexWhereClause);
            gb.Prod(createIndexTailClause).Is(createIndexWithClause);
            gb.Prod(createIndexTailClause).Is(createIndexStorageClause);
            gb.Prod(createIndexTailClause).Is(createIndexFileStreamClause);

            gb.Prod(createIndexIncludeClause).Is("INCLUDE", "(", createIndexIncludeList, ")");
            gb.Prod(createIndexIncludeList).Is(identifierTerm);
            gb.Prod(createIndexIncludeList).Is(createIndexIncludeList, ",", identifierTerm);
            gb.Prod(createIndexWhereClause).Is("WHERE", searchCondition);
            gb.Prod(createIndexWithClause).Is("WITH", "(", indexOptionList, ")");
            gb.Prod(createIndexStorageClause).Is("ON", indexStorageTarget);
            gb.Prod(createIndexFileStreamClause).Is("FILESTREAM_ON", indexFileStreamTarget);

            gb.Prod(indexStorageTarget).Is(qualifiedName);
            gb.Prod(indexStorageTarget).Is("DEFAULT");
            gb.Prod(indexStorageTarget).Is(qualifiedName, "(", identifierTerm, ")");
            gb.Prod(indexFileStreamTarget).Is(qualifiedName);
            gb.Prod(indexFileStreamTarget).Is("NULL");
            gb.Prod(indexFileStreamTarget).Is(qualifiedName, "(", identifierTerm, ")");

            gb.Prod(indexOptionList).Is(indexOption);
            gb.Prod(indexOptionList).Is(indexOptionList, ",", indexOption);
            gb.Prod(indexOptionList).Is(indexOptionList, ","); // allow trailing comma
            gb.Prod(indexOption).Is(indexOptionName, "=", indexOptionValue);
            gb.Prod(indexOption).Is(indexOptionName, "(", indexOptionList, ")");
            gb.Prod(indexOption).Is(indexOptionName, "=", indexOptionValue, "ON", "PARTITIONS", "(", indexPartitionList, ")");

            gb.Rule(indexOptionName)
                .CanBe("PAD_INDEX")
                .Or("FILLFACTOR")
                .Or("SORT_IN_TEMPDB")
                .Or("IGNORE_DUP_KEY")
                .Or("STATISTICS_NORECOMPUTE")
                .Or("STATISTICS_INCREMENTAL")
                .Or("DROP_EXISTING")
                .Or("ONLINE")
                .Or("ALLOW_ROW_LOCKS")
                .Or("ALLOW_PAGE_LOCKS")
                .Or("OPTIMIZE_FOR_SEQUENTIAL_KEY")
                .Or("MAXDOP")
                .Or("MAX_DURATION")
                .Or("DATA_COMPRESSION")
                .Or("XML_COMPRESSION")
                .Or("COMPRESSION_DELAY")
                .Or("WAIT_AT_LOW_PRIORITY")
                .Or("ABORT_AFTER_WAIT")
                .Or("BUCKET_COUNT")
                .Or("COMPRESS_ALL_ROW_GROUPS")
                .Or("LOB_COMPACTION")
                .Or("RESUMABLE");

            gb.Prod(indexOptionValue).Is(expression);
            gb.Prod(indexOptionValue).Is(indexOnOffValue);
            gb.Prod(indexOptionValue).Is("NONE");
            gb.Prod(indexOptionValue).Is("SELF");
            gb.Prod(indexOptionValue).Is("BLOCKERS");
            gb.Prod(indexOptionValue).Is("ROW");
            gb.Prod(indexOptionValue).Is("PAGE");
            gb.Prod(indexOptionValue).Is("COLUMNSTORE");
            gb.Prod(indexOptionValue).Is("COLUMNSTORE_ARCHIVE");
            gb.Prod(indexOptionValue).Is(expression, "MINUTES");
            gb.Prod(indexOptionValue).Is(indexOnOffValue, "(", indexOptionList, ")");
            gb.Prod(indexOptionValue).Is("(", indexOptionList, ")");
            gb.Rule(indexOnOffValue).Keywords("ON", "OFF");

            gb.Prod(namedOptionValue).Is(expression);
            gb.Prod(namedOptionValue).Is(identifierTerm);
            gb.Prod(namedOptionValue).Is(qualifiedName);
            gb.Prod(namedOptionValue).Is("ON");
            gb.Prod(namedOptionValue).Is("OFF");

            gb.Prod("MaskingOptionList").Is("FUNCTION", "=", expression);

            gb.Prod("EncryptionOptionList").Is("COLUMN_ENCRYPTION_KEY", "=", namedOptionValue);
            gb.Prod("EncryptionOptionList").Is(encryptionOptionList, ",", "ENCRYPTION_TYPE", "=", namedOptionValue);
            gb.Prod("EncryptionOptionList").Is(encryptionOptionList, ",", "ALGORITHM", "=", namedOptionValue);
            gb.Prod("EncryptionOptionList").Is(encryptionOptionList, ",", "COLUMN_ENCRYPTION_KEY", "=", namedOptionValue);
            gb.Prod("EncryptionOptionList").Is("ENCRYPTION_TYPE", "=", namedOptionValue);
            gb.Prod("EncryptionOptionList").Is("ALGORITHM", "=", namedOptionValue);

            gb.Prod(indexPartitionList).Is(indexPartitionItem);
            gb.Prod(indexPartitionList).Is(indexPartitionList, ",", indexPartitionItem);
            gb.Prod(indexPartitionItem).Is(expression);
            gb.Prod(indexPartitionItem).Is(expression, "TO", expression);
            gb.Prod(indexPartitionItem).Is("ALL");

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

            gb.Prod("CreateDatabaseOnClause").Is("ON", createDatabaseOnFilespecSequence);
            gb.Prod("CreateDatabaseOnClause").Is("ON", "PRIMARY", createDatabaseOnFilespecSequence);
            gb.Prod("CreateDatabaseOnFilespecSequence").Is(createDatabaseFilespec);
            gb.Prod("CreateDatabaseOnFilespecSequence").Is(createDatabaseFilespec, ",", createDatabaseOnFilespecSequence);
            gb.Prod("CreateDatabaseOnFilespecSequence").Is(createDatabaseFilespec, ",", createDatabaseFilegroup);
            gb.Prod("CreateDatabaseOnFilespecSequence").Is(createDatabaseFilespec, ",", "LOG", "ON", createDatabaseFilespecList);

            gb.Prod("CreateDatabaseFilespecList").Is(createDatabaseFilespec);
            gb.Prod("CreateDatabaseFilespecList").Is(createDatabaseFilespecList, ",", createDatabaseFilespec);

            gb.Prod("CreateDatabaseFilegroup").Is("FILEGROUP", identifierTerm, createDatabaseOnFilespecSequence);
            gb.Prod("CreateDatabaseFilegroup").Is("FILEGROUP", identifierTerm, "DEFAULT", createDatabaseOnFilespecSequence);
            gb.Prod("CreateDatabaseFilegroup").Is("FILEGROUP", identifierTerm, "CONTAINS", "FILESTREAM", createDatabaseOnFilespecSequence);
            gb.Prod("CreateDatabaseFilegroup").Is("FILEGROUP", identifierTerm, "CONTAINS", "FILESTREAM", "DEFAULT", createDatabaseOnFilespecSequence);
            gb.Prod("CreateDatabaseFilegroup").Is("FILEGROUP", identifierTerm, "CONTAINS", "MEMORY_OPTIMIZED_DATA", createDatabaseOnFilespecSequence);

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
                "NAME",
                "=",
                createDatabaseFileName,
                ",",
                "FILENAME",
                "=",
                stringLiteral,
                ")");
            gb.Prod("CreateDatabaseFilespec").Is(
                "(",
                "NAME",
                "=",
                createDatabaseFileName,
                ",",
                "FILENAME",
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

            BuildQueryExpressionGrammar();
            BuildTableSourceGrammar();

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

            gb.Prod("SearchCondition").Is(booleanOrExpression);

            gb.Prod("Expression").Is(additiveExpression);

            gb.Prod("BooleanOrExpression").Is(booleanAndExpression);
            gb.Prod("BooleanOrExpression").Is(booleanOrExpression, "OR", booleanAndExpression);

            gb.Prod("BooleanAndExpression").Is(booleanNotExpression);
            gb.Prod("BooleanAndExpression").Is(booleanAndExpression, "AND", booleanNotExpression);

            gb.Prod("BooleanNotExpression").Is(booleanPrimary);
            gb.Prod("BooleanNotExpression").Is("NOT", booleanNotExpression);

            gb.Prod("BooleanPrimary").Is("(", searchCondition, ")");
            gb.Prod("BooleanPrimary").Is(additiveExpression, comparisonOperator, additiveExpression);
            gb.Prod("BooleanPrimary").Is(additiveExpression, "LIKE", additiveExpression);
            gb.Prod("BooleanPrimary").Is(additiveExpression, "LIKE", additiveExpression, "ESCAPE", additiveExpression);
            gb.Prod("BooleanPrimary").Is(additiveExpression, "NOT", "LIKE", additiveExpression);
            gb.Prod("BooleanPrimary").Is(additiveExpression, "NOT", "LIKE", additiveExpression, "ESCAPE", additiveExpression);
            gb.Prod("BooleanPrimary").Is(additiveExpression, "IN", inPredicateValue);
            gb.Prod("BooleanPrimary").Is(additiveExpression, "NOT", "IN", inPredicateValue);
            gb.Prod("BooleanPrimary").Is(additiveExpression, "IS", "NULL");
            gb.Prod("BooleanPrimary").Is(additiveExpression, "IS", "NOT", "NULL");
            gb.Prod("BooleanPrimary").Is(additiveExpression, "BETWEEN", additiveExpression, "AND", additiveExpression);
            gb.Prod("BooleanPrimary").Is(additiveExpression, "NOT", "BETWEEN", additiveExpression, "AND", additiveExpression);
            gb.Prod("BooleanPrimary").Is("EXISTS", "(", queryExpression, ")");
            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Prod("BooleanPrimary").Is("MATCH", "(", matchGraphPattern, ")");
            }

            gb.Prod("InPredicateValue").Is("(", expressionList, ")");
            gb.Prod("InPredicateValue").Is("(", queryExpression, ")");

            gb.Rule("ComparisonOperator")
                .CanBe("=")
                .Or("<>")
                .Or("!=")
                .Or("<")
                .Or("<=")
                .Or(">")
                .Or(">=");

            gb.Prod("AdditiveExpression").Is(multiplicativeExpression);
            gb.Prod("AdditiveExpression").Is(additiveExpression, "+", multiplicativeExpression);
            gb.Prod("AdditiveExpression").Is(additiveExpression, "-", multiplicativeExpression);
            gb.Prod("AdditiveExpression").Is(additiveExpression, "&", multiplicativeExpression);
            gb.Prod("AdditiveExpression").Is(additiveExpression, "|", multiplicativeExpression);
            gb.Prod("AdditiveExpression").Is(additiveExpression, "^", multiplicativeExpression);

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
            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Prod("PrimaryExpression").Is(graphColumnRef);
            }
            gb.Prod("PrimaryExpression").Is(qualifiedName);
            gb.Prod("PrimaryExpression").Is(functionCall);
            gb.Prod("PrimaryExpression").Is(functionCall, overClause);
            gb.Prod("PrimaryExpression").Is(functionCall, graphWithinGroupClause);
            gb.Prod("PrimaryExpression").Is("CAST", "(", expression, "AS", typeSpec, ")");
            gb.Prod("PrimaryExpression").Is("(", expression, ")");
            gb.Prod("PrimaryExpression").Is("(", queryExpression, ")");
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
            gb.Prod("FunctionCall").Is(qualifiedName, ".", identifierTerm, "(", ")");
            gb.Prod("FunctionCall").Is(qualifiedName, ".", identifierTerm, "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is(variableReference, ".", identifierTerm, "(", ")");
            gb.Prod("FunctionCall").Is(variableReference, ".", identifierTerm, "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("LEFT", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("RIGHT", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("COALESCE", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("NULLIF", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("IIF", "(", iifArgumentList, ")");
            gb.Prod("FunctionCall").Is("UPDATE", "(", functionArgumentList, ")");
            gb.Prod("FunctionCall").Is("NEXT", identifierTerm, "FOR", qualifiedName);
            // OPENROWSET(BULK ...) special form
            gb.Prod("FunctionCall").Is("OPENROWSET", "(", openRowsetBulk, ")");
            gb.Prod("OpenRowsetBulk").Is("BULK", expression, ",", openRowsetBulkOptionList);
            gb.Prod("OpenRowsetBulkOptionList").Is(openRowsetBulkOption);
            gb.Prod("OpenRowsetBulkOptionList").Is(openRowsetBulkOptionList, ",", openRowsetBulkOption);
            gb.Prod("OpenRowsetBulkOption").Is("SINGLE_BLOB");
            gb.Prod("OpenRowsetBulkOption").Is("SINGLE_CLOB");
            gb.Prod("OpenRowsetBulkOption").Is("SINGLE_NCLOB");
            gb.Prod("OpenRowsetBulkOption").Is("DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("CODEPAGE", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("DATAFILETYPE", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("FORMAT", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("FORMATFILE", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("FORMATFILE_DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("FIELDTERMINATOR", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("FIELDQUOTE", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("ROWTERMINATOR", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("FIRSTROW", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("LASTROW", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("MAXERRORS", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("ERRORFILE", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("ERRORFILE_DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("ROWS_PER_BATCH", "=", namedOptionValue);
            gb.Prod("OpenRowsetBulkOption").Is("ORDER", "(", createTableKeyColumnList, ")");
            gb.Prod("FunctionArgumentList").Is(expression);
            gb.Prod("FunctionArgumentList").Is(functionArgumentList, ",", expression);
            gb.Prod("IifArgumentList").Is(searchCondition, ",", expression, ",", expression);
            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Prod("GraphWithinGroupClause").Is("WITHIN", "GROUP", "(", "GRAPH", "PATH", ")");
            }
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
            gb.Prod("CaseWhen").Is("WHEN", searchCondition, "THEN", expression);
            gb.Prod("CaseWhen").Is("WHEN", expression, "THEN", expression);

            gb.Prod("ExpressionList").Is(expression);
            gb.Prod("ExpressionList").Is(expressionList, ",", expression);
            gb.Prod("GroupingSetList").Is(groupingSet);
            gb.Prod("GroupingSetList").Is(groupingSetList, ",", groupingSet);
            gb.Prod("GroupingSet").Is("(", expressionList, ")");
            gb.Prod("GroupingSet").Is("(", ")");
            gb.Prod("IdentifierList").Is(identifierTerm);
            gb.Prod("IdentifierList").Is(identifierList, ",", identifierTerm);

            gb.Rule(strictIdentifierTerm).OneOf(
                identifier,
                bracketIdentifier,
                quotedIdentifier,
                tempIdentifier,
                sqlcmdVariable);
            gb.Rule(identifierTerm).OneOf(strictIdentifierTerm);
            // contextual keywords used as identifiers in SQL Server
            gb.Rule("IdentifierTerm").Keywords(
                "TYPE",
                "OPENQUERY",
                "OPENROWSET",
                "BINARY",
                "XML",
                "JSON",
                "MAX",
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
                "NAME",
                "FILENAME",
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
                "INDEX",
                "MASKED",
                "ENCRYPTED",
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
                "LABEL",
                "LANGUAGE",
                "GENERATED",
                "ALWAYS",
                "HIDDEN",
                "TRANSACTION_ID",
                "SEQUENCE_NUMBER",
                "WINDOWS",
                "PROVIDER",
                "CERTIFICATE",
                "ASYMMETRIC",
                "SID",
                "DEFAULT_DATABASE",
                "DEFAULT_LANGUAGE",
                "CHECK_EXPIRATION",
                "CHECK_POLICY",
                "CREDENTIAL",
                "HASHED",
                "MUST_CHANGE",
                "OBJECT_ID",
                "SINGLE_BLOB",
                "SINGLE_CLOB",
                "SINGLE_NCLOB",
                "CODEPAGE",
                "DATAFILETYPE",
                "FORMATFILE",
                "FORMATFILE_DATA_SOURCE",
                "FIELDTERMINATOR",
                "FIELDQUOTE",
                "ROWTERMINATOR",
                "FIRSTROW",
                "LASTROW",
                "MAXERRORS",
                "ERRORFILE",
                "ERRORFILE_DATA_SOURCE",
                "ROWS_PER_BATCH",
                "SECONDS",
                "MINUTES",
                "GRAPH");

            gb.Prod("TruncateStatement").Is("TRUNCATE", "TABLE", qualifiedName);

            gb.Prod("CreateTableAsSelectStatement").Is("CREATE", "TABLE", qualifiedName, "AS", queryExpression);
            gb.Prod("CreateTableAsSelectStatement").Is("CREATE", "TABLE", qualifiedName, "WITH", "(", createTableOptionList, ")", "AS", queryExpression);

            gb.Prod("AlterDatabaseStatement").Is("ALTER", "DATABASE", identifierTerm, "SET", alterDatabaseSetOption);
            gb.Prod("AlterDatabaseStatement").Is("ALTER", "DATABASE", "SCOPED", "CONFIGURATION", "CLEAR", identifierTerm);
            gb.Prod("AlterDatabaseStatement").Is("ALTER", "DATABASE", "SCOPED", "CONFIGURATION", "SET", identifierTerm, "=", expression);
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
            gb.Rule("AlterDatabaseTerminationClause").OneOf(
                gb.Seq("WITH", "NO_WAIT"),
                gb.Seq("WITH", "ROLLBACK", "IMMEDIATE"),
                gb.Seq("WITH", "ROLLBACK", "AFTER", expression));
            gb.Opt(alterDatabaseTerminationOpt, alterDatabaseTerminationClause);

            gb.Prod("AlterDatabaseSetOption").Is(alterDatabaseSetModeOption);
            gb.Prod("AlterDatabaseSetOption").Is(alterDatabaseSetOnOffOption, "ON", alterDatabaseTerminationOpt);
            gb.Prod("AlterDatabaseSetOption").Is(alterDatabaseSetOnOffOption, "OFF", alterDatabaseTerminationOpt);
            gb.Prod("AlterDatabaseSetOption").Is(alterDatabaseSetEqualsOnOffOption, "=", "ON");
            gb.Prod("AlterDatabaseSetOption").Is(alterDatabaseSetEqualsOnOffOption, "=", "OFF");
            gb.Prod("AlterDatabaseSetOption").Is("COMPATIBILITY_LEVEL", "=", expression);
            gb.Prod("AlterDatabaseSetOption").Is("RECOVERY", alterDatabaseRecoveryModel);
            gb.Prod("AlterDatabaseSetOption").Is("PAGE_VERIFY", alterDatabasePageVerifyMode);
            gb.Prod("AlterDatabaseSetOption").Is("CURSOR_DEFAULT", alterDatabaseCursorDefaultMode);
            gb.Prod("AlterDatabaseSetOption").Is("PARAMETERIZATION", alterDatabaseParameterizationMode);
            gb.Prod("AlterDatabaseSetOption").Is("TARGET_RECOVERY_TIME", "=", expression, alterDatabaseTargetRecoveryUnit);
            gb.Prod("AlterDatabaseSetOption").Is("DELAYED_DURABILITY", "=", alterDatabaseDelayedDurabilityMode);
            gb.Prod("AlterDatabaseSetOption").Is("QUERY_STORE", "CLEAR");
            gb.Prod("AlterDatabaseSetOption").Is("QUERY_STORE", "CLEAR", "ALL");
            gb.Prod("AlterDatabaseSetOption").Is("QUERY_STORE", "=", "ON");
            gb.Prod("AlterDatabaseSetOption").Is("QUERY_STORE", "=", "OFF");
            gb.Prod("AlterDatabaseSetOption").Is("QUERY_STORE", "(", indexOptionList, ")");
            gb.Prod("AlterDatabaseSetOption").Is("QUERY_STORE", "=", "ON", "(", indexOptionList, ")");
            gb.Prod("AlterDatabaseSetOption").Is("QUERY_STORE", "=", "OFF", "(", indexOptionList, ")");
            gb.Prod("AlterDatabaseSetOption").Is("AUTOMATIC_TUNING", "(", indexOptionList, ")");
            gb.Prod("AlterDatabaseSetOption").Is("FILESTREAM", "(", indexOptionList, ")");

            // CREATE LOGIN
            gb.Prod("CreateLoginStatement").Is("CREATE", "LOGIN", identifierTerm, "WITH", createLoginPasswordSpec);
            gb.Prod("CreateLoginStatement").Is("CREATE", "LOGIN", identifierTerm, "WITH", createLoginPasswordSpec, ",", createLoginOptionList);
            gb.Prod("CreateLoginStatement").Is("CREATE", "LOGIN", identifierTerm, "FROM", "WINDOWS");
            gb.Prod("CreateLoginStatement").Is("CREATE", "LOGIN", identifierTerm, "FROM", "WINDOWS", "WITH", createLoginWindowsOptionList);
            gb.Prod("CreateLoginStatement").Is("CREATE", "LOGIN", identifierTerm, "FROM", "EXTERNAL", "PROVIDER");
            gb.Prod("CreateLoginStatement").Is("CREATE", "LOGIN", identifierTerm, "FROM", "EXTERNAL", "PROVIDER", "WITH", "OBJECT_ID", "=", stringLiteral);
            gb.Prod("CreateLoginStatement").Is("CREATE", "LOGIN", identifierTerm, "FROM", "CERTIFICATE", identifierTerm);
            gb.Prod("CreateLoginStatement").Is("CREATE", "LOGIN", identifierTerm, "FROM", "ASYMMETRIC", "KEY", identifierTerm);
            gb.Prod("CreateLoginPasswordSpec").Is("PASSWORD", "=", expression);
            gb.Prod("CreateLoginPasswordSpec").Is("PASSWORD", "=", expression, "HASHED");
            gb.Prod("CreateLoginPasswordSpec").Is("PASSWORD", "=", expression, "MUST_CHANGE");
            gb.Prod("CreateLoginPasswordSpec").Is("PASSWORD", "=", expression, "HASHED", "MUST_CHANGE");
            gb.Rule("CreateLoginOptionList").SeparatedBy(",", createLoginOption);
            gb.Prod("CreateLoginOption").Is("SID", "=", expression);
            gb.Prod("CreateLoginOption").Is("DEFAULT_DATABASE", "=", namedOptionValue);
            gb.Prod("CreateLoginOption").Is("DEFAULT_LANGUAGE", "=", namedOptionValue);
            gb.Prod("CreateLoginOption").Is("CHECK_EXPIRATION", "=", indexOnOffValue);
            gb.Prod("CreateLoginOption").Is("CHECK_POLICY", "=", indexOnOffValue);
            gb.Prod("CreateLoginOption").Is("CREDENTIAL", "=", namedOptionValue);
            gb.Rule("CreateLoginWindowsOptionList").SeparatedBy(",", createLoginWindowsOption);
            gb.Prod("CreateLoginWindowsOption").Is("DEFAULT_DATABASE", "=", namedOptionValue);
            gb.Prod("CreateLoginWindowsOption").Is("DEFAULT_LANGUAGE", "=", namedOptionValue);

            gb.Prod("CreateUserStatement").Is("CREATE", "USER", identifierTerm, "FOR", "LOGIN", identifierTerm);
            gb.Prod("CreateUserStatement").Is("CREATE", "USER", identifierTerm, "WITHOUT", "LOGIN");

            gb.Prod("CreateStatisticsStatement").Is("CREATE", "STATISTICS", identifierTerm, "ON", qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateStatisticsStatement").Is("CREATE", "STATISTICS", identifierTerm, "ON", qualifiedName, "(", identifierList, ")", "WITH", "(", indexOptionList, ")");
            BuildUpdateStatisticsGrammar();

            gb.Prod("DropTypeStatement").Is("DROP", "TYPE", qualifiedName);
            gb.Prod("DropTypeStatement").Is("DROP", "TYPE", "IF", "EXISTS", qualifiedName);

            gb.Prod("DropColumnEncryptionKeyStatement").Is("DROP", "COLUMN", "ENCRYPTION", "KEY", identifierTerm);
            gb.Prod("DropColumnEncryptionKeyStatement").Is("DROP", "COLUMN", "MASTER", "KEY", identifierTerm);

            gb.Rule("RevertStatement").OneOf(
                "REVERT",
                gb.Seq("REVERT", "WITH", "COOKIE", "=", expression));
            // DROP EVENT SESSION ON DATABASE/SERVER
            gb.Rule("DropEventSessionStatement").OneOf(
                gb.Seq("DROP", "EVENT", "SESSION", identifierTerm, "ON", "DATABASE"),
                gb.Seq("DROP", "EVENT", "SESSION", identifierTerm, "ON", "SERVER"));

            // CREATE TYPE ... AS TABLE
            gb.Prod("CreateTypeStatement").Is("CREATE", "TYPE", qualifiedName, "AS", tableTypeDefinition);
            gb.Prod("CreateTypeStatement").Is("CREATE", "TYPE", qualifiedName, "FROM", typeSpec);

            BuildMergeGrammar(
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
                deleteOutputClause,
                optionClause,
                topValue,
                searchCondition,
                updateSetList,
                insertColumnList,
                insertValueList);

            BuildBulkInsertGrammar(
                gb,
                bulkInsertOptionList,
                qualifiedName,
                expression,
                namedOptionValue,
                createTableKeyColumnList);

            gb.Prod("CheckpointStatement").Is("CHECKPOINT");

            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.GraphExtensions))
            {
                BuildGraphGrammar(
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
                gb.Prod("FunctionCall").Is("PREDICT", "(", predictArgList, ")");
                gb.Prod("PredictArgList").Is(predictArg);
                gb.Prod("PredictArgList").Is(predictArgList, ",", predictArg);
                gb.Prod("PredictArg").Is(identifierTerm, "=", expression);
                gb.Prod("PredictArg").Is(identifierTerm, "=", expression, "AS", identifierTerm);
            }

            gb.Prod("StrictQualifiedName").Is(strictIdentifierTerm);
            gb.Prod("StrictQualifiedName").Is(strictQualifiedName, ".", strictIdentifierTerm);
            gb.Prod("StrictQualifiedName").Is(strictQualifiedName, ".", ".", strictIdentifierTerm); // double-dot: master..table
            gb.Prod("QualifiedName").Is(identifierTerm);
            gb.Prod("QualifiedName").Is(qualifiedName, ".", identifierTerm);
            if (HasFeature(dialectFeatures, MsSqlDialectFeatures.GraphExtensions))
            {
                gb.Prod("QualifiedName").Is(qualifiedName, ".", graphColumnRef);
            }
            gb.Prod("QualifiedName").Is(qualifiedName, ".", ".", identifierTerm); // double-dot: master..table

            BuildSecurityPolicyGrammar(
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
                BuildExternalObjectGrammar(
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

        private static void BuildTriggerGrammar(
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

            gb.Prod(createTriggerEventList).Is(createTriggerEvent);
            gb.Prod(createTriggerEventList).Is(createTriggerEventList, ",", createTriggerEvent);
            gb.Rule(createTriggerEvent)
                .CanBe(strictIdentifierTerm)
                .OrKeywords("INSERT", "UPDATE", "DELETE");

            gb.Prod(createTriggerWithOptionList).Is(createTriggerWithOption);
            gb.Prod(createTriggerWithOptionList).Is(createTriggerWithOptionList, ",", createTriggerWithOption);
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

        private static void BuildGraphGrammar(
            GrammarBuilder gb,
            INonTerminal matchGraphPattern,
            INonTerminal matchGraphPath,
            INonTerminal matchGraphStep,
            INonTerminal matchGraphStepChain,
            INonTerminal matchGraphShortestPath,
            INonTerminal matchGraphShortestPathBody,
            INonTerminal identifierTerm,
            ITerminal number)
        {
            gb.Prod(matchGraphPattern).Is(matchGraphPath);
            gb.Prod(matchGraphPattern).Is("SHORTEST_PATH", "(", matchGraphShortestPath, ")");
            gb.Prod(matchGraphPattern).Is(matchGraphPattern, "AND", matchGraphPath);
            gb.Prod(matchGraphPattern).Is(matchGraphPattern, "AND", "SHORTEST_PATH", "(", matchGraphShortestPath, ")");
            gb.Prod(matchGraphShortestPath).Is(matchGraphShortestPathBody);
            gb.Prod(matchGraphShortestPath).Is(matchGraphShortestPathBody, "+");
            gb.Prod(matchGraphShortestPath).Is(matchGraphShortestPathBody, "{", number, ",", number, "}");
            gb.Prod(matchGraphPath).Is(identifierTerm);
            gb.Prod(matchGraphPath).Is(identifierTerm, matchGraphStepChain);
            gb.Prod(matchGraphShortestPathBody).Is(identifierTerm, "(", matchGraphStepChain, ")");
            gb.Rule(matchGraphStep)
                .CanBe("-", "(", identifierTerm, ")", "-", ">", identifierTerm)
                .Or("<", "-", "(", identifierTerm, ")", "-", identifierTerm)
                .Or("-", "(", identifierTerm, ")", "-", identifierTerm);
            gb.Prod(matchGraphStepChain).Is(matchGraphStep);
            gb.Prod(matchGraphStepChain).Is(matchGraphStepChain, matchGraphStep);
        }

        private static void BuildMergeGrammar(
            GrammarBuilder gb,
            INonTerminal mergeTargetTable,
            INonTerminal mergeSourceTable,
            INonTerminal mergeOutputClauseOpt,
            INonTerminal mergeOptionClauseOpt,
            INonTerminal mergeWhenList,
            INonTerminal mergeWhen,
            INonTerminal mergeMatchedAction,
            INonTerminal mergeNotMatchedAction,
            INonTerminal qualifiedName,
            INonTerminal identifierTerm,
            INonTerminal tableHintLimitedList,
            INonTerminal tableSource,
            INonTerminal deleteOutputClause,
            INonTerminal optionClause,
            INonTerminal topValue,
            INonTerminal searchCondition,
            INonTerminal updateSetList,
            INonTerminal insertColumnList,
            INonTerminal insertValueList)
        {
            gb.Prod("MergeTargetTable").Is(qualifiedName);
            gb.Prod("MergeTargetTable").Is(qualifiedName, "AS", identifierTerm);
            gb.Prod("MergeTargetTable").Is(qualifiedName, identifierTerm);
            gb.Prod("MergeTargetTable").Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("MergeTargetTable").Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")", "AS", identifierTerm);
            gb.Prod("MergeTargetTable").Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")", identifierTerm);
            gb.Prod("MergeSourceTable").Is(tableSource);
            gb.Opt(mergeOutputClauseOpt, deleteOutputClause);
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

        private static void BuildSecurityPolicyGrammar(
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
            gb.Prod(securityPolicyClauseList).Is(securityPolicyClause);
            gb.Prod(securityPolicyClauseList).Is(securityPolicyClauseList, ",", securityPolicyClause);
            gb.Rule(securityPolicyClause)
                .CanBe("ADD", "FILTER", "PREDICATE", functionCall, "ON", qualifiedName)
                .Or("ADD", "BLOCK", "PREDICATE", functionCall, "ON", qualifiedName)
                .Or("DROP", "FILTER", "PREDICATE", "ON", qualifiedName)
                .Or("DROP", "BLOCK", "PREDICATE", "ON", qualifiedName);
            gb.Rule(securityPolicyOptionName).Keywords("STATE", "SCHEMABINDING");
            gb.Prod(securityPolicyOptionList).Is(securityPolicyOption);
            gb.Prod(securityPolicyOptionList).Is(securityPolicyOptionList, ",", securityPolicyOption);
            gb.Prod(securityPolicyOption).Is(securityPolicyOptionName, "=", "ON");
            gb.Prod(securityPolicyOption).Is(securityPolicyOptionName, "=", "OFF");
        }

        private static void BuildExternalObjectGrammar(
            GrammarBuilder gb,
            INonTerminal createExternalTableStatement,
            INonTerminal externalTableOptionList,
            INonTerminal createExternalDataSourceStatement,
            INonTerminal externalDataSourceOptionList,
            INonTerminal qualifiedName,
            INonTerminal identifierTerm,
            INonTerminal createTableElementList,
            INonTerminal namedOptionValue)
        {
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

            gb.Prod(createExternalDataSourceStatement).Is("CREATE", "EXTERNAL", "DATA", "SOURCE", identifierTerm, "WITH", "(", externalDataSourceOptionList, ")");
            gb.Prod(createExternalDataSourceStatement).Is("CREATE", "EXTERNAL", "DATA", "SOURCE", qualifiedName, "WITH", "(", externalDataSourceOptionList, ")");
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

        private static void BuildBulkInsertGrammar(
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

