using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Formatting;
using DSLKIT.Lexer;
using DSLKIT.Parser;
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
            var tokens = BuildSignificantTokensWithTrivia(rawTokens);

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
                var isTriviaToken = token.Terminal.Flags == TermFlags.Space ||
                    token.Terminal.Flags == TermFlags.Comment;
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

            var number = new NumberTerminal("Number", NumberStyle.SqlNumber);
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
                .AddTerminal(stringLiteral);
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

            var script = gb.NT("Script");
            var statementList = gb.NT("StatementList");
            var statement = gb.NT("Statement");
            var queryStatement = gb.NT("QueryStatement");
            var updateStatement = gb.NT("UpdateStatement");
            var updateSetList = gb.NT("UpdateSetList");
            var updateSetItem = gb.NT("UpdateSetItem");
            var insertStatement = gb.NT("InsertStatement");
            var insertColumnList = gb.NT("InsertColumnList");
            var insertValueList = gb.NT("InsertValueList");
            var createProcStatement = gb.NT("CreateProcStatement");
            var procStatementList = gb.NT("ProcStatementList");
            var withClause = gb.NT("WithClause");
            var cteDefinitionList = gb.NT("CteDefinitionList");
            var cteDefinition = gb.NT("CteDefinition");
            var queryExpression = gb.NT("QueryExpression");
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
            var joinPart = gb.NT("JoinPart");
            var joinType = gb.NT("JoinType");
            var orderByClause = gb.NT("OrderByClause");
            var orderItemList = gb.NT("OrderItemList");
            var orderItem = gb.NT("OrderItem");
            var offsetFetchClause = gb.NT("OffsetFetchClause");
            var searchCondition = gb.NT("SearchCondition");
            var expression = gb.NT("Expression");
            var binaryOperator = gb.NT("BinaryOperator");
            var unaryOperator = gb.NT("UnaryOperator");
            var primaryExpression = gb.NT("PrimaryExpression");
            var overClause = gb.NT("OverClause");
            var overSpec = gb.NT("OverSpec");
            var frameClause = gb.NT("FrameClause");
            var frameBoundary = gb.NT("FrameBoundary");
            var functionCall = gb.NT("FunctionCall");
            var functionArgumentList = gb.NT("FunctionArgumentList");
            var literal = gb.NT("Literal");
            var caseExpression = gb.NT("CaseExpression");
            var caseWhenList = gb.NT("CaseWhenList");
            var caseWhen = gb.NT("CaseWhen");
            var expressionList = gb.NT("ExpressionList");
            var identifierList = gb.NT("IdentifierList");
            var identifierTerm = gb.NT("IdentifierTerm");
            var qualifiedName = gb.NT("QualifiedName");
            var variableReference = gb.NT("VariableReference");

            gb.Prod("Start").Is(script);
            gb.Prod("Script").Is(statementList);
            gb.Prod("Script").Is(statementList, ";");
            gb.Prod("StatementList").Is(statement);
            gb.Prod("StatementList").Is(statementList, ";", statement);
            gb.Prod("Statement").Is(queryStatement);
            gb.Prod("Statement").Is(updateStatement);
            gb.Prod("Statement").Is(insertStatement);
            gb.Prod("Statement").Is(createProcStatement);

            gb.Prod("QueryStatement").Is(queryExpression);
            gb.Prod("QueryStatement").Is(withClause, queryExpression);
            gb.Prod("UpdateStatement").Is(kw("UPDATE"), tableFactor, kw("SET"), updateSetList);
            gb.Prod("UpdateStatement").Is(kw("UPDATE"), tableFactor, kw("SET"), updateSetList, kw("WHERE"), searchCondition);
            gb.Prod("UpdateSetList").Is(updateSetItem);
            gb.Prod("UpdateSetList").Is(updateSetList, ",", updateSetItem);
            gb.Prod("UpdateSetItem").Is(qualifiedName, "=", expression);

            gb.Prod("InsertStatement").Is(
                kw("INSERT"),
                kw("INTO"),
                qualifiedName,
                "(",
                insertColumnList,
                ")",
                kw("VALUES"),
                "(",
                insertValueList,
                ")");
            gb.Prod("InsertColumnList").Is(identifierTerm);
            gb.Prod("InsertColumnList").Is(insertColumnList, ",", identifierTerm);
            gb.Prod("InsertValueList").Is(expression);
            gb.Prod("InsertValueList").Is(insertValueList, ",", expression);

            gb.Prod("CreateProcStatement").Is(kw("CREATE"), kw("PROC"), identifierTerm, kw("AS"), kw("BEGIN"), procStatementList, kw("END"));
            gb.Prod("CreateProcStatement").Is(kw("CREATE"), kw("PROCEDURE"), identifierTerm, kw("AS"), kw("BEGIN"), procStatementList, kw("END"));
            gb.Prod("ProcStatementList").Is(statement);
            gb.Prod("ProcStatementList").Is(procStatementList, ";", statement);

            gb.Prod("WithClause").Is(kw("WITH"), cteDefinitionList);
            gb.Prod("CteDefinitionList").Is(cteDefinition);
            gb.Prod("CteDefinitionList").Is(cteDefinitionList, ",", cteDefinition);
            gb.Prod("CteDefinition").Is(identifierTerm, kw("AS"), "(", queryExpression, ")");
            gb.Prod("CteDefinition").Is(identifierTerm, "(", identifierList, ")", kw("AS"), "(", queryExpression, ")");

            gb.Prod("QueryExpression").Is(queryPrimary);
            gb.Prod("QueryExpression").Is(queryExpression, setOperator, queryPrimary);

            gb.Prod("SetOperator").Is(kw("UNION"));
            gb.Prod("SetOperator").Is(kw("UNION"), kw("ALL"));
            gb.Prod("SetOperator").Is(kw("INTERSECT"));
            gb.Prod("SetOperator").Is(kw("EXCEPT"));

            gb.Prod("QueryPrimary").Is(querySpecification);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause, offsetFetchClause);
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")");
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")", orderByClause);
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")", orderByClause, offsetFetchClause);

            gb.Prod("SelectCore").Is(kw("SELECT"), selectList, kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(kw("SELECT"), setQuantifier, selectList, kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(kw("SELECT"), topClause, selectList, kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(kw("SELECT"), setQuantifier, topClause, selectList, kw("FROM"), tableSourceList);
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
            gb.Prod("SelectItem").Is(expression);
            gb.Prod("SelectItem").Is(qualifiedName, ".", "*");

            gb.Prod("TableSourceList").Is(tableSource);
            gb.Prod("TableSourceList").Is(tableSourceList, ",", tableSource);
            gb.Prod("TableSource").Is(tableFactor);
            gb.Prod("TableSource").Is(tableSource, joinPart);
            gb.Prod("TableFactor").Is(qualifiedName);
            gb.Prod("TableFactor").Is(qualifiedName, kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is(functionCall, kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", kw("AS"), identifierTerm);

            gb.Prod("JoinPart").Is(kw("JOIN"), tableFactor, kw("ON"), searchCondition);
            gb.Prod("JoinPart").Is(joinType, kw("JOIN"), tableFactor, kw("ON"), searchCondition);
            gb.Prod("JoinPart").Is(kw("CROSS"), kw("JOIN"), tableFactor);
            gb.Prod("JoinPart").Is(kw("CROSS"), kw("APPLY"), tableFactor);
            gb.Prod("JoinPart").Is(kw("OUTER"), kw("APPLY"), tableFactor);

            gb.Prod("JoinType").Is(kw("INNER"));
            gb.Prod("JoinType").Is(kw("LEFT"));
            gb.Prod("JoinType").Is(kw("LEFT"), kw("OUTER"));
            gb.Prod("JoinType").Is(kw("RIGHT"));
            gb.Prod("JoinType").Is(kw("RIGHT"), kw("OUTER"));
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

            gb.Prod("Expression").Is(primaryExpression);
            gb.Prod("Expression").Is(unaryOperator, expression);
            gb.Prod("Expression").Is(expression, binaryOperator, primaryExpression);

            gb.Prod("UnaryOperator").Is(kw("NOT"));
            gb.Prod("UnaryOperator").Is("+");
            gb.Prod("UnaryOperator").Is("-");
            gb.Prod("UnaryOperator").Is("~");

            gb.Prod("BinaryOperator").Is(kw("OR"));
            gb.Prod("BinaryOperator").Is(kw("AND"));
            gb.Prod("BinaryOperator").Is("=");
            gb.Prod("BinaryOperator").Is("<>");
            gb.Prod("BinaryOperator").Is("!=");
            gb.Prod("BinaryOperator").Is("<");
            gb.Prod("BinaryOperator").Is("<=");
            gb.Prod("BinaryOperator").Is(">");
            gb.Prod("BinaryOperator").Is(">=");
            gb.Prod("BinaryOperator").Is(kw("LIKE"));
            gb.Prod("BinaryOperator").Is(kw("IN"));
            gb.Prod("BinaryOperator").Is(kw("IS"));
            gb.Prod("BinaryOperator").Is("+");
            gb.Prod("BinaryOperator").Is("-");
            gb.Prod("BinaryOperator").Is("*");
            gb.Prod("BinaryOperator").Is("/");
            gb.Prod("BinaryOperator").Is("%");

            gb.Prod("PrimaryExpression").Is(literal);
            gb.Prod("PrimaryExpression").Is(variableReference);
            gb.Prod("PrimaryExpression").Is(qualifiedName);
            gb.Prod("PrimaryExpression").Is(functionCall);
            gb.Prod("PrimaryExpression").Is(functionCall, overClause);
            gb.Prod("PrimaryExpression").Is("(", expression, ")");
            gb.Prod("PrimaryExpression").Is("(", expressionList, ")");
            gb.Prod("PrimaryExpression").Is("(", queryExpression, ")");
            gb.Prod("PrimaryExpression").Is(kw("EXISTS"), "(", queryExpression, ")");
            gb.Prod("PrimaryExpression").Is(caseExpression);

            gb.Prod("FunctionCall").Is(qualifiedName, "(", ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", "*", ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", functionArgumentList, ")");
            gb.Prod("FunctionArgumentList").Is(expression);
            gb.Prod("FunctionArgumentList").Is(functionArgumentList, ",", expression);

            gb.Prod("OverClause").Is(kw("OVER"), "(", overSpec, ")");
            gb.Prod("OverSpec").Is(kw("PARTITION"), kw("BY"), expressionList, kw("ORDER"), kw("BY"), orderItemList);
            gb.Prod("OverSpec").Is(kw("PARTITION"), kw("BY"), expressionList, kw("ORDER"), kw("BY"), orderItemList, kw("ROWS"), frameClause);

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

            gb.Prod("QualifiedName").Is(identifierTerm);
            gb.Prod("QualifiedName").Is(qualifiedName, ".", identifierTerm);

            return gb.BuildGrammar("Start");
        }
    }
}


