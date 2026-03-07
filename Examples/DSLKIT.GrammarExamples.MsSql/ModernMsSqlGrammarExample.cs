using System;
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
    /// <summary>
    /// SQL Server 2022 / Azure SQL query-language grammar subset.
    /// Focus: SELECT, CTE, joins, set operators, window functions and CASE.
    /// </summary>
    public static class ModernMsSqlGrammarExample
    {
        private static readonly Lazy<IGrammar> GrammarCache = new(BuildGrammarCore);
        private static readonly RegExpTerminal ControlLineTerminal = new(
            "SqlServerControlLine",
            @"\G.+",
            previewChar: null,
            flags: TermFlags.None);

        public static IGrammar BuildGrammar()
        {
            return GrammarCache.Value;
        }

        public static ParseResult ParseScript(string source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var segments = SqlServerScriptPreprocessor.Split(source);
            if (segments.Count > 1 || (segments.Count == 1 && segments[0].Kind != SqlScriptSegmentKind.Batch))
            {
                var childNodes = new List<ParseTreeNode>(segments.Count);
                foreach (var segment in segments)
                {
                    if (segment.Kind == SqlScriptSegmentKind.Batch)
                    {
                        var batchParseResult = ParseScript(segment.Text);
                        if (!batchParseResult.IsSuccess)
                        {
                            return OffsetParseError(batchParseResult, segment.StartPosition);
                        }

                        if (batchParseResult.ParseTree != null)
                        {
                            childNodes.Add(batchParseResult.ParseTree);
                        }

                        continue;
                    }

                    childNodes.Add(CreateControlNode(segment));
                }

                return CreateCompositeParseResult(childNodes);
            }

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

        private static ParseResult CreateCompositeParseResult(IReadOnlyList<ParseTreeNode> childNodes)
        {
            var scriptDocument = new NonTerminal("ScriptDocument");
            var production = new Production(scriptDocument, childNodes.Select(child => child.Term).ToList());
            var parseTree = new NonTerminalNode(scriptDocument, production, childNodes);
            return new ParseResult
            {
                ParseTree = parseTree
            };
        }

        private static NonTerminalNode CreateControlNode(SqlScriptSegment segment)
        {
            var nodeName = segment.Kind == SqlScriptSegmentKind.BatchSeparator
                ? "BatchSeparator"
                : "SqlcmdCommand";
            var token = new Token(
                segment.StartPosition,
                segment.Text.Length,
                segment.Text,
                segment.Text,
                ControlLineTerminal);
            var terminalNode = new TerminalNode(token);
            var nonTerminal = new NonTerminal(nodeName);
            var production = new Production(nonTerminal, [terminalNode.Term]);
            return new NonTerminalNode(nonTerminal, production, [terminalNode]);
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
                .AddTerminal(withCheckOptionStart)
                .AddTerminal(graphColumnRef);

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
            var cursorOptionList = gb.NT("CursorOptionList");
            var cursorOption = gb.NT("CursorOption");
            var cursorOperationStatement = gb.NT("CursorOperationStatement");
            var fetchStatement = gb.NT("FetchStatement");
            var fetchDirection = gb.NT("FetchDirection");
            var fetchTargetList = gb.NT("FetchTargetList");
            var waitforStatement = gb.NT("WaitforStatement");
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
            object[] statementNoLeadingWithAlternatives =
            [
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
                mergeStatement
            ];
            object[] implicitStatementNoLeadingWithAlternatives =
            [
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
                mergeStatement
            ];
            var createFunctionPreludeStatementNoLeadingWithAlternatives = statementNoLeadingWithAlternatives
                .Where(alternative => !ReferenceEquals(alternative, returnStatement))
                .ToArray();
            var createFunctionImplicitPreludeStatementNoLeadingWithAlternatives = implicitStatementNoLeadingWithAlternatives
                .Where(alternative => !ReferenceEquals(alternative, returnStatement))
                .ToArray();

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
            gb.Rule("StatementNoLeadingWith").OneOf(statementNoLeadingWithAlternatives);
            // Only allow omitted separators before statements with a keyword-led start.
            gb.Rule("ImplicitStatementNoLeadingWith").OneOf(implicitStatementNoLeadingWithAlternatives);
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
            gb.Prod("DeleteQueryHint").Is("MAXDOP", expression);
            gb.Prod("DeleteQueryHint").Is("MAXDOP", "=", expression);
            gb.Prod("DeleteQueryHint").Is("MAXRECURSION", expression);
            gb.Prod("DeleteQueryHint").Is("MAXRECURSION", "=", expression);
            gb.Prod("DeleteQueryHint").Is("QUERYTRACEON", expression);
            gb.Prod("DeleteQueryHint").Is("MIN_GRANT_PERCENT", "=", expression);
            gb.Prod("DeleteQueryHint").Is("MAX_GRANT_PERCENT", "=", expression);
            gb.Prod("DeleteQueryHint").Is("LABEL", "=", expression);
            gb.Prod("DeleteQueryHint").Is("USE", "HINT", "(", expressionList, ")");
            gb.Prod("DeleteQueryHint").Is("HASH", "JOIN");
            gb.Prod("DeleteQueryHint").Is("MERGE", "JOIN");
            gb.Prod("DeleteQueryHint").Is("LOOP", "JOIN");
            gb.Prod("DeleteQueryHint").Is("HASH", "GROUP");
            gb.Prod("DeleteQueryHint").Is("ORDER", "GROUP");
            gb.Prod("DeleteQueryHint").Is("MERGE", "UNION");
            gb.Prod("DeleteQueryHint").Is("HASH", "UNION");
            gb.Prod("DeleteQueryHint").Is("CONCAT", "UNION");
            gb.Prod("DeleteQueryHint").Is("FORCE", "ORDER");
            gb.Prod("DeleteQueryHint").Is("KEEP", "PLAN");
            gb.Prod("DeleteQueryHint").Is("KEEPFIXED", "PLAN");
            gb.Prod("DeleteQueryHint").Is("ROBUST", "PLAN");
            gb.Rule(deleteQueryHintName)
                .Keywords("RECOMPILE", "IGNORE_NONCLUSTERED_COLUMNSTORE_INDEX");

            gb.Prod("OptionClause").Is("OPTION", "(", deleteQueryHintList, ")");

            gb.Prod("IfBranchStatement").Is(statement);
            gb.Prod("IfBranchStatement").Is(statement, ";");
            gb.Prod("IfStatement").Is("IF", searchCondition, ifBranchStatement);
            gb.Prod("IfStatement").Is("IF", searchCondition, ifBranchStatement, "ELSE", ifBranchStatement);
            gb.Prod("IfStatement").Is("IF", searchCondition, ifBranchStatement, "ELSE", ifStatement);
            gb.Prod("BeginEndStatement").Is("BEGIN", statementListOpt, "END");
            gb.Prod("WhileStatement").Is("WHILE", searchCondition, statement);

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
            gb.Prod("SetStatement").Is("SET", variableReference, "=", expression);
            gb.Prod("SetStatement").Is("SET", variableReference, compoundAssignOp, expression);
            gb.Prod("SetStatement").Is("SET", setOptionName, "ON");
            gb.Prod("SetStatement").Is("SET", setOptionName, "OFF");
            gb.Prod("SetStatement").Is("SET", setOptionName, "=", expression);
            gb.Prod("SetStatement").Is("SET", setOptionName, expression);
            gb.Prod("SetStatement").Is("SET", setStatisticsOption, "ON");
            gb.Prod("SetStatement").Is("SET", setStatisticsOption, "OFF");
            gb.Prod("SetStatement").Is("SET", "IDENTITY_INSERT", qualifiedName, "ON");
            gb.Prod("SetStatement").Is("SET", "IDENTITY_INSERT", qualifiedName, "OFF");
            gb.Prod("SetStatement").Is("SET", "TRANSACTION", "ISOLATION", "LEVEL", setTransactionIsolationLevel);

            gb.Rule(setTransactionIsolationLevel)
                .CanBe("READ", "UNCOMMITTED")
                .Or("READ", "COMMITTED")
                .Or("REPEATABLE", "READ")
                .Or("SNAPSHOT")
                .Or("SERIALIZABLE");

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
            gb.Prod("RaiserrorWithOptionList").Is(raiserrorWithOption);
            gb.Prod("RaiserrorWithOptionList").Is(raiserrorWithOptionList, ",", raiserrorWithOption);
            gb.Rule(raiserrorWithOption).Keywords("LOG", "NOWAIT", "SETERROR");

            gb.Rule(throwStatement)
                .CanBe("THROW")
                .Or("THROW", expression, ",", expression, ",", expression);
            gb.Rule(loopControlStatement)
                .CanBe("BREAK")
                .OrKeywords("CONTINUE");
            gb.Prod("GotoStatement").Is("GOTO", identifierTerm);
            gb.Prod("LabelOnlyStatement").Is(identifierTerm, ":");
            gb.Prod("LabelStatement").Is(labelOnlyStatement);

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
                gb.Seq(createProcName, createProcSignatureParameterListOpt, createProcSignatureWithClauseOpt, createProcSignatureForReplicationOpt));

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
            gb.Prod("CreateProcBody").Is("AS", "EXTERNAL", "NAME", createProcExternalName);
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
            gb.Prod("CreateProcNativeAtomicOption").Is("TRANSACTION", "ISOLATION", "LEVEL", "=", setTransactionIsolationLevel);

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
            gb.Prod("CreateFunctionSignature").Is(
                createFunctionName,
                "(",
                createFunctionSignatureParameterListOpt,
                ")");

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

            gb.Prod("CreateFunctionScalarReturnsClause").Is("RETURNS", typeSpec);
            gb.Prod("CreateFunctionInlineTableReturnsClause").Is("RETURNS", "TABLE");
            gb.Prod("CreateFunctionTableVariableReturnsClause").Is("RETURNS", createFunctionTableReturnDefinition);
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
            gb.Prod("CreateFunctionScalarReturnStatement").Is("RETURN", expression);
            gb.Prod("CreateFunctionTableVariableReturnStatement").Is("RETURN");

            gb.Prod("CreateFunctionScalarBody").Is(
                "AS",
                "BEGIN",
                createFunctionPreludeBeforeReturnOpt,
                createFunctionScalarReturnStatement,
                createFunctionBodyTrailingSeparatorsOpt,
                "END");
            gb.Prod("CreateFunctionScalarBody").Is(
                "BEGIN",
                createFunctionPreludeBeforeReturnOpt,
                createFunctionScalarReturnStatement,
                createFunctionBodyTrailingSeparatorsOpt,
                "END");
            gb.Prod("CreateFunctionInlineTableBody").Is("AS", "RETURN", queryExpression);
            gb.Prod("CreateFunctionInlineTableBody").Is("AS", "RETURN", "(", queryExpression, ")");
            gb.Prod("CreateFunctionInlineTableBody").Is("AS", "RETURN", "(", withClause, queryExpression, ")");
            gb.Prod("CreateFunctionInlineTableBody").Is("RETURN", queryExpression);
            gb.Prod("CreateFunctionInlineTableBody").Is("RETURN", "(", queryExpression, ")");
            gb.Prod("CreateFunctionInlineTableBody").Is("RETURN", "(", withClause, queryExpression, ")");
            gb.Prod("CreateFunctionTableVariableBody").Is(
                "AS",
                "BEGIN",
                createFunctionPreludeBeforeReturnOpt,
                createFunctionTableVariableReturnStatement,
                createFunctionBodyTrailingSeparatorsOpt,
                "END");
            gb.Prod("CreateFunctionTableVariableBody").Is(
                "BEGIN",
                createFunctionPreludeBeforeReturnOpt,
                createFunctionTableVariableReturnStatement,
                createFunctionBodyTrailingSeparatorsOpt,
                "END");

            gb.Prod("CreateFunctionStatement").Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionScalarReturnsClause,
                createFunctionScalarBody);
            gb.Prod("CreateFunctionStatement").Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionScalarReturnsClause,
                createFunctionWithClause,
                createFunctionScalarBody);
            gb.Prod("CreateFunctionStatement").Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionInlineTableReturnsClause,
                createFunctionInlineTableBody);
            gb.Prod("CreateFunctionStatement").Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionInlineTableReturnsClause,
                createFunctionWithClause,
                createFunctionInlineTableBody);
            gb.Prod("CreateFunctionStatement").Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionTableVariableReturnsClause,
                createFunctionTableVariableBody);
            gb.Prod("CreateFunctionStatement").Is(
                createFunctionHead,
                createFunctionSignature,
                createFunctionTableVariableReturnsClause,
                createFunctionWithClause,
                createFunctionTableVariableBody);

            gb.Prod("GrantPermissionSet").Is("ALL");
            gb.Prod("GrantPermissionSet").Is("ALL", "PRIVILEGES");
            gb.Prod("GrantPermissionSet").Is(grantPermissionList);
            gb.Prod("GrantPermissionList").Is(grantPermissionItem);
            gb.Prod("GrantPermissionList").Is(grantPermissionList, ",", grantPermissionItem);
            gb.Prod("GrantPermissionItem").Is(grantPermission);
            gb.Prod("GrantPermissionItem").Is(grantPermission, "(", identifierList, ")");

            gb.Prod("GrantPermission").Is(grantPermissionWord);
            gb.Prod("GrantPermission").Is("VIEW", "DEFINITION");
            gb.Prod("GrantPermission").Is("TAKE", "OWNERSHIP");
            gb.Prod("GrantPermission").Is("CREATE", "ANY", "SCHEMA");
            gb.Prod("GrantPermission").Is("VIEW", "ANY", "COLUMN", "MASTER", "KEY", "DEFINITION");
            gb.Prod("GrantPermission").Is("VIEW", "ANY", "COLUMN", "ENCRYPTION", "KEY", "DEFINITION");

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

            gb.Prod("GrantOnClause").Is("ON", grantSecurable);
            gb.Prod("GrantOnClause").Is("ON", grantClassType, "::", grantSecurable);
            gb.Rule(grantClassType).Keywords("LOGIN", "DATABASE", "OBJECT", "ROLE", "SCHEMA", "USER");
            gb.Prod("GrantSecurable").Is(strictQualifiedName);
            gb.Prod("GrantSecurable").Is(strictIdentifierTerm);

            gb.Prod("GrantPrincipalList").Is(grantPrincipal);
            gb.Prod("GrantPrincipalList").Is(grantPrincipalList, ",", grantPrincipal);
            gb.Prod("GrantPrincipal").Is(strictIdentifierTerm);
            gb.Prod("GrantPrincipal").Is(strictQualifiedName);
            gb.Prod("GrantPrincipal").Is("PUBLIC");

            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, "TO", grantPrincipalList);
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList);
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION");
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION");
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, "TO", grantPrincipalList, "AS", grantPrincipal);
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList, "AS", grantPrincipal);
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION", "AS", grantPrincipal);
            gb.Prod("GrantStatement").Is("GRANT", grantPermissionSet, grantOnClause, "TO", grantPrincipalList, "WITH", "GRANT", "OPTION", "AS", grantPrincipal);

            gb.Rule("DbccCommand").Keywords(
                "CHECKDB",
                "DROPCLEANBUFFERS",
                "TRACESTATUS",
                "FREEPROCCACHE",
                "SHRINKFILE",
                "PDW_SHOWSPACEUSED",
                "LOGINFO",
                "TRACEON",
                "PAGE",
                "WRITEPAGE");
            gb.Prod("DbccStatement").Is("DBCC", dbccCommand);
            gb.Prod("DbccStatement").Is("DBCC", dbccCommand, "(", dbccParamList, ")");
            gb.Prod("DbccStatement").Is("DBCC", dbccCommand, "WITH", dbccOptionList);
            gb.Prod("DbccStatement").Is("DBCC", dbccCommand, "(", dbccParamList, ")", "WITH", dbccOptionList);
            gb.Prod("DbccParamList").Is(dbccParam);
            gb.Prod("DbccParamList").Is(dbccParamList, ",", dbccParam);
            gb.Prod("DbccParam").Is(expression);
            gb.Prod("DbccParam").Is(strictIdentifierTerm);
            gb.Prod("DbccParam").Is(strictQualifiedName);
            gb.Prod("DbccOptionList").Is(dbccOption);
            gb.Prod("DbccOptionList").Is(dbccOptionList, ",", dbccOption);
            gb.Prod("DbccOption").Is(dbccOptionName);
            gb.Prod("DbccOption").Is(dbccOptionName, "=", dbccOptionValue);
            gb.Rule("DbccOptionName")
                .Keywords("NO_INFOMSGS", "ALL_ERRORMSGS", "MAXDOP", "TABLERESULTS");
            gb.Rule("DbccOptionValue")
                .CanBe(expression)
                .Or(strictIdentifierTerm)
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
            gb.Prod("ProcStatementList").Is(procStatementList, implicitStatementNoLeadingWith);

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
            gb.Prod("CreateViewColumnList").Is("(", identifierList, ")");
            gb.Prod("CreateViewOptionClause").Is("WITH", createViewOptionList);
            gb.Prod("CreateViewQuery").Is(queryExpression);
            gb.Prod("CreateViewQuery").Is(withClause, queryExpression);
            gb.Prod("CreateViewBody").Is("AS", createViewQuery);
            gb.Prod("CreateViewBody").Is("AS", createViewQuery, createViewCheckOptionOpt);
            gb.Opt(createViewCheckOptionOpt, withCheckOptionStart, "CHECK", "OPTION");
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, createViewBody);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, createViewColumnList, createViewBody);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, createViewOptionClause, createViewBody);
            gb.Prod("CreateViewStatement").Is(createViewHead, qualifiedName, createViewColumnList, createViewOptionClause, createViewBody);
            gb.Rule("CreateViewOptionList").SeparatedBy(",", createViewOption);
            gb.Rule("CreateViewOption").Keywords("ENCRYPTION", "SCHEMABINDING", "VIEW_METADATA");

            gb.Prod("CreateTableFileTableClause").Is("AS", "FILETABLE");
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")");
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause);
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, createIndexFileStreamClause);
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, createTableOptions);
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, createTableFileTableClause, createIndexFileStreamClause, createTableOptions);
            gb.Prod("CreateTableStatement").Is("CREATE", "TABLE", qualifiedName, "(", createTableElementList, ")", createTableTailClauseList);
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
            gb.Prod("CreateTableTailClause").Is(createIndexFileStreamClause);

            gb.Prod("CreateTableElementList").Is(createTableElement);
            gb.Prod("CreateTableElementList").Is(createTableElementList, ",", createTableElement);
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
            gb.Prod("CreateTableColumnOption").Is("FILESTREAM");
            gb.Prod("CreateTableColumnOption").Is("DEFAULT", expression);
            gb.Prod("CreateTableColumnOption").Is("DEFAULT", "(", expression, ")");
            gb.Prod("CreateTableColumnOption").Is("IDENTITY");
            gb.Prod("CreateTableColumnOption").Is("IDENTITY", "(", expression, ",", expression, ")");
            gb.Prod("CreateTableColumnOption").Is("COLLATE", strictIdentifierTerm);
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "ROW", "START");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "ROW", "START", "HIDDEN");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "ROW", "END");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "ROW", "END", "HIDDEN");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "START");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "START", "HIDDEN");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "END");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "TRANSACTION_ID", "END", "HIDDEN");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "START");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "START", "HIDDEN");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "END");
            gb.Prod("CreateTableColumnOption").Is("GENERATED", "ALWAYS", "AS", "SEQUENCE_NUMBER", "END", "HIDDEN");
            gb.Prod("CreateTableColumnOption").Is("MASKED", "WITH", "(", maskingOptionList, ")");
            gb.Prod("CreateTableColumnOption").Is("ENCRYPTED", "WITH", "(", encryptionOptionList, ")");
            gb.Prod("CreateTableColumnOption").Is("NOT", "FOR", "REPLICATION");
            gb.Prod("CreateTableColumnOption").Is("CHECK", "(", searchCondition, ")");
            gb.Prod("CreateTableColumnOption").Is("REFERENCES", qualifiedName);
            gb.Prod("CreateTableColumnOption").Is("REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateTableColumnOption").Is("CONSTRAINT", strictIdentifierTerm, createTableColumnConstraintBody);

            gb.Prod("CreateTableComputedColumn").Is(identifierTerm, "AS", expression);
            gb.Prod("CreateTableComputedColumn").Is(identifierTerm, "AS", expression, createTableColumnOptionList);

            gb.Prod("CreateTableColumnSet").Is(identifierTerm, typeSpec, "COLUMN_SET", "FOR", "ALL_SPARSE_COLUMNS");

            gb.Prod("CreateTableConstraint").Is(createTableTableConstraintBody);
            gb.Prod("CreateTableConstraint").Is("CONSTRAINT", identifierTerm, createTableTableConstraintBody);

            gb.Prod("CreateTableColumnConstraintBody").Is("PRIMARY", "KEY");
            gb.Prod("CreateTableColumnConstraintBody").Is("PRIMARY", "KEY", createTableColumnKeyClusterType);
            gb.Prod("CreateTableColumnConstraintBody").Is("UNIQUE");
            gb.Prod("CreateTableColumnConstraintBody").Is("UNIQUE", createTableColumnKeyClusterType);
            gb.Prod("CreateTableColumnConstraintBody").Is("CHECK", "(", searchCondition, ")");
            gb.Prod("CreateTableColumnConstraintBody").Is("FOREIGN", "KEY", "REFERENCES", qualifiedName);
            gb.Prod("CreateTableColumnConstraintBody").Is("FOREIGN", "KEY", "REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateTableColumnConstraintBody").Is("REFERENCES", qualifiedName);
            gb.Prod("CreateTableColumnConstraintBody").Is("REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateTableColumnConstraintBody").Is("DEFAULT", expression);
            gb.Prod("CreateTableColumnConstraintBody").Is("DEFAULT", "(", expression, ")");

            gb.Prod("CreateTableTableConstraintBody").Is("PRIMARY", "KEY", "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("PRIMARY", "KEY", "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("PRIMARY", "KEY", "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "ON", indexStorageTarget);
            gb.Prod("CreateTableTableConstraintBody").Is("PRIMARY", "KEY", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("PRIMARY", "KEY", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")", "ON", indexStorageTarget);
            gb.Prod("CreateTableTableConstraintBody").Is("UNIQUE", "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("UNIQUE", createTableConstraintClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("UNIQUE", "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("UNIQUE", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("UNIQUE", createTableConstraintClusterType, "(", createTableKeyColumnList, ")", "WITH", "(", indexOptionList, ")", "ON", indexStorageTarget);
            gb.Prod("CreateTableTableConstraintBody").Is("CHECK", "(", searchCondition, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("FOREIGN", "KEY", "(", identifierList, ")", "REFERENCES", qualifiedName);
            gb.Prod("CreateTableTableConstraintBody").Is("FOREIGN", "KEY", "(", identifierList, ")", "REFERENCES", qualifiedName, "(", identifierList, ")");
            gb.Prod("CreateTableTableConstraintBody").Is("DEFAULT", expression, "FOR", identifierTerm);
            gb.Prod("CreateTableTableConstraintBody").Is("DEFAULT", "(", expression, ")", "FOR", identifierTerm);

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

            gb.Prod("CreateTableKeyColumnList").Is(createTableKeyColumn);
            gb.Prod("CreateTableKeyColumnList").Is(createTableKeyColumnList, ",", createTableKeyColumn);
            gb.Prod("CreateTableKeyColumn").Is(identifierTerm);
            gb.Prod("CreateTableKeyColumn").Is(identifierTerm, "ASC");
            gb.Prod("CreateTableKeyColumn").Is(identifierTerm, "DESC");

            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")");
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, "NONCLUSTERED", "HASH", "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, createTableClusterType, "(", createTableKeyColumnList, ")", createIndexWithClause);
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, "CLUSTERED", "COLUMNSTORE");
            gb.Prod("CreateTableTableIndex").Is("INDEX", identifierTerm, "CLUSTERED", "COLUMNSTORE", createIndexWithClause);

            gb.Prod("CreateTablePeriodClause").Is("PERIOD", forSystemTimeStart, "SYSTEM_TIME", "(", identifierTerm, ",", identifierTerm, ")");
            gb.Prod("CreateTableOptions").Is("WITH", "(", createTableOptionList, ")");
            gb.Prod("CreateTableOptionList").Is(createTableOption);
            gb.Prod("CreateTableOptionList").Is(createTableOptionList, ",", createTableOption);
            gb.Prod("CreateTableOption").Is("MEMORY_OPTIMIZED", "=", indexOnOffValue);
            gb.Prod("CreateTableOption").Is("DURABILITY", "=", createTableDurabilityMode);
            gb.Prod("CreateTableOption").Is("FILETABLE_DIRECTORY", "=", namedOptionValue);
            gb.Prod("CreateTableOption").Is("FILETABLE_COLLATE_FILENAME", "=", namedOptionValue);
            gb.Prod("CreateTableOption").Is("CLUSTERED", "COLUMNSTORE", "INDEX");
            gb.Rule(createTableDurabilityMode).Keywords("SCHEMA_AND_DATA", "SCHEMA_ONLY");
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
            gb.Prod("AlterTableAction").Is("DROP", "PERIOD", forSystemTimeStart, "SYSTEM_TIME");
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

            gb.Prod("CreateKeyListIndexHead").Is("CREATE", "INDEX", identifierTerm);
            gb.Prod("CreateKeyListIndexHead").Is("CREATE", "UNIQUE", "INDEX", identifierTerm);
            gb.Prod("CreateKeyListIndexHead").Is("CREATE", "CLUSTERED", "INDEX", identifierTerm);
            gb.Prod("CreateKeyListIndexHead").Is("CREATE", "NONCLUSTERED", "INDEX", identifierTerm);
            gb.Prod("CreateKeyListIndexHead").Is("CREATE", "UNIQUE", "CLUSTERED", "INDEX", identifierTerm);
            gb.Prod("CreateKeyListIndexHead").Is("CREATE", "UNIQUE", "NONCLUSTERED", "INDEX", identifierTerm);
            gb.Prod("CreateKeyListIndexHead").Is("CREATE", "NONCLUSTERED", "COLUMNSTORE", "INDEX", identifierTerm);
            gb.Prod("CreateKeylessIndexHead").Is("CREATE", "CLUSTERED", "COLUMNSTORE", "INDEX", identifierTerm);
            gb.Prod("CreateIndexStatement").Is(createKeyListIndexHead, "ON", qualifiedName, "(", createIndexKeyList, ")");
            gb.Prod("CreateIndexStatement").Is(createKeyListIndexHead, "ON", qualifiedName, "(", createIndexKeyList, ")", createIndexTailClauseList);
            gb.Prod("CreateIndexStatement").Is(createKeylessIndexHead, "ON", qualifiedName);
            gb.Prod("CreateIndexStatement").Is(createKeylessIndexHead, "ON", qualifiedName, createIndexTailClauseList);

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

            gb.Prod("IndexOptionValue").Is(expression);
            gb.Prod("IndexOptionValue").Is(indexOnOffValue);
            gb.Prod("IndexOptionValue").Is("NONE");
            gb.Prod("IndexOptionValue").Is("SELF");
            gb.Prod("IndexOptionValue").Is("BLOCKERS");
            gb.Prod("IndexOptionValue").Is("ROW");
            gb.Prod("IndexOptionValue").Is("PAGE");
            gb.Prod("IndexOptionValue").Is("COLUMNSTORE");
            gb.Prod("IndexOptionValue").Is("COLUMNSTORE_ARCHIVE");
            gb.Prod("IndexOptionValue").Is(expression, "MINUTES");
            gb.Prod("IndexOptionValue").Is(indexOnOffValue, "(", indexOptionList, ")");
            gb.Prod("IndexOptionValue").Is("(", indexOptionList, ")");
            gb.Rule(indexOnOffValue).Keywords("ON", "OFF");

            gb.Prod("NamedOptionValue").Is(expression);
            gb.Prod("NamedOptionValue").Is(identifierTerm);
            gb.Prod("NamedOptionValue").Is(qualifiedName);
            gb.Prod("NamedOptionValue").Is("ON");
            gb.Prod("NamedOptionValue").Is("OFF");

            gb.Prod("MaskingOptionList").Is("FUNCTION", "=", expression);

            gb.Prod("EncryptionOptionList").Is("COLUMN_ENCRYPTION_KEY", "=", namedOptionValue);
            gb.Prod("EncryptionOptionList").Is(encryptionOptionList, ",", "ENCRYPTION_TYPE", "=", namedOptionValue);
            gb.Prod("EncryptionOptionList").Is(encryptionOptionList, ",", "ALGORITHM", "=", namedOptionValue);
            gb.Prod("EncryptionOptionList").Is(encryptionOptionList, ",", "COLUMN_ENCRYPTION_KEY", "=", namedOptionValue);
            gb.Prod("EncryptionOptionList").Is("ENCRYPTION_TYPE", "=", namedOptionValue);
            gb.Prod("EncryptionOptionList").Is("ALGORITHM", "=", namedOptionValue);

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

            gb.Prod("ImplicitQueryExpression").Is(implicitQueryUnionExpression, queryExpressionTail);
            gb.Prod("ImplicitQueryUnionExpression").Is(implicitQueryIntersectExpression);
            gb.Prod("ImplicitQueryUnionExpression").Is(implicitQueryUnionExpression, setOperator, queryIntersectExpression);
            gb.Prod("ImplicitQueryIntersectExpression").Is(querySpecification);
            gb.Prod("ImplicitQueryIntersectExpression").Is(implicitQueryIntersectExpression, "INTERSECT", queryPrimary);
            gb.Prod("QueryExpression").Is(queryUnionExpression, queryExpressionTail);
            gb.Prod("QueryUnionExpression").Is(queryIntersectExpression);
            gb.Prod("QueryUnionExpression").Is(queryUnionExpression, setOperator, queryIntersectExpression);
            gb.Prod("QueryIntersectExpression").Is(queryPrimary);
            gb.Prod("QueryIntersectExpression").Is(queryIntersectExpression, "INTERSECT", queryPrimary);

            gb.Rule(setOperator)
                .CanBe("UNION")
                .Or("UNION", "ALL")
                .OrKeywords("EXCEPT");

            gb.Prod("QueryExpressionTail").Is(queryExpressionOrderByAndOffsetOpt, queryExpressionForOpt, queryExpressionOptionOpt);
            gb.Opt(queryExpressionOrderByAndOffsetOpt, orderByClause);
            gb.Prod("QueryExpressionOrderByAndOffsetOpt").Is(orderByClause, offsetFetchClause);
            gb.Opt(queryExpressionForOpt, forClause);
            gb.Opt(queryExpressionOptionOpt, optionClause);
            gb.Prod("QueryPrimary").Is(querySpecification);
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")");

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
            gb.Prod("SelectCoreIntoClause").Is("INTO", qualifiedName);
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
            gb.Prod("TableFactor").Is(qualifiedName, forPathStart, "PATH");
            gb.Prod("TableFactor").Is(qualifiedName, forPathStart, "PATH", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("TableFactor").Is(qualifiedName, "AS", identifierTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("TableFactor").Is(qualifiedName, identifierTerm, "WITH", "(", tableHintLimitedList, ")");
            gb.Prod("TableFactor").Is(qualifiedName, temporalClause);
            gb.Prod("TableFactor").Is(qualifiedName, temporalClause, "AS", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, temporalClause, identifierTerm);
            gb.Rule("TemporalClause").OneOf(
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "AS", "OF", additiveExpression),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "ALL"),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "BETWEEN", additiveExpression, "AND", additiveExpression),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "FROM", additiveExpression, "TO", additiveExpression),
                gb.Seq(forSystemTimeStart, "SYSTEM_TIME", "CONTAINED", "IN", "(", additiveExpression, ",", additiveExpression, ")"));
            gb.Prod("TableFactor").Is(variableReference);
            gb.Prod("TableFactor").Is(variableReference, "AS", identifierTerm);
            gb.Prod("TableFactor").Is(variableReference, identifierTerm);
            gb.Prod("TableFactor").Is(functionCall);
            gb.Prod("TableFactor").Is(functionCall, "AS", identifierTerm);
            gb.Prod("TableFactor").Is(functionCall, identifierTerm);
            gb.Prod("TableFactor").Is(functionCall, "AS", identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is(functionCall, identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is(openJsonCall);
            gb.Prod("TableFactor").Is(openJsonCall, "AS", identifierTerm);
            gb.Prod("TableFactor").Is(openJsonCall, identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", "AS", identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, "(", insertColumnList, ")");
            // PIVOT / UNPIVOT applied to derived table
            gb.Prod("TableFactor").Is("(", queryExpression, ")", "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, identifierTerm, "PIVOT", "(", pivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, "AS", identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, identifierTerm, "PIVOT", "(", pivotClause, ")", identifierTerm);
            gb.Prod("PivotClause").Is(functionCall, "FOR", identifierTerm, "IN", "(", pivotValueList, ")");
            gb.Prod("PivotValueList").Is(expression);
            gb.Prod("PivotValueList").Is(pivotValueList, ",", expression);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, identifierTerm, "UNPIVOT", "(", unpivotClause, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, "AS", identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
            gb.Prod("TableFactor").Is(qualifiedName, identifierTerm, "UNPIVOT", "(", unpivotClause, ")", identifierTerm);
            gb.Prod("UnpivotClause").Is(identifierTerm, "FOR", identifierTerm, "IN", "(", unpivotColumnList, ")");
            gb.Prod("UnpivotColumnList").Is(identifierTerm);
            gb.Prod("UnpivotColumnList").Is(unpivotColumnList, ",", identifierTerm);
            gb.Prod("TableFactor").Is("(", "VALUES", rowValueList, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", "VALUES", rowValueList, ")", identifierTerm);
            gb.Prod("TableFactor").Is("(", "VALUES", rowValueList, ")", "AS", identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is("(", "VALUES", rowValueList, ")", identifierTerm, "(", insertColumnList, ")");
            gb.Prod("TableFactor").Is("(", tableSource, ")");
            gb.Prod("TableFactor").Is("(", tableSource, ")", "AS", identifierTerm);
            gb.Prod("TableFactor").Is("(", tableSource, ")", identifierTerm);

            gb.Prod("OpenJsonCall").Is("OPENJSON", "(", functionArgumentList, ")");
            gb.Prod("OpenJsonCall").Is("OPENJSON", "(", functionArgumentList, ")", openJsonWithClause);
            gb.Prod("OpenJsonWithClause").Is("WITH", "(", openJsonColumnList, ")");
            gb.Prod("OpenJsonColumnList").Is(openJsonColumnDef);
            gb.Prod("OpenJsonColumnList").Is(openJsonColumnList, ",", openJsonColumnDef);
            // col_name typeSpec [ path_expr ] [ AS JSON ]
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec);
            gb.Prod("OpenJsonPath").Is(stringLiteral);
            gb.Prod("OpenJsonPath").Is(unicodeStringLiteral);
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec, openJsonPath);
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec, "AS", "JSON");
            gb.Prod("OpenJsonColumnDef").Is(identifierTerm, typeSpec, openJsonPath, "AS", "JSON");

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
            gb.Prod("BooleanPrimary").Is("MATCH", "(", matchGraphPattern, ")");

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
            gb.Prod("PrimaryExpression").Is(graphColumnRef);
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

            // DECLARE CURSOR
            gb.Rule("DeclareCursorStatement").OneOf(
                gb.Seq("DECLARE", identifierTerm, "CURSOR", "FOR", queryExpression),
                gb.Seq("DECLARE", identifierTerm, "CURSOR", cursorOptionList, "FOR", queryExpression));
            gb.Rule("CursorOptionList").Plus(cursorOption);
            gb.Rule("CursorOption").Keywords(
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
                gb.Seq("WAITFOR", "DELAY", expression),
                gb.Seq("WAITFOR", "TIME", expression));

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
            // DROP EVENT SESSION ON DATABASE/SERVER
            gb.Rule("DropEventSessionStatement").OneOf(
                gb.Seq("DROP", "EVENT", "SESSION", identifierTerm, "ON", "DATABASE"),
                gb.Seq("DROP", "EVENT", "SESSION", identifierTerm, "ON", "SERVER"));

            // CREATE TYPE ... AS TABLE
            gb.Prod("CreateTypeStatement").Is("CREATE", "TYPE", qualifiedName, "AS", tableTypeDefinition);
            gb.Prod("CreateTypeStatement").Is("CREATE", "TYPE", qualifiedName, "FROM", typeSpec);

            // MERGE DML statement
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

            // BULK INSERT
            gb.Prod("BulkInsertStatement").Is("BULK", "INSERT", qualifiedName, "FROM", expression);
            gb.Prod("BulkInsertStatement").Is("BULK", "INSERT", qualifiedName, "FROM", expression, "WITH", "(", bulkInsertOptionList, ")");
            gb.Prod("BulkInsertOptionList").Is("CHECK_CONSTRAINTS");
            gb.Prod("BulkInsertOptionList").Is("KEEPIDENTITY");
            gb.Prod("BulkInsertOptionList").Is("KEEPNULLS");
            gb.Prod("BulkInsertOptionList").Is("TABLOCK");
            gb.Prod("BulkInsertOptionList").Is("FIRE_TRIGGERS");
            gb.Prod("BulkInsertOptionList").Is("CODEPAGE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("DATAFILETYPE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("ERRORFILE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("ERRORFILE_DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("FIRSTROW", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("FORMAT", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("FIELDQUOTE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("FORMATFILE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("FORMATFILE_DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("KILOBYTES_PER_BATCH", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("LASTROW", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("MAXERRORS", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("ROWS_PER_BATCH", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("ROWTERMINATOR", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("FIELDTERMINATOR", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("BATCHSIZE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is("ORDER", "(", createTableKeyColumnList, ")");
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "CHECK_CONSTRAINTS");
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "KEEPIDENTITY");
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "KEEPNULLS");
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "TABLOCK");
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "FIRE_TRIGGERS");
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "CODEPAGE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "DATAFILETYPE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "ERRORFILE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "ERRORFILE_DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "FIRSTROW", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "FORMAT", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "FIELDQUOTE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "FORMATFILE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "FORMATFILE_DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "KILOBYTES_PER_BATCH", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "LASTROW", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "MAXERRORS", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "ROWS_PER_BATCH", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "ROWTERMINATOR", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "FIELDTERMINATOR", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "BATCHSIZE", "=", namedOptionValue);
            gb.Prod("BulkInsertOptionList").Is(bulkInsertOptionList, ",", "ORDER", "(", createTableKeyColumnList, ")");

            gb.Prod("CheckpointStatement").Is("CHECKPOINT");

            // SQL Graph: MATCH is a search condition predicate and is handled by BooleanPrimary.
            gb.Prod("MatchGraphPattern").Is(matchGraphPath);
            gb.Prod("MatchGraphPattern").Is("SHORTEST_PATH", "(", matchGraphShortestPath, ")");
            gb.Prod("MatchGraphPattern").Is(matchGraphPattern, "AND", matchGraphPath);
            gb.Prod("MatchGraphPattern").Is(matchGraphPattern, "AND", "SHORTEST_PATH", "(", matchGraphShortestPath, ")");
            gb.Prod("MatchGraphShortestPath").Is(matchGraphShortestPathBody);
            gb.Prod("MatchGraphShortestPath").Is(matchGraphShortestPathBody, "+");
            gb.Prod("MatchGraphShortestPath").Is(matchGraphShortestPathBody, "{", number, ",", number, "}");
            gb.Prod("MatchGraphPath").Is(identifierTerm);
            gb.Prod("MatchGraphPath").Is(identifierTerm, matchGraphStepChain);
            gb.Prod("MatchGraphShortestPathBody").Is(identifierTerm, "(", matchGraphStepChain, ")");
            gb.Rule(matchGraphStep)
                .CanBe("-", "(", identifierTerm, ")", "-", ">", identifierTerm)
                .Or("<", "-", "(", identifierTerm, ")", "-", identifierTerm)
                .Or("-", "(", identifierTerm, ")", "-", identifierTerm);
            gb.Prod("MatchGraphStepChain").Is(matchGraphStep);
            gb.Prod("MatchGraphStepChain").Is(matchGraphStepChain, matchGraphStep);

            // PREDICT ML function
            gb.Prod("FunctionCall").Is("PREDICT", "(", predictArgList, ")");
            gb.Prod("PredictArgList").Is(predictArg);
            gb.Prod("PredictArgList").Is(predictArgList, ",", predictArg);
            gb.Prod("PredictArg").Is(identifierTerm, "=", expression);
            gb.Prod("PredictArg").Is(identifierTerm, "=", expression, "AS", identifierTerm);

            gb.Prod("StrictQualifiedName").Is(strictIdentifierTerm);
            gb.Prod("StrictQualifiedName").Is(strictQualifiedName, ".", strictIdentifierTerm);
            gb.Prod("StrictQualifiedName").Is(strictQualifiedName, ".", ".", strictIdentifierTerm); // double-dot: master..table
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
            gb.Prod("CreateExternalTableStatement").Is("CREATE", "EXTERNAL", "TABLE", qualifiedName, "(", createTableElementList, ")", "WITH", "(", externalTableOptionList, ")");
            gb.Prod("ExternalTableOptionList").Is("LOCATION", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is("DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is("FILE_FORMAT", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is("REJECT_TYPE", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is("REJECT_VALUE", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is("REJECT_SAMPLE_VALUE", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is("DISTRIBUTION", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is("SCHEMA_NAME", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is("OBJECT_NAME", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is(externalTableOptionList, ",", "LOCATION", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is(externalTableOptionList, ",", "DATA_SOURCE", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is(externalTableOptionList, ",", "FILE_FORMAT", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is(externalTableOptionList, ",", "REJECT_TYPE", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is(externalTableOptionList, ",", "REJECT_VALUE", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is(externalTableOptionList, ",", "REJECT_SAMPLE_VALUE", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is(externalTableOptionList, ",", "DISTRIBUTION", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is(externalTableOptionList, ",", "SCHEMA_NAME", "=", namedOptionValue);
            gb.Prod("ExternalTableOptionList").Is(externalTableOptionList, ",", "OBJECT_NAME", "=", namedOptionValue);

            // CREATE EXTERNAL DATA SOURCE name WITH (TYPE=..., LOCATION=..., ...)
            gb.Prod("CreateExternalDataSourceStatement").Is("CREATE", "EXTERNAL", "DATA", "SOURCE", identifierTerm, "WITH", "(", externalDataSourceOptionList, ")");
            gb.Prod("CreateExternalDataSourceStatement").Is("CREATE", "EXTERNAL", "DATA", "SOURCE", qualifiedName, "WITH", "(", externalDataSourceOptionList, ")");
            gb.Prod("ExternalDataSourceOptionList").Is("TYPE", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is("LOCATION", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is("RESOURCE_MANAGER_LOCATION", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is("DATABASE_NAME", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is("SHARD_MAP_NAME", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is("CREDENTIAL", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is("CONNECTION_OPTIONS", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is("PUSHDOWN", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is(externalDataSourceOptionList, ",", "TYPE", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is(externalDataSourceOptionList, ",", "LOCATION", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is(externalDataSourceOptionList, ",", "RESOURCE_MANAGER_LOCATION", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is(externalDataSourceOptionList, ",", "DATABASE_NAME", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is(externalDataSourceOptionList, ",", "SHARD_MAP_NAME", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is(externalDataSourceOptionList, ",", "CREDENTIAL", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is(externalDataSourceOptionList, ",", "CONNECTION_OPTIONS", "=", namedOptionValue);
            gb.Prod("ExternalDataSourceOptionList").Is(externalDataSourceOptionList, ",", "PUSHDOWN", "=", namedOptionValue);

            return gb.BuildGrammar("Start");
        }
    }
}

