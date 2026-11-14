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
        SnowflakeCompat = 32,
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

        public static IGrammar BuildGrammar(SqlDialect dialect)
        {
            return BuildGrammar(SqlDialectGrammarModules.GetDefaultFeatures(dialect));
        }

        public static IGrammar BuildGrammar(MsSqlDialectFeatures dialectFeatures)
        {
            return GetGrammarResources(dialectFeatures).Grammar;
        }

        public static ParseResult ParseBatch(string source)
        {
            return ParseBatch(source, MsSqlDialectFeatures.All);
        }

        public static ParseResult ParseBatch(string source, SqlDialect dialect)
        {
            return ParseBatch(source, SqlDialectGrammarModules.GetDefaultFeatures(dialect));
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
                    Error = new ParseErrorDescription
                    {
                        Message = $"Lexer error: {lexerError.ErrorMessage}",
                        ErrorPosition = lexerError.Position,
                        ActualTokenText = lexerError.OriginalString
                    }
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

        public static SqlScriptDocumentParseResult ParseDocument(string source, SqlDialect dialect)
        {
            return ParseDocument(source, SqlDialectGrammarModules.GetDefaultFeatures(dialect));
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
            var grammarContext = new MsSqlGrammarContext(
                gb,
                dialectFeatures,
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
            var dialectModuleContext = new SqlDialectGrammarModuleContext(
                grammarContext,
                new SqlDialectGrammarExtensionPoints(
                    gb.NT("WithClause"),
                    gb.NT("CreateViewHead"),
                    gb.NT("BulkInsertStatement"),
                    gb.NT("BulkInsertOptionList"),
                    gb.NT("MatchGraphPath"),
                    gb.NT("MatchGraphStep"),
                    gb.NT("MatchGraphStepChain"),
                    gb.NT("MatchGraphShortestPath"),
                    gb.NT("MatchGraphShortestPathBody"),
                    gb.NT("CreateTableAsSelectStatement"),
                    gb.NT("PredictArgList"),
                    gb.NT("PredictArg"),
                    gb.NT("CreateSecurityPolicyStatement"),
                    gb.NT("AlterSecurityPolicyStatement"),
                    gb.NT("SecurityPolicyClauseList"),
                    gb.NT("SecurityPolicyClause"),
                    gb.NT("SecurityPolicyOptionList"),
                    gb.NT("SecurityPolicyOption"),
                    gb.NT("SecurityPolicyOptionName"),
                    gb.NT("CreateExternalTableStatement"),
                    gb.NT("ExternalTableOptionList"),
                    gb.NT("CreateExternalDataSourceStatement"),
                    gb.NT("ExternalDataSourceOptionList"),
                    gb.NT("MergeStatement"),
                    gb.NT("CreateTableElementList")));
            var statementRegistry = new MsSqlStatementRegistry();
            statementRegistry.Add(gb.NT("QueryStatementNoLeadingWith"), gb.NT("ImplicitQueryStatementNoLeadingWith"));
            statementRegistry.Add(gb.NT("UpdateStatement"));
            statementRegistry.Add(gb.NT("InsertStatement"));
            statementRegistry.Add(gb.NT("DeleteStatement"));
            statementRegistry.Add(gb.NT("IfStatement"));
            statementRegistry.Add(gb.NT("BeginEndStatement"));
            statementRegistry.Add(gb.NT("WhileStatement"));
            statementRegistry.Add(gb.NT("SetStatement"));
            statementRegistry.Add(gb.NT("PrintStatement"));
            statementRegistry.Add(gb.NT("DeclareStatement"));
            statementRegistry.Add(gb.NT("ReturnStatement"), allowedInFunctionPrelude: false);
            statementRegistry.Add(gb.NT("TransactionStatement"));
            statementRegistry.Add(gb.NT("RaiserrorStatement"));
            statementRegistry.Add(gb.NT("ThrowStatement"));
            statementRegistry.Add(gb.NT("LoopControlStatement"));
            statementRegistry.Add(gb.NT("GotoStatement"));
            statementRegistry.Add(gb.NT("LabelStatement"), gb.NT("LabelOnlyStatement"));
            statementRegistry.Add(gb.NT("ExecuteStatement"));
            statementRegistry.Add(gb.NT("UseStatement"));
            statementRegistry.Add(gb.NT("CreateProcStatement"));
            statementRegistry.Add(gb.NT("CreateFunctionStatement"));
            statementRegistry.Add(gb.NT("GrantStatement"));
            statementRegistry.Add(gb.NT("DbccStatement"));
            statementRegistry.Add(gb.NT("DropProcStatement"));
            statementRegistry.Add(gb.NT("DropTableStatement"));
            statementRegistry.Add(gb.NT("DropViewStatement"));
            statementRegistry.Add(gb.NT("DropIndexStatement"));
            statementRegistry.Add(gb.NT("DropStatisticsStatement"));
            statementRegistry.Add(gb.NT("DropDatabaseStatement"));
            statementRegistry.Add(gb.NT("CreateRoleStatement"));
            statementRegistry.Add(gb.NT("CreateSchemaStatement"));
            statementRegistry.Add(gb.NT("CreateViewStatement"));
            statementRegistry.Add(gb.NT("CreateTableStatement"));
            statementRegistry.Add(gb.NT("AlterTableStatement"));
            statementRegistry.Add(gb.NT("CreateIndexStatement"));
            statementRegistry.Add(gb.NT("AlterIndexStatement"));
            statementRegistry.Add(gb.NT("CreateDatabaseStatement"));
            statementRegistry.Add(gb.NT("CreateTriggerStatement"));
            statementRegistry.Add(gb.NT("DropTriggerStatement"));
            statementRegistry.Add(gb.NT("TryCatchStatement"));
            statementRegistry.Add(gb.NT("TruncateStatement"));
            statementRegistry.Add(gb.NT("AlterDatabaseStatement"));
            statementRegistry.Add(gb.NT("DeclareCursorStatement"));
            statementRegistry.Add(gb.NT("CursorOperationStatement"));
            statementRegistry.Add(gb.NT("WaitforStatement"));
            statementRegistry.Add(gb.NT("CreateLoginStatement"));
            statementRegistry.Add(gb.NT("CheckpointStatement"));
            statementRegistry.Add(gb.NT("CreateUserStatement"));
            statementRegistry.Add(gb.NT("CreateStatisticsStatement"));
            statementRegistry.Add(gb.NT("UpdateStatisticsStatement"));
            statementRegistry.Add(gb.NT("DropTypeStatement"));
            statementRegistry.Add(gb.NT("DropColumnEncryptionKeyStatement"));
            statementRegistry.Add(gb.NT("RevertStatement"));
            statementRegistry.Add(gb.NT("DropEventSessionStatement"));
            statementRegistry.Add(gb.NT("CreateTypeStatement"));
            SqlDialectGrammarModules.Resolve(dialectFeatures).RegisterStatements(statementRegistry, dialectModuleContext);

            var statementNoLeadingWithAlternatives = statementRegistry.CreateTopLevelAlternatives();
            var implicitStatementNoLeadingWithAlternatives = statementRegistry.CreateImplicitAlternatives();
            var createFunctionPreludeStatementNoLeadingWithAlternatives = statementRegistry.CreateFunctionPreludeAlternatives();
            var createFunctionImplicitPreludeStatementNoLeadingWithAlternatives = statementRegistry.CreateFunctionImplicitPreludeAlternatives();
            var leadingWithStatementAlternatives = SqlDialectGrammarModules
                .Resolve(dialectFeatures)
                .CreateLeadingWithStatementAlternatives(dialectModuleContext);

            MsSqlScriptGrammar.Build(grammarContext, leadingWithStatementAlternatives, statementNoLeadingWithAlternatives, implicitStatementNoLeadingWithAlternatives);
            MsSqlDmlGrammar.Build(grammarContext);
            MsSqlProceduralGrammar.Build(grammarContext);

            MsSqlExecutionGrammar.Build(grammarContext);
            MsSqlProgrammableObjectsGrammar.Build(grammarContext, withCheckOptionStart, createFunctionPreludeStatementNoLeadingWithAlternatives, createFunctionImplicitPreludeStatementNoLeadingWithAlternatives);

            MsSqlSecurityAndAdminGrammar.BuildPreQuery(grammarContext);
            MsSqlSchemaDdlGrammar.BuildTableAndIndexGrammar(grammarContext);

            MsSqlSchemaDdlGrammar.BuildDatabaseGrammar(grammarContext);

            MsSqlQueryGrammar.Build(grammarContext);
            MsSqlTableSourceGrammar.Build(grammarContext);
            MsSqlExpressionGrammar.Build(grammarContext);

            MsSqlSecurityAndAdminGrammar.BuildPostQuery(grammarContext);
            MsSqlUpdateStatisticsGrammar.Build(grammarContext);
            MsSqlExtensionsGrammar.BuildMergeGrammar(gb);
            SqlDialectGrammarModules.Resolve(dialectFeatures).Apply(dialectModuleContext);

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
            return SqlDialectGrammarModules.NormalizeFeatures(dialectFeatures);
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

