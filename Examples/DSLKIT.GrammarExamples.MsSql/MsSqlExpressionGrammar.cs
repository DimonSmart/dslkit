using DSLKIT.SpecialTerms;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlExpressionGrammar
    {
        public static void Build(MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var symbols = context.Symbols;
            var additiveExpression = symbols.AdditiveExpression;
            var booleanAndExpression = symbols.BooleanAndExpression;
            var booleanNotExpression = symbols.BooleanNotExpression;
            var booleanOrExpression = symbols.BooleanOrExpression;
            var booleanPrimary = symbols.BooleanPrimary;
            var caseExpression = symbols.CaseExpression;
            var caseWhen = symbols.CaseWhen;
            var caseWhenList = symbols.CaseWhenList;
            var collateExpression = symbols.CollateExpression;
            var comparisonOperator = symbols.ComparisonOperator;
            var createTableKeyColumnList = symbols.CreateTableKeyColumnList;
            var expression = symbols.Expression;
            var expressionList = symbols.ExpressionList;
            var frameBoundary = symbols.FrameBoundary;
            var frameClause = symbols.FrameClause;
            var functionArgumentList = symbols.FunctionArgumentList;
            var functionCall = symbols.FunctionCall;
            var graphWithinGroupClause = symbols.GraphWithinGroupClause;
            var groupingSet = symbols.GroupingSet;
            var groupingSetList = symbols.GroupingSetList;
            var identifierList = symbols.IdentifierList;
            var identifierTerm = symbols.IdentifierTerm;
            var iifArgumentList = symbols.IifArgumentList;
            var inPredicateValue = symbols.InPredicateValue;
            var literal = symbols.Literal;
            var matchGraphPattern = symbols.MatchGraphPattern;
            var multiplicativeExpression = symbols.MultiplicativeExpression;
            var namedOptionValue = symbols.NamedOptionValue;
            var openRowsetBulk = symbols.OpenRowsetBulk;
            var openRowsetBulkOption = symbols.OpenRowsetBulkOption;
            var openRowsetBulkOptionList = symbols.OpenRowsetBulkOptionList;
            var orderByClause = symbols.OrderByClause;
            var orderItem = symbols.OrderItem;
            var orderItemList = symbols.OrderItemList;
            var offsetFetchClause = symbols.OffsetFetchClause;
            var overClause = symbols.OverClause;
            var overFrameExtentOpt = symbols.OverFrameExtentOpt;
            var overOrderClause = symbols.OverOrderClause;
            var overPartitionClause = symbols.OverPartitionClause;
            var overSpec = symbols.OverSpec;
            var primaryExpression = symbols.PrimaryExpression;
            var qualifiedName = symbols.QualifiedName;
            var queryExpression = symbols.QueryExpression;
            var searchCondition = symbols.SearchCondition;
            var strictIdentifierTerm = symbols.StrictIdentifierTerm;
            var typeSpec = symbols.TypeSpec;
            var unaryExpression = symbols.UnaryExpression;
            var unicodeStringLiteral = symbols.UnicodeStringLiteral;
            var variableReference = symbols.VariableReference;
            var graphColumnRef = context.GraphColumnRefTerminal;
            var hasGraphExtensions = context.HasFeature(MsSqlDialectFeatures.GraphExtensions);
            var number = context.NumberTerminal;
            var sqlcmdVariable = context.SqlcmdVariableTerminal;
            var stringLiteral = context.StringLiteralTerminal;
            var variable = context.VariableTerminal;
            var identifier = context.IdentifierTerminal;
            var bracketIdentifier = context.BracketIdentifierTerminal;
            var quotedIdentifier = context.QuotedIdentifierTerminal;
            var tempIdentifier = context.TempIdentifierTerminal;

            gb.Prod(orderByClause).Is("ORDER", "BY", orderItemList);
            gb.Prod(orderItemList).Is(orderItem);
            gb.Prod(orderItemList).Is(orderItemList, ",", orderItem);
            gb.Prod(orderItem).Is(expression);
            gb.Prod(orderItem).Is(expression, "ASC");
            gb.Prod(orderItem).Is(expression, "DESC");

            gb.Prod(offsetFetchClause).Is("OFFSET", expression, "ROWS");
            gb.Prod(offsetFetchClause).Is(
                "OFFSET",
                expression,
                "ROWS",
                "FETCH",
                "NEXT",
                expression,
                "ROWS",
                "ONLY");

            gb.Prod(searchCondition).Is(booleanOrExpression);

            gb.Prod(expression).Is(additiveExpression);

            gb.Prod(booleanOrExpression).Is(booleanAndExpression);
            gb.Prod(booleanOrExpression).Is(booleanOrExpression, "OR", booleanAndExpression);

            gb.Prod(booleanAndExpression).Is(booleanNotExpression);
            gb.Prod(booleanAndExpression).Is(booleanAndExpression, "AND", booleanNotExpression);

            gb.Prod(booleanNotExpression).Is(booleanPrimary);
            gb.Prod(booleanNotExpression).Is("NOT", booleanNotExpression);

            gb.Prod(booleanPrimary).Is("(", searchCondition, ")");
            gb.Prod(booleanPrimary).Is(additiveExpression, comparisonOperator, additiveExpression);
            gb.Prod(booleanPrimary).Is(additiveExpression, "LIKE", additiveExpression);
            gb.Prod(booleanPrimary).Is(additiveExpression, "LIKE", additiveExpression, "ESCAPE", additiveExpression);
            gb.Prod(booleanPrimary).Is(additiveExpression, "NOT", "LIKE", additiveExpression);
            gb.Prod(booleanPrimary).Is(additiveExpression, "NOT", "LIKE", additiveExpression, "ESCAPE", additiveExpression);
            gb.Prod(booleanPrimary).Is(additiveExpression, "IN", inPredicateValue);
            gb.Prod(booleanPrimary).Is(additiveExpression, "NOT", "IN", inPredicateValue);
            gb.Prod(booleanPrimary).Is(additiveExpression, "IS", "NULL");
            gb.Prod(booleanPrimary).Is(additiveExpression, "IS", "NOT", "NULL");
            gb.Prod(booleanPrimary).Is(additiveExpression, "BETWEEN", additiveExpression, "AND", additiveExpression);
            gb.Prod(booleanPrimary).Is(additiveExpression, "NOT", "BETWEEN", additiveExpression, "AND", additiveExpression);
            gb.Prod(booleanPrimary).Is("EXISTS", "(", queryExpression, ")");
            if (hasGraphExtensions)
            {
                gb.Prod(booleanPrimary).Is("MATCH", "(", matchGraphPattern, ")");
            }

            gb.Prod(inPredicateValue).Is("(", expressionList, ")");
            gb.Prod(inPredicateValue).Is("(", queryExpression, ")");

            gb.Rule(comparisonOperator)
                .CanBe("=")
                .Or("<>")
                .Or("!=")
                .Or("<")
                .Or("<=")
                .Or(">")
                .Or(">=");

            gb.Prod(additiveExpression).Is(multiplicativeExpression);
            gb.Prod(additiveExpression).Is(additiveExpression, "+", multiplicativeExpression);
            gb.Prod(additiveExpression).Is(additiveExpression, "-", multiplicativeExpression);
            gb.Prod(additiveExpression).Is(additiveExpression, "&", multiplicativeExpression);
            gb.Prod(additiveExpression).Is(additiveExpression, "|", multiplicativeExpression);
            gb.Prod(additiveExpression).Is(additiveExpression, "^", multiplicativeExpression);

            gb.Prod(multiplicativeExpression).Is(unaryExpression);
            gb.Prod(multiplicativeExpression).Is(multiplicativeExpression, "*", unaryExpression);
            gb.Prod(multiplicativeExpression).Is(multiplicativeExpression, "/", unaryExpression);
            gb.Prod(multiplicativeExpression).Is(multiplicativeExpression, "%", unaryExpression);

            gb.Prod(unaryExpression).Is(collateExpression);
            gb.Prod(unaryExpression).Is("+", unaryExpression);
            gb.Prod(unaryExpression).Is("-", unaryExpression);
            gb.Prod(unaryExpression).Is("~", unaryExpression);

            gb.Prod(collateExpression).Is(primaryExpression);
            gb.Prod(collateExpression).Is(collateExpression, "COLLATE", strictIdentifierTerm);

            gb.Prod(primaryExpression).Is(literal);
            gb.Prod(primaryExpression).Is(unicodeStringLiteral);
            gb.Prod(primaryExpression).Is(sqlcmdVariable);
            gb.Prod(primaryExpression).Is(variableReference);
            if (hasGraphExtensions)
            {
                gb.Prod(primaryExpression).Is(graphColumnRef);
            }
            gb.Prod(primaryExpression).Is(qualifiedName);
            gb.Prod(primaryExpression).Is(functionCall);
            gb.Prod(primaryExpression).Is(functionCall, overClause);
            gb.Prod(primaryExpression).Is(functionCall, graphWithinGroupClause);
            gb.Prod(primaryExpression).Is("CAST", "(", expression, "AS", typeSpec, ")");
            gb.Prod(primaryExpression).Is("(", expression, ")");
            gb.Prod(primaryExpression).Is("(", queryExpression, ")");
            gb.Prod(primaryExpression).Is(caseExpression);
            gb.Prod(primaryExpression).Is("LANGUAGE", primaryExpression);

            gb.Prod(functionCall).Is(qualifiedName, "(", ")");
            gb.Prod(functionCall).Is(qualifiedName, "(", "*", ")");
            gb.Prod(functionCall).Is(qualifiedName, "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("::", qualifiedName, "(", ")");
            gb.Prod(functionCall).Is("::", qualifiedName, "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is(qualifiedName, "(", "DISTINCT", functionArgumentList, ")");
            gb.Prod(functionCall).Is(qualifiedName, "(", "ALL", functionArgumentList, ")");
            gb.Prod(functionCall).Is(qualifiedName, "(", functionArgumentList, ",", "*", ",", functionArgumentList, ")");
            gb.Prod(functionCall).Is(qualifiedName, ".", identifierTerm, "(", ")");
            gb.Prod(functionCall).Is(qualifiedName, ".", identifierTerm, "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is(variableReference, ".", identifierTerm, "(", ")");
            gb.Prod(functionCall).Is(variableReference, ".", identifierTerm, "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("LEFT", "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("RIGHT", "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("COALESCE", "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("NULLIF", "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("IIF", "(", iifArgumentList, ")");
            gb.Prod(functionCall).Is("UPDATE", "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("NEXT", identifierTerm, "FOR", qualifiedName);
            gb.Prod(functionCall).Is("OPENROWSET", "(", openRowsetBulk, ")");
            gb.Prod(openRowsetBulk).Is("BULK", expression, ",", openRowsetBulkOptionList);
            gb.Prod(openRowsetBulkOptionList).Is(openRowsetBulkOption);
            gb.Prod(openRowsetBulkOptionList).Is(openRowsetBulkOptionList, ",", openRowsetBulkOption);
            gb.Prod(openRowsetBulkOption).Is("SINGLE_BLOB");
            gb.Prod(openRowsetBulkOption).Is("SINGLE_CLOB");
            gb.Prod(openRowsetBulkOption).Is("SINGLE_NCLOB");
            gb.Prod(openRowsetBulkOption).Is("DATA_SOURCE", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("CODEPAGE", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("DATAFILETYPE", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("FORMAT", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("FORMATFILE", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("FORMATFILE_DATA_SOURCE", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("FIELDTERMINATOR", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("FIELDQUOTE", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("ROWTERMINATOR", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("FIRSTROW", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("LASTROW", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("MAXERRORS", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("ERRORFILE", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("ERRORFILE_DATA_SOURCE", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("ROWS_PER_BATCH", "=", namedOptionValue);
            gb.Prod(openRowsetBulkOption).Is("ORDER", "(", createTableKeyColumnList, ")");
            gb.Prod(functionArgumentList).Is(expression);
            gb.Prod(functionArgumentList).Is(functionArgumentList, ",", expression);
            gb.Prod(iifArgumentList).Is(searchCondition, ",", expression, ",", expression);
            if (hasGraphExtensions)
            {
                gb.Prod(graphWithinGroupClause).Is("WITHIN", "GROUP", "(", "GRAPH", "PATH", ")");
            }
            gb.Prod(graphWithinGroupClause).Is("WITHIN", "GROUP", "(", "ORDER", "BY", orderItemList, ")");

            gb.Prod(overClause).Is("OVER", "(", overSpec, ")");
            gb.Prod(overPartitionClause).Is("PARTITION", "BY", expressionList);
            gb.Rule(overFrameExtentOpt).OneOf(
                EmptyTerm.Empty,
                gb.Seq("ROWS", frameClause),
                gb.Seq("RANGE", frameClause));
            gb.Prod(overOrderClause).Is("ORDER", "BY", orderItemList, overFrameExtentOpt);
            gb.Rule(overSpec).OneOf(
                EmptyTerm.Empty,
                overPartitionClause,
                overOrderClause,
                gb.Seq(overPartitionClause, overOrderClause));

            gb.Prod(frameClause).Is(frameBoundary);
            gb.Prod(frameClause).Is("BETWEEN", frameBoundary, "AND", frameBoundary);

            gb.Prod(frameBoundary).Is("UNBOUNDED", "PRECEDING");
            gb.Prod(frameBoundary).Is("UNBOUNDED", "FOLLOWING");
            gb.Prod(frameBoundary).Is("CURRENT", "ROW");
            gb.Prod(frameBoundary).Is(number, "PRECEDING");
            gb.Prod(frameBoundary).Is(number, "FOLLOWING");

            gb.Prod(literal).Is(number);
            gb.Prod(literal).Is(stringLiteral);
            gb.Prod(literal).Is("NULL");
            gb.Prod(unicodeStringLiteral).Is("N", stringLiteral);
            gb.Prod(variableReference).Is(variable);

            gb.Prod(caseExpression).Is("CASE", caseWhenList, "END");
            gb.Prod(caseExpression).Is("CASE", caseWhenList, "ELSE", expression, "END");
            gb.Prod(caseExpression).Is("CASE", expression, caseWhenList, "END");
            gb.Prod(caseExpression).Is("CASE", expression, caseWhenList, "ELSE", expression, "END");
            gb.Prod(caseWhenList).Is(caseWhen);
            gb.Prod(caseWhenList).Is(caseWhenList, caseWhen);
            gb.Prod(caseWhen).Is("WHEN", searchCondition, "THEN", expression);
            gb.Prod(caseWhen).Is("WHEN", expression, "THEN", expression);

            gb.Prod(expressionList).Is(expression);
            gb.Prod(expressionList).Is(expressionList, ",", expression);
            gb.Prod(groupingSetList).Is(groupingSet);
            gb.Prod(groupingSetList).Is(groupingSetList, ",", groupingSet);
            gb.Prod(groupingSet).Is("(", expressionList, ")");
            gb.Prod(groupingSet).Is("(", ")");
            gb.Prod(identifierList).Is(identifierTerm);
            gb.Prod(identifierList).Is(identifierList, ",", identifierTerm);

            gb.Rule(strictIdentifierTerm).OneOf(
                identifier,
                bracketIdentifier,
                quotedIdentifier,
                tempIdentifier,
                sqlcmdVariable);
            gb.Rule(identifierTerm).OneOf(strictIdentifierTerm);
            gb.Rule(identifierTerm).Keywords(
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
        }
    }
}
