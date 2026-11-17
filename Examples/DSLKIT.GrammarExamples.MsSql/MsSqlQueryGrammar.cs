using DSLKIT.SpecialTerms;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal static class MsSqlQueryGrammar
    {
        public static void Build(MsSqlGrammarContext context)
        {
            var gb = context.Gb;
            var expression = gb.NT("Expression");
            var expressionList = gb.NT("ExpressionList");
            var forClause = gb.NT("ForClause");
            var forJsonMode = gb.NT("ForJsonMode");
            var forJsonOption = gb.NT("ForJsonOption");
            var forJsonOptionList = gb.NT("ForJsonOptionList");
            var forXmlMode = gb.NT("ForXmlMode");
            var forXmlOption = gb.NT("ForXmlOption");
            var forXmlOptionList = gb.NT("ForXmlOptionList");
            var groupingSetList = gb.NT("GroupingSetList");
            var implicitQueryExpression = gb.NT("ImplicitQueryExpression");
            var implicitQueryIntersectExpression = gb.NT("ImplicitQueryIntersectExpression");
            var implicitQueryUnionExpression = gb.NT("ImplicitQueryUnionExpression");
            var optionClause = gb.NT("OptionClause");
            var orderByClause = gb.NT("OrderByClause");
            var offsetFetchClause = gb.NT("OffsetFetchClause");
            var qualifiedName = gb.NT("QualifiedName");
            var queryExpression = gb.NT("QueryExpression");
            var queryExpressionForOpt = gb.NT("QueryExpressionForOpt");
            var queryExpressionOptionOpt = gb.NT("QueryExpressionOptionOpt");
            var queryExpressionOrderByAndOffsetOpt = gb.NT("QueryExpressionOrderByAndOffsetOpt");
            var queryExpressionTail = gb.NT("QueryExpressionTail");
            var queryIntersectExpression = gb.NT("QueryIntersectExpression");
            var queryPrimary = gb.NT("QueryPrimary");
            var parenthesizedQuery = gb.NT("ParenthesizedQuery");
            var querySpecification = gb.NT("QuerySpecification");
            var querySpecificationGroupByClause = gb.NT("QuerySpecificationGroupByClause");
            var querySpecificationGroupByExpressionList = gb.NT("QuerySpecificationGroupByExpressionList");
            var querySpecificationGroupByGroupingSets = gb.NT("QuerySpecificationGroupByGroupingSets");
            var querySpecificationGroupByWithOpt = gb.NT("QuerySpecificationGroupByWithOpt");
            var querySpecificationHavingOpt = gb.NT("QuerySpecificationHavingOpt");
            var querySpecificationWhereClause = gb.NT("QuerySpecificationWhereClause");
            var queryUnionExpression = gb.NT("QueryUnionExpression");
            var searchCondition = gb.NT("SearchCondition");
            var selectCore = gb.NT("SelectCore");
            var selectCoreIntoClause = gb.NT("SelectCoreIntoClause");
            var selectCorePrefix = gb.NT("SelectCorePrefix");
            var selectCoreTail = gb.NT("SelectCoreTail");
            var selectItem = gb.NT("SelectItem");
            var selectItemList = gb.NT("SelectItemList");
            var selectList = gb.NT("SelectList");
            var setOperator = gb.NT("SetOperator");
            var setQuantifier = gb.NT("SetQuantifier");
            var strictIdentifierTerm = gb.NT("StrictIdentifierTerm");
            var tableSourceList = gb.NT("TableSourceList");
            var topClause = gb.NT("TopClause");
            var topClauseTail = gb.NT("TopClauseTail");
            var topValue = gb.NT("TopValue");
            var variableReference = gb.NT("VariableReference");
            var compoundAssignOp = gb.NT("CompoundAssignOp");
            var number = context.NumberTerminal;
            var stringLiteral = context.StringLiteralTerminal;
            var queryAliasTerm = gb.NT("QueryAliasTerm");

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
            gb.Prod(queryPrimary).Is(parenthesizedQuery);
            gb.Prod(parenthesizedQuery).Is("(", queryExpression, ")");

            gb.Rule(forClause)
                .CanBe("FOR", "BROWSE")
                .Or("FOR", "JSON", forJsonMode)
                .Or("FOR", "JSON", forJsonMode, ",", forJsonOptionList)
                .Or("FOR", "XML", forXmlMode)
                .Or("FOR", "XML", forXmlMode, ",", forXmlOptionList);

            gb.Rule(forJsonMode).Keywords("AUTO", "PATH", "NONE");

            gb.Rule(forJsonOptionList).SeparatedBy(",", forJsonOption);
            gb.Rule(forJsonOption).Keywords("WITHOUT_ARRAY_WRAPPER", "INCLUDE_NULL_VALUES", "ROOT");
            gb.Prod(forJsonOption).Is("ROOT", "(", expression, ")");

            gb.Rule(forXmlMode)
                .CanBe("AUTO")
                .Or("PATH")
                .Or("PATH", "(", expression, ")")
                .Or("RAW")
                .Or("RAW", "(", expression, ")")
                .Or("EXPLICIT");

            gb.Rule(forXmlOptionList).SeparatedBy(",", forXmlOption);
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
            gb.Rule(queryAliasTerm).OneOf(strictIdentifierTerm);
            gb.Prod(selectItem).Is("*");
            gb.Prod(selectItem).Is(expression, "AS", queryAliasTerm);
            gb.Prod(selectItem).Is(expression, "AS", stringLiteral);
            gb.Prod(selectItem).Is(expression, queryAliasTerm);
            gb.Prod(selectItem).Is(expression, stringLiteral);
            gb.Prod(selectItem).Is(expression);
            gb.Prod(selectItem).Is(qualifiedName, ".", "*");
            gb.Prod(selectItem).Is(variableReference, "=", expression);
            gb.Prod(selectItem).Is(variableReference, compoundAssignOp, expression);
        }
    }
}
