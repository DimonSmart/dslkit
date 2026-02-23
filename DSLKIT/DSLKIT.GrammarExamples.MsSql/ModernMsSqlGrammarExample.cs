using System;
using System.Linq;
using System.Text.RegularExpressions;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Terminals;

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
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var grammar = BuildGrammar();
            var lexer = new Lexer.Lexer(CreateLexerSettings(grammar));
            var parser = new SyntaxParser(grammar);

            var tokens = lexer.GetTokens(new StringSourceStream(source))
                .Where(token => token.Terminal.Flags != TermFlags.Space && token.Terminal.Flags != TermFlags.Comment)
                .ToList();

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

        private static IGrammar BuildGrammarCore()
        {
            var identifier = new RegExpTerminal(
                "Identifier",
                @"\G(?i)[a-z_][a-z0-9_$#]*",
                previewChar: null,
                flags: TermFlags.Identifier);

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

            var number = new RegExpTerminal(
                "Number",
                @"\G(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?",
                previewChar: null,
                flags: TermFlags.Const);

            var stringLiteral = new RegExpTerminal(
                "String",
                @"\G(?i)N?'(?:''|[^'])*'",
                previewChar: null,
                flags: TermFlags.Const);

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

            var script = gb.NT("Script");
            var statementList = gb.NT("StatementList");
            var statement = gb.NT("Statement");
            var queryStatement = gb.NT("QueryStatement");
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

            gb.Prod("QueryStatement").Is(queryExpression);
            gb.Prod("QueryStatement").Is(withClause, queryExpression);

            gb.Prod("WithClause").Is(Kw("WITH"), cteDefinitionList);
            gb.Prod("CteDefinitionList").Is(cteDefinition);
            gb.Prod("CteDefinitionList").Is(cteDefinitionList, ",", cteDefinition);
            gb.Prod("CteDefinition").Is(identifierTerm, Kw("AS"), "(", queryExpression, ")");
            gb.Prod("CteDefinition").Is(identifierTerm, "(", identifierList, ")", Kw("AS"), "(", queryExpression, ")");

            gb.Prod("QueryExpression").Is(queryPrimary);
            gb.Prod("QueryExpression").Is(queryExpression, setOperator, queryPrimary);

            gb.Prod("SetOperator").Is(Kw("UNION"));
            gb.Prod("SetOperator").Is(Kw("UNION"), Kw("ALL"));
            gb.Prod("SetOperator").Is(Kw("INTERSECT"));
            gb.Prod("SetOperator").Is(Kw("EXCEPT"));

            gb.Prod("QueryPrimary").Is(querySpecification);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause);
            gb.Prod("QueryPrimary").Is(querySpecification, orderByClause, offsetFetchClause);
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")");
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")", orderByClause);
            gb.Prod("QueryPrimary").Is("(", queryExpression, ")", orderByClause, offsetFetchClause);

            gb.Prod("SelectCore").Is(Kw("SELECT"), selectList, Kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(Kw("SELECT"), setQuantifier, selectList, Kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(Kw("SELECT"), topClause, selectList, Kw("FROM"), tableSourceList);
            gb.Prod("SelectCore").Is(Kw("SELECT"), setQuantifier, topClause, selectList, Kw("FROM"), tableSourceList);

            gb.Prod("QuerySpecification").Is(selectCore);
            gb.Prod("QuerySpecification").Is(selectCore, Kw("WHERE"), searchCondition);
            gb.Prod("QuerySpecification").Is(selectCore, Kw("GROUP"), Kw("BY"), expressionList);
            gb.Prod("QuerySpecification").Is(selectCore, Kw("WHERE"), searchCondition, Kw("GROUP"), Kw("BY"), expressionList);
            gb.Prod("QuerySpecification").Is(selectCore, Kw("GROUP"), Kw("BY"), expressionList, Kw("HAVING"), searchCondition);
            gb.Prod("QuerySpecification").Is(selectCore, Kw("WHERE"), searchCondition, Kw("GROUP"), Kw("BY"), expressionList, Kw("HAVING"), searchCondition);

            gb.Prod("SetQuantifier").Is(Kw("ALL"));
            gb.Prod("SetQuantifier").Is(Kw("DISTINCT"));

            gb.Prod("TopClause").Is(Kw("TOP"), topValue);
            gb.Prod("TopClause").Is(Kw("TOP"), topValue, Kw("PERCENT"));
            gb.Prod("TopClause").Is(Kw("TOP"), topValue, Kw("WITH"), Kw("TIES"));
            gb.Prod("TopClause").Is(Kw("TOP"), topValue, Kw("PERCENT"), Kw("WITH"), Kw("TIES"));
            gb.Prod("TopValue").Is(number);
            gb.Prod("TopValue").Is("(", expression, ")");

            gb.Prod("SelectList").Is("*");
            gb.Prod("SelectList").Is(selectItemList);
            gb.Prod("SelectItemList").Is(selectItem);
            gb.Prod("SelectItemList").Is(selectItemList, ",", selectItem);
            gb.Prod("SelectItem").Is(expression, Kw("AS"), identifierTerm);
            gb.Prod("SelectItem").Is(qualifiedName, ".", "*");

            gb.Prod("TableSourceList").Is(tableSource);
            gb.Prod("TableSourceList").Is(tableSourceList, ",", tableSource);
            gb.Prod("TableSource").Is(tableFactor);
            gb.Prod("TableSource").Is(tableSource, joinPart);
            gb.Prod("TableFactor").Is(qualifiedName);
            gb.Prod("TableFactor").Is(qualifiedName, Kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is(functionCall, Kw("AS"), identifierTerm);
            gb.Prod("TableFactor").Is("(", queryExpression, ")", Kw("AS"), identifierTerm);

            gb.Prod("JoinPart").Is(joinType, Kw("JOIN"), tableFactor, Kw("ON"), searchCondition);
            gb.Prod("JoinPart").Is(Kw("CROSS"), Kw("JOIN"), tableFactor);
            gb.Prod("JoinPart").Is(Kw("CROSS"), Kw("APPLY"), tableFactor);
            gb.Prod("JoinPart").Is(Kw("OUTER"), Kw("APPLY"), tableFactor);

            gb.Prod("JoinType").Is(Kw("INNER"));
            gb.Prod("JoinType").Is(Kw("LEFT"));
            gb.Prod("JoinType").Is(Kw("LEFT"), Kw("OUTER"));
            gb.Prod("JoinType").Is(Kw("RIGHT"));
            gb.Prod("JoinType").Is(Kw("RIGHT"), Kw("OUTER"));
            gb.Prod("JoinType").Is(Kw("FULL"));
            gb.Prod("JoinType").Is(Kw("FULL"), Kw("OUTER"));

            gb.Prod("OrderByClause").Is(Kw("ORDER"), Kw("BY"), orderItemList);
            gb.Prod("OrderItemList").Is(orderItem);
            gb.Prod("OrderItemList").Is(orderItemList, ",", orderItem);
            gb.Prod("OrderItem").Is(expression);
            gb.Prod("OrderItem").Is(expression, Kw("ASC"));
            gb.Prod("OrderItem").Is(expression, Kw("DESC"));

            gb.Prod("OffsetFetchClause").Is(Kw("OFFSET"), expression, Kw("ROWS"));
            gb.Prod("OffsetFetchClause").Is(
                Kw("OFFSET"),
                expression,
                Kw("ROWS"),
                Kw("FETCH"),
                Kw("NEXT"),
                expression,
                Kw("ROWS"),
                Kw("ONLY"));

            gb.Prod("SearchCondition").Is(expression);

            gb.Prod("Expression").Is(primaryExpression);
            gb.Prod("Expression").Is(unaryOperator, expression);
            gb.Prod("Expression").Is(expression, binaryOperator, primaryExpression);

            gb.Prod("UnaryOperator").Is(Kw("NOT"));
            gb.Prod("UnaryOperator").Is("+");
            gb.Prod("UnaryOperator").Is("-");
            gb.Prod("UnaryOperator").Is("~");

            gb.Prod("BinaryOperator").Is(Kw("OR"));
            gb.Prod("BinaryOperator").Is(Kw("AND"));
            gb.Prod("BinaryOperator").Is("=");
            gb.Prod("BinaryOperator").Is("<>");
            gb.Prod("BinaryOperator").Is("!=");
            gb.Prod("BinaryOperator").Is("<");
            gb.Prod("BinaryOperator").Is("<=");
            gb.Prod("BinaryOperator").Is(">");
            gb.Prod("BinaryOperator").Is(">=");
            gb.Prod("BinaryOperator").Is(Kw("LIKE"));
            gb.Prod("BinaryOperator").Is(Kw("IN"));
            gb.Prod("BinaryOperator").Is(Kw("IS"));
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
            gb.Prod("PrimaryExpression").Is(Kw("EXISTS"), "(", queryExpression, ")");
            gb.Prod("PrimaryExpression").Is(caseExpression);

            gb.Prod("FunctionCall").Is(qualifiedName, "(", ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", "*", ")");
            gb.Prod("FunctionCall").Is(qualifiedName, "(", functionArgumentList, ")");
            gb.Prod("FunctionArgumentList").Is(expression);
            gb.Prod("FunctionArgumentList").Is(functionArgumentList, ",", expression);

            gb.Prod("OverClause").Is(Kw("OVER"), "(", overSpec, ")");
            gb.Prod("OverSpec").Is(Kw("PARTITION"), Kw("BY"), expressionList, Kw("ORDER"), Kw("BY"), orderItemList);
            gb.Prod("OverSpec").Is(Kw("PARTITION"), Kw("BY"), expressionList, Kw("ORDER"), Kw("BY"), orderItemList, Kw("ROWS"), frameClause);

            gb.Prod("FrameClause").Is(frameBoundary);
            gb.Prod("FrameClause").Is(Kw("BETWEEN"), frameBoundary, Kw("AND"), frameBoundary);

            gb.Prod("FrameBoundary").Is(Kw("UNBOUNDED"), Kw("PRECEDING"));
            gb.Prod("FrameBoundary").Is(Kw("UNBOUNDED"), Kw("FOLLOWING"));
            gb.Prod("FrameBoundary").Is(Kw("CURRENT"), Kw("ROW"));
            gb.Prod("FrameBoundary").Is(number, Kw("PRECEDING"));
            gb.Prod("FrameBoundary").Is(number, Kw("FOLLOWING"));

            gb.Prod("Literal").Is(number);
            gb.Prod("Literal").Is(stringLiteral);
            gb.Prod("Literal").Is(Kw("NULL"));
            gb.Prod("VariableReference").Is(variable);

            gb.Prod("CaseExpression").Is(Kw("CASE"), caseWhenList, Kw("END"));
            gb.Prod("CaseExpression").Is(Kw("CASE"), caseWhenList, Kw("ELSE"), expression, Kw("END"));
            gb.Prod("CaseExpression").Is(Kw("CASE"), expression, caseWhenList, Kw("END"));
            gb.Prod("CaseExpression").Is(Kw("CASE"), expression, caseWhenList, Kw("ELSE"), expression, Kw("END"));
            gb.Prod("CaseWhenList").Is(caseWhen);
            gb.Prod("CaseWhenList").Is(caseWhenList, caseWhen);
            gb.Prod("CaseWhen").Is(Kw("WHEN"), expression, Kw("THEN"), expression);

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

        private static SqlKeywordTerminal Kw(string keyword)
        {
            return new SqlKeywordTerminal(keyword);
        }

        private sealed class SqlKeywordTerminal : RegExpTerminalBase
        {
            private readonly string _keyword;

            public SqlKeywordTerminal(string keyword)
                : base(CreatePattern(keyword), previewChar: null)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    throw new ArgumentException("Keyword must not be empty.", nameof(keyword));
                }

                _keyword = keyword.ToUpperInvariant();
            }

            public override string Name => _keyword;
            public override TermFlags Flags => TermFlags.None;
            public override TerminalPriority Priority => TerminalPriority.High;
            public override string DictionaryKey => $"SqlKeyword[{_keyword}]";

            private static string CreatePattern(string keyword)
            {
                var escaped = Regex.Escape(keyword);
                return $@"\G(?i)\b{escaped}\b";
            }
        }
    }
}
