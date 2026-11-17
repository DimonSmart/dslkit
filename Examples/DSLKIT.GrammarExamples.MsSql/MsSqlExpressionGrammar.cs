using DSLKIT.SpecialTerms;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlExpressionGrammar
    {
        public static void Build(MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var additiveExpression = gb.NT("AdditiveExpression");
            var booleanAndExpression = gb.NT("BooleanAndExpression");
            var booleanNotExpression = gb.NT("BooleanNotExpression");
            var booleanOrExpression = gb.NT("BooleanOrExpression");
            var booleanPrimary = gb.NT("BooleanPrimary");
            var caseExpression = gb.NT("CaseExpression");
            var caseWhen = gb.NT("CaseWhen");
            var caseWhenList = gb.NT("CaseWhenList");
            var collateExpression = gb.NT("CollateExpression");
            var comparisonOperator = gb.NT("ComparisonOperator");
            var createTableKeyColumnList = gb.NT("CreateTableKeyColumnList");
            var expression = gb.NT("Expression");
            var expressionList = gb.NT("ExpressionList");
            var frameBoundary = gb.NT("FrameBoundary");
            var frameClause = gb.NT("FrameClause");
            var functionArgumentList = gb.NT("FunctionArgumentList");
            var functionCall = gb.NT("FunctionCall");
            var graphWithinGroupClause = gb.NT("GraphWithinGroupClause");
            var groupingSet = gb.NT("GroupingSet");
            var groupingSetList = gb.NT("GroupingSetList");
            var contextualIdentifierTerm = gb.NT("ContextualIdentifierTerm");
            var iifArgumentList = gb.NT("IifArgumentList");
            var inPredicateValue = gb.NT("InPredicateValue");
            var literal = gb.NT("Literal");
            var matchGraphPattern = gb.NT("MatchGraphPattern");
            var multiplicativeExpression = gb.NT("MultiplicativeExpression");
            var namedOptionValue = gb.NT("NamedOptionValue");
            var openRowsetBulk = gb.NT("OpenRowsetBulk");
            var openRowsetBulkOption = gb.NT("OpenRowsetBulkOption");
            var openRowsetBulkOptionList = gb.NT("OpenRowsetBulkOptionList");
            var orderByClause = gb.NT("OrderByClause");
            var orderItem = gb.NT("OrderItem");
            var orderItemList = gb.NT("OrderItemList");
            var offsetFetchClause = gb.NT("OffsetFetchClause");
            var overClause = gb.NT("OverClause");
            var overFrameExtentOpt = gb.NT("OverFrameExtentOpt");
            var overOrderClause = gb.NT("OverOrderClause");
            var overPartitionClause = gb.NT("OverPartitionClause");
            var overSpec = gb.NT("OverSpec");
            var primaryExpression = gb.NT("PrimaryExpression");
            var parenthesizedQuery = gb.NT("ParenthesizedQuery");
            var qualifiedName = gb.NT("QualifiedName");
            var queryExpression = gb.NT("QueryExpression");
            var searchCondition = gb.NT("SearchCondition");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var typeSpec = gb.NT("TypeSpec");
            var unaryExpression = gb.NT("UnaryExpression");
            var unicodeStringLiteral = gb.NT("UnicodeStringLiteral");
            var variableReference = gb.NT("VariableReference");
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
            var contextualIdentifierList = gb.NT("ContextualIdentifierList");

            gb.Prod(orderByClause).Is("ORDER", "BY", orderItemList);
            gb.Rule(orderItemList).SeparatedBy(",", orderItem);
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
            gb.Prod(primaryExpression).Is(parenthesizedQuery);
            gb.Prod(primaryExpression).Is(caseExpression);
            gb.Prod(primaryExpression).Is("LANGUAGE", primaryExpression);

            gb.Prod(functionCall).Is(qualifiedName, "(", ")");
            gb.Prod(functionCall).Is(qualifiedName, "(", "*", ")");
            gb.Prod(functionCall).Is(qualifiedName, "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is(qualifiedName, "(", "DISTINCT", functionArgumentList, ")");
            gb.Prod(functionCall).Is(qualifiedName, "(", "ALL", functionArgumentList, ")");
            gb.Prod(functionCall).Is(qualifiedName, "(", functionArgumentList, ",", "*", ",", functionArgumentList, ")");
            gb.Prod(functionCall).Is(qualifiedName, ".", contextualIdentifierTerm, "(", ")");
            gb.Prod(functionCall).Is(qualifiedName, ".", contextualIdentifierTerm, "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is(variableReference, ".", contextualIdentifierTerm, "(", ")");
            gb.Prod(functionCall).Is(variableReference, ".", contextualIdentifierTerm, "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("LEFT", "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("RIGHT", "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("COALESCE", "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("NULLIF", "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("IIF", "(", iifArgumentList, ")");
            gb.Prod(functionCall).Is("UPDATE", "(", functionArgumentList, ")");
            gb.Prod(functionCall).Is("NEXT", contextualIdentifierTerm, "FOR", qualifiedName);
            gb.Prod(functionCall).Is("OPENROWSET", "(", openRowsetBulk, ")");
            gb.Prod(openRowsetBulk).Is("BULK", expression, ",", openRowsetBulkOptionList);
            gb.Rule(openRowsetBulkOptionList).SeparatedBy(",", openRowsetBulkOption);
            gb.Rule(openRowsetBulkOption).Keywords("SINGLE_BLOB", "SINGLE_CLOB", "SINGLE_NCLOB");
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
            gb.Rule(functionArgumentList).SeparatedBy(",", expression);
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
            gb.Rule(caseWhenList).Plus(caseWhen);
            gb.Prod(caseWhen).Is("WHEN", searchCondition, "THEN", expression);
            gb.Prod(caseWhen).Is("WHEN", expression, "THEN", expression);

            gb.Rule(expressionList).SeparatedBy(",", expression);
            gb.Rule(groupingSetList).SeparatedBy(",", groupingSet);
            gb.Prod(groupingSet).Is("(", expressionList, ")");
            gb.Prod(groupingSet).Is("(", ")");
            gb.Rule(contextualIdentifierList).SeparatedBy(",", contextualIdentifierTerm);

            gb.Rule(strictIdentifierTerm).OneOf(
                identifier,
                bracketIdentifier,
                quotedIdentifier,
                tempIdentifier,
                sqlcmdVariable);
            gb.Rule(contextualIdentifierTerm).OneOf(strictIdentifierTerm);
            gb.Rule(contextualIdentifierTerm).Keywords(
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
